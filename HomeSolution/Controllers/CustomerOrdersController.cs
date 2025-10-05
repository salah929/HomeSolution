using Home.DTOs;
using Home.ServiceContracts;
using Home.Services;
using Home.Services.Exceptions;
using Home.Services.Mappers;
using Home.Shared.Enums;
using Home.Web.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Home.Web.Controllers
{
    // [AutoValidateAntiforgeryToken] automatically requires a valid anti-forgery token on all non-GET actions (POST/PUT/DELETE).
    // Optional but RECOMMENDED for MVC apps with forms. You can remove it and keep [ValidateAntiForgeryToken] on individual actions instead.
    [AutoValidateAntiforgeryToken]
    public class CustomerOrdersController : Controller
    {
        private readonly ICustomerOrderService _customerOrderService;
        private readonly ICustomerService _customerService;
        private readonly IProductService _productService;

        public CustomerOrdersController(ICustomerOrderService orderService, ICustomerService customerService,
                                        IProductService productService)
        {
            _customerOrderService = orderService;
            _customerService = customerService;
            _productService = productService;
        }

        // Helpers
        private async Task PopulateDropdownsAsync(Guid? customerId = null, CancellationToken ct = default)
        {
            var customers = (await _customerService.GetAllAsync(ct)).OrderBy(c => c.Name).ToList();
            var products = (await _productService.GetAllAsync(ct)).OrderBy(p => p.Code).
                Select (p => new { p.ProductId, Display = $"{p.Code} - {p.Name}" }).ToList();

            // Insert default "please select" at index 0
            customers.Insert(0, new CustomerDto
            {
                CustomerId = Guid.Empty,
                Name = "-- please select --"
            });

            products.Insert(0, new
            {
                ProductId = Guid.Empty,
                Display = "-- please select --"
            });

            ViewBag.Customers = new SelectList(customers, "CustomerId", "Name", customerId);
            ViewBag.Products = new SelectList(products, "ProductId", "Display");
        }

        // GET: /customerorders  (and /customerorders/index)
        [HttpGet] // optional because *no attribute* defaults to GET in MVC.
        public async Task<IActionResult> Index(string? searchString, string? searchBy, Guid? customerId, CustomerOrderStatus? status,
                                               DateTime? from, DateTime? to, string? sortBy, bool desc = false,
                                               int page = 1, int pageSize = 20, CancellationToken ct = default)
        {
            var orders = await _customerOrderService.SearchAsync(searchString, searchBy, customerId, status, from, to, sortBy, desc, page, pageSize, ct);
            int totalCount = await _customerOrderService.CountAsync(searchString, searchBy, customerId, status, from, to, ct);
            var vm = new CustomerOrdersViewModel
            {
                CustomerOrders = orders,
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize,
                SearchString = searchString,
                SearchBy = searchBy,
                SortBy = sortBy,
                Desc = desc,
                From = from,
                To = to,
                Status = status
            };
            return View(vm);
        }

        // GET: /customerorders/details/{id}
        [HttpGet] // optional, it is GET by default
        public async Task<IActionResult> Details(Guid id, CancellationToken ct = default)
        {
            // Always load with items so the view can display order lines.
            var order = await _customerOrderService.GetByIdAsync(id, includeItems: true, ct);
            if (order == null) return NotFound(); // 404 if not found
            return View(order);
        }

        // GET: /customerorders/create
        [HttpGet] // optional
        public async Task<IActionResult> Create(Guid id, bool c, CancellationToken ct)
        {
            await PopulateDropdownsAsync(null, ct);
            var dto = new CreateCustomerOrderDto();
            if (c) // c = fromCustomerDetailsPage
            {
                dto.CustomerId = id; // the create form is called from the customer details page or create page reloads when error (id exists)
                dto.FromCustomer = true;
            }
            return View(dto); // normal create call
        }

        // POST: /customerorders/create
        [HttpPost] // required for POST
        public async Task<IActionResult> Create(CreateCustomerOrderDto dto, CancellationToken ct = default)
        {
            if (dto.CustomerId == Guid.Empty || (await _customerService.GetByIdAsync(dto.CustomerId, false, ct) is null))
                ModelState.AddModelError(nameof(dto.CustomerId), "Invalid Customer.");

            // Order number required
            if (string.IsNullOrWhiteSpace(dto.OrderNumber)) ModelState.AddModelError(nameof(dto.OrderNumber), "Invalid order number.");

            // Items required + per-item checks
            if (dto.Items == null || !dto.Items.Any()) ModelState.AddModelError(string.Empty, "Add at least one item.");
            else
            {
                for (int i = 0; i < dto.Items.Count; i++)
                {
                    var item = dto.Items[i];
                    if (item.ProductId == Guid.Empty) ModelState.AddModelError(string.Empty, $"Please select a valid product for item {i + 1}.");
                    if (item.Quantity < 1) ModelState.AddModelError(string.Empty, $"Please select a valid quantity for item {i + 1}.");
                    if (item.UnitPrice <= 0) ModelState.AddModelError(string.Empty, $"Please select a valid unit price for item {i + 1}.");
                }
            }

            if (!ModelState.IsValid)
            {
                await PopulateDropdownsAsync(dto.CustomerId, ct);
                return View(dto);
            }

            try
            {
                await _customerOrderService.CreateAsync(dto, ct);
                return RedirectToAction(nameof(Index));
            }
            catch (DomainValidationException ex)
            {
                foreach (var (key, msg) in ex.Errors)
                    //ModelState.AddModelError(string.IsNullOrEmpty(key) ? string.Empty : key, msg);
                    ModelState.AddModelError(string.Empty, msg);
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
            }
            await PopulateDropdownsAsync(dto.CustomerId, ct);
            return View(dto);
        }

        // GET: /customerorders/edit/{id}
        [HttpGet] // optional
        public async Task<IActionResult> Edit(Guid id, bool c, CancellationToken ct = default)
        {
            var order = await _customerOrderService.GetByIdAsync(id, includeItems: true, ct);
            if (order == null) return NotFound();
            await PopulateDropdownsAsync(id, ct);
            var dto = order.ToUpdateCustomerOrderDto();
            if (dto == null) return NotFound();
            if (c) // c = FromCustomerDetailsPage
            {
                dto.FromCustomer = true;
            }
            
            return View(dto);
        }

        // POST: /customerorders/edit/{id}
        [HttpPost] // required for POST
        public async Task<IActionResult> Edit([Bind] UpdateCustomerOrderDto dto, CancellationToken ct = default)
        {
            // [Bind] optional; use it to whitelist fields to prevent overposting.

            // Validate Customer
            if (dto.CustomerId == Guid.Empty || (await _customerService.GetByIdAsync(dto.CustomerId, false, ct) is null))
                ModelState.AddModelError(nameof(dto.CustomerId), "Invalid Customer.");
            
            // Order number required
            if (string.IsNullOrWhiteSpace(dto.OrderNumber))
                ModelState.AddModelError(nameof(dto.OrderNumber), "Invalid order number.");

            // Items required + per-item checks
            if (dto.Items == null || !dto.Items.Any())
                ModelState.AddModelError(string.Empty, "Add at least one item.");
            else
            {
                for (int i = 0; i < dto.Items.Count; i++)
                {
                    var it = dto.Items[i];
                    if (it.ProductId == Guid.Empty) ModelState.AddModelError(string.Empty, $"Please select a valid product for item {i + 1}.");
                    if (it.Quantity < 1) ModelState.AddModelError(string.Empty, $"Please select a valid quantity for item {i + 1}.");
                    if (it.UnitPrice <= 0) ModelState.AddModelError(string.Empty, $"Please select a valid unit price for item {i + 1}.");
                }
            }
            if (!ModelState.IsValid)
            {
                await PopulateDropdownsAsync(dto.CustomerId, ct);
                return View(dto);
            }

            try
            {
                await _customerOrderService.UpdateAsync(dto, ct);
                return RedirectToAction(nameof(Index));
            }
            catch (DomainValidationException ex)
            {
                foreach (var (key, msg) in ex.Errors)
                    ModelState.AddModelError(string.IsNullOrEmpty(key) ? string.Empty : key, msg);
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
            }

            await PopulateDropdownsAsync(dto.CustomerId, ct);
            return View(dto);
        }

        // GET: /customerorders/delete/{id}
        [HttpGet] // optional
        public async Task<IActionResult> Delete([FromRoute] Guid id, CancellationToken ct = default) // [FromRoute] optional
        {
            var order = await _customerOrderService.GetByIdAsync(id, includeItems: false, ct);
            if (order == null) return NotFound();
            return View(order);
        }

        // POST: /customerorders/delete/{id}
        [HttpPost] // required for POST
        [ActionName("Delete")] // required here because method name is DeleteConfirmed but route must stay "Delete"
        // [ValidateAntiForgeryToken] // optional here due to controller-level auto validation
        public async Task<IActionResult> DeleteConfirmed([FromRoute] Guid id, CancellationToken ct = default) // [FromRoute] optional
        {
            var deleted = await _customerOrderService.DeleteAsync(id, ct);
            if (!deleted) return NotFound();
            return RedirectToAction(nameof(Index));
        }
    }
}

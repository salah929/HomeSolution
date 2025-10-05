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
    public class SupplierOrdersController : Controller
    {
        private readonly ISupplierOrderService _supplierOrderService;
        private readonly ISupplierService _supplierService;
        private readonly IProductService _productService;

        public SupplierOrdersController(ISupplierOrderService supplierOrderService, ISupplierService supplierService, IProductService productService)
        {
            _supplierOrderService = supplierOrderService;
            _supplierService = supplierService;
            _productService = productService;
        }

        // Helpers
        private async Task PopulateDropdownsAsync(Guid? supplierId = null, CancellationToken ct = default)
        {
            var suppliers = (await _supplierService.GetAllAsync(ct)).OrderBy(s => s.Name).ToList();

            var allProducts = (await _productService.GetAllAsync(ct))
                .Select(p => new { p.ProductId, Display = $"{p.Code} - {p.Name}", p.SupplierId })
                .OrderBy(p => p.Display)
                .ToList();

            var products = (await _productService.GetAllAsync(ct))
                .Where(p => supplierId != null && supplierId != Guid.Empty && p.SupplierId == supplierId)
                .Select(p => new { p.ProductId, Display = $"{p.Code} - {p.Name}", p.SupplierId })
                .OrderBy(p => p.Display)
                .ToList();
            //var productsList = (await _productService.GetAllAsync(ct))
            //    .Select(p => new
            //    {
            //        ProductId = p.ProductId,
            //        Code = p.Code,
            //        Name = p.Name,
            //        SupplierId = p.SupplierId
            //    })
            //    .ToList();

            // Insert default "please select" at index 0
            suppliers.Insert(0, new SupplierDto
            {
                SupplierId = Guid.Empty,
                Name = "-- please select --"
            });

            allProducts.Insert(0, new
            {
                ProductId = Guid.Empty,
                Display = "-- please select --",
                SupplierId = Guid.Empty
            });

            products.Insert(0, new
            {
                ProductId = Guid.Empty,
                Display = "-- please select --",
                SupplierId = Guid.Empty
            });

            ViewBag.Suppliers = new SelectList(suppliers, "SupplierId", "Name", supplierId);
            ViewBag.AllProducts = new SelectList(allProducts, "ProductId", "Display");
            ViewBag.Products = new SelectList(products, "ProductId", "Display");
            //ViewBag.ProductsList = productsList;
        }

        // GET: /supplierorders  (and /supplierorders/index)
        [HttpGet] // optional, default is GET
        public async Task<IActionResult> Index(string? searchString, string? searchBy, Guid? supplierId, SupplierOrderStatus? status,
                                       DateTime? from, DateTime? to, string? sortBy, bool desc = false,
                                       int page = 1, int pageSize = 20, CancellationToken ct = default)
        {
            var orders = await _supplierOrderService.SearchAsync(searchString, searchBy, supplierId, status, from, to, sortBy, desc, page, pageSize, ct);
            int totalCount = await _supplierOrderService.CountAsync(searchString, searchBy, supplierId, status, from, to, ct);
            var vm = new SupplierOrdersViewModel
            {
                SupplierOrders = orders,
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


        // GET: /supplierorders/details/{id}
        [HttpGet] // optional, it is GET by default
        public async Task<IActionResult> Details(Guid id, CancellationToken ct = default)
        {
            // Always load with items so the view can display order lines.
            var order = await _supplierOrderService.GetByIdAsync(id, includeItems: true, ct);
            if (order == null) return NotFound(); // 404 if not found
            return View(order);
        }


        // GET: /supplierorders/create
        [HttpGet] // optional
        public async Task<IActionResult> Create(Guid supplierid, bool s, CancellationToken ct = default)
        {
            await PopulateDropdownsAsync(supplierid, ct);
            var dto = new CreateSupplierOrderDto();
            if (supplierid != Guid.Empty || s) // s = FromSupplierDetailsPage
            {
                dto.SupplierId = supplierid; // the create form is called from the supplier details page or create page reloads with error (id exists)
                dto.FromSupplier = s;
            }
            return View(dto); // normal create call
        }

        // POST: /supplierorders/create
        [HttpPost] // required for POST
        // [ValidateAntiForgeryToken] // optional here due to [AutoValidateAntiforgeryToken], but good for explicitness
        [HttpPost] // required for POST
                   // [ValidateAntiForgeryToken] // optional if you already have controller-level auto validation
        public async Task<IActionResult> Create(CreateSupplierOrderDto dto, CancellationToken ct = default)
        {
            // Supplier required
            if (dto.SupplierId == Guid.Empty || (await _supplierService.GetByIdAsync(dto.SupplierId, false, false, ct) is null))
                ModelState.AddModelError(nameof(dto.SupplierId), "Invalid Supplier.");

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
                await PopulateDropdownsAsync(dto.SupplierId, ct);
                return View(dto);
            }

            try
            {
                await _supplierOrderService.CreateAsync(dto, ct);
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

            await PopulateDropdownsAsync(dto.SupplierId, ct);
            return View(dto);
        }


        // GET: /supplierorders/edit/{id}
        [HttpGet] // optional
        public async Task<IActionResult> Edit(Guid id, bool s, CancellationToken ct = default)
        {
            var order = await _supplierOrderService.GetByIdAsync(id, includeItems: true, ct);
            if (order == null) return NotFound();
            var dto = order.ToUpdateSupplierOrderDto();
            if (dto == null) return NotFound();
            await PopulateDropdownsAsync(dto.SupplierId, ct);
            if (s) // c = FromSupplierDetailsPage
            {
                dto.FromSupplier = true;
            }

            return View(dto);
        }


        // POST: /supplierorders/edit/{id}
        [HttpPost] // required for POST
        public async Task<IActionResult> Edit([Bind] UpdateSupplierOrderDto dto, CancellationToken ct = default)
        {
            // Validate Supplier
            if (dto.SupplierId == Guid.Empty || (await _supplierService.GetByIdAsync(dto.SupplierId, false, false, ct) is null))
                ModelState.AddModelError(nameof(dto.SupplierId), "Invalid Supplier.");
            
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
                await PopulateDropdownsAsync(dto.SupplierId, ct);
                return View(dto);
            }

            try
            {
                await _supplierOrderService.UpdateAsync(dto, ct);
                return RedirectToAction(nameof(Index));
            }
            catch (DomainValidationException ex)
            {
                foreach (var (key, msg) in ex.Errors)
                    ModelState.AddModelError(string.Empty, msg);
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
            }

            await PopulateDropdownsAsync(dto.SupplierId, ct);
            return View(dto);
        }


        // GET: /supplierorders/delete/{id}
        [HttpGet] // optional
        public async Task<IActionResult> Delete([FromRoute] Guid id, CancellationToken ct = default) // [FromRoute] optional
        {
            var order = await _supplierOrderService.GetByIdAsync(id, includeItems: false, ct);
            if (order == null) return NotFound();
            return View(order);
        }

        // POST: /supplierorders/delete/{id}
        [HttpPost] // required for POST
        [ActionName("Delete")] // required here because method name is DeleteConfirmed but route must stay "Delete"
        // [ValidateAntiForgeryToken] // optional here due to controller-level auto validation
        public async Task<IActionResult> DeleteConfirmed([FromRoute] Guid id, CancellationToken ct = default) // [FromRoute] optional
        {
            var deleted = await _supplierOrderService.DeleteAsync(id, ct);
            if (!deleted) return NotFound();
            return RedirectToAction(nameof(Index));
        }
    }
}

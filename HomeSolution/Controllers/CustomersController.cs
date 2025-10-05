using Home.DTOs;
using Home.ServiceContracts;
using Home.Services.Exceptions;
using Home.Services.Mappers;
using Home.Web.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace Home.Web.Controllers
{
    // [AutoValidateAntiforgeryToken] automatically requires a valid anti-forgery token on all non-GET actions (POST/PUT/DELETE).
    // Optional but RECOMMENDED for MVC apps with forms. You can remove it and keep [ValidateAntiForgeryToken] on individual actions instead.
    [AutoValidateAntiforgeryToken]
    public class CustomersController : Controller
    {
        private readonly ICustomerService _customerService;

        public CustomersController(ICustomerService customerService)
        {
            _customerService = customerService;
        }

        // GET: /customers  (and /customers/index)
        [HttpGet] // optional because *no attribute* defaults to GET in MVC.
        public async Task<IActionResult> Index(string? searchString, string? searchBy,
                                               string? sortBy, bool desc = false,
                                               int page = 1, int pageSize = 20,
                                               CancellationToken ct = default)
        {
            var customers = await _customerService.SearchAsync(searchString, searchBy, sortBy, desc, page, pageSize, ct);
            int totalCount = await _customerService.CountAsync(searchString, searchBy, ct);
            var vm = new CustomersViewModel
            {
                Customers = customers,
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize,
                SearchString = searchString,
                SearchBy = searchBy,
                SortBy = sortBy,
                Desc = desc
            };
            return View(vm);
        }

        // GET: /customers/details/{id}
        [HttpGet] // optional, it is GET by default
        public async Task<IActionResult> Details([FromRoute] Guid id, CancellationToken ct)
        {
            // [FromRoute] optional
            // MVC will bind simple types like Guid from route/query automatically.
            if (id == Guid.Empty) return BadRequest();
            var customer = await _customerService.GetByIdAsync(id, true, ct);
            if (customer == null) return NotFound();
            return View(customer);
        }

        // GET: /customers/create
        [HttpGet] // optional
        public IActionResult Create()
        {
            // Just show the empty create form
            return View();
        }

        // POST: /customers/create
        [HttpPost] // required for POST
        // [ValidateAntiForgeryToken] // optional here due to [AutoValidateAntiforgeryToken], but good for explicitness
        public async Task<IActionResult> Create([Bind] CreateCustomerDto createCustomerDto, CancellationToken ct)
        {
            // [Bind] can restrict which properties are bound to mitigate overposting
            // (optional but recommended if you expose your DTO publicly).
            if (!ModelState.IsValid) return View(createCustomerDto);

            try
            {
                await _customerService.CreateAsync(createCustomerDto, ct);
                return RedirectToAction(nameof(Index));
            }
            catch (DomainValidationException ex)
            {
                foreach (var (key, message) in ex.Errors)
                    ModelState.AddModelError(key, message);
                return View(createCustomerDto);
            }
        }

        // GET: /customers/edit/{id}
        [HttpGet]
        public async Task<IActionResult> Edit([FromRoute] Guid id, CancellationToken ct)
        {
            var customerDto = await _customerService.GetByIdAsync(id, false, ct);
            if (customerDto == null) return NotFound();

            return View(customerDto.ToUpdateCustomerDto());
        }


        // POST: /customers/edit/{id}
        [HttpPost] // required for POST
        public async Task<IActionResult> Edit([Bind] UpdateCustomerDto updateCustomerDto, CancellationToken ct)
        {
            // [Bind] optional; use it to whitelist fields to prevent overposting.
            if (!ModelState.IsValid) return View(updateCustomerDto);

            try
            {
                await _customerService.UpdateAsync(updateCustomerDto, ct);
                return RedirectToAction(nameof(Index));
            }
            catch (DomainValidationException ex)
            {
                foreach (var (key, message) in ex.Errors)
                    ModelState.AddModelError(key, message);
                return View(updateCustomerDto);
            }
        }

        // GET: /customers/delete/{id}
        [HttpGet] // optional
        public async Task<IActionResult> Delete([FromRoute] Guid id, CancellationToken ct) // [FromRoute] optional
        {
            var customer = await _customerService.GetByIdAsync(id, true, ct);
            if (customer == null) return NotFound();
            if (customer.Orders?.Any() == true) ViewBag.CustomerHasOrders = true; // pass flag to the view
            return View(customer);
        }

        // POST: /customers/delete/{id}
        [HttpPost] // required for POST
        [ActionName("Delete")] // required here because method name is DeleteConfirmed but route must stay "Delete"
        // [ValidateAntiForgeryToken] // optional here due to controller-level auto validation
        public async Task<IActionResult> DeleteConfirmed([FromRoute] Guid id, CancellationToken ct) // [FromRoute] optional
        {
            var customer = await _customerService.GetByIdAsync(id, true, ct);
            if (customer == null) return NotFound();

            // First check if the customer has orders
            if (customer.Orders?.Any() == true)
            {
                ModelState.AddModelError(string.Empty, "This customer cannot be deleted because they have existing orders.");
                ViewBag.CustomerHasOrders = true; // keep the flag for the view
                return View(customer);
            }

            // No orders → safe to delete
            var deleted = await _customerService.DeleteAsync(id, ct);
            if (!deleted)
            {
                ModelState.AddModelError(string.Empty, "This customer cannot be deleted.");
                return View(customer);
            }

            return RedirectToAction(nameof(Index));
        }
    }
}

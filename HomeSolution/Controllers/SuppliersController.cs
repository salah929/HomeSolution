using Home.DTOs;
using Home.Entities;
using Home.ServiceContracts;
using Home.Services;
using Home.Services.Exceptions;
using Home.Services.Mappers;
using Home.Web.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace Home.Web.Controllers
{
    // [AutoValidateAntiforgeryToken] automatically requires a valid anti-forgery token on all non-GET actions (POST/PUT/DELETE).
    // Optional but RECOMMENDED for MVC apps with forms. You can remove it and keep [ValidateAntiForgeryToken] on individual actions instead.
    [AutoValidateAntiforgeryToken]
    public class SuppliersController : Controller
    {
        private readonly ISupplierService _supplierService;

        public SuppliersController(ISupplierService supplierService)
        {
            _supplierService = supplierService;
        }

        // GET: /suppliers  (and /suppliers/index)
        [HttpGet] // optional because *no attribute* defaults to GET in MVC.
        public async Task<IActionResult> Index(string? searchString, string? searchBy,
                                               string? sortBy, bool desc = false,
                                               int page = 1, int pageSize = 20,
                                               CancellationToken ct = default)
        {
            var suppliers = await _supplierService.SearchAsync(searchString, searchBy, sortBy, desc, page, pageSize, ct);
            int totalCount = await _supplierService.CountAsync(searchString, searchBy, ct);
            var vm = new SuppliersViewModel
            {
                Suppliers = suppliers,
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

        // GET: /suppliers/details/{id}
        [HttpGet] // optional, it is GET by default
        public async Task<IActionResult> Details([FromRoute] Guid id, CancellationToken ct)
        {
            // [FromRoute] optional
            // MVC will bind simple types like Guid from route/query automatically.
            if (id == Guid.Empty) return BadRequest();
            var supplier = await _supplierService.GetByIdAsync(id, true, true, ct);
            if (supplier == null) return NotFound();
            return View(supplier);
        }

        // GET: /suppliers/create
        [HttpGet] // optional
        public IActionResult Create()
        {
            // Just show the empty create form
            return View();
        }

        // POST: /suppliers/create
        [HttpPost] // required for POST
        // [ValidateAntiForgeryToken] // optional here due to [AutoValidateAntiforgeryToken], but good for explicitness
        public async Task<IActionResult> Create([Bind] CreateSupplierDto createSupplierDto, CancellationToken ct)
        {
            // [Bind] can restrict which properties are bound to mitigate overposting
            // (optional but recommended if you expose your DTO publicly).
            if (!ModelState.IsValid) return View(createSupplierDto);

            try
            {
                await _supplierService.CreateAsync(createSupplierDto, ct);
                return RedirectToAction(nameof(Index));
            }
            catch (DomainValidationException ex)
            {
                foreach (var (key, message) in ex.Errors)
                    ModelState.AddModelError(key, message);
                return View(createSupplierDto);
            }
        }

        // GET: /suppliers/edit/{id}
        [HttpGet]
        public async Task<IActionResult> Edit([FromRoute] Guid id, CancellationToken ct)
        {
            SupplierDto? supplierDto = await _supplierService.GetByIdAsync(id, false, false, ct);
            if (supplierDto == null) return NotFound();
            return View(supplierDto.ToUpdateSupplierDto());
        }

        // POST: /suppliers/edit/{id}
        [HttpPost] // required for POST
        public async Task<IActionResult> Edit([Bind] UpdateSupplierDto updateSupplierDto, CancellationToken ct)
        {
            // [Bind] optional; use it to whitelist fields to prevent overposting.
            if (!ModelState.IsValid) return View(updateSupplierDto);

            try
            {
                await _supplierService.UpdateAsync(updateSupplierDto, ct);
                return RedirectToAction(nameof(Index));
            }
            catch (DomainValidationException ex)
            {
                foreach (var (key, message) in ex.Errors)
                    ModelState.AddModelError(key, message);
                return View(updateSupplierDto);
            }
        }

        // GET: /suppliers/delete/{id}
        [HttpGet] // optional
        public async Task<IActionResult> Delete([FromRoute] Guid id, CancellationToken ct) // [FromRoute] optional
        {
            var supplier = await _supplierService.GetByIdAsync(id, true, true, ct);
            if (supplier == null) return NotFound();
            if (supplier.Orders?.Any() == true) ViewBag.SupplierHasOrders = true; // pass flag to the view
            if (supplier.Products?.Any() == true) ViewBag.SupplierHasProducts = true; // pass flag to the view
            return View(supplier);
        }

        // POST: /suppliers/delete/{id}
        [HttpPost] // required for POST
        [ActionName("Delete")] // required here because method name is DeleteConfirmed but route must stay "Delete"
        // [ValidateAntiForgeryToken] // optional here due to controller-level auto validation
        public async Task<IActionResult> DeleteConfirmed([FromRoute] Guid id, CancellationToken ct) // [FromRoute] optional
        {
            var supplier = await _supplierService.GetByIdAsync(id, true, true, ct);
            if (supplier == null) return NotFound();

            bool supplierHasOrders = supplier.Orders?.Any() == true;
            bool supplierHasProducts = supplier.Products?.Any() == true;
            if (supplierHasOrders || supplierHasProducts)
            {
                ModelState.AddModelError(string.Empty, "Cannot delete supplier with existing orders.");
                ViewBag.SupplierHasOrders = supplierHasOrders; // keep the flag for the view
                ViewBag.SupplierHasProducts = supplierHasProducts; // keep the flag for the view
                return View(supplier);
            }

            var deleted = await _supplierService.DeleteAsync(id, ct);
            if (!deleted)
            {
                ModelState.AddModelError(string.Empty, "This supplier cannot be deleted.");
                return View(supplier);
            }
            return RedirectToAction(nameof(Index));
        }
    }
}

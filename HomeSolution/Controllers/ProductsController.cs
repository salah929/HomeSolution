using Home.DTOs;
using Home.Entities;
using Home.ServiceContracts;
using Home.Services;
using Home.Services.Exceptions;
using Home.Services.Mappers;
using Home.Web.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace Home.Web.Controllers
{
    // [AutoValidateAntiforgeryToken] automatically requires a valid anti-forgery token on all non-GET actions (POST/PUT/DELETE).
    // Optional but RECOMMENDED for MVC apps with forms. You can remove it and keep [ValidateAntiForgeryToken] on individual actions instead.
    [AutoValidateAntiforgeryToken]
    public class ProductsController : Controller
    {
        private readonly IProductService _productService;
        private readonly ISupplierService _supplierService;
        private readonly ICustomerOrderService _customerOrderService;
        private readonly ISupplierOrderService _supplierOrderService;

        public ProductsController(IProductService productService, ISupplierService supplierService,
                                  ICustomerOrderService customerOrderService, ISupplierOrderService supplierOrderService)
        {
            _productService = productService;
            _supplierService = supplierService;
            _customerOrderService = customerOrderService;
            _supplierOrderService = supplierOrderService;
        }

        private async Task<SelectList> GetSupplierListAsync(Guid? supplierId = null, CancellationToken ct = default)
        {
            var suppliers = (await _supplierService.GetAllAsync(ct)).ToList();
            suppliers.Insert(0, new SupplierDto
            {
                SupplierId = Guid.Empty,
                Name = "-- please select --"
            });
            return new SelectList(suppliers, "SupplierId", "Name", supplierId);
        }

        // GET: /products  (and /products/index)
        [HttpGet] // optional because *no attribute* defaults to GET in MVC.
        public async Task<IActionResult> Index(string? searchString, string? searchBy, Guid? supplierId,
                                               string? sortBy, bool desc = false, int page = 1, int pageSize = 20,
                                               CancellationToken ct = default)
        {
            var products = await _productService.SearchAsync( searchString, searchBy, supplierId, sortBy, desc, page, pageSize, ct);

            int totalCount = await _productService.CountAsync(searchString, searchBy, supplierId, ct);
            IReadOnlyList<SupplierDto> suppliers = await _supplierService.GetAllAsync(ct);
            var vm = new ProductsViewModel
            {
                Products = products,
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize,
                SearchString = searchString,
                SearchBy = searchBy,
                SupplierId = supplierId,
                SortBy = sortBy,
                Desc = desc,
                SupplierList = new SelectList(suppliers.OrderBy(s => s.Name), "SupplierId", "Name", supplierId)
            };

            return View(vm);
        }

        // GET: /products/details/{id}
        [HttpGet] // optional, it is GET by default
        public async Task<IActionResult> Details([FromRoute] Guid id, CancellationToken ct)
        {
            // [FromRoute] optional
            // MVC will bind simple types like Guid from route/query automatically.
            var product = await _productService.GetByIdAsync(id, ct);
            if (product == null) return NotFound(); // 404 if not found
            return View(product);
        }

        // GET: /products/create
        [HttpGet] // optional
        public async Task<IActionResult> Create(CancellationToken ct)
        {
            var vm = new ProductViewModel();
            vm.SupplierList = await GetSupplierListAsync(null, ct);
            return View(vm);
        }

        // POST: /products/create
        [HttpPost] // required for POST
        // [ValidateAntiForgeryToken] // optional if you already have AutoValidateAntiforgeryToken
        public async Task<IActionResult> Create(ProductViewModel vm, CancellationToken ct)
        {
            if (!ModelState.IsValid)
            {
                vm.SupplierList = await GetSupplierListAsync(vm.SupplierId, ct);
                return View(vm); // return the correct view model
            }

            try
            {
                // Map from ProductViewModel.Product (ProductDto) to CreateProductDto
                var dto = vm.ToCreateProductDto();

                var id = await _productService.CreateAsync(dto, ct);
                return RedirectToAction(nameof(Index));
            }
            catch (DomainValidationException ex)
            {
                foreach (var (key, msg) in ex.Errors)
                    ModelState.AddModelError(string.IsNullOrEmpty(key) ? string.Empty : key, msg);

                vm.SupplierList = await GetSupplierListAsync(vm.SupplierId, ct);
                return View(vm); // return the correct view model
            }
        }


        // GET: /products/edit/{id}
        [HttpGet] // optional
        public async Task<IActionResult> Edit([FromRoute] Guid id, CancellationToken ct)
        {
            // [FromRoute] optional; default model binding handles simple route params.
            var product = await _productService.GetByIdAsync(id, ct);
            if (product == null) return NotFound();
            var vm = new ProductViewModel()
            {
                ProductId = product.ProductId,
                Code = product.Code,
                Name = product.Name,
                Description = product.Description,
                SupplierId = product.SupplierId,
                SupplierList = await GetSupplierListAsync(null, ct)
            };
            return View(vm);
        }

        // POST: /products/edit/{id}
        [HttpPost] // required for POST
        public async Task<IActionResult> Edit(ProductViewModel vm, CancellationToken ct)
        {
            // [Bind] optional; use it to whitelist fields to prevent overposting.
            if (!ModelState.IsValid)
            {
                vm.SupplierList = await GetSupplierListAsync(vm.SupplierId, ct);
                return View(vm); // return the correct view model
            }

            try
            {
                var dto = vm.ToUpdateProductDto();
                var updated = await _productService.UpdateAsync(dto, ct);
                if (!updated) return NotFound();
                return RedirectToAction(nameof(Index));
            }
            catch (DomainValidationException ex)
            {
                foreach (var (key, msg) in ex.Errors)
                    ModelState.AddModelError(string.IsNullOrEmpty(key) ? string.Empty : key, msg);

                vm.SupplierList = await GetSupplierListAsync(vm.SupplierId, ct);
                return View(vm); // return the correct view model
            }
        }

        // GET: /products/delete/{id}
        [HttpGet] // optional
        public async Task<IActionResult> Delete(Guid id, CancellationToken ct) // [FromRoute] optional
        {
            var product = await _productService.GetByIdAsync(id, ct);
            if (product == null) return NotFound();
            var customerOrders = await _productService.GetCustomerOrdersByProductId(id, ct);
            if (customerOrders.Any()) ViewBag.ProductExistsInCustomerOrders = true;
            var supplierOrders = await _productService.GetSupplierOrdersByProductId(id, ct);
            if (supplierOrders.Any()) ViewBag.ProductExistsInSupplierOrders = true;

            return View(product);
        }

        // POST: /products/delete/{id}
        [HttpPost] // required for POST
        [ActionName("Delete")] // required here because method name is DeleteConfirmed but route must stay "Delete"
        // [ValidateAntiForgeryToken] // optional here due to controller-level auto validation
        public async Task<IActionResult> DeleteConfirmed([FromRoute] Guid id, CancellationToken ct) // [FromRoute] optional
        {
            try
            {
                var deleted = await _productService.DeleteAsync(id, ct);
                if (!deleted) return NotFound();
                return RedirectToAction(nameof(Index));
            }
            catch (DomainValidationException ex)
            {
                foreach (var (key, msg) in ex.Errors)
                    ModelState.AddModelError(string.IsNullOrEmpty(key) ? string.Empty : key, msg);
                var product = await _productService.GetByIdAsync(id, ct);
                return View(product); // re-show confirmation view with error
            }
        }
    }
}

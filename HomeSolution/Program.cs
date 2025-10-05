using Home.Data;
using Home.Repositories;
using Home.RepositoryContracts;
using Home.ServiceContracts;
using Home.Services;
using Home.Web;
using Microsoft.EntityFrameworkCore;

// MVC (controllers + views)
var builder = WebApplication.CreateBuilder(args);
builder.Services.AddControllersWithViews();

// DI – Services
builder.Services.AddScoped<IProductService, ProductService>();
builder.Services.AddScoped<ICustomerService, CustomerService>();
builder.Services.AddScoped<ISupplierService, SupplierService>();
builder.Services.AddScoped<ICustomerOrderService, CustomerOrderService>();
builder.Services.AddScoped<ISupplierOrderService, SupplierOrderService>();

// DI – Repositories
builder.Services.AddScoped<IProductRepository, ProductRepository>();
builder.Services.AddScoped<ICustomerRepository, CustomerRepository>();
builder.Services.AddScoped<ISupplierRepository, SupplierRepository>();
builder.Services.AddScoped<ICustomerOrderRepository, CustomerOrderRepository>();
builder.Services.AddScoped<ISupplierOrderRepository, SupplierOrderRepository>();

builder.Services.AddDbContext<HomeDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddRouting(options =>
{
    options.LowercaseUrls = true;
    options.LowercaseQueryStrings = true;
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
    // Optional during development:
    // builder.Services.AddRazorPages().AddRazorRuntimeCompilation();
}
else
{
    app.UseExceptionHandler("/Home/Error"); // make an Error view at Views/Home/Error.cshtml
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

// Conventional MVC route (adjust default controller/action)
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

await SeedData.EnsureSeededAsync(app.Services);

app.Run();

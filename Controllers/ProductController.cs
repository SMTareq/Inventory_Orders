using InventoryOrderSystem.Services;
using InventoryOrderSystem.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace InventoryOrderSystem.Controllers
{
    [Authorize]
    public class ProductController : Controller
    {
        private readonly IProductService _productService;
        private readonly ILogger<ProductController> _logger;

        public ProductController(IProductService productService, ILogger<ProductController> logger)
        {
            _productService = productService;
            _logger = logger;
        }

        // GET /Product
        public async Task<IActionResult> Index(string? search, int page = 1, int pageSize = 10)
        {
            var (items, total) = await _productService.GetPagedAsync(page, pageSize, search);
            ViewBag.Search = search;
            ViewBag.Page = page;
            ViewBag.PageSize = pageSize;
            ViewBag.TotalCount = total;
            ViewBag.TotalPages = (int)Math.Ceiling(total / (double)pageSize);
            return View(items);
        }

        // GET /Product/Search?term=
        [HttpGet]
        public async Task<IActionResult> Search(string term)
        {
            if (string.IsNullOrWhiteSpace(term))
                return Json(new { data = new object[] { } });

            var products = await _productService.SearchAsync(term);
            var result = products.Select(p => new
            {
                p.Id, p.Name, p.SKU,
                Price = p.Price.ToString("C"),
                p.QuantityInStock,
                CreatedAt = p.CreatedAt.ToString("yyyy-MM-dd")
            });
            return Json(new { data = result });
        }

        // GET /Product/Create
        public IActionResult Create() => View(new ProductViewModel());

        // POST /Product/Create
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ProductViewModel vm)
        {
            if (!ModelState.IsValid) return View(vm);

            var (success, error) = await _productService.CreateAsync(vm);
            if (!success)
            {
                ModelState.AddModelError(nameof(vm.SKU), error!);
                return View(vm);
            }

            TempData["Success"] = "Product created successfully.";
            return RedirectToAction(nameof(Index));
        }

        // GET /Product/Edit/{id}
        public async Task<IActionResult> Edit(int id)
        {
            var product = await _productService.GetByIdAsync(id);
            if (product == null) return NotFound();

            var vm = new ProductViewModel
            {
                Id = product.Id,
                Name = product.Name,
                SKU = product.SKU,
                Price = product.Price,
                QuantityInStock = product.QuantityInStock,
                CreatedAt = product.CreatedAt
            };
            return View(vm);
        }

        // POST /Product/Edit
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(ProductViewModel vm)
        {
            if (!ModelState.IsValid) return View(vm);

            var (success, error) = await _productService.UpdateAsync(vm);
            if (!success)
            {
                ModelState.AddModelError(string.Empty, error!);
                return View(vm);
            }

            TempData["Success"] = "Product updated successfully.";
            return RedirectToAction(nameof(Index));
        }

        // POST /Product/Delete/{id}
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var (success, error) = await _productService.DeleteAsync(id);
            if (!success)
                TempData["Error"] = error;
            else
                TempData["Success"] = "Product deleted successfully.";

            return RedirectToAction(nameof(Index));
        }

        // GET /Product/GetProducts (for order form dropdown)
        [HttpGet]
        public async Task<IActionResult> GetProducts()
        {
            var products = await _productService.GetSelectListAsync();
            return Json(products);
        }
    }
}

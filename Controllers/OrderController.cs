using System.Globalization;
using System.Text;
using InventoryOrderSystem.Services;
using InventoryOrderSystem.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace InventoryOrderSystem.Controllers
{
    [Authorize]
    public class OrderController : Controller
    {
        private readonly IOrderService _orderService;
        private readonly IProductService _productService;
        private readonly ILogger<OrderController> _logger;

        public OrderController(IOrderService orderService, IProductService productService, ILogger<OrderController> logger)
        {
            _orderService = orderService;
            _productService = productService;
            _logger = logger;
        }

        // GET /Order
        public async Task<IActionResult> Index()
        {
            var orders = await _orderService.GetAllAsync();
            return View(orders);
        }

        // GET /Order/Create
        public async Task<IActionResult> Create()
        {
            ViewBag.Products = await _productService.GetSelectListAsync();
            return View(new OrderCreateViewModel());
        }

        // POST /Order/Create
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(OrderCreateViewModel vm)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.Products = await _productService.GetSelectListAsync();
                return View(vm);
            }

            var (success, error, orderId) = await _orderService.CreateAsync(vm);
            if (!success)
            {
                TempData["Error"] = error;
                ViewBag.Products = await _productService.GetSelectListAsync();
                return View(vm);
            }

            TempData["Success"] = $"Order #{orderId} placed successfully!";
            return RedirectToAction(nameof(Details), new { id = orderId });
        }

        // GET /Order/Details/{id}
        public async Task<IActionResult> Details(int id)
        {
            var order = await _orderService.GetDetailsAsync(id);
            if (order == null) return NotFound();
            return View(order);
        }

        // POST /Order/Delete/{id}
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var (success, error) = await _orderService.DeleteAsync(id);
            TempData[success ? "Success" : "Error"] = success ? "Order deleted successfully." : error;
            return RedirectToAction(nameof(Index));
        }

        // GET /Order/ExportCsv
        public async Task<IActionResult> ExportCsv()
        {
            var orders = await _orderService.GetAllAsync();
            var sb = new StringBuilder();
            sb.AppendLine("Order ID,Customer Name,Order Date,Total Amount,Item Count");

            foreach (var o in orders)
                sb.AppendLine($"{o.Id},\"{o.CustomerName}\",{o.OrderDate:yyyy-MM-dd},{o.TotalAmount:F2},{o.ItemCount}");

            var bytes = Encoding.UTF8.GetBytes(sb.ToString());
            return File(bytes, "text/csv", $"orders_{DateTime.Now:yyyyMMdd}.csv");
        }
    }
}

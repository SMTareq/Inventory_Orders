using InventoryOrderSystem.Models;
using InventoryOrderSystem.Repositories;
using InventoryOrderSystem.ViewModels;

namespace InventoryOrderSystem.Services
{
    public interface IProductService
    {
        Task<IEnumerable<Product>> GetAllAsync();
        Task<IEnumerable<Product>> SearchAsync(string term);
        Task<Product?> GetByIdAsync(int id);
        Task<(bool Success, string? Error)> CreateAsync(ProductViewModel vm);
        Task<(bool Success, string? Error)> UpdateAsync(ProductViewModel vm);
        Task<(bool Success, string? Error)> DeleteAsync(int id);
        Task<IEnumerable<ProductSelectViewModel>> GetSelectListAsync();
        Task<(IEnumerable<Product> Items, int TotalCount)> GetPagedAsync(int page, int pageSize, string? search);
    }

    public interface IOrderService
    {
        Task<IEnumerable<OrderListViewModel>> GetAllAsync();
        Task<OrderDetailsViewModel?> GetDetailsAsync(int id);
        Task<(bool Success, string? Error, int OrderId)> CreateAsync(OrderCreateViewModel vm);
        Task<(bool Success, string? Error)> DeleteAsync(int id);
        Task<DashboardViewModel> GetDashboardAsync();
    }

    public class ProductService : IProductService
    {
        private readonly IProductRepository _repo;
        private readonly ILogger<ProductService> _logger;

        public ProductService(IProductRepository repo, ILogger<ProductService> logger)
        {
            _repo = repo;
            _logger = logger;
        }

        public Task<IEnumerable<Product>> GetAllAsync() => _repo.GetAllAsync();
        public Task<IEnumerable<Product>> SearchAsync(string term) => _repo.SearchAsync(term);
        public Task<Product?> GetByIdAsync(int id) => _repo.GetByIdAsync(id);
        public Task<IEnumerable<ProductSelectViewModel>> GetSelectListAsync() => _repo.GetSelectListAsync();
        public Task<(IEnumerable<Product> Items, int TotalCount)> GetPagedAsync(int page, int pageSize, string? search)
            => _repo.GetPagedAsync(page, pageSize, search);

        public async Task<(bool Success, string? Error)> CreateAsync(ProductViewModel vm)
        {
            if (await _repo.SKUExistsAsync(vm.SKU))
                return (false, $"SKU '{vm.SKU}' already exists.");

            var product = new Product
            {
                Name = vm.Name,
                SKU = vm.SKU,
                Price = vm.Price,
                QuantityInStock = vm.QuantityInStock
            };
            await _repo.CreateAsync(product);
            return (true, null);
        }

        public async Task<(bool Success, string? Error)> UpdateAsync(ProductViewModel vm)
        {
            var product = await _repo.GetByIdAsync(vm.Id);
            if (product == null) return (false, "Product not found.");

            if (await _repo.SKUExistsAsync(vm.SKU, vm.Id))
                return (false, $"SKU '{vm.SKU}' already exists.");

            product.Name = vm.Name;
            product.SKU = vm.SKU;
            product.Price = vm.Price;
            product.QuantityInStock = vm.QuantityInStock;

            await _repo.UpdateAsync(product);
            return (true, null);
        }

        public async Task<(bool Success, string? Error)> DeleteAsync(int id)
        {
            var product = await _repo.GetByIdAsync(id);
            if (product == null) return (false, "Product not found.");

            await _repo.DeleteAsync(id);
            return (true, null);
        }
    }

    public class OrderService : IOrderService
    {
        private readonly IOrderRepository _repo;
        private readonly ILogger<OrderService> _logger;

        public OrderService(IOrderRepository repo, ILogger<OrderService> logger)
        {
            _repo = repo;
            _logger = logger;
        }

        public Task<IEnumerable<OrderListViewModel>> GetAllAsync() => _repo.GetAllAsync();
        public Task<OrderDetailsViewModel?> GetDetailsAsync(int id) => _repo.GetDetailsAsync(id);
        public Task<DashboardViewModel> GetDashboardAsync() => _repo.GetDashboardDataAsync();

        public async Task<(bool Success, string? Error, int OrderId)> CreateAsync(OrderCreateViewModel vm)
        {
            if (!vm.Items.Any())
                return (false, "Order must have at least one item.", 0);

            var order = new Order { CustomerName = vm.CustomerName, OrderDate = DateTime.Now };
            var items = vm.Items.Select(i => new OrderItem
            {
                ProductId = i.ProductId,
                Quantity = i.Quantity
            }).ToList();

            try
            {
                var id = await _repo.CreateAsync(order, items);
                return (true, null, id);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning("Order creation failed: {Message}", ex.Message);
                return (false, ex.Message, 0);
            }
        }

        public async Task<(bool Success, string? Error)> DeleteAsync(int id)
        {
            var result = await _repo.DeleteAsync(id);
            return result ? (true, null) : (false, "Order not found.");
        }
    }
}

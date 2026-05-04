using InventoryOrderSystem.Models;
using InventoryOrderSystem.ViewModels;

namespace InventoryOrderSystem.Repositories
{
    public interface IProductRepository
    {
        Task<IEnumerable<Product>> GetAllAsync();
        Task<IEnumerable<Product>> SearchAsync(string term);
        Task<Product?> GetByIdAsync(int id);
        Task<bool> SKUExistsAsync(string sku, int excludeId = 0);
        Task<Product> CreateAsync(Product product);
        Task UpdateAsync(Product product);
        Task DeleteAsync(int id);
        Task<IEnumerable<ProductSelectViewModel>> GetSelectListAsync();
        Task<(IEnumerable<Product> Items, int TotalCount)> GetPagedAsync(int page, int pageSize, string? search = null);
    }

    public interface IOrderRepository
    {
        Task<IEnumerable<OrderListViewModel>> GetAllAsync();
        Task<OrderDetailsViewModel?> GetDetailsAsync(int id);
        Task<int> CreateAsync(Models.Order order, List<OrderItem> items);
        Task<bool> DeleteAsync(int id);
        Task<DashboardViewModel> GetDashboardDataAsync();
    }
}

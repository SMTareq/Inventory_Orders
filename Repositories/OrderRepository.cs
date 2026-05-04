using Dapper;
using InventoryOrderSystem.Data;
using InventoryOrderSystem.Models;
using InventoryOrderSystem.ViewModels;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace InventoryOrderSystem.Repositories
{
    public class OrderRepository : IOrderRepository
    {
        private readonly AppDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly ILogger<OrderRepository> _logger;

        public OrderRepository(AppDbContext context, IConfiguration configuration, ILogger<OrderRepository> logger)
        {
            _context = context;
            _configuration = configuration;
            _logger = logger;
        }

        private SqlConnection GetConnection() =>
            new SqlConnection(_configuration.GetConnectionString("DefaultConnection"));

        // Dapper reads
        public async Task<IEnumerable<OrderListViewModel>> GetAllAsync()
        {
            using var conn = GetConnection();
            return await conn.QueryAsync<OrderListViewModel>("""
                SELECT o.Id, o.CustomerName, o.OrderDate, o.TotalAmount,
                       COUNT(oi.Id) AS ItemCount
                FROM Orders o
                LEFT JOIN OrderItems oi ON oi.OrderId = o.Id
                GROUP BY o.Id, o.CustomerName, o.OrderDate, o.TotalAmount
                ORDER BY o.OrderDate DESC
                """);
        }

        public async Task<OrderDetailsViewModel?> GetDetailsAsync(int id)
        {
            using var conn = GetConnection();

            var orderSql = "SELECT Id, CustomerName, OrderDate, TotalAmount FROM Orders WHERE Id = @id";
            var order = await conn.QueryFirstOrDefaultAsync<OrderDetailsViewModel>(orderSql, new { id });

            if (order == null) return null;

            var itemsSql = """
                SELECT p.Name AS ProductName, p.SKU, oi.Quantity, oi.UnitPrice
                FROM OrderItems oi
                INNER JOIN Products p ON p.Id = oi.ProductId
                WHERE oi.OrderId = @id
                """;
            order.Items = (await conn.QueryAsync<OrderItemDetailsViewModel>(itemsSql, new { id })).ToList();
            return order;
        }

        public async Task<DashboardViewModel> GetDashboardDataAsync()
        {
            using var conn = GetConnection();
            var summary = await conn.QueryFirstAsync("""
                SELECT 
                    ISNULL(SUM(TotalAmount), 0) AS TotalSales,
                    COUNT(*) AS TotalOrders
                FROM Orders
                """);

            var totalProducts = await conn.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM Products");

            var lowStock = await conn.QueryAsync<LowStockProductViewModel>(
                "SELECT Id, Name, SKU, QuantityInStock FROM Products WHERE QuantityInStock <= 10 ORDER BY QuantityInStock ASC");

            var recentOrders = await conn.QueryAsync<RecentOrderViewModel>("""
                SELECT TOP 5 Id, CustomerName, OrderDate, TotalAmount 
                FROM Orders ORDER BY OrderDate DESC
                """);

            return new DashboardViewModel
            {
                TotalSales = (decimal)summary.TotalSales,
                TotalOrders = (int)summary.TotalOrders,
                TotalProducts = totalProducts,
                LowStockProducts = lowStock.ToList(),
                RecentOrders = recentOrders.ToList()
            };
        }

        // EF Core writes with transaction
        public async Task<int> CreateAsync(Order order, List<OrderItem> items)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // Check stock and deduct
                foreach (var item in items)
                {
                    var product = await _context.Products.FindAsync(item.ProductId)
                        ?? throw new InvalidOperationException($"Product {item.ProductId} not found.");

                    if (product.QuantityInStock < item.Quantity)
                        throw new InvalidOperationException($"Insufficient stock for '{product.Name}'. Available: {product.QuantityInStock}");

                    product.QuantityInStock -= item.Quantity;
                    item.UnitPrice = product.Price;
                }

                order.TotalAmount = items.Sum(i => i.Quantity * i.UnitPrice);
                order.OrderItems = items;

                _context.Orders.Add(order);
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                _logger.LogInformation("Order created: Id={Id}, Customer={Customer}, Total={Total}",
                    order.Id, order.CustomerName, order.TotalAmount);
                return order.Id;
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var order = await _context.Orders.Include(o => o.OrderItems).FirstOrDefaultAsync(o => o.Id == id);
            if (order == null) return false;

            // Restore stock
            foreach (var item in order.OrderItems)
            {
                var product = await _context.Products.FindAsync(item.ProductId);
                if (product != null) product.QuantityInStock += item.Quantity;
            }

            _context.Orders.Remove(order);
            await _context.SaveChangesAsync();
            _logger.LogInformation("Order deleted: Id={Id}", id);
            return true;
        }
    }
}

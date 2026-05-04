using Dapper;
using InventoryOrderSystem.Data;
using InventoryOrderSystem.Models;
using InventoryOrderSystem.ViewModels;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace InventoryOrderSystem.Repositories
{
    public class ProductRepository : IProductRepository
    {
        private readonly AppDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly ILogger<ProductRepository> _logger;

        public ProductRepository(AppDbContext context, IConfiguration configuration, ILogger<ProductRepository> logger)
        {
            _context = context;
            _configuration = configuration;
            _logger = logger;
        }

        private SqlConnection GetConnection() =>
            new SqlConnection(_configuration.GetConnectionString("DefaultConnection"));

        // Read via Dapper
        public async Task<IEnumerable<Product>> GetAllAsync()
        {
            using var conn = GetConnection();
            return await conn.QueryAsync<Product>("SELECT * FROM Products ORDER BY CreatedAt DESC");
        }

        public async Task<IEnumerable<Product>> SearchAsync(string term)
        {
            using var conn = GetConnection();
            return await conn.QueryAsync<Product>(
                "SELECT * FROM Products WHERE Name LIKE @term OR SKU LIKE @term ORDER BY Name",
                new { term = $"%{term}%" });
        }

        public async Task<Product?> GetByIdAsync(int id)
        {
            using var conn = GetConnection();
            return await conn.QueryFirstOrDefaultAsync<Product>(
                "SELECT * FROM Products WHERE Id = @id", new { id });
        }

        public async Task<bool> SKUExistsAsync(string sku, int excludeId = 0)
        {
            using var conn = GetConnection();
            var count = await conn.ExecuteScalarAsync<int>(
                "SELECT COUNT(1) FROM Products WHERE SKU = @sku AND Id != @excludeId",
                new { sku, excludeId });
            return count > 0;
        }

        public async Task<IEnumerable<ProductSelectViewModel>> GetSelectListAsync()
        {
            using var conn = GetConnection();
            return await conn.QueryAsync<ProductSelectViewModel>(
                "SELECT Id, Name, SKU, Price, QuantityInStock FROM Products WHERE QuantityInStock > 0 ORDER BY Name");
        }

        public async Task<(IEnumerable<Product> Items, int TotalCount)> GetPagedAsync(int page, int pageSize, string? search = null)
        {
            using var conn = GetConnection();
            var offset = (page - 1) * pageSize;
            var whereClause = string.IsNullOrWhiteSpace(search)
                ? ""
                : "WHERE Name LIKE @search OR SKU LIKE @search";

            var countSql = $"SELECT COUNT(1) FROM Products {whereClause}";
            var dataSql = $"""
                SELECT * FROM Products {whereClause}
                ORDER BY CreatedAt DESC
                OFFSET @offset ROWS FETCH NEXT @pageSize ROWS ONLY
                """;

            var param = new { search = $"%{search}%", offset, pageSize };
            var total = await conn.ExecuteScalarAsync<int>(countSql, param);
            var items = await conn.QueryAsync<Product>(dataSql, param);
            return (items, total);
        }

        // Write via EF Core
        public async Task<Product> CreateAsync(Product product)
        {
            _context.Products.Add(product);
            await _context.SaveChangesAsync();
            _logger.LogInformation("Product created: {Name} ({SKU})", product.Name, product.SKU);
            return product;
        }

        public async Task UpdateAsync(Product product)
        {
            _context.Products.Update(product);
            await _context.SaveChangesAsync();
            _logger.LogInformation("Product updated: Id={Id}", product.Id);
        }

        public async Task DeleteAsync(int id)
        {
            var product = await _context.Products.FindAsync(id);
            if (product != null)
            {
                _context.Products.Remove(product);
                await _context.SaveChangesAsync();
                _logger.LogInformation("Product deleted: Id={Id}", id);
            }
        }
    }
}

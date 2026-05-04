using InventoryOrderSystem.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace InventoryOrderSystem.Data
{
    public class AppDbContext : IdentityDbContext<ApplicationUser>
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<Product> Products { get; set; }
        public DbSet<Order> Orders { get; set; }
        public DbSet<OrderItem> OrderItems { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.Entity<Product>(e =>
            {
                e.HasKey(p => p.Id);
                e.Property(p => p.Name).IsRequired().HasMaxLength(150);
                e.Property(p => p.SKU).IsRequired().HasMaxLength(50);
                e.HasIndex(p => p.SKU).IsUnique();
                e.Property(p => p.Price).HasColumnType("decimal(10,2)");
                e.Property(p => p.CreatedAt).HasDefaultValueSql("GETDATE()");
            });

            builder.Entity<Order>(e =>
            {
                e.HasKey(o => o.Id);
                e.Property(o => o.CustomerName).HasMaxLength(150);
                e.Property(o => o.TotalAmount).HasColumnType("decimal(10,2)");
                e.Property(o => o.OrderDate).HasDefaultValueSql("GETDATE()");
                e.HasMany(o => o.OrderItems)
                 .WithOne(oi => oi.Order)
                 .HasForeignKey(oi => oi.OrderId)
                 .OnDelete(DeleteBehavior.Cascade);
            });

            builder.Entity<OrderItem>(e =>
            {
                e.HasKey(oi => oi.Id);
                e.Property(oi => oi.UnitPrice).HasColumnType("decimal(10,2)");
                e.HasOne(oi => oi.Product)
                 .WithMany()
                 .HasForeignKey(oi => oi.ProductId)
                 .OnDelete(DeleteBehavior.Restrict);
            });
        }
    }
}

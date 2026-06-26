using System.Data;
using System.Data.Entity;
using WpfAppMobileShop.Models;

namespace WpfAppMobileShop.Data
{
    public class StoreDbContext : DbContext
    {
        public StoreDbContext() : base("name=StoreDbConnection")
        {
            Database.Connection.StateChange += (sender, args) =>
            {
                if (args.CurrentState == ConnectionState.Open)
                {
                    using (var cmd = Database.Connection.CreateCommand())
                    {
                        cmd.CommandText = "PRAGMA busy_timeout=5000";
                        cmd.ExecuteNonQuery();
                    }
                }
            };
        }

        public DbSet<User> Users { get; set; }
        public DbSet<Customer> Customers { get; set; }
        public DbSet<Category> Categories { get; set; }
        public DbSet<Product> Products { get; set; }
        public DbSet<Order> Orders { get; set; }
        public DbSet<OrderDetail> OrderDetails { get; set; }
        public DbSet<StockTransaction> StockTransactions { get; set; }
        public DbSet<Supplier> Suppliers { get; set; }
        public DbSet<PromoCode> PromoCodes { get; set; }
        public DbSet<Warranty> Warranties { get; set; }
        public DbSet<Setting> Settings { get; set; }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            modelBuilder.Entity<OrderDetail>()
                .HasRequired(od => od.Order)
                .WithMany(o => o.OrderDetails)
                .HasForeignKey(od => od.OrderId);

            modelBuilder.Entity<OrderDetail>()
                .HasRequired(od => od.Product)
                .WithMany()
                .HasForeignKey(od => od.ProductId);

            modelBuilder.Entity<Order>()
                .HasOptional(o => o.Customer)
                .WithMany(c => c.Orders)
                .HasForeignKey(o => o.CustomerId);

            modelBuilder.Entity<Order>()
                .HasRequired(o => o.User)
                .WithMany(u => u.Orders)
                .HasForeignKey(o => o.UserId);

            modelBuilder.Entity<Category>()
                .HasMany(c => c.Products)
                .WithRequired(p => p.Category)
                .HasForeignKey(p => p.CategoryId);
        }
    }
}

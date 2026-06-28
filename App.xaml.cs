using System;
using System.Data.Entity;
using System.IO;
using System.Linq;
using System.Windows;
using WpfAppMobileShop.Data;
using WpfAppMobileShop.Models;
using WpfAppMobileShop.Views;

namespace WpfAppMobileShop
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            Database.SetInitializer<StoreDbContext>(null);

            var dbPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "StoreDB.sqlite");
            var dbDir = Path.GetDirectoryName(dbPath) ?? "";

            System.Data.SQLite.SQLiteConnection.ClearAllPools();
            GC.Collect();
            GC.WaitForPendingFinalizers();

            using (var conn = new System.Data.SQLite.SQLiteConnection("Data Source=" + dbPath))
            {
                conn.Open();
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = @"
                        CREATE TABLE IF NOT EXISTS Users (
                            UserId INTEGER PRIMARY KEY AUTOINCREMENT,
                            Username TEXT NOT NULL,
                            Password TEXT NOT NULL,
                            FullName TEXT NOT NULL,
                            Role TEXT,
                            Phone TEXT,
                            Email TEXT,
                            IsActive INTEGER NOT NULL DEFAULT 1
                        );
                        CREATE TABLE IF NOT EXISTS Categories (
                            CategoryId INTEGER PRIMARY KEY AUTOINCREMENT,
                            CategoryName TEXT NOT NULL,
                            Description TEXT
                        );
                        CREATE TABLE IF NOT EXISTS Products (
                            ProductId INTEGER PRIMARY KEY AUTOINCREMENT,
                            ProductName TEXT NOT NULL,
                            CategoryId INTEGER NOT NULL,
                            Brand TEXT,
                            Model TEXT,
                            Price REAL NOT NULL,
                            StockQuantity INTEGER NOT NULL DEFAULT 0,
                            Description TEXT,
                            ImageUrl TEXT
                        );
                        CREATE TABLE IF NOT EXISTS Customers (
                            CustomerId INTEGER PRIMARY KEY AUTOINCREMENT,
                            FullName TEXT NOT NULL,
                            Phone TEXT NOT NULL,
                            Email TEXT,
                            Address TEXT,
                            CreatedDate TEXT NOT NULL
                        );
                        CREATE TABLE IF NOT EXISTS Orders (
                            OrderId INTEGER PRIMARY KEY AUTOINCREMENT,
                            OrderDate TEXT NOT NULL,
                            TotalAmount REAL NOT NULL,
                            DiscountAmount REAL NOT NULL DEFAULT 0,
                            FinalAmount REAL NOT NULL DEFAULT 0,
                            Status TEXT DEFAULT 'Completed',
                            Notes TEXT,
                            CustomerId INTEGER,
                            UserId INTEGER NOT NULL
                        );
                        CREATE TABLE IF NOT EXISTS PromoCodes (
                            PromoCodeId INTEGER PRIMARY KEY AUTOINCREMENT,
                            Code TEXT NOT NULL,
                            DiscountType TEXT NOT NULL,
                            DiscountValue REAL NOT NULL,
                            MinOrderAmount REAL NOT NULL DEFAULT 0,
                            ExpiryDate TEXT,
                            IsActive INTEGER NOT NULL DEFAULT 1
                        );
                        CREATE TABLE IF NOT EXISTS Warranties (
                            WarrantyId INTEGER PRIMARY KEY AUTOINCREMENT,
                            OrderDetailId INTEGER NOT NULL,
                            ProductId INTEGER NOT NULL,
                            CustomerId INTEGER NOT NULL,
                            StartDate TEXT NOT NULL,
                            EndDate TEXT NOT NULL,
                            Status TEXT DEFAULT 'Active',
                            Notes TEXT
                        );
                        CREATE TABLE IF NOT EXISTS Settings (
                            Key TEXT PRIMARY KEY,
                            Value TEXT
                        );
                        CREATE TABLE IF NOT EXISTS OrderDetails (
                            OrderDetailId INTEGER PRIMARY KEY AUTOINCREMENT,
                            OrderId INTEGER NOT NULL,
                            ProductId INTEGER NOT NULL,
                            Quantity INTEGER NOT NULL,
                            UnitPrice REAL NOT NULL
                        );
                        CREATE TABLE IF NOT EXISTS Suppliers (
                            SupplierId INTEGER PRIMARY KEY AUTOINCREMENT,
                            SupplierName TEXT NOT NULL,
                            Phone TEXT,
                            Email TEXT,
                            Address TEXT,
                            Notes TEXT
                        );
                        CREATE TABLE IF NOT EXISTS StockTransactions (
                            StockTransactionId INTEGER PRIMARY KEY AUTOINCREMENT,
                            ProductId INTEGER NOT NULL,
                            Quantity INTEGER NOT NULL,
                            Type TEXT NOT NULL,
                            Date TEXT NOT NULL,
                            Notes TEXT,
                            UserId INTEGER NOT NULL,
                            SupplierId INTEGER
                        );
                    ";
                    cmd.ExecuteNonQuery();
                }
            }

            using (var conn2 = new System.Data.SQLite.SQLiteConnection("Data Source=" + dbPath))
            {
                conn2.Open();
                using (var cmd = conn2.CreateCommand())
                {
                    cmd.CommandText = "ALTER TABLE Orders ADD COLUMN DiscountAmount REAL NOT NULL DEFAULT 0";
                    try { cmd.ExecuteNonQuery(); } catch { }
                    cmd.CommandText = "ALTER TABLE Orders ADD COLUMN FinalAmount REAL NOT NULL DEFAULT 0";
                    try { cmd.ExecuteNonQuery(); } catch { }
                    cmd.CommandText = "ALTER TABLE Orders ADD COLUMN Status TEXT DEFAULT 'Completed'";
                    try { cmd.ExecuteNonQuery(); } catch { }
                    cmd.CommandText = "ALTER TABLE Orders ADD COLUMN Notes TEXT";
                    try { cmd.ExecuteNonQuery(); } catch { }
                    cmd.CommandText = "ALTER TABLE Orders ADD COLUMN VatAmount REAL NOT NULL DEFAULT 0";
                    try { cmd.ExecuteNonQuery(); } catch { }
                    cmd.CommandText = "ALTER TABLE Products ADD COLUMN CostPrice REAL NOT NULL DEFAULT 0";
                    try { cmd.ExecuteNonQuery(); } catch { }
                    cmd.CommandText = "UPDATE Products SET CostPrice = 0 WHERE CostPrice IS NULL";
                    try { cmd.ExecuteNonQuery(); } catch { }
                }
            }

            using (var context = new StoreDbContext())
            {
                if (!context.Users.Any())
                {
                    SeedDatabase(context);
                }
                if (!context.Settings.Any())
                {
                    context.Settings.Add(new Setting { Key = "StoreName", Value = "MobileShop" });
                    context.Settings.Add(new Setting { Key = "StoreAddress", Value = "" });
                    context.Settings.Add(new Setting { Key = "StorePhone", Value = "" });
                    context.Settings.Add(new Setting { Key = "VatPercent", Value = "10" });
                    context.Settings.Add(new Setting { Key = "LowStockThreshold", Value = "10" });
                    context.Settings.Add(new Setting { Key = "WarrantyMonths", Value = "12" });
                    context.SaveChanges();
                }
            }

            base.OnStartup(e);
        }

        protected override void OnExit(ExitEventArgs e)
        {
            try
            {
                var dbPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "StoreDB.sqlite");
                if (!File.Exists(dbPath)) return;
                var backupDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Backups");
                if (!Directory.Exists(backupDir)) Directory.CreateDirectory(backupDir);
                var name = $"StoreDB_Auto_{DateTime.Now:yyyyMMdd_HHmmss}.sqlite";
                var dest = Path.Combine(backupDir, name);
                System.Data.SQLite.SQLiteConnection.ClearAllPools();
                GC.Collect(); GC.WaitForPendingFinalizers();
                File.Copy(dbPath, dest, true);
                var backups = Directory.GetFiles(backupDir, "StoreDB_Auto_*.sqlite")
                    .OrderByDescending(f => f).ToList();
                while (backups.Count > 10)
                {
                    File.Delete(backups.Last());
                    backups.RemoveAt(backups.Count - 1);
                }
            }
            catch { }
            base.OnExit(e);
        }

        private void SeedDatabase(StoreDbContext context)
        {
            var admin = new User
            {
                Username = "admin",
                Password = "admin123",
                FullName = "Quản trị viên",
                Role = "Admin",
                IsActive = true
            };
            context.Users.Add(admin);
            context.SaveChanges();

            var catSmartphone = new Category { CategoryName = "Điện thoại thông minh", Description = "Smartphone các hãng" };
            var catBasic = new Category { CategoryName = "Điện thoại phổ thông", Description = "Điện thoại cơ bản" };
            var catAccessory = new Category { CategoryName = "Phụ kiện", Description = "Ốp lưng, sạc, tai nghe" };
            context.Categories.Add(catSmartphone);
            context.Categories.Add(catBasic);
            context.Categories.Add(catAccessory);
            context.SaveChanges();

            var products = new[]
            {
                new Product { ProductName = "iPhone 14 Pro Max", CategoryId = catSmartphone.CategoryId, Brand = "Apple", Model = "14 Pro Max", Price = 30000000, StockQuantity = 15, Description = "Smartphone cao cấp nhất của Apple" },
                new Product { ProductName = "Samsung Galaxy S24 Ultra", CategoryId = catSmartphone.CategoryId, Brand = "Samsung", Model = "S24 Ultra", Price = 25000000, StockQuantity = 20, Description = "Flagship Android mạnh mẽ" },
                new Product { ProductName = "Xiaomi 14 Pro", CategoryId = catSmartphone.CategoryId, Brand = "Xiaomi", Model = "14 Pro", Price = 18000000, StockQuantity = 30, Description = "Flagship giá tốt" },
                new Product { ProductName = "OPPO Find X6 Pro", CategoryId = catSmartphone.CategoryId, Brand = "OPPO", Model = "Find X6 Pro", Price = 20000000, StockQuantity = 12, Description = "Chụp ảnh chuyên nghiệp" },
                new Product { ProductName = "Google Pixel 8 Pro", CategoryId = catSmartphone.CategoryId, Brand = "Google", Model = "Pixel 8 Pro", Price = 22000000, StockQuantity = 8, Description = "Thuần Android, cập nhật nhanh" },
                new Product { ProductName = "Nokia 105", CategoryId = catBasic.CategoryId, Brand = "Nokia", Model = "105", Price = 500000, StockQuantity = 50, Description = "Điện thoại phổ thông pin trâu" },
                new Product { ProductName = "Samsung Guru", CategoryId = catBasic.CategoryId, Brand = "Samsung", Model = "Guru", Price = 400000, StockQuantity = 40, Description = "Điện thoại cơ bản, bền bỉ" },
                new Product { ProductName = "Nokia 3310", CategoryId = catBasic.CategoryId, Brand = "Nokia", Model = "3310", Price = 600000, StockQuantity = 35, Description = "Huyền thoại trở lại" },
                new Product { ProductName = "Ốp lưng iPhone 14 Pro", CategoryId = catAccessory.CategoryId, Brand = "Generic", Model = "iPhone 14 Pro", Price = 150000, StockQuantity = 100, Description = "Ốp lưng silicon mềm" },
                new Product { ProductName = "Sạc nhanh 65W", CategoryId = catAccessory.CategoryId, Brand = "Baseus", Model = "65W GaN", Price = 350000, StockQuantity = 80, Description = "Sạc nhanh GaN 65W" },
                new Product { ProductName = "Tai nghe Bluetooth", CategoryId = catAccessory.CategoryId, Brand = "Xiaomi", Model = "Redmi Buds 4", Price = 500000, StockQuantity = 60, Description = "Tai nghe không dây chống ồn" },
                new Product { ProductName = "Cáp USB-C 2m", CategoryId = catAccessory.CategoryId, Brand = "Anker", Model = "PowerLine III", Price = 100000, StockQuantity = 200, Description = "Cáp sạc bền bỉ" },
                new Product { ProductName = "Pin dự phòng 20000mAh", CategoryId = catAccessory.CategoryId, Brand = "Xiaomi", Model = "Mi Power Bank 3", Price = 600000, StockQuantity = 45, Description = "Pin sạc dự phòng dung lượng lớn" },
                new Product { ProductName = "Miếng dán màn hình", CategoryId = catAccessory.CategoryId, Brand = "Generic", Model = "Universal", Price = 50000, StockQuantity = 150, Description = "Miếng dán cường lực" }
            };
            context.Products.AddRange(products);
            context.SaveChanges();

            var customers = new[]
            {
                new Customer { FullName = "Nguyễn Văn An", Phone = "0901234567", Email = "an@gmail.com", Address = "Hà Nội", CreatedDate = new DateTime(2026, 1, 15) },
                new Customer { FullName = "Trần Thị Bình", Phone = "0912345678", Email = "binh@yahoo.com", Address = "TP HCM", CreatedDate = new DateTime(2026, 2, 20) },
                new Customer { FullName = "Lê Văn Cường", Phone = "0923456789", Email = "cuong@gmail.com", Address = "Đà Nẵng", CreatedDate = new DateTime(2026, 3, 10) },
                new Customer { FullName = "Phạm Thị Dung", Phone = "0934567890", Email = "dung@outlook.com", Address = "Hải Phòng", CreatedDate = new DateTime(2026, 4, 5) },
                new Customer { FullName = "Hoàng Văn Em", Phone = "0945678901", Email = "em@gmail.com", Address = "Cần Thơ", CreatedDate = new DateTime(2026, 5, 1) }
            };
            context.Customers.AddRange(customers);
            context.SaveChanges();

            var today = DateTime.Today;
            var orders = new[]
            {
                new Order { OrderDate = today.AddDays(-6), TotalAmount = 30500000, DiscountAmount = 0, FinalAmount = 30500000, Status = OrderStatus.Completed, CustomerId = customers[0].CustomerId, UserId = admin.UserId },
                new Order { OrderDate = today.AddDays(-5), TotalAmount = 18500000, DiscountAmount = 0, FinalAmount = 18500000, Status = OrderStatus.Completed, CustomerId = customers[1].CustomerId, UserId = admin.UserId },
                new Order { OrderDate = today.AddDays(-4), TotalAmount = 500000, DiscountAmount = 0, FinalAmount = 500000, Status = OrderStatus.Completed, CustomerId = customers[2].CustomerId, UserId = admin.UserId },
                new Order { OrderDate = today.AddDays(-3), TotalAmount = 23050000, DiscountAmount = 0, FinalAmount = 23050000, Status = OrderStatus.Completed, CustomerId = customers[0].CustomerId, UserId = admin.UserId },
                new Order { OrderDate = today.AddDays(-2), TotalAmount = 25700000, DiscountAmount = 0, FinalAmount = 25700000, Status = OrderStatus.Completed, CustomerId = customers[3].CustomerId, UserId = admin.UserId },
                new Order { OrderDate = today.AddDays(-1), TotalAmount = 800000, DiscountAmount = 0, FinalAmount = 800000, Status = OrderStatus.Completed, CustomerId = customers[4].CustomerId, UserId = admin.UserId },
                new Order { OrderDate = today, TotalAmount = 31150000, DiscountAmount = 0, FinalAmount = 31150000, Status = OrderStatus.Completed, CustomerId = customers[1].CustomerId, UserId = admin.UserId }
            };
            context.Orders.AddRange(orders);
            context.SaveChanges();

            context.OrderDetails.AddRange(new[]
            {
                new OrderDetail { OrderId = orders[0].OrderId, ProductId = products[0].ProductId, Quantity = 1, UnitPrice = 30000000 },
                new OrderDetail { OrderId = orders[0].OrderId, ProductId = products[10].ProductId, Quantity = 1, UnitPrice = 500000 },
                new OrderDetail { OrderId = orders[1].OrderId, ProductId = products[2].ProductId, Quantity = 1, UnitPrice = 18000000 },
                new OrderDetail { OrderId = orders[1].OrderId, ProductId = products[11].ProductId, Quantity = 5, UnitPrice = 100000 },
                new OrderDetail { OrderId = orders[2].OrderId, ProductId = products[5].ProductId, Quantity = 1, UnitPrice = 500000 },
                new OrderDetail { OrderId = orders[3].OrderId, ProductId = products[4].ProductId, Quantity = 1, UnitPrice = 22000000 },
                new OrderDetail { OrderId = orders[3].OrderId, ProductId = products[9].ProductId, Quantity = 3, UnitPrice = 350000 },
                new OrderDetail { OrderId = orders[4].OrderId, ProductId = products[1].ProductId, Quantity = 1, UnitPrice = 25000000 },
                new OrderDetail { OrderId = orders[4].OrderId, ProductId = products[12].ProductId, Quantity = 1, UnitPrice = 600000 },
                new OrderDetail { OrderId = orders[4].OrderId, ProductId = products[13].ProductId, Quantity = 2, UnitPrice = 50000 },
                new OrderDetail { OrderId = orders[5].OrderId, ProductId = products[8].ProductId, Quantity = 2, UnitPrice = 150000 },
                new OrderDetail { OrderId = orders[5].OrderId, ProductId = products[11].ProductId, Quantity = 5, UnitPrice = 100000 },
                new OrderDetail { OrderId = orders[6].OrderId, ProductId = products[0].ProductId, Quantity = 1, UnitPrice = 30000000 },
                new OrderDetail { OrderId = orders[6].OrderId, ProductId = products[6].ProductId, Quantity = 2, UnitPrice = 400000 },
                new OrderDetail { OrderId = orders[6].OrderId, ProductId = products[9].ProductId, Quantity = 1, UnitPrice = 350000 }
            });
            context.SaveChanges();
        }
    }
}

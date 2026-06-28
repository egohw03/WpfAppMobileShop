using System;
using System.Data.Entity;
using System.Drawing;
using System.Drawing.Printing;
using System.Linq;
using WpfAppMobileShop.Data;
using WpfAppMobileShop.Models;

namespace WpfAppMobileShop.Helpers
{
    public static class ReceiptPrinter
    {
        public static void PrintReceipt(Order order, IOrderedQueryable<OrderDetail> details, decimal paymentAmount, decimal changeAmount)
        {
            string storeName = "MOBILESHOP";
            string storeAddress = "";
            string storePhone = "";
            decimal vatPercent = 10;
            try
            {
                using (var ctx = new StoreDbContext())
                {
                    storeName = ctx.Settings.Find("StoreName")?.Value ?? "MOBILESHOP";
                    storeAddress = ctx.Settings.Find("StoreAddress")?.Value ?? "";
                    storePhone = ctx.Settings.Find("StorePhone")?.Value ?? "";
                    decimal.TryParse(ctx.Settings.Find("VatPercent")?.Value ?? "10", out vatPercent);
                }
            }
            catch { }

            var doc = new PrintDocument();
            doc.PrintPage += (sender, e) =>
            {
                using (var font = new Font("Consolas", 10))
                using (var boldFont = new Font("Consolas", 11, FontStyle.Bold))
                using (var headerFont = new Font("Consolas", 14, FontStyle.Bold))
                {
                    var y = 10;
                    var left = 10;

                    e.Graphics.DrawString(storeName, headerFont, Brushes.Black, left, y);
                    y += 22;
                    if (!string.IsNullOrWhiteSpace(storeAddress))
                    {
                        e.Graphics.DrawString(storeAddress, font, Brushes.Black, left, y);
                        y += 18;
                    }
                    if (!string.IsNullOrWhiteSpace(storePhone))
                    {
                        e.Graphics.DrawString($"Tel: {storePhone}", font, Brushes.Black, left, y);
                        y += 18;
                    }
                    e.Graphics.DrawString("HOA DON BAN HANG", boldFont, Brushes.Black, left, y);
                    y += 25;
                    e.Graphics.DrawString($"Ma don: #{order.OrderId}", font, Brushes.Black, left, y);
                    y += 18;
                    e.Graphics.DrawString($"Ngay: {order.OrderDate:dd/MM/yyyy HH:mm}", font, Brushes.Black, left, y);
                    y += 18;
                    if (order.Customer != null)
                    {
                        e.Graphics.DrawString($"KH: {order.Customer.FullName}", font, Brushes.Black, left, y);
                        y += 18;
                    }
                    e.Graphics.DrawString($"NV: {order.User?.FullName ?? ""}", font, Brushes.Black, left, y);
                    y += 10;
                    e.Graphics.DrawString(new string('-', 48), font, Brushes.Black, left, y);
                    y += 18;

                    foreach (var item in details.ToList())
                    {
                        var line = $"{item.Product?.ProductName ?? "SP#" + item.ProductId}";
                        e.Graphics.DrawString(line, font, Brushes.Black, left, y);
                        y += 16;
                        line = $"  {item.Quantity} x {item.UnitPrice:N0} = {item.Total:N0}";
                        e.Graphics.DrawString(line, font, Brushes.Black, left, y);
                        y += 18;
                    }

                    e.Graphics.DrawString(new string('-', 48), font, Brushes.Black, left, y);
                    y += 20;
                    e.Graphics.DrawString($"Tong cong: {order.TotalAmount,14:N0}", boldFont, Brushes.Black, left, y);
                    y += 20;
                    if (order.DiscountAmount > 0)
                    {
                        e.Graphics.DrawString($"Giam gia:  {order.DiscountAmount,14:N0}", font, Brushes.Black, left, y);
                        y += 18;
                    }
                    if (order.VatAmount > 0)
                    {
                        e.Graphics.DrawString($"VAT ({vatPercent}%): {order.VatAmount,10:N0}", font, Brushes.Black, left, y);
                        y += 18;
                    }
                    e.Graphics.DrawString($"Thanh toan: {order.FinalAmount,14:N0}", boldFont, Brushes.Black, left, y);
                    y += 20;
                    e.Graphics.DrawString($"Khach dua:  {paymentAmount,14:N0}", font, Brushes.Black, left, y);
                    y += 18;
                    e.Graphics.DrawString($"Tien thua:  {changeAmount,14:N0}", font, Brushes.Black, left, y);
                    y += 28;
                    e.Graphics.DrawString("Cam on quy khach!", boldFont, Brushes.Black, left, y);
                }
            };

            try { doc.Print(); }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Loi in: {ex.Message}", "Loi",
                    System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
            finally { doc.Dispose(); }
        }
    }
}

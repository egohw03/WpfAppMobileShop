using System;
using System.Drawing;
using System.Drawing.Printing;
using System.Linq;
using WpfAppMobileShop.Models;

namespace WpfAppMobileShop.Helpers
{
    public static class ReceiptPrinter
    {
        public static void PrintReceipt(Order order, IOrderedQueryable<OrderDetail> details, decimal paymentAmount, decimal changeAmount)
        {
            var doc = new PrintDocument();
            doc.PrintPage += (sender, e) =>
            {
                var font = new Font("Consolas", 10);
                var boldFont = new Font("Consolas", 11, FontStyle.Bold);
                var headerFont = new Font("Consolas", 14, FontStyle.Bold);
                var y = 10;
                var left = 10;

                e.Graphics.DrawString("MOBILESHOP", headerFont, Brushes.Black, left, y);
                y += 30;
                e.Graphics.DrawString("HOA DON BAN HANG", boldFont, Brushes.Black, left, y);
                y += 25;
                e.Graphics.DrawString($"Ma don: #{order.OrderId}", font, Brushes.Black, left, y);
                y += 20;
                e.Graphics.DrawString($"Ngay: {order.OrderDate:dd/MM/yyyy HH:mm}", font, Brushes.Black, left, y);
                y += 20;
                if (order.Customer != null)
                {
                    e.Graphics.DrawString($"KH: {order.Customer.FullName}", font, Brushes.Black, left, y);
                    y += 20;
                }
                e.Graphics.DrawString($"NV: {order.User?.FullName ?? ""}", font, Brushes.Black, left, y);
                y += 10;
                e.Graphics.DrawString(new string('-', 50), font, Brushes.Black, left, y);
                y += 20;

                foreach (var item in details.ToList())
                {
                    var line = $"{item.Product?.ProductName ?? "SP#" + item.ProductId}";
                    e.Graphics.DrawString(line, font, Brushes.Black, left, y);
                    y += 18;
                    line = $"  {item.Quantity} x {item.UnitPrice:N0} = {item.Total:N0}";
                    e.Graphics.DrawString(line, font, Brushes.Black, left, y);
                    y += 20;
                }

                e.Graphics.DrawString(new string('-', 50), font, Brushes.Black, left, y);
                y += 20;
                e.Graphics.DrawString($"Tong cong: {order.TotalAmount,14:N0}", boldFont, Brushes.Black, left, y);
                y += 22;
                if (order.DiscountAmount > 0)
                {
                    e.Graphics.DrawString($"Giam gia:  {order.DiscountAmount,14:N0}", font, Brushes.Black, left, y);
                    y += 20;
                    e.Graphics.DrawString($"Thanh toan: {order.FinalAmount,14:N0}", boldFont, Brushes.Black, left, y);
                    y += 22;
                }
                e.Graphics.DrawString($"Khach dua:  {paymentAmount,14:N0}", font, Brushes.Black, left, y);
                y += 20;
                e.Graphics.DrawString($"Tien thua:  {changeAmount,14:N0}", font, Brushes.Black, left, y);
                y += 30;
                e.Graphics.DrawString("Cam on quy khach!", boldFont, Brushes.Black, left, y);
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

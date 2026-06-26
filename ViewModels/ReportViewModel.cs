using System;
using System.Collections.ObjectModel;
using System.Data.Entity;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Input;
using WpfAppMobileShop.Data;
using WpfAppMobileShop.Helpers;
using WpfAppMobileShop.Models;

namespace WpfAppMobileShop.ViewModels
{
    public class ReportViewModel : ViewModelBase
    {
        private readonly StoreDbContext _context;
        private ObservableCollection<Order> _orders;
        private DateTime _fromDate;
        private DateTime _toDate;
        private decimal _totalRevenue;
        private decimal _totalDiscount;
        private decimal _totalFinal;
        private int _orderCount;
        private ObservableCollection<object> _topProducts;

        public string Title => "Báo cáo";

        public ObservableCollection<Order> Orders
        {
            get => _orders;
            set => SetProperty(ref _orders, value);
        }
        public DateTime FromDate
        {
            get => _fromDate;
            set { SetProperty(ref _fromDate, value); LoadReport(); }
        }
        public DateTime ToDate
        {
            get => _toDate;
            set { SetProperty(ref _toDate, value); LoadReport(); }
        }
        public decimal TotalRevenue { get => _totalRevenue; set => SetProperty(ref _totalRevenue, value); }
        public decimal TotalDiscount { get => _totalDiscount; set => SetProperty(ref _totalDiscount, value); }
        public decimal TotalFinal { get => _totalFinal; set => SetProperty(ref _totalFinal, value); }
        public int OrderCount { get => _orderCount; set => SetProperty(ref _orderCount, value); }
        public ObservableCollection<object> TopProducts { get => _topProducts; set => SetProperty(ref _topProducts, value); }

        public ICommand ExportCsvCommand { get; }

        public ReportViewModel()
        {
            _context = new StoreDbContext();
            _fromDate = DateTime.Today.AddDays(-30);
            _toDate = DateTime.Today.AddDays(1);
            ExportCsvCommand = new RelayCommand(ExportCsv);
            try { LoadReport(); } catch { InitEmpty(); }
        }

        public override void Dispose() { _context?.Dispose(); base.Dispose(); }
        private void InitEmpty() { Orders = new ObservableCollection<Order>(); TopProducts = new ObservableCollection<object>(); }

        private void LoadReport()
        {
            var q = _context.Orders.Include(o => o.Customer).Include(o => o.User)
                .Where(o => o.OrderDate >= _fromDate && o.OrderDate <= _toDate).ToList();
            Orders = new ObservableCollection<Order>(q.OrderByDescending(o => o.OrderDate));
            TotalRevenue = q.Sum(o => o.TotalAmount);
            TotalDiscount = q.Sum(o => o.DiscountAmount);
            TotalFinal = q.Sum(o => o.FinalAmount);
            OrderCount = q.Count;

            var top = _context.OrderDetails
                .Where(od => od.Order.OrderDate >= _fromDate && od.Order.OrderDate <= _toDate)
                .GroupBy(od => od.Product.ProductName)
                .Select(g => new { SanPham = g.Key, SL = g.Sum(od => od.Quantity), DoanhThu = g.Sum(od => od.Quantity * od.UnitPrice) })
                .OrderByDescending(x => x.DoanhThu)
                .Take(10)
                .ToList();
            TopProducts = new ObservableCollection<object>(top);
        }

        private void ExportCsv()
        {
            try
            {
                var path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
                    $"BaoCao_{_fromDate:yyyyMMdd}_{_toDate:yyyyMMdd}.csv");
                var sb = new StringBuilder();
                sb.AppendLine("Ma don,Ngay,Khach hang,Tong tien,Giam gia,Thanh tien,Trang thai,NV ban");
                foreach (var o in Orders)
                    sb.AppendLine($"{o.OrderId},{o.OrderDate:dd/MM/yyyy HH:mm},{o.Customer?.FullName},{o.TotalAmount},{o.DiscountAmount},{o.FinalAmount},{o.Status},{o.User?.FullName}");
                sb.AppendLine();
                sb.AppendLine("Top san pham ban chay");
                sb.AppendLine("San pham,So luong,Doanh thu");
                foreach (dynamic t in TopProducts)
                    sb.AppendLine($"{t.SanPham},{t.SL},{t.DoanhThu}");
                File.WriteAllText(path, sb.ToString(), Encoding.UTF8);
                System.Windows.MessageBox.Show($"Da xuat bao cao: {path}", "Thanh cong",
                    System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Loi xuat file: {ex.Message}", "Loi",
                    System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }
    }
}

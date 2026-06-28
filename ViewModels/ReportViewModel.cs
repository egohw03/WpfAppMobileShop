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
        private decimal _totalVat;
        private decimal _totalFinal;
        private decimal _totalProfit;
        private int _orderCount;
        private ObservableCollection<object> _topProducts;
        private ObservableCollection<object> _topCustomers;

        public string Title => "Báo cáo";

        public ObservableCollection<Order> Orders
        {
            get => _orders;
            set => SetProperty(ref _orders, value);
        }
        public DateTime FromDate
        {
            get => _fromDate;
            set { SetProperty(ref _fromDate, value); try { LoadReport(); } catch { InitEmpty(); } }
        }
        public DateTime ToDate
        {
            get => _toDate;
            set { SetProperty(ref _toDate, value); try { LoadReport(); } catch { InitEmpty(); } }
        }
        public decimal TotalRevenue { get => _totalRevenue; set => SetProperty(ref _totalRevenue, value); }
        public decimal TotalDiscount { get => _totalDiscount; set => SetProperty(ref _totalDiscount, value); }
        public decimal TotalVat { get => _totalVat; set => SetProperty(ref _totalVat, value); }
        public decimal TotalProfit { get => _totalProfit; set => SetProperty(ref _totalProfit, value); }
        public decimal TotalFinal { get => _totalFinal; set => SetProperty(ref _totalFinal, value); }
        public int OrderCount { get => _orderCount; set => SetProperty(ref _orderCount, value); }
        public ObservableCollection<object> TopProducts { get => _topProducts; set => SetProperty(ref _topProducts, value); }
        public ObservableCollection<object> TopCustomers { get => _topCustomers; set => SetProperty(ref _topCustomers, value); }

        public ICommand ExportCsvCommand { get; }
        public ICommand TodayCommand { get; }
        public ICommand ThisWeekCommand { get; }
        public ICommand ThisMonthCommand { get; }
        public ICommand ThisYearCommand { get; }

        public ReportViewModel()
        {
            _context = new StoreDbContext();
            _fromDate = DateTime.Today.AddDays(-30);
            _toDate = DateTime.Today.AddDays(1);
            ExportCsvCommand = new RelayCommand(ExportCsv);
            TodayCommand = new RelayCommand(() => { FromDate = DateTime.Today; ToDate = DateTime.Today.AddDays(1); });
            ThisWeekCommand = new RelayCommand(() => { FromDate = DateTime.Today.AddDays(-(int)DateTime.Today.DayOfWeek); ToDate = DateTime.Today.AddDays(1); });
            ThisMonthCommand = new RelayCommand(() => { FromDate = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1); ToDate = DateTime.Today.AddDays(1); });
            ThisYearCommand = new RelayCommand(() => { FromDate = new DateTime(DateTime.Today.Year, 1, 1); ToDate = DateTime.Today.AddDays(1); });
            try { LoadReport(); } catch { InitEmpty(); }
        }

        public override void Dispose() { _context?.Dispose(); base.Dispose(); }
        private void InitEmpty() { Orders = new ObservableCollection<Order>(); TopProducts = new ObservableCollection<object>(); TopCustomers = new ObservableCollection<object>(); }

        private void LoadReport()
        {
            var q = _context.Orders.Include(o => o.Customer).Include(o => o.User)
                .Where(o => o.OrderDate >= _fromDate && o.OrderDate <= _toDate).ToList();
            Orders = new ObservableCollection<Order>(q.OrderByDescending(o => o.OrderDate));
            TotalRevenue = q.Sum(o => o.TotalAmount);
            TotalDiscount = q.Sum(o => o.DiscountAmount);
            TotalVat = q.Sum(o => o.VatAmount);
            TotalFinal = q.Sum(o => o.FinalAmount);
            OrderCount = q.Count;

            var orderIds = q.Select(o => o.OrderId).ToList();
            var costSum = _context.OrderDetails
                .Where(od => orderIds.Contains(od.OrderId) && od.Product != null)
                .Select(od => (decimal?)(od.Quantity * od.Product.CostPrice))
                .Sum() ?? 0;
            TotalProfit = _totalFinal - costSum;

            var top = _context.OrderDetails
                .Where(od => od.Order.OrderDate >= _fromDate && od.Order.OrderDate <= _toDate && od.Product != null)
                .GroupBy(od => od.Product.ProductName)
                .Select(g => new { SanPham = g.Key, SL = g.Sum(od => od.Quantity), DoanhThu = g.Sum(od => od.Quantity * od.UnitPrice) })
                .OrderByDescending(x => x.DoanhThu)
                .Take(10)
                .ToList();
            TopProducts = new ObservableCollection<object>(top);

            var customers = q.Where(o => o.Customer != null)
                .GroupBy(o => o.Customer.FullName)
                .Select(g => new { KhachHang = g.Key, SoDon = g.Count(), TongChi = g.Sum(o => o.FinalAmount) })
                .OrderByDescending(x => x.TongChi)
                .Take(10)
                .ToList();
            TopCustomers = new ObservableCollection<object>(customers);
        }

        private void ExportCsv()
        {
            try
            {
                var path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
                    $"BaoCao_{_fromDate:yyyyMMdd}_{_toDate:yyyyMMdd}.csv");
                var sb = new StringBuilder();
                sb.AppendLine("Ma don,Ngay,Khach hang,Tong tien,Giam gia,VAT,Thanh tien,Trang thai,NV ban");
                foreach (var o in Orders)
                {
                    var custName = (o.Customer?.FullName ?? "").Replace(",", " ");
                    var userName = (o.User?.FullName ?? "").Replace(",", " ");
                    sb.AppendLine($"{o.OrderId},{o.OrderDate:dd/MM/yyyy HH:mm},{custName},{o.TotalAmount},{o.DiscountAmount},{o.VatAmount},{o.FinalAmount},{o.Status},{userName}");
                }
                sb.AppendLine();
                sb.AppendLine("Top san pham ban chay");
                sb.AppendLine("San pham,So luong,Doanh thu");
                foreach (dynamic t in TopProducts)
                    sb.AppendLine($"{(t.SanPham ?? "").Replace(",", " ")},{t.SL},{t.DoanhThu}");
                sb.AppendLine();
                sb.AppendLine("Top khach hang");
                sb.AppendLine("Khach hang,So don,Tong chi");
                foreach (dynamic c in TopCustomers)
                    sb.AppendLine($"{(c.KhachHang ?? "").Replace(",", " ")},{c.SoDon},{c.TongChi}");
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

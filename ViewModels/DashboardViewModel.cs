using System;
using System.Linq;
using LiveCharts;
using LiveCharts.Wpf;
using WpfAppMobileShop.Data;
using WpfAppMobileShop.Helpers;

namespace WpfAppMobileShop.ViewModels
{
    public class DashboardViewModel : ViewModelBase
    {
        private readonly StoreDbContext _context;
        private SeriesCollection _revenueSeries;
        private SeriesCollection _topProductsSeries;
        private string[] _revenueLabels;
        private string[] _topProductLabels;
        private int _totalOrders;
        private int _totalProducts;
        private int _totalCustomers;
        private decimal _todayRevenue;

        public string Title => "Bảng điều khiển";

        public SeriesCollection RevenueSeries
        {
            get => _revenueSeries;
            set => SetProperty(ref _revenueSeries, value);
        }

        public SeriesCollection TopProductsSeries
        {
            get => _topProductsSeries;
            set => SetProperty(ref _topProductsSeries, value);
        }

        public string[] RevenueLabels
        {
            get => _revenueLabels;
            set => SetProperty(ref _revenueLabels, value);
        }

        public string[] TopProductLabels
        {
            get => _topProductLabels;
            set => SetProperty(ref _topProductLabels, value);
        }

        public int TotalOrders
        {
            get => _totalOrders;
            set => SetProperty(ref _totalOrders, value);
        }

        public int TotalProducts
        {
            get => _totalProducts;
            set => SetProperty(ref _totalProducts, value);
        }

        public int TotalCustomers
        {
            get => _totalCustomers;
            set => SetProperty(ref _totalCustomers, value);
        }

        public decimal TodayRevenue
        {
            get => _todayRevenue;
            set => SetProperty(ref _todayRevenue, value);
        }

        public DashboardViewModel()
        {
            try
            {
                _context = new StoreDbContext();
                LoadStatistics();
            }
            catch
            {
                TotalOrders = TotalProducts = TotalCustomers = 0;
                TodayRevenue = 0;
                RevenueSeries = new SeriesCollection();
                TopProductsSeries = new SeriesCollection();
                RevenueLabels = new string[0];
                TopProductLabels = new string[0];
            }
        }

        public override void Dispose()
        {
            _context?.Dispose();
            base.Dispose();
        }

        private void LoadStatistics()
        {
            var today = DateTime.Today;
            var sevenDaysAgo = today.AddDays(-6);

            TotalOrders = _context.Orders.Count();
            TotalProducts = _context.Products.Sum(p => p.StockQuantity);
            TotalCustomers = _context.Customers.Count();
            TodayRevenue = _context.Orders
                .Where(o => o.OrderDate >= today)
                .Sum(o => (decimal?)o.TotalAmount) ?? 0;

            var orders = _context.Orders
                .Where(o => o.OrderDate >= sevenDaysAgo)
                .ToList();

            var revenueByDay = orders
                .GroupBy(o => o.OrderDate.Date)
                .Select(g => new { Date = g.Key, Total = g.Sum(o => o.TotalAmount) })
                .ToList();

            RevenueSeries = new SeriesCollection
            {
                new ColumnSeries
                {
                    Title = "Doanh thu",
                    Values = new ChartValues<decimal>(
                        Enumerable.Range(0, 7).Select(i =>
                        {
                            var date = sevenDaysAgo.AddDays(i);
                            var match = revenueByDay.FirstOrDefault(r => r.Date == date);
                            return match?.Total ?? 0;
                        })
                    )
                }
            };

            RevenueLabels = Enumerable.Range(0, 7)
                .Select(i => sevenDaysAgo.AddDays(i).ToString("dd/MM"))
                .ToArray();

            var topProducts = _context.OrderDetails
                .GroupBy(od => od.Product.ProductName)
                .Select(g => new { Name = g.Key, Total = g.Sum(od => od.Quantity) })
                .OrderByDescending(p => p.Total)
                .Take(5)
                .ToList();

            TopProductsSeries = new SeriesCollection
            {
                new PieSeries
                {
                    Title = "Sản phẩm",
                    Values = new ChartValues<int>(topProducts.Select(p => p.Total))
                }
            };

            TopProductLabels = topProducts.Select(p => p.Name).ToArray();
        }
    }
}

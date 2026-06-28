using System;
using System.Collections.ObjectModel;
using System.Data.Entity;
using System.Linq;
using System.Windows.Input;
using LiveCharts;
using LiveCharts.Wpf;
using WpfAppMobileShop.Data;
using WpfAppMobileShop.Helpers;
using WpfAppMobileShop.Models;

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
        private int _pendingOrders;
        private int _lowStockProducts;
        private decimal _monthlyRevenue;
        private string _topCustomerName;
        private decimal _topCustomerTotal;
        private ObservableCollection<Order> _recentOrders;
        private bool _isLoading;
        private string _revenueTrend;

        public string Title => "Bảng điều khiển";

        public event Action<string> NavigateRequest;

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

        public int PendingOrders
        {
            get => _pendingOrders;
            set => SetProperty(ref _pendingOrders, value);
        }

        public int LowStockProducts
        {
            get => _lowStockProducts;
            set => SetProperty(ref _lowStockProducts, value);
        }

        public decimal MonthlyRevenue
        {
            get => _monthlyRevenue;
            set => SetProperty(ref _monthlyRevenue, value);
        }

        public string TopCustomerName
        {
            get => _topCustomerName;
            set => SetProperty(ref _topCustomerName, value);
        }

        public decimal TopCustomerTotal
        {
            get => _topCustomerTotal;
            set => SetProperty(ref _topCustomerTotal, value);
        }

        public ObservableCollection<Order> RecentOrders
        {
            get => _recentOrders;
            set => SetProperty(ref _recentOrders, value);
        }

        public bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
        }

        public string RevenueTrend
        {
            get => _revenueTrend;
            set => SetProperty(ref _revenueTrend, value);
        }

        public Func<double, string> RevenueFormatter => value => value.ToString("N0");

        public ICommand GoToSalesCommand { get; }
        public ICommand GoToProductsCommand { get; }
        public ICommand GoToReportsCommand { get; }

        public DashboardViewModel()
        {
            _context = new StoreDbContext();
            GoToSalesCommand = new RelayCommand(() => NavigateRequest?.Invoke("Sales"));
            GoToProductsCommand = new RelayCommand(() => NavigateRequest?.Invoke("Products"));
            GoToReportsCommand = new RelayCommand(() => NavigateRequest?.Invoke("Reports"));

            IsLoading = true;
            try
            {
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
            finally
            {
                IsLoading = false;
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
            var firstOfMonth = new DateTime(today.Year, today.Month, 1);
            var lowThresholdStr = _context.Settings.Find("LowStockThreshold")?.Value ?? "10";
            if (!int.TryParse(lowThresholdStr, out var lowStockThreshold))
                lowStockThreshold = 10;

            TotalOrders = _context.Orders.Count();
            TotalProducts = _context.Products.Sum(p => p.StockQuantity);
            TotalCustomers = _context.Customers.Count();
            TodayRevenue = _context.Orders
                .Where(o => o.OrderDate >= today && o.Status != "Cancelled")
                .Sum(o => (decimal?)o.FinalAmount) ?? 0;

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
                .Where(od => od.Product != null)
                .GroupBy(od => od.Product.ProductName)
                .Select(g => new { Name = g.Key, Total = g.Sum(od => od.Quantity) })
                .OrderByDescending(p => p.Total)
                .Take(5)
                .ToList();

            var topValues = new ChartValues<int>(topProducts.Select(p => p.Total));
            TopProductsSeries = new SeriesCollection();
            var pieSeries = new PieSeries
            {
                Title = "Sản phẩm",
                Values = topValues,
                DataLabels = true,
                LabelPoint = point =>
                {
                    var idx = point.SeriesView.Values.IndexOf(point.Instance);
                    return idx >= 0 && idx < topProducts.Count ? $"{topProducts[idx].Name}: {point.Y}" : point.Y.ToString();
                }
            };
            TopProductsSeries.Add(pieSeries);

            TopProductLabels = topProducts.Select(p => p.Name).ToArray();

            PendingOrders = _context.Orders.Count(o => o.Status == "Pending");
            LowStockProducts = _context.Products.Count(p => p.StockQuantity > 0 && p.StockQuantity < lowStockThreshold);
            MonthlyRevenue = _context.Orders
                .Where(o => o.OrderDate >= firstOfMonth && o.Status != "Cancelled")
                .Sum(o => (decimal?)o.FinalAmount) ?? 0;

            var topCustomerData = _context.Orders
                .Where(o => o.CustomerId != null && o.Status != "Cancelled")
                .GroupBy(o => o.CustomerId.Value)
                .Select(g => new { CustomerId = g.Key, Total = g.Sum(o => o.FinalAmount) })
                .OrderByDescending(c => c.Total)
                .FirstOrDefault();
            if (topCustomerData != null)
            {
                var customer = _context.Customers.Find(topCustomerData.CustomerId);
                TopCustomerName = customer?.FullName ?? "N/A";
                TopCustomerTotal = topCustomerData.Total;
            }
            else
            {
                TopCustomerName = "N/A";
                TopCustomerTotal = 0;
            }

            RecentOrders = new ObservableCollection<Order>(
                _context.Orders.Include(o => o.User).Include(o => o.Customer)
                    .OrderByDescending(o => o.OrderDate)
                    .Take(5).ToList());

            var yesterdayRevenue = _context.Orders
                .Where(o => o.OrderDate >= today.AddDays(-1) && o.OrderDate < today && o.Status != "Cancelled")
                .Sum(o => (decimal?)o.FinalAmount) ?? 0;
            if (yesterdayRevenue > 0)
            {
                var change = ((TodayRevenue - yesterdayRevenue) / yesterdayRevenue) * 100;
                RevenueTrend = change >= 0 ? $"+{change:F1}%" : $"{change:F1}%";
            }
            else
            {
                RevenueTrend = "Mới";
            }
        }
    }
}

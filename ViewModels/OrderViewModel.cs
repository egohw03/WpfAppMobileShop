using System;
using System.Collections.ObjectModel;
using System.Data.Entity;
using System.Linq;
using System.Windows.Input;
using WpfAppMobileShop.Data;
using WpfAppMobileShop.Helpers;
using WpfAppMobileShop.Models;

namespace WpfAppMobileShop.ViewModels
{
    public class OrderViewModel : ViewModelBase
    {
        private readonly StoreDbContext _context;
        private ObservableCollection<Order> _orders;
        private Order _selectedOrder;
        private ObservableCollection<OrderDetail> _orderDetails;
        private DateTime _fromDate;
        private DateTime _toDate;
        private string _searchText;
        private string _statusFilter;

        public string Title => "Quản lý đơn hàng";

        public ObservableCollection<Order> Orders
        {
            get => _orders;
            set => SetProperty(ref _orders, value);
        }

        public Order SelectedOrder
        {
            get => _selectedOrder;
            set
            {
                SetProperty(ref _selectedOrder, value);
                if (value != null)
                    LoadOrderDetails(value.OrderId);
                else
                    OrderDetails = new ObservableCollection<OrderDetail>();
            }
        }

        public ObservableCollection<OrderDetail> OrderDetails
        {
            get => _orderDetails;
            set => SetProperty(ref _orderDetails, value);
        }

        public DateTime FromDate
        {
            get => _fromDate;
            set
            {
                SetProperty(ref _fromDate, value);
                Search();
            }
        }

        public DateTime ToDate
        {
            get => _toDate;
            set
            {
                SetProperty(ref _toDate, value);
                Search();
            }
        }

        public string SearchText
        {
            get => _searchText;
            set
            {
                SetProperty(ref _searchText, value);
                Search();
            }
        }

        public string StatusFilter
        {
            get => _statusFilter;
            set { SetProperty(ref _statusFilter, value); Search(); }
        }

        public ICommand CancelOrderCommand { get; }

        public OrderViewModel()
        {
            _context = new StoreDbContext();
            _fromDate = DateTime.Today.AddMonths(-1);
            _toDate = DateTime.Today.AddDays(1);
            _statusFilter = "Tất cả";
            CancelOrderCommand = new RelayCommand(CancelOrder, CanCancelOrder);
            try { LoadData(); } catch { Orders = new ObservableCollection<Order>(); OrderDetails = new ObservableCollection<OrderDetail>(); }
        }

        public override void Dispose()
        {
            _context?.Dispose();
            base.Dispose();
        }

        private void LoadData()
        {
            var query = _context.Orders
                .Include(o => o.Customer)
                .Include(o => o.User)
                .Where(o => o.OrderDate >= _fromDate && o.OrderDate <= _toDate)
                .OrderByDescending(o => o.OrderDate)
                .ToList();
            Orders = new ObservableCollection<Order>(query);
        }

        private void Search()
        {
            var query = _context.Orders
                .Include(o => o.Customer)
                .Include(o => o.User)
                .Where(o => o.OrderDate >= _fromDate && o.OrderDate <= _toDate);

            if (_statusFilter != "Tất cả")
                query = query.Where(o => o.Status == _statusFilter);

            if (!string.IsNullOrWhiteSpace(SearchText))
            {
                query = query.Where(o => (o.Customer != null && o.Customer.FullName.Contains(SearchText))
                    || (o.Customer != null && o.Customer.Phone.Contains(SearchText))
                    || o.OrderId.ToString() == SearchText);
            }

            Orders = new ObservableCollection<Order>(query.OrderByDescending(o => o.OrderDate).ToList());
        }

        private void LoadOrderDetails(int orderId)
        {
            OrderDetails = new ObservableCollection<OrderDetail>(
                _context.OrderDetails
                    .Include(od => od.Product)
                    .Where(od => od.OrderId == orderId)
                    .ToList());
        }

        private bool CanCancelOrder()
        {
            return SelectedOrder != null
                && SelectedOrder.Status != OrderStatus.Cancelled;
        }

        private void CancelOrder()
        {
            if (SelectedOrder == null) return;

            var result = System.Windows.MessageBox.Show(
                $"Xác nhận huỷ đơn #{SelectedOrder.OrderId}?\nHàng tồn kho sẽ được hoàn trả.",
                "Xác nhận", System.Windows.MessageBoxButton.YesNo, System.Windows.MessageBoxImage.Warning);
            if (result != System.Windows.MessageBoxResult.Yes) return;

            using (var transaction = _context.Database.BeginTransaction())
            {
                try
                {
                    var order = _context.Orders.Find(SelectedOrder.OrderId);
                    if (order == null) return;

                    order.Status = OrderStatus.Cancelled;

                    var details = _context.OrderDetails.Where(od => od.OrderId == order.OrderId).ToList();
                    foreach (var detail in details)
                    {
                        var product = _context.Products.Find(detail.ProductId);
                        if (product != null)
                            product.StockQuantity += detail.Quantity;
                    }

                    _context.SaveChanges();
                    transaction.Commit();

                    LoadData();
                    OrderDetails = new ObservableCollection<OrderDetail>();
                    SelectedOrder = null;

                    System.Windows.MessageBox.Show($"Đã huỷ đơn #{order.OrderId} và hoàn trả tồn kho.", "Thông báo",
                        System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    System.Windows.MessageBox.Show($"Lỗi: {ex.Message}", "Lỗi",
                        System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                }
            }
        }
    }
}

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
    public class SalesViewModel : ViewModelBase
    {
        private readonly StoreDbContext _context;
        private ObservableCollection<Product> _products;
        private ObservableCollection<CartItem> _cart;
        private CartItem _selectedCartItem;
        private Product _selectedProduct;
        private string _searchText;
        private string _customerPhone;
        private Customer _selectedCustomer;
        private decimal _totalAmount;
        private decimal _discountAmount;
        private decimal _finalAmount;
        private decimal _paymentAmount;
        private decimal _changeAmount;
        private string _promoCodeInput;
        private int? _lastOrderId;
        private decimal _lastPaymentAmount;
        private decimal _lastChangeAmount;

        public string Title => "Bán hàng";

        public ObservableCollection<Product> Products
        {
            get => _products;
            set => SetProperty(ref _products, value);
        }
        public ObservableCollection<CartItem> Cart
        {
            get => _cart;
            set => SetProperty(ref _cart, value);
        }
        public CartItem SelectedCartItem
        {
            get => _selectedCartItem;
            set => SetProperty(ref _selectedCartItem, value);
        }
        public Product SelectedProduct
        {
            get => _selectedProduct;
            set => SetProperty(ref _selectedProduct, value);
        }
        public string SearchText
        {
            get => _searchText;
            set { SetProperty(ref _searchText, value); SearchProducts(); }
        }
        public string CustomerPhone
        {
            get => _customerPhone;
            set { SetProperty(ref _customerPhone, value); FindCustomer(); }
        }
        public Customer SelectedCustomer
        {
            get => _selectedCustomer;
            set => SetProperty(ref _selectedCustomer, value);
        }
        public decimal TotalAmount
        {
            get => _totalAmount;
            set => SetProperty(ref _totalAmount, value);
        }
        public decimal DiscountAmount
        {
            get => _discountAmount;
            set => SetProperty(ref _discountAmount, value);
        }
        public decimal FinalAmount
        {
            get => _finalAmount;
            set => SetProperty(ref _finalAmount, value);
        }
        public decimal PaymentAmount
        {
            get => _paymentAmount;
            set
            {
                SetProperty(ref _paymentAmount, value);
                ChangeAmount = Math.Max(0, value - FinalAmount);
            }
        }
        public decimal ChangeAmount
        {
            get => _changeAmount;
            set => SetProperty(ref _changeAmount, value);
        }
        public string PromoCodeInput
        {
            get => _promoCodeInput;
            set => SetProperty(ref _promoCodeInput, value);
        }
        public int? LastOrderId
        {
            get => _lastOrderId;
            set
            {
                SetProperty(ref _lastOrderId, value);
                System.Windows.Input.CommandManager.InvalidateRequerySuggested();
            }
        }

        public ICommand AddToCartCommand { get; }
        public ICommand RemoveFromCartCommand { get; }
        public ICommand IncreaseQuantityCommand { get; }
        public ICommand DecreaseQuantityCommand { get; }
        public ICommand CheckoutCommand { get; }
        public ICommand NewOrderCommand { get; }
        public ICommand ApplyPromoCommand { get; }
        public ICommand ClearPromoCommand { get; }
        public ICommand PrintReceiptCommand { get; }

        public SalesViewModel()
        {
            _context = new StoreDbContext();
            AddToCartCommand = new RelayCommand(AddToCart);
            RemoveFromCartCommand = new RelayCommand(RemoveFromCart, () => SelectedCartItem != null);
            IncreaseQuantityCommand = new RelayCommand(IncreaseQuantity, () => SelectedCartItem != null);
            DecreaseQuantityCommand = new RelayCommand(DecreaseQuantity, () => SelectedCartItem != null);
            CheckoutCommand = new RelayCommand(Checkout, CanCheckout);
            NewOrderCommand = new RelayCommand(NewOrder);
            ApplyPromoCommand = new RelayCommand(ApplyPromo);
            ClearPromoCommand = new RelayCommand(ClearPromo);
            PrintReceiptCommand = new RelayCommand(PrintReceipt, () => LastOrderId.HasValue);
            try { LoadData(); } catch { LoadEmpty(); }
        }

        public override void Dispose()
        {
            _context?.Dispose();
            base.Dispose();
        }

        private void LoadEmpty()
        {
            Products = new ObservableCollection<Product>();
            Cart = new ObservableCollection<CartItem>();
            TotalAmount = 0; DiscountAmount = 0; FinalAmount = 0;
            PaymentAmount = 0; ChangeAmount = 0;
        }

        private void LoadData()
        {
            Products = new ObservableCollection<Product>(_context.Products.Include(p => p.Category).ToList());
            Cart = new ObservableCollection<CartItem>();
            TotalAmount = 0; DiscountAmount = 0; FinalAmount = 0;
            PaymentAmount = 0; ChangeAmount = 0; LastOrderId = null;
        }

        private void SearchProducts()
        {
            var query = _context.Products.Include(p => p.Category).AsQueryable();
            if (!string.IsNullOrWhiteSpace(SearchText))
                query = query.Where(p => p.ProductName.Contains(SearchText) || (p.Brand ?? "").Contains(SearchText) || (p.Model ?? "").Contains(SearchText));
            Products = new ObservableCollection<Product>(query.ToList());
        }

        private void FindCustomer()
        {
            if (!string.IsNullOrWhiteSpace(CustomerPhone))
                SelectedCustomer = _context.Customers.FirstOrDefault(c => c.Phone.Contains(CustomerPhone));
            else
                SelectedCustomer = null;
        }

        private void AddToCart(object productObj)
        {
            var product = productObj as Product;
            if (product == null || product.StockQuantity <= 0) return;
            var existing = Cart.FirstOrDefault(c => c.ProductId == product.ProductId);
            if (existing != null) existing.Quantity++;
            else Cart.Add(new CartItem { ProductId = product.ProductId, ProductName = product.ProductName, UnitPrice = product.Price, Quantity = 1, MaxQuantity = product.StockQuantity });
            RecalculateTotal();
        }

        private void RemoveFromCart()
        {
            if (SelectedCartItem != null) { Cart.Remove(SelectedCartItem); RecalculateTotal(); }
        }

        private void IncreaseQuantity()
        {
            if (SelectedCartItem != null && SelectedCartItem.Quantity < SelectedCartItem.MaxQuantity) { SelectedCartItem.Quantity++; RecalculateTotal(); }
        }

        private void DecreaseQuantity()
        {
            if (SelectedCartItem != null && SelectedCartItem.Quantity > 1) { SelectedCartItem.Quantity--; RecalculateTotal(); }
        }

        private void RecalculateTotal()
        {
            TotalAmount = Cart.Sum(c => c.Quantity * c.UnitPrice);
            FinalAmount = TotalAmount - DiscountAmount;
            if (FinalAmount < 0) FinalAmount = 0;
            ChangeAmount = Math.Max(0, PaymentAmount - FinalAmount);
        }

        private bool CanCheckout() => Cart.Any();

        private void ApplyPromo()
        {
            if (string.IsNullOrWhiteSpace(PromoCodeInput))
            {
                System.Windows.MessageBox.Show("Vui lòng nhập mã giảm giá!", "Thông báo",
                    System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
                return;
            }
            var promo = _context.PromoCodes.FirstOrDefault(p => p.Code == PromoCodeInput && p.IsActive);
                    if (promo == null)
            {
                NotificationService.ShowError("Mã giảm giá không hợp lệ!");
                return;
            }
            if (promo.ExpiryDate.HasValue && promo.ExpiryDate.Value < DateTime.Today)
            {
                NotificationService.ShowError("Mã giảm giá đã hết hạn!");
                return;
            }
            if (TotalAmount < promo.MinOrderAmount)
            {
                NotificationService.ShowError($"Đơn tối thiểu {promo.MinOrderAmount:N0}đ để áp dụng mã này!");
                return;
            }
            if (promo.DiscountType == "Percent")
                DiscountAmount = TotalAmount * promo.DiscountValue / 100;
            else
                DiscountAmount = promo.DiscountValue;

            if (DiscountAmount > TotalAmount) DiscountAmount = TotalAmount;
            RecalculateTotal();
            System.Windows.MessageBox.Show($"Giảm {DiscountAmount:N0}đ", "Thông báo",
                System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
        }

        private void ClearPromo()
        {
            PromoCodeInput = "";
            DiscountAmount = 0;
            RecalculateTotal();
        }

        private void Checkout()
        {
            using (var transaction = _context.Database.BeginTransaction())
            {
                try
                {
                    var order = new Order
                    {
                        OrderDate = DateTime.Now,
                        TotalAmount = TotalAmount,
                        DiscountAmount = DiscountAmount,
                        FinalAmount = FinalAmount,
                        Status = OrderStatus.Completed,
                        CustomerId = SelectedCustomer?.CustomerId,
                        UserId = UserSession.CurrentUser?.UserId ?? 0,
                        OrderDetails = new ObservableCollection<OrderDetail>()
                    };

                    foreach (var item in Cart)
                    {
                        var product = _context.Products.Find(item.ProductId);
                        if (product == null || product.StockQuantity < item.Quantity)
                            throw new InvalidOperationException($"Sản phẩm '{item.ProductName}' không đủ hàng trong kho!");

                        product.StockQuantity -= item.Quantity;

                        var od = new OrderDetail
                        {
                            ProductId = item.ProductId,
                            Quantity = item.Quantity,
                            UnitPrice = item.UnitPrice
                        };
                        order.OrderDetails.Add(od);
                    }

                    _context.Orders.Add(order);
                    _context.SaveChanges();

                    var warrantyMonthsStr = _context.Settings.Find("WarrantyMonths")?.Value ?? "12";
                    if (!int.TryParse(warrantyMonthsStr, out var warrantyMonths))
                        warrantyMonths = 12;
                    foreach (var od in order.OrderDetails)
                    {
                        if (SelectedCustomer != null)
                        {
                            _context.Warranties.Add(new Warranty
                            {
                                OrderDetailId = od.OrderDetailId,
                                ProductId = od.ProductId,
                                CustomerId = SelectedCustomer.CustomerId,
                                StartDate = DateTime.Today,
                                EndDate = DateTime.Today.AddMonths(warrantyMonths),
                                Status = "Active"
                            });
                        }
                    }

                    _context.SaveChanges();
                    transaction.Commit();

                    var savedOrderId = order.OrderId;
                    _lastPaymentAmount = PaymentAmount;
                    _lastChangeAmount = ChangeAmount;

                    NewOrder();
                    LastOrderId = savedOrderId;
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    System.Windows.MessageBox.Show($"Lỗi thanh toán: {ex.Message}", "Lỗi",
                        System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                }
            }
        }

        private void NewOrder()
        {
            Cart = new ObservableCollection<CartItem>();
            SelectedCustomer = null;
            CustomerPhone = null;
            PromoCodeInput = "";
            DiscountAmount = 0;
            TotalAmount = 0; FinalAmount = 0;
            PaymentAmount = 0; ChangeAmount = 0;
            LoadData();
        }

        private void PrintReceipt()
        {
            if (!LastOrderId.HasValue) return;
            var order = _context.Orders.Include(o => o.Customer).Include(o => o.User).FirstOrDefault(o => o.OrderId == LastOrderId.Value);
            if (order == null) return;
            var details = _context.OrderDetails.Include(od => od.Product).Where(od => od.OrderId == LastOrderId.Value).OrderBy(od => od.OrderDetailId);
            ReceiptPrinter.PrintReceipt(order, details, _lastPaymentAmount, _lastChangeAmount);
        }
    }

    public class CartItem : ViewModelBase
    {
        private int _quantity;

        public int ProductId { get; set; }
        public string ProductName { get; set; }
        public decimal UnitPrice { get; set; }
        public int MaxQuantity { get; set; }

        public int Quantity
        {
            get => _quantity;
            set
            {
                if (SetProperty(ref _quantity, value))
                    OnPropertyChanged(nameof(LineTotal));
            }
        }

        public decimal LineTotal => Quantity * UnitPrice;
    }
}

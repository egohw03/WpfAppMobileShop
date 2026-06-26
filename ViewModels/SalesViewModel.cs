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
        private decimal _paymentAmount;
        private decimal _changeAmount;

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
            set
            {
                SetProperty(ref _searchText, value);
                SearchProducts();
            }
        }

        public string CustomerPhone
        {
            get => _customerPhone;
            set
            {
                SetProperty(ref _customerPhone, value);
                FindCustomer();
            }
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

        public decimal PaymentAmount
        {
            get => _paymentAmount;
            set
            {
                SetProperty(ref _paymentAmount, value);
                ChangeAmount = value - TotalAmount;
            }
        }

        public decimal ChangeAmount
        {
            get => _changeAmount;
            set => SetProperty(ref _changeAmount, value);
        }

        public ICommand AddToCartCommand { get; }
        public ICommand RemoveFromCartCommand { get; }
        public ICommand IncreaseQuantityCommand { get; }
        public ICommand DecreaseQuantityCommand { get; }
        public ICommand CheckoutCommand { get; }
        public ICommand NewOrderCommand { get; }

        public SalesViewModel()
        {
            _context = new StoreDbContext();
            AddToCartCommand = new RelayCommand(AddToCart);
            RemoveFromCartCommand = new RelayCommand(RemoveFromCart, () => SelectedCartItem != null);
            IncreaseQuantityCommand = new RelayCommand(IncreaseQuantity, () => SelectedCartItem != null);
            DecreaseQuantityCommand = new RelayCommand(DecreaseQuantity, () => SelectedCartItem != null);
            CheckoutCommand = new RelayCommand(Checkout, CanCheckout);
            NewOrderCommand = new RelayCommand(NewOrder);
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
            TotalAmount = 0;
            PaymentAmount = 0;
            ChangeAmount = 0;
        }

        private void LoadData()
        {
            Products = new ObservableCollection<Product>(_context.Products.Include(p => p.Category).ToList());
            Cart = new ObservableCollection<CartItem>();
            TotalAmount = 0;
            PaymentAmount = 0;
            ChangeAmount = 0;
        }

        private void SearchProducts()
        {
            var query = _context.Products.Include(p => p.Category).AsQueryable();
            if (!string.IsNullOrWhiteSpace(SearchText))
            {
                query = query.Where(p => p.ProductName.Contains(SearchText)
                    || p.Brand.Contains(SearchText)
                    || p.Model.Contains(SearchText));
            }
            Products = new ObservableCollection<Product>(query.ToList());
        }

        private void FindCustomer()
        {
            if (!string.IsNullOrWhiteSpace(CustomerPhone))
            {
                SelectedCustomer = _context.Customers.FirstOrDefault(c => c.Phone.Contains(CustomerPhone));
            }
            else
            {
                SelectedCustomer = null;
            }
        }

        private void AddToCart(object productObj)
        {
            var product = productObj as Product;
            if (product == null || product.StockQuantity <= 0) return;

            var existing = Cart.FirstOrDefault(c => c.ProductId == product.ProductId);
            if (existing != null)
            {
                existing.Quantity++;
            }
            else
            {
                Cart.Add(new CartItem
                {
                    ProductId = product.ProductId,
                    ProductName = product.ProductName,
                    UnitPrice = product.Price,
                    Quantity = 1,
                    MaxQuantity = product.StockQuantity
                });
            }
            RecalculateTotal();
        }

        private void RemoveFromCart()
        {
            if (SelectedCartItem != null)
            {
                Cart.Remove(SelectedCartItem);
                RecalculateTotal();
            }
        }

        private void IncreaseQuantity()
        {
            if (SelectedCartItem != null && SelectedCartItem.Quantity < SelectedCartItem.MaxQuantity)
            {
                SelectedCartItem.Quantity++;
                RecalculateTotal();
            }
        }

        private void DecreaseQuantity()
        {
            if (SelectedCartItem != null && SelectedCartItem.Quantity > 1)
            {
                SelectedCartItem.Quantity--;
                RecalculateTotal();
            }
        }

        private void RecalculateTotal()
        {
            TotalAmount = Cart.Sum(c => c.Quantity * c.UnitPrice);
            ChangeAmount = PaymentAmount - TotalAmount;
        }

        private bool CanCheckout()
        {
            return Cart.Any();
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
                        CustomerId = SelectedCustomer?.CustomerId,
                        UserId = 1,
                        OrderDetails = new ObservableCollection<OrderDetail>()
                    };

                    foreach (var item in Cart)
                    {
                        var product = _context.Products.Find(item.ProductId);
                        if (product == null || product.StockQuantity < item.Quantity)
                        {
                            throw new InvalidOperationException($"Sản phẩm '{item.ProductName}' không đủ hàng trong kho!");
                        }

                        product.StockQuantity -= item.Quantity;

                        order.OrderDetails.Add(new OrderDetail
                        {
                            ProductId = item.ProductId,
                            Quantity = item.Quantity,
                            UnitPrice = item.UnitPrice
                        });
                    }

                    _context.Orders.Add(order);
                    _context.SaveChanges();
                    transaction.Commit();

                    NewOrder();
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
            TotalAmount = 0;
            PaymentAmount = 0;
            ChangeAmount = 0;
            LoadData();
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
                {
                    OnPropertyChanged(nameof(LineTotal));
                }
            }
        }

        public decimal LineTotal => Quantity * UnitPrice;
    }
}

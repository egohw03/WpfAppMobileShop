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
    public class InventoryViewModel : ViewModelBase
    {
        private readonly StoreDbContext _context;
        private ObservableCollection<Product> _products;
        private ObservableCollection<StockTransaction> _transactions;
        private Product _selectedProduct;
        private string _searchText;
        private string _filterType;
        private int _adjustQuantity;
        private string _adjustNotes;
        private bool _isAdjusting;
        private string _adjustType;
        private int _lowStockThreshold = 10;
        private int _lowStockCount;
        private string _filterCategory;
        private Supplier _selectedSupplier;

        public string Title => "Quản lý tồn kho";
        public int LowStockThreshold
        {
            get => _lowStockThreshold;
            set => SetProperty(ref _lowStockThreshold, value);
        }
        public int LowStockCount
        {
            get => _lowStockCount;
            set => SetProperty(ref _lowStockCount, value);
        }
        public string FilterCategory
        {
            get => _filterCategory;
            set { SetProperty(ref _filterCategory, value); Search(); }
        }
        public ObservableCollection<Category> Categories { get; set; }

        public ObservableCollection<Product> Products
        {
            get => _products;
            set => SetProperty(ref _products, value);
        }

        public ObservableCollection<StockTransaction> Transactions
        {
            get => _transactions;
            set => SetProperty(ref _transactions, value);
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
                Search();
            }
        }

        public string FilterType
        {
            get => _filterType;
            set
            {
                SetProperty(ref _filterType, value);
                LoadTransactions();
            }
        }

        public int AdjustQuantity
        {
            get => _adjustQuantity;
            set => SetProperty(ref _adjustQuantity, value);
        }

        public string AdjustNotes
        {
            get => _adjustNotes;
            set => SetProperty(ref _adjustNotes, value);
        }

        public bool IsAdjusting
        {
            get => _isAdjusting;
            set => SetProperty(ref _isAdjusting, value);
        }

        public string AdjustType
        {
            get => _adjustType;
            set => SetProperty(ref _adjustType, value);
        }
        public Supplier SelectedSupplier
        {
            get => _selectedSupplier;
            set => SetProperty(ref _selectedSupplier, value);
        }
        public ObservableCollection<Supplier> Suppliers { get; set; }

        public ICommand ImportCommand { get; }
        public ICommand ExportCommand { get; }
        public ICommand SaveAdjustCommand { get; }
        public ICommand CancelAdjustCommand { get; }

        public InventoryViewModel()
        {
            _context = new StoreDbContext();
            _filterType = "Tất cả";
            _filterCategory = "Tất cả";
            ImportCommand = new RelayCommand(() => StartAdjust("Import"));
            ExportCommand = new RelayCommand(() => StartAdjust("Export"));
            SaveAdjustCommand = new RelayCommand(SaveAdjust, CanSaveAdjust);
            CancelAdjustCommand = new RelayCommand(() => IsAdjusting = false);
            try
            {
                LoadSettings();
                LoadCategories();
                LoadSuppliers();
                LoadData();
                LoadTransactions();
            }
            catch
            {
                Products = new ObservableCollection<Product>();
                Transactions = new ObservableCollection<StockTransaction>();
            }
        }

        public override void Dispose()
        {
            _context?.Dispose();
            base.Dispose();
        }

        private void LoadSettings()
        {
            var thresholdStr = _context.Settings.Find("LowStockThreshold")?.Value ?? "10";
            int.TryParse(thresholdStr, out _lowStockThreshold);
        }

        private void LoadCategories()
        {
            var cats = _context.Categories.ToList();
            Categories = new ObservableCollection<Category>(cats);
        }

        private void LoadSuppliers()
        {
            var sups = _context.Suppliers.ToList();
            Suppliers = new ObservableCollection<Supplier>(sups);
        }

        private void LoadData()
        {
            var query = _context.Products.Include(p => p.Category).OrderBy(p => p.StockQuantity);
            Products = new ObservableCollection<Product>(query.ToList());
            UpdateLowStockCount();
        }

        private void UpdateLowStockCount()
        {
            LowStockCount = Products.Count(p => p.StockQuantity <= _lowStockThreshold);
        }

        private void Search()
        {
            var query = _context.Products.Include(p => p.Category).AsQueryable();
            if (!string.IsNullOrWhiteSpace(SearchText))
                query = query.Where(p => p.ProductName.Contains(SearchText) || (p.Brand ?? "").Contains(SearchText));
            if (_filterCategory != "Tất cả")
                query = query.Where(p => p.Category.CategoryName == _filterCategory);
            Products = new ObservableCollection<Product>(query.OrderBy(p => p.StockQuantity).ToList());
            UpdateLowStockCount();
        }

        private void LoadTransactions()
        {
            var query = _context.StockTransactions
                .Include(t => t.Product)
                .Include(t => t.User)
                .AsQueryable();

            if (_filterType != "Tất cả")
                query = query.Where(t => t.Type == _filterType);

            Transactions = new ObservableCollection<StockTransaction>(query.OrderByDescending(t => t.Date).Take(100).ToList());
        }

        private void StartAdjust(string type)
        {
            if (SelectedProduct == null)
            {
                System.Windows.MessageBox.Show("Vui lòng chọn sản phẩm!", "Thông báo",
                    System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
                return;
            }
            AdjustType = type;
            AdjustQuantity = 0;
            AdjustNotes = "";
            SelectedSupplier = null;
            IsAdjusting = true;
        }

        private bool CanSaveAdjust()
        {
            return IsAdjusting && AdjustQuantity > 0;
        }

        private void SaveAdjust()
        {
            if (SelectedProduct == null) return;

            using (var transaction = _context.Database.BeginTransaction())
            {
                try
                {
                    var product = _context.Products.Find(SelectedProduct.ProductId);
                    if (product == null) return;

                    if (AdjustType == "Export" && product.StockQuantity < AdjustQuantity)
                    {
                        System.Windows.MessageBox.Show("Số lượng xuất vượt quá tồn kho!", "Lỗi",
                            System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                        return;
                    }

                    product.StockQuantity += AdjustType == "Import" ? AdjustQuantity : -AdjustQuantity;

                    _context.StockTransactions.Add(new StockTransaction
                    {
                        ProductId = product.ProductId,
                        Quantity = AdjustQuantity,
                        Type = AdjustType,
                        Date = DateTime.Now,
                        Notes = AdjustNotes,
                        UserId = UserSession.CurrentUser?.UserId ?? 0,
                        SupplierId = SelectedSupplier?.SupplierId
                    });

                    _context.SaveChanges();
                    transaction.Commit();

                    IsAdjusting = false;
                    LoadData();
                    LoadTransactions();
                    UpdateLowStockCount();

                    System.Windows.MessageBox.Show(
                        AdjustType == "Import" ? "Nhập hàng thành công!" : "Xuất hàng thành công!",
                        "Thông báo", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
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

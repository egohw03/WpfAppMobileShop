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
    public class ProductViewModel : ViewModelBase
    {
        private readonly StoreDbContext _context;
        private ObservableCollection<Product> _products;
        private ObservableCollection<Category> _categories;
        private Product _selectedProduct;
        private Product _editingProduct;
        private string _searchText;
        private bool _isEditing;

        public string Title => "Quản lý sản phẩm";

        public ObservableCollection<Product> Products
        {
            get => _products;
            set => SetProperty(ref _products, value);
        }

        public ObservableCollection<Category> Categories
        {
            get => _categories;
            set => SetProperty(ref _categories, value);
        }

        public Product SelectedProduct
        {
            get => _selectedProduct;
            set
            {
                SetProperty(ref _selectedProduct, value);
                if (value != null && !_isEditing)
                {
                    EditingProduct = new Product
                    {
                        ProductId = value.ProductId,
                        ProductName = value.ProductName,
                        CategoryId = value.CategoryId,
                        Brand = value.Brand,
                        Model = value.Model,
                        Price = value.Price,
                        StockQuantity = value.StockQuantity,
                        Description = value.Description,
                        ImageUrl = value.ImageUrl
                    };
                }
            }
        }

        public Product EditingProduct
        {
            get => _editingProduct;
            set => SetProperty(ref _editingProduct, value);
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

        public bool IsEditing
        {
            get => _isEditing;
            set => SetProperty(ref _isEditing, value);
        }

        public ICommand AddCommand { get; }
        public ICommand SaveCommand { get; }
        public ICommand DeleteCommand { get; }
        public ICommand CancelCommand { get; }

        public ProductViewModel()
        {
            _context = new StoreDbContext();
            AddCommand = new RelayCommand(Add);
            SaveCommand = new RelayCommand(Save, () => IsEditing);
            DeleteCommand = new RelayCommand(Delete, () => SelectedProduct != null);
            CancelCommand = new RelayCommand(Cancel);
            try { LoadData(); } catch { Products = new ObservableCollection<Product>(); Categories = new ObservableCollection<Category>(); }
        }

        public override void Dispose()
        {
            _context?.Dispose();
            base.Dispose();
        }

        private void LoadData()
        {
            Products = new ObservableCollection<Product>(_context.Products.Include(p => p.Category).ToList());
            Categories = new ObservableCollection<Category>(_context.Categories.ToList());
        }

        private void Search()
        {
            var query = _context.Products.Include(p => p.Category).AsQueryable();
            if (!string.IsNullOrWhiteSpace(SearchText))
            {
                query = query.Where(p => p.ProductName.Contains(SearchText)
                    || (p.Brand ?? "").Contains(SearchText)
                    || (p.Model ?? "").Contains(SearchText));
            }
            Products = new ObservableCollection<Product>(query.ToList());
        }

        private void Add()
        {
            EditingProduct = new Product();
            IsEditing = true;
        }

        private void Save()
        {
            if (string.IsNullOrWhiteSpace(EditingProduct.ProductName))
            { System.Windows.MessageBox.Show("Vui lòng nhập tên sản phẩm!", "Lỗi", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning); return; }
            if (EditingProduct.ProductName.Length > 200)
            { System.Windows.MessageBox.Show("Tên sản phẩm không quá 200 ký tự!", "Lỗi", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning); return; }
            if (EditingProduct.Price < 0)
            { System.Windows.MessageBox.Show("Giá sản phẩm không hợp lệ!", "Lỗi", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning); return; }
            if (EditingProduct.CategoryId <= 0)
            { System.Windows.MessageBox.Show("Vui lòng chọn danh mục!", "Lỗi", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning); return; }
            try
            {
                if (EditingProduct.ProductId == 0) _context.Products.Add(EditingProduct);
                else
                {
                    var existing = _context.Products.Find(EditingProduct.ProductId);
                    if (existing != null)
                    {
                        existing.ProductName = EditingProduct.ProductName;
                        existing.CategoryId = EditingProduct.CategoryId;
                        existing.Brand = EditingProduct.Brand;
                        existing.Model = EditingProduct.Model;
                        existing.Price = EditingProduct.Price;
                        existing.StockQuantity = EditingProduct.StockQuantity;
                        existing.Description = EditingProduct.Description;
                        existing.ImageUrl = EditingProduct.ImageUrl;
                    }
                }
                _context.SaveChanges();
                LoadData();
                IsEditing = false;
                EditingProduct = null;
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Lỗi lưu: {ex.Message}", "Lỗi", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }

        private void Delete()
        {
            if (SelectedProduct == null) return;
            if (_context.OrderDetails.Any(od => od.ProductId == SelectedProduct.ProductId))
            { System.Windows.MessageBox.Show("Không thể xóa sản phẩm đã có trong đơn hàng!", "Lỗi", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning); return; }
            try
            {
                var entity = _context.Products.Find(SelectedProduct.ProductId);
                if (entity != null) { _context.Products.Remove(entity); _context.SaveChanges(); }
                LoadData();
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Lỗi xóa: {ex.Message}", "Lỗi", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }

        private void Cancel()
        {
            IsEditing = false;
            EditingProduct = null;
        }
    }
}

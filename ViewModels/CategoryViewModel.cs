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
    public class CategoryViewModel : ViewModelBase
    {
        private readonly StoreDbContext _context;
        private ObservableCollection<Category> _categories;
        private Category _selectedCategory;
        private Category _editingCategory;
        private string _searchText;
        private bool _isEditing;

        public string Title => "Quản lý danh mục";

        public ObservableCollection<Category> Categories
        {
            get => _categories;
            set => SetProperty(ref _categories, value);
        }

        public Category SelectedCategory
        {
            get => _selectedCategory;
            set
            {
                SetProperty(ref _selectedCategory, value);
                if (value != null && !_isEditing)
                {
                    EditingCategory = new Category
                    {
                        CategoryId = value.CategoryId,
                        CategoryName = value.CategoryName,
                        Description = value.Description
                    };
                }
            }
        }

        public Category EditingCategory
        {
            get => _editingCategory;
            set => SetProperty(ref _editingCategory, value);
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

        public CategoryViewModel()
        {
            _context = new StoreDbContext();
            AddCommand = new RelayCommand(Add);
            SaveCommand = new RelayCommand(Save, () => IsEditing);
            DeleteCommand = new RelayCommand(Delete, () => SelectedCategory != null);
            CancelCommand = new RelayCommand(Cancel);
            try { LoadData(); } catch { Categories = new ObservableCollection<Category>(); }
        }

        public override void Dispose()
        {
            _context?.Dispose();
            base.Dispose();
        }

        private void LoadData()
        {
            Categories = new ObservableCollection<Category>(_context.Categories.ToList());
        }

        private void Search()
        {
            var query = _context.Categories.AsQueryable();
            if (!string.IsNullOrWhiteSpace(SearchText))
            {
                query = query.Where(c => c.CategoryName.Contains(SearchText) || (c.Description ?? "").Contains(SearchText));
            }
            Categories = new ObservableCollection<Category>(query.ToList());
        }

        private void Add()
        {
            EditingCategory = new Category();
            IsEditing = true;
        }

        private void Save()
        {
            if (string.IsNullOrWhiteSpace(EditingCategory.CategoryName))
            { System.Windows.MessageBox.Show("Vui lòng nhập tên danh mục!", "Lỗi", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning); return; }
            if (EditingCategory.CategoryName.Length > 100)
            { System.Windows.MessageBox.Show("Tên danh mục không quá 100 ký tự!", "Lỗi", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning); return; }
            try
            {
                if (EditingCategory.CategoryId == 0) _context.Categories.Add(EditingCategory);
                else
                {
                    var existing = _context.Categories.Find(EditingCategory.CategoryId);
                    if (existing != null)
                    {
                        existing.CategoryName = EditingCategory.CategoryName;
                        existing.Description = EditingCategory.Description;
                    }
                }
                _context.SaveChanges();
                LoadData();
                IsEditing = false;
                EditingCategory = null;
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Lỗi lưu: {ex.Message}", "Lỗi", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }

        private void Delete()
        {
            if (SelectedCategory == null) return;
            if (_context.Products.Any(p => p.CategoryId == SelectedCategory.CategoryId))
            { System.Windows.MessageBox.Show("Không thể xóa danh mục đang có sản phẩm!", "Lỗi", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning); return; }
            try
            {
                var entity = _context.Categories.Find(SelectedCategory.CategoryId);
                if (entity != null) { _context.Categories.Remove(entity); _context.SaveChanges(); }
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
            EditingCategory = null;
        }
    }
}

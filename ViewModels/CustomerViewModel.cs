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
    public class CustomerViewModel : ViewModelBase
    {
        private readonly StoreDbContext _context;
        private ObservableCollection<Customer> _customers;
        private Customer _selectedCustomer;
        private Customer _editingCustomer;
        private string _searchText;
        private bool _isEditing;

        public string Title => "Quản lý khách hàng";

        public ObservableCollection<Customer> Customers
        {
            get => _customers;
            set => SetProperty(ref _customers, value);
        }

        public Customer SelectedCustomer
        {
            get => _selectedCustomer;
            set
            {
                SetProperty(ref _selectedCustomer, value);
                if (value != null && !_isEditing)
                {
                    EditingCustomer = new Customer
                    {
                        CustomerId = value.CustomerId,
                        FullName = value.FullName,
                        Phone = value.Phone,
                        Email = value.Email,
                        Address = value.Address,
                        CreatedDate = value.CreatedDate
                    };
                }
            }
        }

        public Customer EditingCustomer
        {
            get => _editingCustomer;
            set => SetProperty(ref _editingCustomer, value);
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
        public ICommand EditCommand { get; }

        public CustomerViewModel()
        {
            _context = new StoreDbContext();
            AddCommand = new RelayCommand(Add);
            SaveCommand = new RelayCommand(Save, () => IsEditing);
            DeleteCommand = new RelayCommand(Delete, () => SelectedCustomer != null && !IsEditing);
            CancelCommand = new RelayCommand(Cancel);
            EditCommand = new RelayCommand(Edit, () => SelectedCustomer != null && !IsEditing);
            try { LoadData(); } catch { Customers = new ObservableCollection<Customer>(); }
        }

        public override void Dispose()
        {
            _context?.Dispose();
            base.Dispose();
        }

        private void LoadData()
        {
            Customers = new ObservableCollection<Customer>(_context.Customers.ToList());
        }

        private void Search()
        {
            try
            {
                var query = _context.Customers.AsQueryable();
                if (!string.IsNullOrWhiteSpace(SearchText))
                {
                    query = query.Where(c => c.FullName.Contains(SearchText)
                        || c.Phone.Contains(SearchText)
                        || (c.Email ?? "").Contains(SearchText));
                }
                Customers = new ObservableCollection<Customer>(query.ToList());
            }
            catch
            {
                Customers = new ObservableCollection<Customer>();
            }
        }

        private void Add()
        {
            EditingCustomer = new Customer { CreatedDate = DateTime.Now };
            IsEditing = true;
        }

        private void Edit()
        {
            if (SelectedCustomer == null) return;
            EditingCustomer = new Customer
            {
                CustomerId = SelectedCustomer.CustomerId,
                FullName = SelectedCustomer.FullName,
                Phone = SelectedCustomer.Phone,
                Email = SelectedCustomer.Email,
                Address = SelectedCustomer.Address,
                CreatedDate = SelectedCustomer.CreatedDate
            };
            IsEditing = true;
        }

        private void Save()
        {
            if (string.IsNullOrWhiteSpace(EditingCustomer.FullName))
            { System.Windows.MessageBox.Show("Vui lòng nhập tên khách hàng!", "Lỗi", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning); return; }
            if (EditingCustomer.FullName.Length > 200)
            { System.Windows.MessageBox.Show("Tên khách hàng không quá 200 ký tự!", "Lỗi", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning); return; }
            if (string.IsNullOrWhiteSpace(EditingCustomer.Phone))
            { System.Windows.MessageBox.Show("Vui lòng nhập số điện thoại!", "Lỗi", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning); return; }
            if (EditingCustomer.Phone.Length > 20)
            { System.Windows.MessageBox.Show("Số điện thoại không quá 20 ký tự!", "Lỗi", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning); return; }
            if (_context.Customers.Any(c => c.Phone == EditingCustomer.Phone && c.CustomerId != EditingCustomer.CustomerId))
            { System.Windows.MessageBox.Show("Số điện thoại đã tồn tại!", "Lỗi", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning); return; }
            try
            {
                if (EditingCustomer.CustomerId == 0) _context.Customers.Add(EditingCustomer);
                else
                {
                    var existing = _context.Customers.Find(EditingCustomer.CustomerId);
                    if (existing != null)
                    {
                        existing.FullName = EditingCustomer.FullName;
                        existing.Phone = EditingCustomer.Phone;
                        existing.Email = EditingCustomer.Email;
                        existing.Address = EditingCustomer.Address;
                        existing.CreatedDate = EditingCustomer.CreatedDate;
                    }
                }
                _context.SaveChanges();
                LoadData();
                IsEditing = false;
                EditingCustomer = null;
            }
            catch (Exception ex)
            {
                if (EditingCustomer != null && EditingCustomer.CustomerId == 0)
                    _context.Entry(EditingCustomer).State = System.Data.Entity.EntityState.Detached;
                System.Windows.MessageBox.Show($"Lỗi lưu: {ex.Message}", "Lỗi", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }

        private void Delete()
        {
            if (SelectedCustomer == null) return;
            if (_context.Orders.Any(o => o.CustomerId == SelectedCustomer.CustomerId))
            {
                System.Windows.MessageBox.Show("Không thể xóa khách hàng đã có đơn hàng!", "Lỗi",
                    System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
                return;
            }
            var result = System.Windows.MessageBox.Show($"Xóa khách hàng '{SelectedCustomer.FullName}'?", "Xác nhận",
                System.Windows.MessageBoxButton.YesNo, System.Windows.MessageBoxImage.Question);
            if (result != System.Windows.MessageBoxResult.Yes) return;
            try
            {
                var entity = _context.Customers.Find(SelectedCustomer.CustomerId);
                if (entity != null) { _context.Customers.Remove(entity); _context.SaveChanges(); }
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
            EditingCustomer = null;
        }
    }
}

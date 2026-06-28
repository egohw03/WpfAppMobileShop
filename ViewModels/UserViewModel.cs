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
    public class UserViewModel : ViewModelBase
    {
        private readonly StoreDbContext _context;
        private ObservableCollection<User> _users;
        private User _selectedUser;
        private User _editingUser;
        private string _searchText;
        private bool _isEditing;

        public string Title => "Quản lý người dùng";

        public ObservableCollection<User> Users
        {
            get => _users;
            set => SetProperty(ref _users, value);
        }

        public User SelectedUser
        {
            get => _selectedUser;
            set
            {
                SetProperty(ref _selectedUser, value);
                if (value != null && !_isEditing)
                {
                    EditingUser = new User
                    {
                        UserId = value.UserId,
                        Username = value.Username,
                        Password = value.Password,
                        FullName = value.FullName,
                        Role = value.Role,
                        Phone = value.Phone,
                        Email = value.Email,
                        IsActive = value.IsActive
                    };
                }
            }
        }

        public User EditingUser
        {
            get => _editingUser;
            set => SetProperty(ref _editingUser, value);
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

        public UserViewModel()
        {
            _context = new StoreDbContext();
            AddCommand = new RelayCommand(Add);
            SaveCommand = new RelayCommand(Save, () => IsEditing);
            DeleteCommand = new RelayCommand(Delete, () => SelectedUser != null && !IsEditing);
            CancelCommand = new RelayCommand(Cancel);
            EditCommand = new RelayCommand(Edit, () => SelectedUser != null && !IsEditing);
            try { LoadData(); } catch { Users = new ObservableCollection<User>(); }
        }

        public override void Dispose()
        {
            _context?.Dispose();
            base.Dispose();
        }

        private void LoadData()
        {
            Users = new ObservableCollection<User>(_context.Users.ToList());
        }

        private void Search()
        {
            try
            {
                var query = _context.Users.AsQueryable();
                if (!string.IsNullOrWhiteSpace(SearchText))
                {
                    query = query.Where(u => u.FullName.Contains(SearchText)
                        || u.Username.Contains(SearchText)
                        || (u.Phone ?? "").Contains(SearchText)
                        || (u.Email ?? "").Contains(SearchText));
                }
                Users = new ObservableCollection<User>(query.ToList());
            }
            catch
            {
                Users = new ObservableCollection<User>();
            }
        }

        private void Add()
        {
            EditingUser = new User { IsActive = true, Role = "Staff" };
            IsEditing = true;
        }

        private void Edit()
        {
            if (SelectedUser == null) return;
            EditingUser = new User
            {
                UserId = SelectedUser.UserId,
                Username = SelectedUser.Username,
                Password = SelectedUser.Password,
                FullName = SelectedUser.FullName,
                Role = SelectedUser.Role,
                Phone = SelectedUser.Phone,
                Email = SelectedUser.Email,
                IsActive = SelectedUser.IsActive
            };
            IsEditing = true;
        }

        private void Save()
        {
            if (string.IsNullOrWhiteSpace(EditingUser.Username))
            { System.Windows.MessageBox.Show("Tên đăng nhập không được để trống!", "Lỗi", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning); return; }
            if (EditingUser.Username.Length > 50)
            { System.Windows.MessageBox.Show("Tên đăng nhập không quá 50 ký tự!", "Lỗi", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning); return; }
            if (string.IsNullOrWhiteSpace(EditingUser.FullName))
            { System.Windows.MessageBox.Show("Họ tên không được để trống!", "Lỗi", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning); return; }
            if (EditingUser.FullName.Length > 200)
            { System.Windows.MessageBox.Show("Họ tên không quá 200 ký tự!", "Lỗi", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning); return; }

            try
            {
                if (EditingUser.UserId == 0)
                {
                    if (string.IsNullOrWhiteSpace(EditingUser.Password))
                    { System.Windows.MessageBox.Show("Vui lòng nhập mật khẩu!", "Lỗi", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning); return; }
                    _context.Users.Add(EditingUser);
                }
                else
                {
                    var existing = _context.Users.Find(EditingUser.UserId);
                    if (existing != null)
                    {
                        existing.Username = EditingUser.Username;
                        if (!string.IsNullOrWhiteSpace(EditingUser.Password))
                            existing.Password = EditingUser.Password;
                        existing.FullName = EditingUser.FullName;
                        existing.Role = EditingUser.Role;
                        existing.Phone = EditingUser.Phone;
                        existing.Email = EditingUser.Email;
                        existing.IsActive = EditingUser.IsActive;
                    }
                }
                _context.SaveChanges();
                LoadData();
                IsEditing = false;
                EditingUser = null;
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Lỗi lưu: {ex.Message}", "Lỗi", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }

        private void Delete()
        {
            if (SelectedUser == null) return;

            if (UserSession.CurrentUser == null || SelectedUser.UserId == UserSession.CurrentUser.UserId)
            {
                System.Windows.MessageBox.Show("Không thể xóa chính mình!", "Lỗi",
                    System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
                return;
            }

            if (_context.Orders.Any(o => o.UserId == SelectedUser.UserId))
            {
                System.Windows.MessageBox.Show("Không thể xóa người dùng đã có đơn hàng!", "Lỗi",
                    System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
                return;
            }

            var result = System.Windows.MessageBox.Show($"Xóa người dùng '{SelectedUser.FullName}'?", "Xác nhận",
                System.Windows.MessageBoxButton.YesNo, System.Windows.MessageBoxImage.Question);
            if (result != System.Windows.MessageBoxResult.Yes) return;

            try
            {
                var entity = _context.Users.Find(SelectedUser.UserId);
                if (entity != null) { _context.Users.Remove(entity); _context.SaveChanges(); }
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
            EditingUser = null;
        }
    }
}

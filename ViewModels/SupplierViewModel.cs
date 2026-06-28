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
    public class SupplierViewModel : ViewModelBase
    {
        private readonly StoreDbContext _context;
        private ObservableCollection<Supplier> _suppliers;
        private Supplier _selectedSupplier;
        private Supplier _editingSupplier;
        private string _searchText;
        private bool _isEditing;

        public string Title => "Quản lý nhà cung cấp";

        public ObservableCollection<Supplier> Suppliers
        {
            get => _suppliers;
            set => SetProperty(ref _suppliers, value);
        }
        public Supplier SelectedSupplier
        {
            get => _selectedSupplier;
            set
            {
                SetProperty(ref _selectedSupplier, value);
                if (value != null && !_isEditing)
                    EditingSupplier = new Supplier { SupplierId = value.SupplierId, SupplierName = value.SupplierName, Phone = value.Phone, Email = value.Email, Address = value.Address, Notes = value.Notes };
            }
        }
        public Supplier EditingSupplier
        {
            get => _editingSupplier;
            set => SetProperty(ref _editingSupplier, value);
        }
        public string SearchText
        {
            get => _searchText;
            set { SetProperty(ref _searchText, value); Search(); }
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

        public SupplierViewModel()
        {
            _context = new StoreDbContext();
            AddCommand = new RelayCommand(Add);
            SaveCommand = new RelayCommand(Save, () => IsEditing);
            DeleteCommand = new RelayCommand(Delete, () => SelectedSupplier != null && !IsEditing);
            CancelCommand = new RelayCommand(Cancel);
            EditCommand = new RelayCommand(Edit, () => SelectedSupplier != null && !IsEditing);
            try { LoadData(); } catch { Suppliers = new ObservableCollection<Supplier>(); }
        }

        public override void Dispose() { _context?.Dispose(); base.Dispose(); }
        private void LoadData() { Suppliers = new ObservableCollection<Supplier>(_context.Suppliers.ToList()); }
        private void Search()
        {
            try
            {
                var q = _context.Suppliers.AsQueryable();
                if (!string.IsNullOrWhiteSpace(SearchText))
                    q = q.Where(s => s.SupplierName.Contains(SearchText) || (s.Phone ?? "").Contains(SearchText) || (s.Email ?? "").Contains(SearchText));
                Suppliers = new ObservableCollection<Supplier>(q.ToList());
            }
            catch
            {
                Suppliers = new ObservableCollection<Supplier>();
            }
        }
        private void Add() { EditingSupplier = new Supplier(); IsEditing = true; }
        private void Edit()
        {
            if (SelectedSupplier == null) return;
            EditingSupplier = new Supplier
            {
                SupplierId = SelectedSupplier.SupplierId,
                SupplierName = SelectedSupplier.SupplierName,
                Phone = SelectedSupplier.Phone,
                Email = SelectedSupplier.Email,
                Address = SelectedSupplier.Address,
                Notes = SelectedSupplier.Notes
            };
            IsEditing = true;
        }
        private void Save()
        {
            if (string.IsNullOrWhiteSpace(EditingSupplier.SupplierName))
            { System.Windows.MessageBox.Show("Vui lòng nhập tên nhà cung cấp!", "Lỗi", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning); return; }
            if (EditingSupplier.SupplierName.Length > 200)
            { System.Windows.MessageBox.Show("Tên nhà cung cấp không quá 200 ký tự!", "Lỗi", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning); return; }
            if (_context.Suppliers.Any(s => s.SupplierName == EditingSupplier.SupplierName && s.SupplierId != EditingSupplier.SupplierId))
            { System.Windows.MessageBox.Show("Tên nhà cung cấp đã tồn tại!", "Lỗi", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning); return; }
            try
            {
                if (EditingSupplier.SupplierId == 0) _context.Suppliers.Add(EditingSupplier);
                else
                {
                    var e = _context.Suppliers.Find(EditingSupplier.SupplierId);
                    if (e != null) { e.SupplierName = EditingSupplier.SupplierName; e.Phone = EditingSupplier.Phone; e.Email = EditingSupplier.Email; e.Address = EditingSupplier.Address; e.Notes = EditingSupplier.Notes; }
                }
                _context.SaveChanges(); LoadData(); IsEditing = false; EditingSupplier = null;
            }
            catch (Exception ex)
            {
                if (EditingSupplier != null && EditingSupplier.SupplierId == 0)
                    _context.Entry(EditingSupplier).State = System.Data.Entity.EntityState.Detached;
                System.Windows.MessageBox.Show($"Lỗi lưu: {ex.Message}", "Lỗi", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }
        private void Delete()
        {
            if (SelectedSupplier == null) return;
            var result = System.Windows.MessageBox.Show($"Xóa nhà cung cấp '{SelectedSupplier.SupplierName}'?", "Xác nhận",
                System.Windows.MessageBoxButton.YesNo, System.Windows.MessageBoxImage.Question);
            if (result != System.Windows.MessageBoxResult.Yes) return;
            try
            {
                var e = _context.Suppliers.Find(SelectedSupplier.SupplierId);
                if (e != null) { _context.Suppliers.Remove(e); _context.SaveChanges(); }
                LoadData();
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Lỗi xóa: {ex.Message}", "Lỗi", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }
        private void Cancel() { IsEditing = false; EditingSupplier = null; }
    }
}

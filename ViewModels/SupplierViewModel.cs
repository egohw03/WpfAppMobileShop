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

        public SupplierViewModel()
        {
            _context = new StoreDbContext();
            AddCommand = new RelayCommand(Add);
            SaveCommand = new RelayCommand(Save, () => IsEditing);
            DeleteCommand = new RelayCommand(Delete, () => SelectedSupplier != null);
            CancelCommand = new RelayCommand(Cancel);
            try { LoadData(); } catch { Suppliers = new ObservableCollection<Supplier>(); }
        }

        public override void Dispose() { _context?.Dispose(); base.Dispose(); }
        private void LoadData() { Suppliers = new ObservableCollection<Supplier>(_context.Suppliers.ToList()); }
        private void Search()
        {
            var q = _context.Suppliers.AsQueryable();
            if (!string.IsNullOrWhiteSpace(SearchText))
                q = q.Where(s => s.SupplierName.Contains(SearchText) || s.Phone.Contains(SearchText) || s.Email.Contains(SearchText));
            Suppliers = new ObservableCollection<Supplier>(q.ToList());
        }
        private void Add() { EditingSupplier = new Supplier(); IsEditing = true; }
        private void Save()
        {
            if (EditingSupplier.SupplierId == 0) _context.Suppliers.Add(EditingSupplier);
            else
            {
                var e = _context.Suppliers.Find(EditingSupplier.SupplierId);
                if (e != null) { e.SupplierName = EditingSupplier.SupplierName; e.Phone = EditingSupplier.Phone; e.Email = EditingSupplier.Email; e.Address = EditingSupplier.Address; e.Notes = EditingSupplier.Notes; }
            }
            _context.SaveChanges(); LoadData(); IsEditing = false; EditingSupplier = null;
        }
        private void Delete()
        {
            if (SelectedSupplier == null) return;
            var e = _context.Suppliers.Find(SelectedSupplier.SupplierId);
            if (e != null) { _context.Suppliers.Remove(e); _context.SaveChanges(); }
            LoadData();
        }
        private void Cancel() { IsEditing = false; EditingSupplier = null; }
    }
}

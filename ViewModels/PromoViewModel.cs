using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using WpfAppMobileShop.Data;
using WpfAppMobileShop.Helpers;
using WpfAppMobileShop.Models;

namespace WpfAppMobileShop.ViewModels
{
    public class PromoViewModel : ViewModelBase
    {
        private readonly StoreDbContext _context;
        private ObservableCollection<PromoCode> _promoCodes;
        private PromoCode _selectedPromo;
        private PromoCode _editingPromo;
        private string _searchText;
        private bool _isEditing;

        public string Title => "Quản lý khuyến mãi";

        public ObservableCollection<PromoCode> PromoCodes
        {
            get => _promoCodes;
            set => SetProperty(ref _promoCodes, value);
        }
        public PromoCode SelectedPromo
        {
            get => _selectedPromo;
            set
            {
                SetProperty(ref _selectedPromo, value);
                if (value != null && !_isEditing)
                    EditingPromo = new PromoCode { PromoCodeId = value.PromoCodeId, Code = value.Code, DiscountType = value.DiscountType, DiscountValue = value.DiscountValue, MinOrderAmount = value.MinOrderAmount, ExpiryDate = value.ExpiryDate, IsActive = value.IsActive };
            }
        }
        public PromoCode EditingPromo
        {
            get => _editingPromo;
            set => SetProperty(ref _editingPromo, value);
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

        public PromoViewModel()
        {
            _context = new StoreDbContext();
            AddCommand = new RelayCommand(Add);
            SaveCommand = new RelayCommand(Save, () => IsEditing);
            DeleteCommand = new RelayCommand(Delete, () => SelectedPromo != null && !IsEditing);
            CancelCommand = new RelayCommand(Cancel);
            EditCommand = new RelayCommand(Edit, () => SelectedPromo != null && !IsEditing);
            try { LoadData(); } catch { PromoCodes = new ObservableCollection<PromoCode>(); }
        }

        public override void Dispose() { _context?.Dispose(); base.Dispose(); }
        private void LoadData() { PromoCodes = new ObservableCollection<PromoCode>(_context.PromoCodes.ToList()); }
        private void Search()
        {
            try
            {
                var q = _context.PromoCodes.AsQueryable();
                if (!string.IsNullOrWhiteSpace(SearchText))
                    q = q.Where(p => p.Code.Contains(SearchText));
                PromoCodes = new ObservableCollection<PromoCode>(q.ToList());
            }
            catch
            {
                PromoCodes = new ObservableCollection<PromoCode>();
            }
        }
        private void Add() { EditingPromo = new PromoCode { DiscountType = "Percent", IsActive = true }; IsEditing = true; }
        private void Edit()
        {
            if (SelectedPromo == null) return;
            EditingPromo = new PromoCode
            {
                PromoCodeId = SelectedPromo.PromoCodeId,
                Code = SelectedPromo.Code,
                DiscountType = SelectedPromo.DiscountType,
                DiscountValue = SelectedPromo.DiscountValue,
                MinOrderAmount = SelectedPromo.MinOrderAmount,
                ExpiryDate = SelectedPromo.ExpiryDate,
                IsActive = SelectedPromo.IsActive
            };
            IsEditing = true;
        }
        private void Save()
        {
            if (string.IsNullOrWhiteSpace(EditingPromo.Code))
            { System.Windows.MessageBox.Show("Vui lòng nhập mã giảm giá!", "Lỗi", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning); return; }
            if (EditingPromo.Code.Length > 50)
            { System.Windows.MessageBox.Show("Mã giảm giá không quá 50 ký tự!", "Lỗi", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning); return; }
            if (string.IsNullOrWhiteSpace(EditingPromo.DiscountType))
            { System.Windows.MessageBox.Show("Vui lòng chọn loại giảm giá!", "Lỗi", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning); return; }
            if (EditingPromo.DiscountValue < 0)
            { System.Windows.MessageBox.Show("Giá trị giảm giá không hợp lệ!", "Lỗi", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning); return; }
            if (EditingPromo.DiscountType == "Percent" && EditingPromo.DiscountValue > 100)
            { System.Windows.MessageBox.Show("Giảm giá phần trăm không quá 100%!", "Lỗi", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning); return; }
            if (EditingPromo.MinOrderAmount < 0)
            { System.Windows.MessageBox.Show("Giá trị đơn tối thiểu không hợp lệ!", "Lỗi", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning); return; }
            if (EditingPromo.ExpiryDate.HasValue && EditingPromo.ExpiryDate.Value < DateTime.Today)
            { System.Windows.MessageBox.Show("Ngày hết hạn phải từ hôm nay trở đi!", "Lỗi", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning); return; }
            if (_context.PromoCodes.Any(p => p.Code == EditingPromo.Code && p.PromoCodeId != EditingPromo.PromoCodeId))
            { System.Windows.MessageBox.Show("Mã giảm giá đã tồn tại!", "Lỗi", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning); return; }
            try
            {
                if (EditingPromo.PromoCodeId == 0) _context.PromoCodes.Add(EditingPromo);
                else
                {
                    var e = _context.PromoCodes.Find(EditingPromo.PromoCodeId);
                    if (e != null) { e.Code = EditingPromo.Code; e.DiscountType = EditingPromo.DiscountType; e.DiscountValue = EditingPromo.DiscountValue; e.MinOrderAmount = EditingPromo.MinOrderAmount; e.ExpiryDate = EditingPromo.ExpiryDate; e.IsActive = EditingPromo.IsActive; }
                }
                _context.SaveChanges(); LoadData(); IsEditing = false; EditingPromo = null;
            }
            catch (Exception ex)
            {
                if (EditingPromo != null && EditingPromo.PromoCodeId == 0)
                    _context.Entry(EditingPromo).State = System.Data.Entity.EntityState.Detached;
                System.Windows.MessageBox.Show($"Lỗi lưu: {ex.Message}", "Lỗi", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }
        private void Delete()
        {
            if (SelectedPromo == null) return;
            var result = System.Windows.MessageBox.Show($"Xóa mã giảm giá '{SelectedPromo.Code}'?", "Xác nhận",
                System.Windows.MessageBoxButton.YesNo, System.Windows.MessageBoxImage.Question);
            if (result != System.Windows.MessageBoxResult.Yes) return;
            try
            {
                var e = _context.PromoCodes.Find(SelectedPromo.PromoCodeId);
                if (e != null) { _context.PromoCodes.Remove(e); _context.SaveChanges(); }
                LoadData();
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Lỗi xóa: {ex.Message}", "Lỗi", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }
        private void Cancel() { IsEditing = false; EditingPromo = null; }
    }
}

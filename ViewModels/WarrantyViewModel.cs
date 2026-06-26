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
    public class WarrantyViewModel : ViewModelBase
    {
        private readonly StoreDbContext _context;
        private ObservableCollection<Warranty> _warranties;
        private Warranty _selectedWarranty;
        private string _filterStatus;
        private string _warrantyNotes;

        public string Title => "Quản lý bảo hành";

        public ObservableCollection<Warranty> Warranties
        {
            get => _warranties;
            set => SetProperty(ref _warranties, value);
        }
        public Warranty SelectedWarranty
        {
            get => _selectedWarranty;
            set => SetProperty(ref _selectedWarranty, value);
        }
        public string FilterStatus
        {
            get => _filterStatus;
            set { SetProperty(ref _filterStatus, value); LoadData(); }
        }
        public string WarrantyNotes
        {
            get => _warrantyNotes;
            set => SetProperty(ref _warrantyNotes, value);
        }

        public ICommand RecordClaimCommand { get; }

        public WarrantyViewModel()
        {
            _context = new StoreDbContext();
            _filterStatus = "All";
            RecordClaimCommand = new RelayCommand(RecordClaim, () => SelectedWarranty != null && SelectedWarranty.Status == "Active");
            try { LoadData(); } catch { Warranties = new ObservableCollection<Warranty>(); }
        }

        public override void Dispose() { _context?.Dispose(); base.Dispose(); }

        private void LoadData()
        {
            var q = _context.Warranties.Include(w => w.Product).Include(w => w.Customer).Include(w => w.OrderDetail).AsQueryable();
            if (_filterStatus != "All")
                q = q.Where(w => w.Status == _filterStatus);
            Warranties = new ObservableCollection<Warranty>(q.OrderByDescending(w => w.StartDate).ToList());
        }

        private void RecordClaim()
        {
            if (SelectedWarranty == null) return;
            var w = _context.Warranties.Find(SelectedWarranty.WarrantyId);
            if (w == null) return;
            w.Status = "Used";
            w.Notes = WarrantyNotes;
            _context.SaveChanges();
            LoadData();
            WarrantyNotes = "";
            System.Windows.MessageBox.Show("Da ghi nhan bao hanh.", "Thong bao",
                System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
        }
    }
}

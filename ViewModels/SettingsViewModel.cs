using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using WpfAppMobileShop.Data;
using WpfAppMobileShop.Helpers;
using WpfAppMobileShop.Models;

namespace WpfAppMobileShop.ViewModels
{
    public class SettingsViewModel : ViewModelBase
    {
        private readonly StoreDbContext _context;
        private string _storeName;
        private string _storeAddress;
        private string _storePhone;
        private decimal _vatPercent;
        private int _lowStockThreshold;
        private int _warrantyMonths;

        public string Title => "Cai dat";

        public string StoreName { get => _storeName; set => SetProperty(ref _storeName, value); }
        public string StoreAddress { get => _storeAddress; set => SetProperty(ref _storeAddress, value); }
        public string StorePhone { get => _storePhone; set => SetProperty(ref _storePhone, value); }
        public decimal VatPercent { get => _vatPercent; set => SetProperty(ref _vatPercent, value); }
        public int LowStockThreshold { get => _lowStockThreshold; set => SetProperty(ref _lowStockThreshold, value); }
        public int WarrantyMonths { get => _warrantyMonths; set => SetProperty(ref _warrantyMonths, value); }

        public ICommand SaveCommand { get; }

        public SettingsViewModel()
        {
            _context = new StoreDbContext();
            SaveCommand = new RelayCommand(Save);
            try { LoadSettings(); } catch { }
        }

        public override void Dispose() { _context?.Dispose(); base.Dispose(); }

        private void LoadSettings()
        {
            StoreName = GetSetting("StoreName", "MobileShop");
            StoreAddress = GetSetting("StoreAddress", "");
            StorePhone = GetSetting("StorePhone", "");
            decimal.TryParse(GetSetting("VatPercent", "10"), out var vat);
            VatPercent = vat;
            int.TryParse(GetSetting("LowStockThreshold", "10"), out var threshold);
            LowStockThreshold = threshold;
            int.TryParse(GetSetting("WarrantyMonths", "12"), out var months);
            WarrantyMonths = months;
        }

        private string GetSetting(string key, string defaultValue)
        {
            var s = _context.Settings.Find(key);
            return s?.Value ?? defaultValue;
        }

        private void Save()
        {
            try
            {
                SetSetting("StoreName", StoreName);
                SetSetting("StoreAddress", StoreAddress);
                SetSetting("StorePhone", StorePhone);
                SetSetting("VatPercent", VatPercent.ToString());
                SetSetting("LowStockThreshold", LowStockThreshold.ToString());
                SetSetting("WarrantyMonths", WarrantyMonths.ToString());
                _context.SaveChanges();
                System.Windows.MessageBox.Show("Da luu cai dat.", "Thong bao",
                    System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Loi luu cai dat: {ex.Message}", "Loi",
                    System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }

        private void SetSetting(string key, string value)
        {
            var s = _context.Settings.Find(key);
            if (s == null) { _context.Settings.Add(new Setting { Key = key, Value = value }); }
            else { s.Value = value; }
        }
    }
}

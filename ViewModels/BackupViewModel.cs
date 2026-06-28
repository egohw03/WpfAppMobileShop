using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows.Input;
using WpfAppMobileShop.Helpers;

namespace WpfAppMobileShop.ViewModels
{
    public class BackupViewModel : ViewModelBase
    {
        private ObservableCollection<BackupInfo> _backups;
        private BackupInfo _selectedBackup;

        public string Title => "Sao luu & Phuc hoi";

        public ObservableCollection<BackupInfo> Backups
        {
            get => _backups;
            set => SetProperty(ref _backups, value);
        }
        public BackupInfo SelectedBackup
        {
            get => _selectedBackup;
            set => SetProperty(ref _selectedBackup, value);
        }

        public ICommand BackupNowCommand { get; }
        public ICommand RestoreCommand { get; }
        public ICommand DeleteBackupCommand { get; }

        public BackupViewModel()
        {
            BackupNowCommand = new RelayCommand(BackupNow);
            RestoreCommand = new RelayCommand(Restore, () => SelectedBackup != null);
            DeleteBackupCommand = new RelayCommand(DeleteBackup, () => SelectedBackup != null);
            LoadBackups();
        }

        private string BackupDir
        {
            get
            {
                var dir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Backups");
                if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
                return dir;
            }
        }

        private string DbPath => Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "StoreDB.sqlite");

        private void LoadBackups()
        {
            var files = Directory.GetFiles(BackupDir, "*.sqlite").Select(f => new BackupInfo
            {
                FileName = Path.GetFileName(f),
                FullPath = f,
                Date = File.GetCreationTime(f),
                Size = new FileInfo(f).Length
            }).OrderByDescending(b => b.Date).ToList();
            Backups = new ObservableCollection<BackupInfo>(files);
        }

        private void BackupNow()
        {
            try
            {
                var name = $"StoreDB_{DateTime.Now:yyyyMMdd_HHmmss}.sqlite";
                var dest = Path.Combine(BackupDir, name);
                System.Data.SQLite.SQLiteConnection.ClearAllPools();
                GC.Collect(); GC.WaitForPendingFinalizers();
                File.Copy(DbPath, dest, true);
                LoadBackups();
                System.Windows.MessageBox.Show($"Da sao luu: {name}", "Thanh cong",
                    System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Loi sao luu: {ex.Message}", "Loi",
                    System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }

        private void Restore()
        {
            if (SelectedBackup == null) return;
            var result = System.Windows.MessageBox.Show(
                $"Phuc hoi tu file: {SelectedBackup.FileName}?\nUng dung se khoi dong lai.",
                "Xac nhan", System.Windows.MessageBoxButton.YesNo, System.Windows.MessageBoxImage.Warning);
            if (result != System.Windows.MessageBoxResult.Yes) return;
            try
            {
                System.Data.SQLite.SQLiteConnection.ClearAllPools();
                GC.Collect(); GC.WaitForPendingFinalizers();
                File.Copy(SelectedBackup.FullPath, DbPath, true);
                System.Windows.MessageBox.Show("Phuc hoi thanh cong. Vui long khoi dong lai ung dung.", "Thong bao",
                    System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
                System.Windows.Application.Current.Shutdown();
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Loi phuc hoi: {ex.Message}", "Loi",
                    System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }

        private void DeleteBackup()
        {
            if (SelectedBackup == null) return;
            var result = System.Windows.MessageBox.Show($"Xoa file {SelectedBackup.FileName}?", "Xac nhan",
                System.Windows.MessageBoxButton.YesNo, System.Windows.MessageBoxImage.Question);
            if (result != System.Windows.MessageBoxResult.Yes) return;
            try { File.Delete(SelectedBackup.FullPath); LoadBackups(); }
            catch (Exception ex) { System.Windows.MessageBox.Show($"Loi: {ex.Message}", "Loi", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error); }
        }
    }

    public class BackupInfo : ViewModelBase
    {
        public string FileName { get; set; }
        public string FullPath { get; set; }
        public DateTime Date { get; set; }
        public long Size { get; set; }
        public string SizeDisplay
        {
            get
            {
                if (Size < 1024) return $"{Size} B";
                if (Size < 1024 * 1024) return $"{Size / 1024} KB";
                return $"{(double)Size / (1024 * 1024):F1} MB";
            }
        }
        public string DateDisplay => Date.ToString("dd/MM/yyyy HH:mm");
    }
}

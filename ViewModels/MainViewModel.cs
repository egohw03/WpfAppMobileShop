using System;
using System.Windows;
using System.Windows.Input;
using WpfAppMobileShop.Helpers;
using WpfAppMobileShop.Views;

namespace WpfAppMobileShop.ViewModels
{
    public class MainViewModel : ViewModelBase
    {
        private object _currentViewModel;
        private bool _isSidebarExpanded = true;

        public object CurrentViewModel
        {
            get => _currentViewModel;
            set => SetProperty(ref _currentViewModel, value);
        }

        public bool IsSidebarExpanded
        {
            get => _isSidebarExpanded;
            set => SetProperty(ref _isSidebarExpanded, value);
        }

        public string CurrentUserDisplay => $"Xin chào, {UserSession.CurrentUser?.FullName}";
        public string CurrentUserRole => UserSession.CurrentUser?.Role;
        public bool IsAdmin => UserSession.IsAdmin;

        public ICommand NavigateCommand { get; }
        public ICommand ToggleSidebarCommand { get; }
        public ICommand LogoutCommand { get; }

        public MainViewModel()
        {
            NavigateCommand = new RelayCommand(Navigate);
            ToggleSidebarCommand = new RelayCommand(() => IsSidebarExpanded = !IsSidebarExpanded);
            LogoutCommand = new RelayCommand(Logout);
            CurrentViewModel = new DashboardViewModel();
        }

        private void Navigate(object parameter)
        {
            var target = parameter as string;
            object newVM = null;
            switch (target)
            {
                case "Dashboard":
                    newVM = new DashboardViewModel();
                    break;
                case "Products":
                    newVM = new ProductViewModel();
                    break;
                case "Categories":
                    newVM = new CategoryViewModel();
                    break;
                case "Customers":
                    newVM = new CustomerViewModel();
                    break;
                case "Sales":
                    newVM = new SalesViewModel();
                    break;
                case "Users":
                    newVM = new UserViewModel();
                    break;
            }
            if (newVM != null)
            {
                if (CurrentViewModel is IDisposable old)
                    old.Dispose();
                CurrentViewModel = newVM;
            }
        }

        private void Logout()
        {
            var result = MessageBox.Show("Bạn có chắc muốn đăng xuất?", "Xác nhận",
                MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result != MessageBoxResult.Yes) return;

            if (CurrentViewModel is IDisposable old)
                old.Dispose();

            UserSession.CurrentUser = null;

            foreach (Window window in Application.Current.Windows)
            {
                if (window != Application.Current.MainWindow)
                {
                    window.Close();
                }
            }

            var loginView = new LoginView();
            if (loginView.ShowDialog() == true)
            {
                CurrentViewModel = new DashboardViewModel();
                OnPropertyChanged(nameof(CurrentUserDisplay));
                OnPropertyChanged(nameof(CurrentUserRole));
                OnPropertyChanged(nameof(IsAdmin));
            }
            else
            {
                Application.Current.Shutdown();
            }
        }
    }
}

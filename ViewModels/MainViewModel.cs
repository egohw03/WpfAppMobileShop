using System;
using System.Windows.Input;
using WpfAppMobileShop.Helpers;

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

        public ICommand NavigateCommand { get; }
        public ICommand ToggleSidebarCommand { get; }

        public MainViewModel()
        {
            NavigateCommand = new RelayCommand(Navigate);
            ToggleSidebarCommand = new RelayCommand(() => IsSidebarExpanded = !IsSidebarExpanded);
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
            }
            if (newVM != null)
            {
                if (CurrentViewModel is IDisposable old)
                    old.Dispose();
                CurrentViewModel = newVM;
            }
        }
    }
}

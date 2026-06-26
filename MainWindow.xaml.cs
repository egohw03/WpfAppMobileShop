using System.Windows;
using WpfAppMobileShop.ViewModels;
using WpfAppMobileShop.Views;

namespace WpfAppMobileShop
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            ShowLogin();
        }

        private void ShowLogin()
        {
            var loginView = new LoginView();
            if (loginView.ShowDialog() != true)
            {
                Application.Current.Shutdown();
                return;
            }
            DataContext = new MainViewModel();
        }
    }
}

using System.Windows;
using System.Windows.Input;
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

        private void SidebarHeader_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                var vm = DataContext as MainViewModel;
                vm?.ToggleSidebarCommand.Execute(null);
            }
        }
    }
}

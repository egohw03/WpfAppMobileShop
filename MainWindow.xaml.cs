using System.Windows;
using WpfAppMobileShop.ViewModels;

namespace WpfAppMobileShop
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            DataContext = new MainViewModel();
        }
    }
}

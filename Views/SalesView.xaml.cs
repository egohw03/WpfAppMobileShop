using System.Windows.Controls;
using System.Windows.Input;
using WpfAppMobileShop.Models;

namespace WpfAppMobileShop.Views
{
    public partial class SalesView : UserControl
    {
        public SalesView()
        {
            InitializeComponent();
        }

        private void Product_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (DataContext is ViewModels.SalesViewModel viewModel && sender is DataGrid grid && grid.SelectedItem is Product product)
            {
                viewModel.AddToCartCommand.Execute(product);
            }
        }
    }
}

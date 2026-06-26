using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using WpfAppMobileShop.ViewModels;

namespace WpfAppMobileShop.Views
{
    public partial class LoginView : Window
    {
        private readonly LoginViewModel _viewModel;

        public LoginView()
        {
            InitializeComponent();
            _viewModel = new LoginViewModel();
            DataContext = _viewModel;

            _viewModel.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(LoginViewModel.IsLoginSuccess) && _viewModel.IsLoginSuccess)
                {
                    DialogResult = true;
                    Close();
                }
            };
        }

        private void LoginButton_Click(object sender, RoutedEventArgs e)
        {
            _viewModel.Password = PasswordBox.Password;
            if (_viewModel.LoginCommand.CanExecute(null))
            {
                _viewModel.LoginCommand.Execute(null);
            }
        }

        private void PasswordBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                LoginButton_Click(sender, e);
            }
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                DialogResult = false;
                Close();
            }
            base.OnKeyDown(e);
        }
    }
}

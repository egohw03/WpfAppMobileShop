using System.Linq;
using System.Windows.Input;
using WpfAppMobileShop.Data;
using WpfAppMobileShop.Helpers;

namespace WpfAppMobileShop.ViewModels
{
    public class LoginViewModel : ViewModelBase
    {
        private string _username;
        private string _password;
        private string _errorMessage;
        private bool _isLoginSuccess;

        public string Username
        {
            get => _username;
            set => SetProperty(ref _username, value);
        }

        public string Password
        {
            get => _password;
            set => SetProperty(ref _password, value);
        }

        public string ErrorMessage
        {
            get => _errorMessage;
            set => SetProperty(ref _errorMessage, value);
        }

        public bool IsLoginSuccess
        {
            get => _isLoginSuccess;
            set => SetProperty(ref _isLoginSuccess, value);
        }

        public ICommand LoginCommand { get; }

        public LoginViewModel()
        {
            LoginCommand = new RelayCommand(Login);
        }

        private void Login()
        {
            if (string.IsNullOrWhiteSpace(Username) || string.IsNullOrWhiteSpace(Password))
            {
                ErrorMessage = "Vui lòng nhập tên đăng nhập và mật khẩu!";
                return;
            }

            using (var context = new StoreDbContext())
            {
                var user = context.Users
                    .FirstOrDefault(u => u.Username == Username && u.Password == Password && u.IsActive);

                if (user != null)
                {
                    UserSession.CurrentUser = user;
                    IsLoginSuccess = true;
                }
                else
                {
                    ErrorMessage = "Tên đăng nhập hoặc mật khẩu không đúng!";
                }
            }
        }
    }
}

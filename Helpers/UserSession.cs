using WpfAppMobileShop.Models;

namespace WpfAppMobileShop.Helpers
{
    public static class UserSession
    {
        public static User CurrentUser { get; set; }
        public static bool IsAdmin => CurrentUser?.Role == "Admin";
        public static bool IsLoggedIn => CurrentUser != null;
    }
}

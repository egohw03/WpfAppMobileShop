using MaterialDesignThemes.Wpf;

namespace WpfAppMobileShop.Helpers
{
    public static class NotificationService
    {
        private static readonly SnackbarMessageQueue _messageQueue = new SnackbarMessageQueue();

        public static SnackbarMessageQueue MessageQueue => _messageQueue;

        public static void ShowMessage(string message)
        {
            _messageQueue.Enqueue(message, null, null, false, false);
        }

        public static void ShowError(string message)
        {
            _messageQueue.Enqueue(message, null, null, true, false);
        }

        public static void ShowSuccess(string message)
        {
            _messageQueue.Enqueue(message, null, null, false, false);
        }
    }
}

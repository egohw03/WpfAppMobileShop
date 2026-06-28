using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace WpfAppMobileShop.Helpers
{
    public class BoolToVisibilityConverter : IValueConverter
    {
        public bool Invert { get; set; }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool boolValue;
            if (value is bool b)
                boolValue = b;
            else
                boolValue = value != null;
            if (Invert) boolValue = !boolValue;
            return boolValue ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return ((Visibility)value) == Visibility.Visible;
        }
    }

    public class LowStockToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is int count && count > 0)
                return Visibility.Visible;
            return Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class AdjustTypeToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string type && type == "Import")
                return Visibility.Visible;
            return Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class StockStatusConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Length < 2 || !(values[0] is int stock) || !(values[1] is int threshold))
                return "Bình thường";
            if (stock == 0) return "Hết hàng";
            if (stock <= Math.Max(1, threshold / 3)) return "Cực thấp";
            if (stock <= Math.Max(2, threshold / 2)) return "Rất thấp";
            if (stock <= threshold) return "Sắp hết";
            return "Bình thường";
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class StockStatusColorConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Length < 2 || !(values[0] is int stock) || !(values[1] is int threshold))
                return System.Windows.Media.Brushes.Green;
            if (stock == 0) return System.Windows.Media.Brushes.Red;
            if (stock <= Math.Max(1, threshold / 3)) return System.Windows.Media.Brushes.Red;
            if (stock <= Math.Max(2, threshold / 2)) return System.Windows.Media.Brushes.OrangeRed;
            if (stock <= threshold) return System.Windows.Media.Brushes.Orange;
            return System.Windows.Media.Brushes.Green;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}

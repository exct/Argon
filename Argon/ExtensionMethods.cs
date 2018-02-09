using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Drawing;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows.Data;
using System.Windows.Media.Imaging;

namespace Argon
{
    public static class ExtensionMethods
    {
        static readonly string[] SizeSuffixes = { " B", "KB", "MB", "GB", "TB", "PB", "EB", "ZB", "YB" };

        public static string AddSizeSuffix(this double value)
        {

            if (value < 0) { return "-" + AddSizeSuffix(-value); }

            int i = 0;
            double adjustedSize = value;

            while (adjustedSize >= 1000) {
                i += 1;
                adjustedSize /= 1000;
            }

            return string.Format("{0} {1}", Math.Round(adjustedSize, i > 0 ? i - 1 : 0), SizeSuffixes[i]);
        }

        public static long NextSecond(this long i)
        {
            return (long)Math.Ceiling((decimal)i / 10000000) * 10000000;
        }

        public static ObservableCollection<T> ToObservableCollection<T>(this IQueryable<T> enumeration)
        {
            return new ObservableCollection<T>(enumeration);
        }

        public static byte[] ToByteArray(this Image image, ImageFormat format)
        {
            using (MemoryStream ms = new MemoryStream()) {
                image.Save(ms, format);
                return ms.ToArray();
            }
        }

        public static BitmapSource GetIcon(this string filePath)
        {
            if (File.Exists(filePath))
                using (var sysicon = System.Drawing.Icon.ExtractAssociatedIcon(filePath)) {
                    var i = System.Windows.Interop.Imaging.CreateBitmapSourceFromHIcon(
                           sysicon.Handle,
                           System.Windows.Int32Rect.Empty,
                           BitmapSizeOptions.FromEmptyOptions());
                    i.Freeze();
                    return i;
                }
            else
                return null;
        }

        public static ObservableCollection<T> ToObservableCollection<T>(this IEnumerable<T> col)
        {
            return new ObservableCollection<T>(col);
        }
    }

    public class DataSizeConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value == null) return null;
            return ((double)value).AddSizeSuffix();
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}

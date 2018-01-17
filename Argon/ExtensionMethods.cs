using System;
using System.Collections.ObjectModel;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Windows.Media.Imaging;

namespace Argon
{
    public static class ExtensionMethods
    {
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


    }
}

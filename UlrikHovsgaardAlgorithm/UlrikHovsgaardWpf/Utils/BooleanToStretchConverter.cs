using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Media;

namespace UlrikHovsgaardWpf.Utils
{
    public class BooleanToStretchConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter,
                System.Globalization.CultureInfo culture)
        {
            var b = value as bool?;
            if (b == null || b.Value) // Uniform @ null-param, as Uniform is default value
            {
                return Stretch.Uniform; // "Image is larger than canvas"
            }
            return Stretch.None; // "Image is smaller than canvas"
        }

        public object ConvertBack(object value, Type targetType, object parameter,
                System.Globalization.CultureInfo culture)
        {
            if (value is Stretch)
            {
                switch ((Stretch) value)
                {
                    case Stretch.None:
                        return false;
                    case Stretch.Uniform:
                        return true;
                }
            }
            return false;
        }
    }
}

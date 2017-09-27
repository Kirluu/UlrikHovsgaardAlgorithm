using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Media;
using RedundancyRemoverComparerWpf.ViewModels;

namespace RedundancyRemoverComparerWpf.Converters
{
    [ValueConversion(typeof(ComparerViewModel.GraphDisplayMode), typeof(Color))]
    public class FullRedRemGraphToDisplayToBackgroundColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (!(value is ComparerViewModel.GraphDisplayMode))
                throw new ArgumentException("value not of type GraphDisplayMode");
            var sv = (ComparerViewModel.GraphDisplayMode)value;
            
            if (sv == ComparerViewModel.GraphDisplayMode.Original)
                return Colors.White;
            else if (sv == ComparerViewModel.GraphDisplayMode.FullyRedundancyRemoved)
                return Colors.LimeGreen;
            else if (sv == ComparerViewModel.GraphDisplayMode.ErrorContext)
                return Colors.White;

            // default
            return Colors.White;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return null; // Don't need convert-back
        }
    }

    [ValueConversion(typeof(ComparerViewModel.GraphDisplayMode), typeof(Color))]
    public class OriginalGraphToDisplayToBackgroundColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (!(value is ComparerViewModel.GraphDisplayMode))
                throw new ArgumentException("value not of type GraphDisplayMode");
            var sv = (ComparerViewModel.GraphDisplayMode)value;

            if (sv == ComparerViewModel.GraphDisplayMode.Original)
                return Colors.LimeGreen;
            else if (sv == ComparerViewModel.GraphDisplayMode.FullyRedundancyRemoved)
                return Colors.White;
            else if (sv == ComparerViewModel.GraphDisplayMode.ErrorContext)
                return Colors.White;

            // default
            return Colors.White;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return null; // Don't need convert-back
        }
    }

    [ValueConversion(typeof(ComparerViewModel.GraphDisplayMode), typeof(Color))]
    public class ErrorContextGraphToDisplayToBackgroundColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (!(value is ComparerViewModel.GraphDisplayMode))
                throw new ArgumentException("value not of type GraphDisplayMode");
            var sv = (ComparerViewModel.GraphDisplayMode)value;

            if (sv == ComparerViewModel.GraphDisplayMode.Original)
                return Colors.White;
            else if (sv == ComparerViewModel.GraphDisplayMode.FullyRedundancyRemoved)
                return Colors.White;
            else if (sv == ComparerViewModel.GraphDisplayMode.ErrorContext)
                return Colors.LimeGreen;

            // default
            return Colors.White;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return null; // Don't need convert-back
        }
    }
}

using System;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using System.Windows.Data;
using System.Windows.Markup;

namespace CropControl
{
    public class AspectRatioConverter : IValueConverter
    {

        



        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return (AspectRatios)Enum.Parse(typeof(AspectRatios), value as string, true);
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }

    }
}

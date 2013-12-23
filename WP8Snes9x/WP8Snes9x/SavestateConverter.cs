using PhoneDirect3DXamlAppInterop.Resources;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace PhoneDirect3DXamlAppInterop
{
    public class SavestateConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            int slot = (int)value;

            string result = AppResources.ManageSlotLabel;
            if (slot < 9)
            {
                result += slot;
            }
            else
            {
                result = AppResources.ManageAutoSlotLabel + result;
            }

            return result;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}

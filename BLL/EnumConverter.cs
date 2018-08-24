using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace VRegistration.BLL
{
    public class EnumConverter : IValueConverter
    {
        public object Convert(object val, Type target, object param, System.Globalization.CultureInfo culture)
        {
            return val.Equals(param);
        }

        public object ConvertBack(object val, Type target, object param, System.Globalization.CultureInfo culture)
        {
            return val.Equals(true) ? param : Binding.DoNothing;
        }
    }
    //nanidafaq
}

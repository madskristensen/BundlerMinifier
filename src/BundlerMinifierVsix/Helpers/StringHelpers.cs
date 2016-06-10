using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BundlerMinifierVsix
{
    static class StringHelpers
    {
        public static string AddParams(this string format, params object[] parameters)
        {
            return string.Format(CultureInfo.CurrentCulture, format, parameters);
        }
    }
}

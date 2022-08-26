using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Token
{
    public static class Extensions
    {
        private static readonly CultureInfo _invariantCulture = CultureInfo.InvariantCulture;

        /// <summary>
        /// Convert an integer to its string representation with invariant culture
        /// </summary>
        public static string AsInvariantString ( this int @this)
        {
            return @this.ToString(_invariantCulture);
        }

        /// <summary>
        /// Convert an long to its string representation with invariant culture
        /// </summary>
        public static string AsInvariantString(this long @this)
        {
            return @this.ToString(_invariantCulture);
        }

        /// <summary>
        /// Convert a sting generated with AsInvariantString method back to its numeric value
        /// </summary>
        public static long FromInvariantString (this string @this)
        {
            long result = 0;
            if (long.TryParse(@this, out result) == false)
                result = 0;

            return result;
        }

    }
}

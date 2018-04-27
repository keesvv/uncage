using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Uncage
{
    public static class Extensions
    {
        public static string RemoveInvalidChars(this string str)
        {
            string invalidString = "/\\?%*:|\"<>.";
            char[] invalidChars = invalidString.ToArray();
            foreach (var item in invalidChars)
            {
                str = str.Replace(item.ToString(), string.Empty);
            }

            return str;
        }
    }
}

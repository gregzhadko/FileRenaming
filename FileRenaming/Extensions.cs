using System;
using System.Collections.Generic;
using System.Linq;

namespace FileRenaming
{
    public static class Extensions
    {
        public static bool InListCaseIgnore(this string s, IEnumerable<string> list)
        {
            return list.Any(l => string.Equals(s, l, StringComparison.OrdinalIgnoreCase));
        }
    }
}
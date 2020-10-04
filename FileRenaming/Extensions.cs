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

        public static string ReplaceAt(this string str, int index, int length, string replace)
        {
            return string.Create(str.Length - length + replace.Length, (str, index, length, replace),
                (span, state) =>
                {
                    state.str.AsSpan().Slice(0, state.index).CopyTo(span);
                    state.replace.AsSpan().CopyTo(span.Slice(state.index));
                    state.str.AsSpan().Slice(state.index + state.length).CopyTo(span.Slice(state.index + state.replace.Length));
                });
        }

        public static string ReplaceAt(this string str, int index, string symbol)
        {
            return str.ReplaceAt(index, 1, symbol);
        }

        public static string ReplaceAt(this string str, int index, char symbol)
        {
            return str.ReplaceAt(index, 1, symbol.ToString());
        }
    }
}
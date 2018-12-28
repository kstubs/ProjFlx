using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace ProjectFlx.Utility
{
    public static class Paths
    {
        /// <summary>
        /// Return a path with forward whacks front and back and combined
        /// </summary>
        /// <param name="Paths"></param>
        /// <returns></returns>
        public static string CombinePaths(params string[] Paths)
        {
            StringBuilder sb = new StringBuilder();
            foreach (string s in Paths)
            {
                if (String.IsNullOrEmpty(s))
                    continue;

                var val = s.Replace("\\", "/").Trim();
                if (sb.Length == 0 && (Path.IsPathRooted(val) || Regex.Match(val, "^http[s]?://").Success))
                {
                    sb.Append(val);
                }
                else
                {
                    if (sb.ToString().EndsWith("/"))
                        if (val.StartsWith("/"))
                            sb.Append(val.Substring(1));
                        else
                            sb.Append(val);
                    else
                        sb.Append(val.BeginWhack());
                }
            }

            return sb.ToString();
        }

        // Return string ending with whack
        public static string EndWhack(this string Value)
        {
            if (Value.EndsWith("/"))
                return Value.Substring(0, Value.LastIndexOf("/"));

            return Value.TrimEnd() + "/";

        }

        /// <summary>
        /// Return string starting with whack
        /// </summary>
        /// <param name="Value"></param>
        /// <returns></returns>
        public static string BeginWhack(this string Value)
        {
            if (Value.StartsWith("/"))
                return Value;

            return "/" + Value;
        }

    }
}

using System;
using System.IO;
using System.Net;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace ProjectFlx
{
    public static class Extensions
    {
        public static string Flatten(this List<string> StringList)
        {
            StringBuilder sb = new StringBuilder();
            foreach (string s in StringList)
                sb.Append(s);

            return sb.ToString();

        }

        public static bool HasValue(this String Value)
        {
            return !String.IsNullOrEmpty(Value);
        }

        public static bool Exists(this Uri uri)
        {
            try
            {
                WebRequest request = WebRequest.Create(uri);
                request.Timeout = 5000;//Timeout set to 5 seconds

                WebResponse response;

                response = request.GetResponse();
                if (request.RequestUri != response.ResponseUri)
                    return false;

                return true;
            }
            catch { }
            return false;
        }
    }
}

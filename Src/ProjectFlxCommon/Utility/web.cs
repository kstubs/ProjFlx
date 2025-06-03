using System;
using System.Collections.Generic;
using System.Collections.Specialized;

using System.IO;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;

namespace ProjectFlx.Utility
{
    public enum PostContentType { TextXml };


    public class Web
    {
        private static Dictionary<PostContentType, string> _contentType = new Dictionary<PostContentType, string>();

        static Web()
        {
            _contentType.Add(PostContentType.TextXml, "text/xml");
        }


        public static HttpWebResponse PostData(string postUrl, NameValueCollection Parms, PostContentType contentType, byte[] formData)
        {
            #region Post Url + QueryString
            var bldr = new StringBuilder();
            bldr.Append(postUrl);
            bldr.Append("?");
            var flgFirst = true;
            foreach (var parm in Parms.AllKeys)
            {
                if (!flgFirst)
                    bldr.Append("&");

                var safeval = HttpUtility.UrlEncode(Parms[parm]);
                bldr.AppendFormat("{0}={1}", parm, safeval);

                flgFirst = false;
            }
            #endregion

            var request = WebRequest.Create(bldr.ToString()) as HttpWebRequest;

            if (request == null)
            {
                throw new NullReferenceException("request is not a http request");
            }

            // Set up the request properties.
            request.Method = "POST";
            request.ContentType = _contentType[contentType];
            request.CookieContainer = new CookieContainer();
            request.ContentLength = formData.Length;

            // Send the form data to the request.
            using (var requestStream = request.GetRequestStream())
            {
                requestStream.Write(formData, 0, formData.Length);
                requestStream.Close();
            }
            return request.GetResponse() as HttpWebResponse;
        }

        private static HttpWebResponse PostForm(string postUrl, string userAgent, string contentType, byte[] formData)
        {
            HttpWebRequest request = WebRequest.Create(postUrl) as HttpWebRequest;

            if (request == null)
            {
                throw new NullReferenceException("request is not a http request");
            }

            // Set up the request properties.
            request.Method = "POST";
            request.ContentType = contentType;
            request.UserAgent = userAgent;
            request.CookieContainer = new CookieContainer();
            request.ContentLength = formData.Length;

            // You could add authentication here as well if needed:
            // request.PreAuthenticate = true;
            // request.AuthenticationLevel = System.Net.Security.AuthenticationLevel.MutualAuthRequested;
            // request.Headers.Add("Authorization", "Basic " + Convert.ToBase64String(System.Text.Encoding.Default.GetBytes("username" + ":" + "password")));

            // Send the form data to the request.
            using (Stream requestStream = request.GetRequestStream())
            {
                requestStream.Write(formData, 0, formData.Length);
                requestStream.Close();
            }

            return request.GetResponse() as HttpWebResponse;
        }

        public static HttpWebResponse MultipartFormDataPost(string postUrl, string userAgent, Dictionary<string, object> postParameters)
        {
            string formDataBoundary = String.Format("----------{0:N}", Guid.NewGuid());
            string contentType = "multipart/form-data; boundary=" + formDataBoundary;

            byte[] formData = GetMultipartFormData(postParameters, formDataBoundary);

            return PostForm(postUrl, userAgent, contentType, formData);
        }

        public static string RemoveWhiteSpaceFromStylesheets(string body)
        {
            body = Regex.Replace(body, @"[a-zA-Z]+#", "#");
            body = Regex.Replace(body, @"[\n\r]+\s*", string.Empty);
            body = Regex.Replace(body, @"\s+", " ");
            body = Regex.Replace(body, @"\s?([:,;{}])\s?", "$1");
            body = body.Replace(";}", "}");
            body = Regex.Replace(body, @"([\s:]0)(px|pt|%|em)", "$1");

            // Remove comments from CSS
            body = Regex.Replace(body, @"/\*[\d\D]*?\*/", string.Empty);

            return body;
        }

        public static string getWebResource(string WebResourceUri)
        {
            System.Net.ServicePointManager.SecurityProtocol = System.Net.SecurityProtocolType.Tls12;

            try
            {
                var rq = WebRequest.Create(WebResourceUri);
                rq.Credentials = CredentialCache.DefaultCredentials;

                using (StreamReader reader = new StreamReader(rq.GetResponse().GetResponseStream()))
                {
                    return reader.ReadToEnd();
                }
            }
            catch
            {
                return null;
            }
        }
        public static byte[] getWebResourceBytes(string WebResourceUri)
        {
            System.Net.ServicePointManager.SecurityProtocol = System.Net.SecurityProtocolType.Tls12;

            try
            {

                var rq = WebRequest.Create(WebResourceUri);
                rq.Credentials = CredentialCache.DefaultCredentials;

                var ms = new MemoryStream();
                byte[] buffer = new byte[16 * 1024];
                using (var stream = rq.GetResponse().GetResponseStream())
                {
                    int read;
                    while ((read = stream.Read(buffer, 0, buffer.Length)) > 0)
                    {
                        ms.Write(buffer, 0, read);
                    }
                }
                return ms.ToArray();
            }
            catch
            {
                return null;
            }
        }

        public static MemoryStream getWebResourceStream(string WebResourceUri)
        {
            System.Net.ServicePointManager.SecurityProtocol = System.Net.SecurityProtocolType.Tls12;

            //try
            //{

            var rq = WebRequest.Create(WebResourceUri);
            rq.Credentials = CredentialCache.DefaultCredentials;

            var ms = new MemoryStream();
            byte[] buffer = new byte[16 * 1024];
            using (var stream = rq.GetResponse().GetResponseStream())
            {
                int read;
                while ((read = stream.Read(buffer, 0, buffer.Length)) > 0)
                {
                    ms.Write(buffer, 0, read);
                }
            }
            return ms;
            //}
            //catch
            //{
            //    return null;
            //}
        }

        private static byte[] GetMultipartFormData(Dictionary<string, object> postParameters, string boundary)
        {
            var encoding = Encoding.UTF8;
            Stream formDataStream = new System.IO.MemoryStream();
            bool needsCLRF = false;

            foreach (var param in postParameters)
            {
                // Thanks to feedback from commenters, add a CRLF to allow multiple parameters to be added.
                // Skip it on the first parameter, add it to subsequent parameters.
                if (needsCLRF)
                    formDataStream.Write(encoding.GetBytes("\r\n"), 0, encoding.GetByteCount("\r\n"));

                needsCLRF = true;

                if (param.Value is FileParameter)
                {
                    FileParameter fileToUpload = (FileParameter)param.Value;

                    // Add just the first part of this param, since we will write the file data directly to the Stream
                    string header = string.Format("--{0}\r\nContent-Disposition: form-data; name=\"{1}\"; filename=\"{2}\"\r\nContent-Type: {3}\r\n\r\n",
                        boundary,
                        param.Key,
                        fileToUpload.FileName ?? param.Key,
                        fileToUpload.ContentType ?? "application/octet-stream");

                    formDataStream.Write(encoding.GetBytes(header), 0, encoding.GetByteCount(header));

                    // Write the file data directly to the Stream, rather than serializing it to a string.
                    formDataStream.Write(fileToUpload.File, 0, fileToUpload.File.Length);
                }
                else if (param.Value.GetType().IsArray)
                {
                    var array = (Array)param.Value;
                    foreach (var a in array)
                    {
                        string postData = string.Format("--{0}\r\nContent-Disposition: form-data; name=\"{1}[]\"\r\n\r\n{2}",
                            boundary,
                            param.Key,
                            a);
                        formDataStream.Write(encoding.GetBytes(postData), 0, encoding.GetByteCount(postData));
                    }

                }
                else
                {
                    string postData = string.Format("--{0}\r\nContent-Disposition: form-data; name=\"{1}\"\r\n\r\n{2}",
                        boundary,
                        param.Key,
                        param.Value);
                    formDataStream.Write(encoding.GetBytes(postData), 0, encoding.GetByteCount(postData));
                }
            }

            // Add the end of the request.  Start with a newline
            string footer = "\r\n--" + boundary + "--\r\n";
            formDataStream.Write(encoding.GetBytes(footer), 0, encoding.GetByteCount(footer));

            // Dump the Stream into a byte[]
            formDataStream.Position = 0;
            byte[] formData = new byte[formDataStream.Length];
            formDataStream.Read(formData, 0, formData.Length);
            formDataStream.Close();

            return formData;
        }

        public class FileParameter
        {
            public byte[] File { get; set; }
            public string FileName { get; set; }
            public string ContentType { get; set; }
            public FileParameter(byte[] file) : this(file, null) { }
            public FileParameter(byte[] file, string filename) : this(file, filename, null) { }
            public FileParameter(byte[] file, string filename, string contenttype)
            {
                File = file;
                FileName = filename;
                ContentType = contenttype;
            }
        }

        public static string HASH_NAME_SEPARATOR = "____";


        /// <summary>
        /// Set a cookie value, all cookie values are hashed
        /// </summary>
        /// <param name="CookieName"></param>
        /// <param name="CookieValue"></param>
        /// <param name="ExpireHours"></param>
        public static void setCookieValue(string CookieName, object CookieValue, int ExpireMinutes, bool Protected, string Domain, byte[] CookieSalt = null)
        {
            var response = HttpContext.Current.Response;

            var cookie = new HttpCookie(CookieName, CookieValue.ToString()); // HttpUtility.UrlEncode(CookieValue.ToString()));
            cookie.Domain = Domain;
            cookie.Expires = DateTime.Now.AddMinutes(ExpireMinutes);
            cookie.HttpOnly = true;
            response.Cookies.Add(cookie);

            // if cookie is meant to be protected then save hash copy of it for verification purposes
            if (Protected)
            {
                var sb = new StringBuilder();
                var names = new List<string>();
                var dts = new List<DateTime>();
                dts.Add(cookie.Expires);

                // create our protected projFlx object
                ////var flxCookie = response.Cookies["ProjFLX"];
                ////if (flxCookie == null)
                ////    flxCookie = new HttpCookie("ProjFLX");

                ////if (!String.IsNullOrEmpty(flxCookie.Value))
                ////{
                ////    var raw = Encoding.UTF8.GetString(Convert.FromBase64String(flxCookie.Value));
                ////    var vals = Regex.Split(raw, Utility.Web.HASH_NAME_SEPARATOR);

                ////    foreach(var n in vals[1].Split(','))
                ////    {
                ////        var c = response.Cookies[n];
                ////        sb.AppendFormat("{0}{1}", n, c.Value.ToString());
                ////        names.Add(n);
                ////        dts.Add(c.Expires);
                ////    }
                ////}

                sb.AppendFormat("{0}{1}", CookieName, CookieValue.ToString());
                names.Add(CookieName);

                if (CookieSalt == null)
                    throw new Exception("CookieSalt byte[] Required for Protected Cookie");

                var value = HttpUtility.UrlEncode(SimpleHash.ComputeHash(CookieValue.ToString(), "MD5", CookieSalt));
                var cookie_h = CookieName + "_h";
                cookie = new HttpCookie(cookie_h, value); // HttpUtility.UrlEncode(value));
                cookie.Domain = Domain;
                cookie.Expires = DateTime.Now.AddMinutes(ExpireMinutes);
                cookie.HttpOnly = true;
                response.Cookies.Add(cookie);

                sb.AppendFormat("{0}{1}", cookie_h, value);
                names.Add(cookie_h);

                var hash = Utility.SimpleHash.ComputeHash(sb.ToString(), "MD5", CookieSalt);
                var bytes = Encoding.UTF8.GetBytes(hash + Utility.Web.HASH_NAME_SEPARATOR + String.Join(",", names.ToArray()));

                System.Diagnostics.Debug.WriteLine(sb.ToString());
                System.Diagnostics.Debug.WriteLine(hash);

                ////// ProjFLX cookie "the glue"
                ////flxCookie.Expires = dts.Min();
                ////if (flxCookie.Expires < DateTime.Now)
                ////    flxCookie.Expires = DateTime.Now.AddMinutes(300);    // some default expires date time greater than now

                ////flxCookie.Domain = Domain;
                ////flxCookie.HttpOnly = true;
                ////flxCookie.Value = Convert.ToBase64String(bytes);

                ////response.Cookies.Add(flxCookie);
            }
        }
        public static bool IsMobile(string UserAgent)
        {
            Regex b = new Regex(@"(android|bb\d+|meego).+mobile|avantgo|bada\/|blackberry|blazer|compal|elaine|fennec|hiptop|iemobile|ip(hone|od)|iris|kindle|lge|maemo|midp|mmp|netfront|opera m(ob|in)i|palm( os)?|phone|p(ixi|re)\/|plucker|pocket|psp|series(4|6)0|symbian|treo|up\.(browser|link)|vodafone|wap|windows (ce|phone)|xda|xiino|android|ipad|playbook|silk", RegexOptions.IgnoreCase | RegexOptions.Multiline);
            Regex v = new Regex(@"1207|6310|6590|3gso|4thp|50[1-6]i|770s|802s|a wa|abac|ac(er|oo|s\-)|ai(ko|rn)|al(av|ca|co)|amoi|an(ex|ny|yw)|aptu|ar(ch|go)|as(te|us)|attw|au(di|\-m|r |s )|avan|be(ck|ll|nq)|bi(lb|rd)|bl(ac|az)|br(e|v)w|bumb|bw\-(n|u)|c55\/|capi|ccwa|cdm\-|cell|chtm|cldc|cmd\-|co(mp|nd)|craw|da(it|ll|ng)|dbte|dc\-s|devi|dica|dmob|do(c|p)o|ds(12|\-d)|el(49|ai)|em(l2|ul)|er(ic|k0)|esl8|ez([4-7]0|os|wa|ze)|fetc|fly(\-|_)|g1 u|g560|gene|gf\-5|g\-mo|go(\.w|od)|gr(ad|un)|haie|hcit|hd\-(m|p|t)|hei\-|hi(pt|ta)|hp( i|ip)|hs\-c|ht(c(\-| |_|a|g|p|s|t)|tp)|hu(aw|tc)|i\-(20|go|ma)|i230|iac( |\-|\/)|ibro|idea|ig01|ikom|im1k|inno|ipaq|iris|ja(t|v)a|jbro|jemu|jigs|kddi|keji|kgt( |\/)|klon|kpt |kwc\-|kyo(c|k)|le(no|xi)|lg( g|\/(k|l|u)|50|54|\-[a-w])|libw|lynx|m1\-w|m3ga|m50\/|ma(te|ui|xo)|mc(01|21|ca)|m\-cr|me(rc|ri)|mi(o8|oa|ts)|mmef|mo(01|02|bi|de|do|t(\-| |o|v)|zz)|mt(50|p1|v )|mwbp|mywa|n10[0-2]|n20[2-3]|n30(0|2)|n50(0|2|5)|n7(0(0|1)|10)|ne((c|m)\-|on|tf|wf|wg|wt)|nok(6|i)|nzph|o2im|op(ti|wv)|oran|owg1|p800|pan(a|d|t)|pdxg|pg(13|\-([1-8]|c))|phil|pire|pl(ay|uc)|pn\-2|po(ck|rt|se)|prox|psio|pt\-g|qa\-a|qc(07|12|21|32|60|\-[2-7]|i\-)|qtek|r380|r600|raks|rim9|ro(ve|zo)|s55\/|sa(ge|ma|mm|ms|ny|va)|sc(01|h\-|oo|p\-)|sdk\/|se(c(\-|0|1)|47|mc|nd|ri)|sgh\-|shar|sie(\-|m)|sk\-0|sl(45|id)|sm(al|ar|b3|it|t5)|so(ft|ny)|sp(01|h\-|v\-|v )|sy(01|mb)|t2(18|50)|t6(00|10|18)|ta(gt|lk)|tcl\-|tdg\-|tel(i|m)|tim\-|t\-mo|to(pl|sh)|ts(70|m\-|m3|m5)|tx\-9|up(\.b|g1|si)|utst|v400|v750|veri|vi(rg|te)|vk(40|5[0-3]|\-v)|vm40|voda|vulc|vx(52|53|60|61|70|80|81|83|85|98)|w3c(\-| )|webc|whit|wi(g |nc|nw)|wmlb|wonu|x700|yas\-|your|zeto|zte\-", RegexOptions.IgnoreCase | RegexOptions.Multiline);
            return (b.IsMatch(UserAgent) || v.IsMatch(UserAgent.Substring(0, 4)));
        }
    }

}

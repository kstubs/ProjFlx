using System;
using System.IO;
using System.Web;
using System.Net;
using System.Configuration;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ProjectFlx
{
    public class googleCaptcha : IHttpHandler
    {
        public bool IsReusable
        {
            get { return true; }
        }

        public void ProcessRequest(HttpContext context)
        {

            try
            {
                var parms = String.Format("secret={0}&response={1}&remoteid={2}", ConfigurationManager.AppSettings["GoogleCaptchaSecretKey"], context.Request.Form["g-recaptcha-response"], ProjectFlx.FlxMain.getUserIP());
                var bytes = Encoding.UTF8.GetBytes(parms);


                var request = (HttpWebRequest)WebRequest.Create("https://www.google.com/recaptcha/api/siteverify");
                request.Method = "POST";
                request.ContentType = "application/x-www-form-urlencoded";
                request.ContentLength = bytes.Length;

                var s = request.GetRequestStream();
                s.Write(bytes, 0, bytes.Length);
                s.Flush();
                s.Close();

                using (HttpWebResponse response = request.GetResponse() as HttpWebResponse)
                {
                    if (response.StatusCode != HttpStatusCode.OK)
                        throw new Exception("Expecting HTTPStatus OK from google.com recaptcha siteverify");

                    string responsetext;
                    using (StreamReader rstream = new StreamReader(response.GetResponseStream()))
                    {
                        responsetext = rstream.ReadToEnd();
                    }
                    var jsoObj = Newtonsoft.Json.Linq.JObject.Parse(responsetext);
                    var ishuman = Convert.ToBoolean(jsoObj["success"]);

                    if (!ishuman)
                    {
                        context.Response.StatusCode = (int)HttpStatusCode.Forbidden;
                        context.Response.ContentType = "text";
                        context.Response.Write("Content on this site shall not be crawled by bot");
                    }
                    else
                    {
                        context.Response.Cookies.Add(new HttpCookie(context.Request.Form["cookie"], "true") { Expires = DateTime.Now.AddYears(3) });
                        string redirect = context.Server.UrlDecode(context.Request.Form["redirect"]);
                        context.Response.Redirect(redirect);
                    }
                }
            }
            catch (System.Threading.ThreadAbortException) { }
            catch (Exception)
            {
                context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
            }
        }
    }
}

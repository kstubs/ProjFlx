using System;
using System.IO;
using System.Text;
using System.Linq;
using System.Configuration;
using System.Collections;
using System.Collections.Generic;
using System.Web;
using System.Runtime.Caching;
using System.Xml;
using ProjectFlx.Exceptions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Text.RegularExpressions;
using closure = ProjectFlx.Utility.Closure;

namespace ProjectFlx
{
    public delegate void FlxTWebSiteExceptions(Exception exc);

    public class FlxMain : IHttpHandler, FlxSiteInterface, FlxPageInterface
    {
        // private class variables
        private XmlDocument clsXM = new XmlDocument();
        protected bool wbtProcessPageScript = true;
        protected HttpContext Current;
        protected HttpResponse Response;
        protected HttpRequest Request;
        protected System.Web.SessionState.HttpSessionState Session;
        protected HttpServerUtility Server;

        private ObjectCache _cache = MemoryCache.Default;
        private string _cacheDependencyPath = null;
        private string clsCurrentPage;
        private List<string> _pageHeirarchy = new List<string>();
        private bool clsClearProcess = false;
        private enumRequestType _requestType;

        protected ProjectExceptionHandler _handler = new ProjectExceptionHandler();
        protected bool _debug = false;
        protected string _projectFlxPath = null;
        protected string _clientFlxpath = null;
        private bool _AuthenticatedUser = false;
        private bool _LoggedOnUser = false;

        public enum CacheKeyEnum { XmlCacheKey, ContextCacheKey, XslCacheKey, PHCacheKey, ScriptCacheKey, WaitClosureKey, ContentPathKey };
        private Dictionary<CacheKeyEnum, String> _cacheKeys = new Dictionary<CacheKeyEnum, string>();

        protected Utility.TimingDebugger TimingDebugger { get; set; }
        protected Utility.Timing Timing { get; set; }

        public FlxMain()
        {
            NSMGR = new XmlNamespaceManager(new XmlDocument().NameTable);
            NSMGR.AddNamespace("wbt", "myWebTemplater.1.0");
            NSMGR.AddNamespace("sbt", "mySiteTemplater.1.0");
            NSMGR.AddNamespace("pbt", "myPageTemplater.1.0");
        }

        private string GetConfigValue(string Name, string DefaultValue)
        {
            var val = App.Config[Name];
            if (String.IsNullOrEmpty(val))
                return DefaultValue;
            else
                return val;
        }
        private T GetConfigValue<T>(string Name, T DefaultValue)
        {
            T result;
            var val = App.Config[Name];

            if (String.IsNullOrEmpty(val))
                result = DefaultValue;
            else
            {
                try
                {
                    result = (T)System.ComponentModel.TypeDescriptor.GetConverter(typeof(T)).ConvertFromString(val);
                }
                catch
                {
                    result = DefaultValue;
                }
            }

            return result;
        }

        public bool LoggedOnUser
        {
            get { return _LoggedOnUser; }
            set 
            {
                _LoggedOnUser = value;
                TMPLT.AddXslParameter("LoggedOnUser", _LoggedOnUser);
                AddVAR("LoggedOnUser", _LoggedOnUser);
            }
        }

        public bool AuthenticatedUser
        {
            get { return _AuthenticatedUser; }
            set 
            {
                _AuthenticatedUser = value;
                TMPLT.AddXslParameter("AuthenticatedUser", _AuthenticatedUser);
                AddVAR("AuthenticatedUser", _AuthenticatedUser);
            }
        }

        public void AddVAR(String Name, Object Value)
        {
            if (Value.GetType() == typeof(Boolean))
            {
                TMPLT.AddBrowserPageItem("VARS", Value.ToString().ToLower(), Name);
            }
            else
            {
                TMPLT.AddBrowserPageItem("VARS", String.Format("'{0}'", Value.ToString()), Name);
            }
        }

        public FlxTemplater TMPLT;

		bool _exceptionInTerminate = false;

        public void Main()
        {
            using (TMPLT = new FlxTemplater())
            {
                // Execute (private) TMPLT Terminate procedures
                try
                {
                    TimingDebugger = new Utility.TimingDebugger();
                    Timing = TimingDebugger.New("FLX");
                    TMPLT_CONFIG();

                    TMPLT_INIT();
                    TMPLT_MAIN();
                    TMPLT_TERMINATE();
                }
                catch (System.Threading.ThreadAbortException) { }
                catch (ProjectException handled)
                {
                    TMPLT.AddException(handled);
					if (_exceptionInTerminate)
					{
						HandleUnHandledErrors(handled);
					} else
					{
						TMPLT_TERMINATE();
					}
                }
                catch (Exception e)
                {
                    TMPLT.AddException(e);
                    HandleUnHandledErrors(e);
                }
                finally
                {
                    if (_debug)
                    {
                        try
                        {
                            using (FileStream file = new FileStream(Server.MapPath("/ac/TMPLT.xml"), FileMode.Create, FileAccess.Write))
                            {
                                TMPLT._xml.Save(file);
                            }
                        }
                        catch { }
                    }
                    Timing = Timing.End("FLX");
                    FlxWebFinal();
                }
            }
        }

        public virtual void FlxWebFinal() { }

        public enum enumRequestType { OTHER, GET, PUT, POST }
        public enumRequestType RequestType
        {
            get
            {
                return _requestType;
            }
        }
        /// <summary>
        /// (Alias) RequestType
        /// 
        /// </summary>
        public enumRequestType HTTPRequestType
        {
            get
            {
                return _requestType;
            }
        }

        public virtual void TMPLT_CONFIG()
        {
            if(!App.Config.Initialized)
                App.Config.Setup();

            _projFlxUseCache = GetConfigValue<bool>("projectflx-caching", false);
            _cacheContentMinutes = GetConfigValue<int>("projectflx-caching-minutes", (int)(60 * 24 * 1));
            _projFlxUseCacheScript = GetConfigValue<bool>("projectflx-jscript-caching", false);
            _cacheJscriptMinutes = GetConfigValue<int>("projectflx-jscript-caching-minutes", (int)(60 * 24 * 5));

            _useCdn = GetConfigValue<bool>("use-cdn", false);
            _cdnSite = GetConfigValue("project-cdn-path", "");
            _clientFlxpath = GetConfigValue("projectFlxTemplates", "");

        }

        private void TMPLT_INIT()
        {
            try
            {
                Timing.Start("ProjectFlx.FlxMain.TMPLT_INIT");

                _debug = GetConfigValue<bool>("debug", false);

                switch (Request.RequestType)
                {
                    case System.Net.WebRequestMethods.Http.Post:
                        _requestType = enumRequestType.POST;
                        break;
                    case System.Net.WebRequestMethods.Http.Get:
                        _requestType = enumRequestType.GET;
                        break;
                    case System.Net.WebRequestMethods.Http.Put:
                        _requestType = enumRequestType.PUT;
                        break;
                }

                TMPLT.AddBrowserPageItem("HTTP_METHOD", _requestType.ToString());

                // deal with banned IPs
                var bannedips = GetConfigValue("banned_ips","");
                if (!string.IsNullOrEmpty(bannedips))
                {
                    string remoteip = FlxMain.getUserIP();
                    string[] IPs = bannedips.Split(' ');
                    foreach (string ip in IPs)
                    {
                        if (remoteip.StartsWith(ip))
                            Response.Redirect(GetConfigValue("banned-page", "https://theuselessweb.com/"));
                    }
                }

                if (GetConfigValue<bool>("site-down", false))
                    Response.Redirect(GetConfigValue("site-down-redirect", "index.html"));

                TMPLT.AddXslParameter("DEBUG", _debug);

                _projectFlxPath = Server.MapPath(GetConfigValue("project-flx-path", " /ProjectFLX"));

                // parse the path
                List<String> aPath = new List<string>();
                StringBuilder sbpath = null;
                foreach (char c in Request.Path.ToCharArray())
                {
                    switch (c)
                    {
                        // path delimiters
                        case '.':
                        case '/':
                            if (sbpath != null && !(sbpath.ToString().Equals("aspx")))
                                aPath.Add(sbpath.ToString());
                            sbpath = null;
                            break;
                        default:
                            if (sbpath == null)
                                sbpath = new StringBuilder();
                            sbpath.Append(c);
                            break;
                    }
                }

                StringBuilder linksb = null;
                foreach (var s in aPath)
                {
                    if (linksb == null)
                    {
                        linksb = new StringBuilder("/");
                    }
                    else
                        linksb.Append(".");     // TODO: delimiter should be dynamic
                    linksb.Append(s);
                    _pageHeirarchy.Add(s.ToLower());
                    TMPLT.AddBrowserPageItem("PAGE_HEIRARCHY", s.ToLower(), null, linksb.ToString(), s);
                }

                _SiteMap = PageHeirarchyCombined.Equals("sitemap") || PageHeirarchyCombined.Equals("sitemapxml");

                setupCacheKey(CacheKeyEnum.XmlCacheKey, PageHeirarchyCombined);
                setupCacheKey(CacheKeyEnum.XslCacheKey, PageHeirarchyCombined);
                setupCacheKey(CacheKeyEnum.ContextCacheKey, PageHeirarchyCombined);
                setupCacheKey(CacheKeyEnum.PHCacheKey, PageHeirarchyCombined);
                setupCacheKey(CacheKeyEnum.ScriptCacheKey, PageHeirarchyCombined);
                setupCacheKey(CacheKeyEnum.ContentPathKey, PageHeirarchyCombined);
                setupCacheKey(CacheKeyEnum.WaitClosureKey, PageHeirarchyCombined);

                // TODO: cleanup
                //_cacheKeys.Add(CacheKeyEnum.XmlCacheKey, "cache:xml__" + pagecachkey);
                //_cacheKeys.Add(CacheKeyEnum.ContextCacheKey, "cache:context__" + pagecachkey);
                //_cacheKeys.Add(CacheKeyEnum.XslCacheKey, "cache:xsl__" + pagecachkey);
                //_cacheKeys.Add(CacheKeyEnum.PHCacheKey, "cache:PH__" + pagecachkey);
                //_cacheKeys.Add(CacheKeyEnum.ScriptCacheKey, "cache:script__" + pagecachkey);
                //_cacheKeys.Add(CacheKeyEnum.ContentPathKey, "cache:contentpath__" + pagecachkey);
                //_cacheKeys.Add(CacheKeyEnum.WaitClosureKey, "cache:pausegoogleclosure__" + pagecachkey);
            }
            finally
            {
                Timing.Stop("ProjectFlx.FlxMain.TMPLT_INIT");
            }

        }

        private void setupCacheKey(CacheKeyEnum CacheKey, string CacheKeyVal)
        {
            // TODO: coordinate this with other uses, match page combined name value
            var key = (MapPageKeys.ContainsKey(PageHeirarchyCombined) ? MapPageKeys[PageHeirarchyCombined] : PageHeirarchyCombined).Replace("/", "__");

            if (_cacheKeys.ContainsKey(CacheKey))
                _cacheKeys.Remove(CacheKey);

            string format = null;
            switch(CacheKey)
            {
                case CacheKeyEnum.XmlCacheKey:
                    format = "cache:{0}__xml";
                    break;
                case CacheKeyEnum.XslCacheKey:
                    format = "cache:{0}__xsl";
                    break;
                case CacheKeyEnum.WaitClosureKey:
                    format = "cache:{0}__pausegoogleclosure";
                    break;
                case CacheKeyEnum.ScriptCacheKey:
                    format = "cache:{0}__script";
                    break;
                case CacheKeyEnum.PHCacheKey:
                    format = "cache:{0}__PH";
                    break;
                case CacheKeyEnum.ContextCacheKey:
                    format = "cache:{0}__context";
                    break;
                case CacheKeyEnum.ContentPathKey:
                    format = "cache:{0}__contentpath";
                    break;
            }

            _cacheKeys.Add(CacheKey, String.Format(format, key));
        }

        private string parseKeywords(StreamReader r)
        {
            StringBuilder keys = null;
            string s = null;
            while (!String.IsNullOrEmpty(s = r.ReadLine()))
            {
                if (keys == null)
                    keys = new StringBuilder();
                else
                    keys.Append(", ");

                var parse = s.Split(',');
                int i = 0;
                foreach (String p in parse)
                {
                    if (i++ > 0)
                        keys.Append(", ");

                    keys.Append(p.Trim());
                }

            }

            return (keys == null) ? "" : keys.ToString().Trim();

        }

        private void TMPLT_MAIN()
        {
            try
            {
                Timing.Start("ProjectFlx.FlxMain.TMPLT_MAIN");

                // SITE Init - overrideable
                SITE_INIT();

                // Main Call (2 of 3) - Call Site Main
                SITE_MAIN();

                // Site Terminate Event - overrideable
                SITE_TERMINATE();
            }
            finally
            {
                Timing.Stop("ProjectFlx.FlxMain.TMPLT_MAIN");
            }
        }

        private void TMPLT_TERMINATE()
        {
            try
            {
                Timing.Start("ProjectFlx.FlxMain.TMPLT_TERMINATE");
                // abort template terminate when client not connected
                if (!Response.IsClientConnected)
                    return;

                // performance monitoring
                if (_debug)
                {
                    //TMPLT.AddXML(PerformanceMonitoring.getProcessInfoHistory());

                    TMPLT.AddTag("CacheCount", _cache.GetCount());
                    // TODO: support for the following values from System.Runtime.Cache (was available in Web)
                    //TMPLT.AddTag("CacheEffectivePercentagePhysicalMemoryLimit", _cache.EffectivePercentagePhysicalMemoryLimit.ToString());
                    //TMPLT.AddTag("CacheEffectivePrivateBytesLimit", _cache.EffectivePrivateBytesLimit.ToString());
                    foreach(var cache in _cache)
                        TMPLT.AddTag("CacheKey", cache.Key.ToString());
                }

                // resolve Lorem Ipsum requests
                string xpath = "//LoremIpsum";
                var nodes = TMPLT.DOCxml.SelectNodes(xpath);
                foreach (XmlNode node in nodes)
                {
                    var paracount = 1;
                    int.TryParse((node.Attributes["p"] == null) ? "1" : node.Attributes["p"].Value, out paracount);

                    var paralength = (node.Attributes["Size"] == null) ? "Medium" : node.Attributes["Size"].Value;


                    var lip = new NLipsum.Core.LipsumGenerator();
                    var paraOptions = NLipsum.Core.Paragraph.Medium;
                    switch (paralength)
                    {
                        case "Short":
                            paraOptions = NLipsum.Core.Paragraph.Short;
                            paraOptions.MinimumSentences = 2;
                            paraOptions.MaximumSentences = 5;
                            break;
                        case "Medium":
                            paraOptions = NLipsum.Core.Paragraph.Medium;
                            paraOptions.MinimumSentences = 6;
                            paraOptions.MaximumSentences = 12;
                            break;
                        case "Long":
                            paraOptions = NLipsum.Core.Paragraph.Long;
                            paraOptions.MinimumSentences = 15;
                            paraOptions.MaximumSentences = 25;
                            break;
                    }

                    StringBuilder bldr = new StringBuilder();
                    for (int i = 0; i < paracount; i++)
                    {
                        bldr.AppendFormat("<p>{0}</p>", string.Join(" ", lip.GenerateParagraphs(1, paraOptions)));
                    }

                    TMPLT.AddTag(String.Format("LoremIpsum_{0}", paracount), bldr.ToString());
                }

                // this is not available until we enable process metrics
                //TMPLT.AddXML(PerformanceMonitoring.getProcessInfoHistory());

                if (!clsClearProcess)
                {
                    try
                    {
                        TMPLT.AddXMLJson(ProjectFlx.Utility.TimingDebugger.Serialize(TimingDebugger));
                    }
                    catch { }

                    Timing.Start("ProjectFlx.FlxMain.TMPLT_TERMINATE.ProcessTemplate");
                    TMPLT.ProcessTemplateExternal();
                    Timing.End("ProjectFlx.FlxMain.TMPLT_TERMINATE.ProcessTemplate");
                    Response.ContentType = "text/html";
                    Response.Write(TMPLT.Result);
                }
            }
			catch(Exception unhandled)
			{
				_exceptionInTerminate = true;
				throw unhandled;
			}
            finally
            {
                Timing.Stop("ProjectFlx.FlxMain.TMPLT_TERMINATE");
            }
        }

        bool _useCdn = false;

        protected bool UseCdn
        {
            get { return _useCdn; }
            set { _useCdn = value; }
        }
        string _cdnSite = null;

        protected bool FlxUseCache
        {
            get { return _projFlxUseCache; }
            set { _projFlxUseCache = value; }
        }

        protected string CdnSite
        {
            get { return _cdnSite; }
            set { _cdnSite = value; }
        }

        public virtual void SITE_INIT()
        {
            try
            {
                Timing.Start("ProjectFlx.FlxMain.SITE_INIT");

                // bots are bad, block bots
                var whitelist = new List<string>();
                if (ConfigurationManager.AppSettings["bot-white-list"] != null)
                {
                    // TODO: update to new caching objects
                    if(CacheContains("bot-white-list"))
                    {
                        whitelist = GetCache<List<string>>("bot-white-list");
                    }
                    else
                    {
                        var filepath = Server.MapPath(ConfigurationManager.AppSettings["bot-white-list"]);
                        if (File.Exists(filepath))
                        {
                            whitelist = new List<string>();
                            using (StreamReader sreader = new StreamReader(filepath))
                            {
                                string line = null;
                                while ((line = sreader.ReadLine()) != null)
                                {
                                    whitelist.Add(line);
                                }
                            }
                            SaveCache("bot-white-list", whitelist, 24 * 60);
                        }
                    }
                }
                var useragent = Request.ServerVariables["HTTP_USER_AGENT"];
                if (!String.IsNullOrEmpty(useragent))
                {
                    if (!whitelist.Contains(useragent) && useragent.Contains("bot"))
                    {
                        _validUserAgent = false;
                        TMPLT.AddTag("ISBot", "YES");
                    }
                }

                if (_SiteMap)
                    return;

                if (_useCdn)
                {
                    if (_resources == null)
                        throw new Exception("Expecting CDN Resources");
                }
                else
                {
                    if (String.IsNullOrEmpty(_clientFlxpath))
                        throw new Exception("Missing projectFlxTemplates in web config");

                    // TODO: cache result of file resources
                    _resources = FileResources.getFileResources(Server.MapPath(_clientFlxpath), (Request.ServerVariables["HTTPS"] == "on") ? "https://" + Request.ServerVariables["HTTP_HOST"] : "http://" + Request.ServerVariables["HTTP_HOST"], _clientFlxpath);
                }

                XmlDocument content = new XmlDocument();
                content.XmlResolver = new XmlUrlResolver();
                ResourceContentPath = null;

                XmlNode current = null, context = null;
                ResourceContentPath = getXmlResources(content, ref current);

                TMPLT.AddXML("client-context", getXmlContext(content));

                var newAtt = current.OwnerDocument.CreateAttribute("wbt", "loggedonuser", NSMGR.LookupNamespace("wbt"));
                newAtt.Value = (current.SelectSingleNode("ancestor-or-self::content[@loggedonuser='true' or @loggedinuser='true' or @LoggedOnUser='true' or @LoggedInUser='true'] | ancestor-or-self::LoggedOn | ancestor-or-self::LoggedIn | ancestor-or-self::LoggedInUser") != null).ToString().ToLower();
                current.Attributes.Append(newAtt);
                newAtt = current.OwnerDocument.CreateAttribute("wbt", "authenticateduser", NSMGR.LookupNamespace("wbt"));
                newAtt.Value = (current.SelectSingleNode("ancestor-or-self::content[@authenticateduser='true'] | ancestor-or-self::Authenticated") != null).ToString().ToLower();
                current.Attributes.Append(newAtt);

                TMPLT.AddXML("client", current);
                if (content == null)
                    throw new Exception("Project FLX Content not found!  Expecting ProjectFLX XmlDocument resource for the request - and/or - missing ProjectFLX default XmlDocument resource at: /ProjectFlx/ProjectFlx.Xml");

                string[] paths = { "", String.IsNullOrEmpty(ResourceContentPath) ? "SKIP__RESOURCE" : ResourceContentPath };

                #region embed required style and scripts
                foreach (string pickup in paths)
                {
                    if (pickup == "SKIP__RESOURCE")
                        continue;

                    var subs = new String[] { "style", "script" };

                    foreach (var sub in subs)
                    {
                        if (_resources.Exists(Utility.Paths.CombinePaths(pickup, sub, "required.txt")))
                        {
                            StringReader txtreader;
                            if (_useCdn)
                            {
                                txtreader = new StringReader(Utility.Web.getWebResource(_resources.FullWebPath(_resources.IndexOf)));
                            }
                            else
                            {
                                // load required scripts
                                using (StreamReader reader = new StreamReader(Server.MapPath(_resources.AbsolutePath(_resources.IndexOf))))
                                {
                                    txtreader = new StringReader(reader.ReadToEnd());
                                }
                            }
                            var line = txtreader.ReadLine();
                            while (line != null)
                            {
                                if (line.EndsWith(".js"))
                                    TMPLT.AddBrowserPageItem("SCRIPT", line.Replace("\\", "/"));
                                else if(line.EndsWith(".css"))
                                    TMPLT.AddBrowserPageItem("STYLE", line.Replace("\\", "/"));
                                line = txtreader.ReadLine();
                            }
                        }
                    }
                }
                #endregion

                #region embed inline content xml pbt:javascript and pbt:style
                if (current != null)
                {
                    foreach (XmlNode node in current.SelectNodes("//pbt:*", NSMGR))
                    {
                        if (node != null)
                        {
                            var nodes = node.SelectNodes("src", NSMGR);
                            foreach (XmlNode srcNode in nodes)
                            {
                                switch (node.LocalName)
                                {
                                    case "javascript":
                                        TMPLT.AddBrowserPageItem("SCRIPT", Utility.Paths.CombinePaths(node.Attributes["base"] == null ? "" : node.Attributes["base"].Value, srcNode.InnerText));
                                        break;
                                    case "style":
                                        TMPLT.AddBrowserPageItem("STYLE", Utility.Paths.CombinePaths(node.Attributes["base"] == null ? "" : node.Attributes["base"].Value, srcNode.InnerText));
                                        break;
                                }
                            }
                        }
                    }
                }
                #endregion


                // pickup style from local content and sub page heirarchy content

                    // default files
                    foreach (string s in _resources.collectResources("style", ".css"))
                        TMPLT.AddBrowserPageItem("STYLE", (_useCdn) ? Utility.Paths.CombinePaths(_resources.Host, s) : s);

                for (int x = 0; x < _pageHeirarchy.Count; x++)
                {
                    // page defined
                    var sub = new string[2];
                    sub[0] = ResourceContentPath;
                    sub[1] = "style";
                    var subpage = new ArraySegment<string>(_pageHeirarchy.ToArray(), 1, x);     // x for x is: 0 for 0 (ignore 1st item) 1 for

                    var a = new String[sub.Length + subpage.ToArray().Length];
                    Array.Copy(sub, a, sub.Length);
                    if (subpage.ToArray().Length > 0)
                        Array.Copy(subpage.ToArray(), 0, a, sub.Length, subpage.ToArray().Length);

                    foreach (string s in _resources.collectResources(Utility.Paths.CombinePaths(a), ".css"))
                        TMPLT.AddBrowserPageItem("STYLE", (_useCdn) ? Utility.Paths.CombinePaths(_resources.Host, s) : s);
                }

                // pickup script from local content
                foreach (string s in _resources.collectResources("script", ".js"))
                        TMPLT.AddBrowserPageItem("SCRIPT", (_useCdn) ? Utility.Paths.CombinePaths(_resources.Host, s) : s);

                for (int x = 0; x < _pageHeirarchy.Count; x++)
                {
                    // page defined
                    var sub = new string[2];
                    sub[0] = ResourceContentPath;
                    sub[1] = "script";
                    var subpage = new ArraySegment<string>(_pageHeirarchy.ToArray(), 1, x);     // x for x is: 0 for 0 (ignore 1st item) 1 for

                    var a = new String[sub.Length + subpage.ToArray().Length];
                    Array.Copy(sub, a, sub.Length);
                    if(subpage.ToArray().Length > 0)
                        Array.Copy(subpage.ToArray(), 0, a, sub.Length, subpage.ToArray().Length);
                    
                    foreach (string s in _resources.collectResources(Utility.Paths.CombinePaths(a), ".js"))
                        TMPLT.AddBrowserPageItem("SCRIPT", (_useCdn) ? Utility.Paths.CombinePaths(_resources.Host, s) : s);
                }

                // meta tags
                string[] meta = { "DESCRIPTION", "KEYWORDS", "TITLE" };
                foreach (var m in meta)
                {
                    var filename = m.ToLower() + ".txt";
                    if (_resources.Exists(Utility.Paths.CombinePaths(ResourceContentPath, "meta", filename)))
                    {
                        if (_useCdn)
                        {
                            TMPLT.AddBrowserPageItem(m, Utility.Web.getWebResource(_resources.FullWebPath(_resources.IndexOf)));
                        }
                        else
                        {
                            using (StreamReader r = new StreamReader(Server.MapPath(_resources.AbsolutePath(_resources.IndexOf))))
                            {
                                TMPLT.AddBrowserPageItem(m, r.ReadToEnd());
                            }
                        }
                    }
                    else if (_resources.Exists(Utility.Paths.CombinePaths("meta", filename)))
                    {
                        if (_useCdn)
                        {

                            TMPLT.AddBrowserPageItem(m, Utility.Web.getWebResource(_resources.FullWebPath(_resources.IndexOf)));
                        }
                        else
                        {
                            using (StreamReader r = new StreamReader(Server.MapPath(_resources.AbsolutePath(_resources.IndexOf))))
                            {
                                TMPLT.AddBrowserPageItem(m, r.ReadToEnd());
                            }
                        }
                    }

                }

				// get page roles (if exist)
				getPageRoles();
				AssertRoles();		// throws an exception - should be handled
			}
            finally
            {
                Timing.Stop("ProjectFlx.FlxMain.SITE_INIT");
            }

        }

        private void wbtProjSql(XmlNode current, Schema.Extra.commonProj projsql)
        {
            AssertProtectedContent(current);

            var proj = Request.QueryString["wbt_project"];
            var query = Request.QueryString["wbt_query"];

            if (proj == null || query == null)
                return;

            var node = current.SelectSingleNode("//wbt:ProjSql", NSMGR);
            if (node == null)
                return;

            #region User Cert required test
            if (userRequiresCert(node))
                return;
            #endregion

            var xpath = String.Format("*[local-name()='{0}']/query[@name='{1}']", proj, query);
            var qnode = projsql.ProjSqlNode.SelectSingleNode(xpath);

            if (qnode == null)
                return;

            var importnode = current.OwnerDocument.CreateNode(XmlNodeType.Element, "wbt", "query", NSMGR.LookupNamespace("wbt"));
            for (int x = 0; x < qnode.Attributes.Count; x++)
            {
                var attimport = importnode.OwnerDocument.CreateAttribute(qnode.Attributes[x].LocalName);
                attimport.Value = qnode.Attributes[x].Value;
                importnode.Attributes.Append(attimport);
            }

            var att = importnode.OwnerDocument.CreateAttribute("project");
            att.Value = proj;
            importnode.Attributes.Append(att);
            att = importnode.OwnerDocument.CreateAttribute("query");
            att.Value = query;
            importnode.Attributes.Append(att);
            att = importnode.OwnerDocument.CreateAttribute("action");
            att.Value = qnode.SelectSingleNode("command/action").InnerText;
            importnode.Attributes.Append(att);

            if(qnode.SelectSingleNode("parameters") != null)
            {
                var newnode = importnode.OwnerDocument.ImportNode(qnode.SelectSingleNode("parameters"), true);
                importnode.AppendChild(newnode);
            }

            node.ParentNode.AppendChild(importnode);
        }

        private bool userRequiresCert(XmlNode node)
        {
            var att = node.SelectSingleNode("@wbt:loggedonuser", NSMGR);
            if (att != null && att.Value == "true" && !LoggedOnUser)
                return true;

            att = node.SelectSingleNode("@wbt:authenticateduser", NSMGR);
            if (att != null && att.Value == "true" && !AuthenticatedUser)
                return true;


            return false;

        }

        private void wbtQuery(XmlNode current)
        {
            AssertProtectedContent(current);

            // File resource queries
            var qresources = _resources.collectResources("queries", ".xml");
            qresources.AddRange(_resources.collectResources(Utility.Paths.CombinePaths(ResourceContentPath, "queries"), ".xml"));

            if ((qresources == null || qresources.Count == 0) && (current.SelectSingleNode("descendant-or-self::wbt:ProjSql | descendant-or-self::wbt:query", NSMGR) == null))
                return;

            #region User Cert required test
            if (userRequiresCert(current))
                return;
            #endregion

            // TODO: this become global and USECDN
            var projsqlnode = current.SelectSingleNode("descendant-or-self::wbt:ProjSql", NSMGR);
            string projsqldoc = (projsqlnode != null && projsqlnode.Attributes["doc"] != null) ? projsqlnode.Attributes["doc"].Value : "ProjectSql.xml";
            var projsqlpath = (_useCdn) ? Utility.Paths.CombinePaths(_resources.Host, ConfigurationManager.AppSettings["project-sql-path"], projsqldoc) : Path.Combine(Server.MapPath(ConfigurationManager.AppSettings["project-sql-path"]), projsqldoc);
            var projsql = new Schema.Extra.commonProj(projsqlpath);

            ProjectFlx.DB.DatabaseConnection db;
            if (projsql.ProjSqlNode.Attributes["conn-name"] != null)
                db = new DB.DatabaseConnection(ConfigurationManager.ConnectionStrings[projsql.ProjSqlNode.Attributes["conn-name"].Value].ConnectionString);
            else
                db = new DB.DatabaseConnection();

            ProjectFlx.Schema.projectResults result;
            ProjectFlx.DB.SchemaBased.DatabaseQuery dbq = new ProjectFlx.DB.SchemaBased.DatabaseQuery(db, result = new ProjectFlx.Schema.projectResults());
            if (Timing != null)
                dbq.Timing = Timing;

            if (projsql == null)
                return;

            if (ConfigurationManager.AppSettings["validation-regx"] != null)
            {
                projsql.setRegX(Server.MapPath(ConfigurationManager.AppSettings["validation-regx"]));
            }

            TMPLT.AddXslParameter("projSql", projsql.ProjSqlNode);

            wbtProjSql(current, projsql);

            bool isUpdateQuery = false;

            // handle update, inserts, deletes
            if (RequestType == enumRequestType.POST)
            {
                var qproj = Request.Form["wbt_execute_project"];
                var qquery = Request.Form["wbt_execute_query"];

                if(qproj == null)
                    qproj = Request.Form["wbt_update_project"];

                if (qquery == null)
                    qquery = Request.Form["wbt_update_query"];

                if (!(String.IsNullOrEmpty(qproj) || String.IsNullOrEmpty(qquery)))
                {
                    isUpdateQuery = true;

                    projsql.setProject(qproj);
                    projsql.setQuery(qquery);
                    projsql.fillParms(Request.Form);

                    try
                    {
                        projsql.checkInputParms();

                        if (projsql.SchemaQuery.scripttimeoutSpecified)
                            Server.ScriptTimeout = projsql.SchemaQuery.scripttimeout;

                        dbq.Query(projsql);
                    }
                    catch (Exception unhandled)
                    {
                        TMPLT.AddException(unhandled);
                    }
                }
            }

            foreach (string s in qresources)
            {                
                var xm = new XmlDocument();
                xm.Load((_useCdn) ? Utility.Paths.CombinePaths(_resources.Host, s) : Server.MapPath(s));

                XmlNode q = xm.SelectSingleNode("wbt:query", NSMGR);

                projsql.setProject(q.Attributes["project"].Value);
                projsql.setQuery(q.Attributes["query"].Value);

                foreach (XmlNode parm in q.SelectNodes("parameters/parameter"))
                    projsql.setParameter(parm.Attributes["name"].Value, getValueFromWbtParm(parm.InnerText));

                projsql.fillParms(Request.QueryString);
                if(!isUpdateQuery)      // form vars reserved for update query actions
                    projsql.fillParms(Request.Form);

                try
                {
                    projsql.checkInputParms();

                    if (projsql.SchemaQuery.scripttimeoutSpecified)
                        Server.ScriptTimeout = projsql.SchemaQuery.scripttimeout;

                    dbq.Query(projsql);
                }
                catch (Exception unhandled)
                {
                    TMPLT.AddException(unhandled);
                }

            }


            // embeded queries (action result only)
            var queries = current.SelectNodes("descendant-or-self::wbt:query", NSMGR);
            TMPLT.AddCookie("wbt_edits_token", Guid.NewGuid().ToString(), DateTime.Now.AddMinutes(3), true);

            foreach (XmlNode q in queries)
            {
                var logOnNode = q.SelectSingleNode("ancestor-or-self::*[@loggedonuser = 'true'] | ancestor-or-self::*[@wbt:loggedonuser = 'true'] | ancestor-or-self::LoggedOnUser | ancestor-or-self::LoggedOn | ancestor-or-self::LoggedIn | ancestor-or-self::LoggedIn", NSMGR);
                if (logOnNode != null && !LoggedOnUser)
                    continue;

                var authNode = q.SelectSingleNode("ancestor-or-self::*[@authenticated = 'true'] | ancestor-or-self::*[@wbt:authenticateduser = 'true'] | ancestor-or-self::AuthenticatedUser", NSMGR);
                if (authNode != null && !AuthenticatedUser)
                    continue;

                projsql.setProject(q.Attributes["project"].Value);
                if (q.Attributes["query"] != null)
                {
                    projsql.setQuery(q.Attributes["query"].Value);
                }
                else
                {
                    if(q.SelectNodes("fields/field").Count > 0)
                        projsql.setQuery(ProjectFlx.Schema.SchemaQueryType.Create(q.Attributes["name"].Value, Schema.actionType.Result, q.Attributes["name"].Value, q.SelectSingleNode("text").InnerText, q.SelectNodes("fields/field")));
                    else
                        projsql.setQuery(ProjectFlx.Schema.SchemaQueryType.Create(q.Attributes["name"].Value, Schema.actionType.Result, q.Attributes["name"].Value, q.SelectSingleNode("text").InnerText));
                    
                }


                // If NonQuery or Scalar querie continue
                if ((projsql.SchemaQuery.command.action == Schema.actionType.NonQuery || projsql.SchemaQuery.command.action == Schema.actionType.Scalar))
                    continue;

                // embed parameters
                foreach (XmlNode parm in q.SelectNodes("parameters/parameter"))
                    projsql.setParameter(parm.Attributes["name"].Value, getValueFromWbtParm(parm.InnerText));

                // oerriden by query parameter
                projsql.fillParms(Request.QueryString);

                projsql.fillParms(Request.QueryString);
                if (!isUpdateQuery)      // form vars reserved for update query actions
                    projsql.fillParms(Request.Form);

                try
                {
                    projsql.checkInputParms();

                    if (projsql.SchemaQuery.scripttimeoutSpecified)
                        Server.ScriptTimeout = projsql.SchemaQuery.scripttimeout;

                    dbq.Query(projsql);
                }
                catch(Exception unhandled)
                {
                    TMPLT.AddException(unhandled);
                }
            }

            if (result.results.Count > 0)
                TMPLT.AddWBTXml(result.Serialize());
        }

        private void AssertProtectedContent(XmlNode current)
        {
            // check if logged on user required
            var contentnode = current.SelectSingleNode("content");

            if (contentnode != null)
            {
                var att = contentnode.Attributes["loggedonuser"];

                if (att != null && att.Value == "true")
                    if (!LoggedOnUser)
                    {
                        var contentname = contentnode.Attributes["name"];
                        var exargs = new ProjectExceptionArgs("Protected Page Content: " + contentname.Value);
                        throw new ProjectException(exargs);
                    }
            }
        }

        private string getValueFromWbtParm(string parm)
        {
            string pattern = @"\A(?:\{(cookie|queryvar|querystring|query|form|session):(.+)\})\Z";
            string val = parm;

            if (Regex.IsMatch(parm, pattern))
            {
                var search = Regex.Match(parm, pattern).Groups[1].Value;
                switch(search)
                {
                    case "form":
                        val = TMPLT.LookupFormVars(Regex.Match(parm, pattern).Groups[2].Value);
                        break;
                    case "queryvar":
                    case "querystring":
                    case "query":
                        val = TMPLT.LookupQueryVars(Regex.Match(parm, pattern).Groups[2].Value);
                        break;
                    case "cookie":
                        val = TMPLT.LookupCookieVars(Regex.Match(parm, pattern).Groups[2].Value);
                        break;
                    case "session":
                        val = Session[Regex.Match(parm, pattern).Groups[2].Value].ToString();
                        break;
                }
            }

            return val;
        }

        private XmlDocument getXmlContext(XmlDocument content)
        {
            XmlDocument result = new XmlDocument();
            var sb = new StringBuilder();
            XmlWriterSettings settings = new XmlWriterSettings() { Indent = false, OmitXmlDeclaration = true };
            var writer = XmlWriter.Create(sb, settings);

            var mem = new MemoryStream();
            content.Save(mem);
            mem.Flush();
            mem.Seek(0, SeekOrigin.Begin);
            var settingsr = new XmlReaderSettings() { IgnoreProcessingInstructions = true, IgnoreWhitespace = true };
            var reader = XmlReader.Create(mem, settingsr);

            writer.WriteStartDocument();
            int readidx = 0;
            while (reader.Read())
            {
                readidx++;
                if ((reader.NodeType == XmlNodeType.Element || reader.NodeType == XmlNodeType.EndElement) && ((reader.LocalName == "page" || reader.LocalName == "content")))
                {
                    if (reader.IsStartElement())
                    {
                        writer.WriteStartElement(reader.LocalName);

                        if (reader.HasAttributes)
                        {
                            reader.MoveToFirstAttribute();
                            writer.WriteAttributeString(reader.LocalName, reader.Value);

                            while (reader.MoveToNextAttribute())
                                writer.WriteAttributeString(reader.LocalName, reader.Value);

                            reader.MoveToElement();
                        }

                    }

                    if (reader.IsEmptyElement || reader.NodeType == XmlNodeType.EndElement)
                        writer.WriteEndElement();
                }

                if ((reader.NodeType == XmlNodeType.Element || reader.NodeType == XmlNodeType.EndElement) && Regex.Match(reader.LocalName, "[hH][1-6]").Success)
                {
                    writer.WriteStartElement(reader.LocalName);
                    var sub = reader.ReadSubtree();
                    while(sub.Read())
                    {
                        if (sub.NodeType == XmlNodeType.Text)
                            writer.WriteValue(sub.Value);

                        if (sub.Depth == 0 && (sub.NodeType == XmlNodeType.EndElement || sub.IsEmptyElement))
                            writer.WriteEndElement();

                    };

                }                
            }
            writer.WriteEndDocument();
            writer.Flush();
            result.LoadXml(sb.ToString());

            return result;
        }


        /// <summary>
        /// Resolves Xml Content for the current page request and returns the relative location where it was resolved
        /// </summary>
        /// <param name="content"></param>
        /// <param name="LocalPath"></param>
        /// <returns></returns>
        private string getXmlResources(XmlDocument content, ref XmlNode current)
        {
            var mapResource = default(string);

            try
            {
                if (!_projFlxUseCache)
                {
                    try
                    {
                        foreach (var key in _cacheKeys)
                            _cache.Remove(key.Value);
                    }
                    catch { }
                }
                if (_projFlxUseCache)
                {


                    if (CacheContains(
                        CacheKeyEnum.XmlCacheKey, 
                        CacheKeyEnum.XslCacheKey,
                        CacheKeyEnum.ContentPathKey,
                        CacheKeyEnum.ContextCacheKey))
                    {
                        var xm = GetCache<XmlDocument>(CacheKeyEnum.XmlCacheKey);
                        var importnode = content.ImportNode(xm.DocumentElement, true);

                        content.AppendChild(importnode);
                        current = GetCache<XmlNode>(CacheKeyEnum.ContextCacheKey);

                        TMPLT.setXslt(GetCache(CacheKeyEnum.XslCacheKey));
                        //if (!((string)_cache[_cacheKeys[CacheKeyEnum.XslCacheKey]] == "--DEFAULT--"))                 // no stylesheet resolved, this falls back to a default stylesheet
                        //    TMPLT.setXslt((string)_cache[_cacheKeys[CacheKeyEnum.XslCacheKey]]);

                        if (CacheContains(CacheKeyEnum.PHCacheKey))
                        {
                            _pageHeirarchy = new List<string>();
                            TMPLT.ClearBrowserpageItem("PAGE_HEIRARCHY");
                            var browseritems = GetCache<XmlNode>(CacheKeyEnum.PHCacheKey);
                            foreach (XmlNode phnode in browseritems.SelectNodes("item"))
                            {
                                _pageHeirarchy.Add(phnode.InnerText);
                                TMPLT.AddBrowserPageItem("PAGE_HEIRARCHY", phnode.InnerText);
                            }
                        }
                        return GetCache(CacheKeyEnum.ContentPathKey);
                    }
                }

                bool foundXml = false;
                bool foundXsl = false;
                string resultcontentpath = null;
                string xslpath = null;
                string localxmlpath = null;

                //burn down approach, looking for relative content
                for (int x = _pageHeirarchy.Count; x > 0; x--)
                {
                    if (!foundXml)
                    {
                        localxmlpath = String.Join("/", _pageHeirarchy.ToArray(), 0, x);
                        for (int i = _pageHeirarchy.Count; i > 0; i--)
                        {
                            if (foundXml)
                                break;

                            string resourcePath;
                            resourcePath = String.Format("{0}/{1}.xml", localxmlpath, _pageHeirarchy[_pageHeirarchy.Count - 1]);

                            foundXml = _resources.Exists(resourcePath);

                            if(!foundXml)
                            {
                                if (i == 4)
                                    resourcePath = String.Format("{0}/{1}.{2}.{3}.{4}.xml", localxmlpath, _pageHeirarchy[0], _pageHeirarchy[1], _pageHeirarchy[2], _pageHeirarchy[3]);
                                else if (i == 3)
                                    resourcePath = String.Format("{0}/{1}.{2}.{3}.xml", localxmlpath, _pageHeirarchy[0], _pageHeirarchy[1], _pageHeirarchy[2]);
                                else if (i == 2)
                                {
                                    resourcePath = String.Format("{0}/{1}.{2}.xml", localxmlpath, _pageHeirarchy[0], _pageHeirarchy[1]);
                                    foundXml = _resources.Exists(resourcePath);
                                    if(!foundXml)
                                        resourcePath = String.Format("{0}/{2}.xml", localxmlpath, _pageHeirarchy[0], _pageHeirarchy[1]);
                                }
                                else
                                    resourcePath = String.Format("{0}/{1}.xml", localxmlpath, _pageHeirarchy[0]);

                                foundXml = foundXml || _resources.Exists(resourcePath);
                            }

                        }

                        if(foundXml)
                        {
                            // if page heirarchy doesn't match found xml, map the resource
                            if(!_pageHeirarchy.Count.Equals(x))
                            {
                                // CONSIDER: assuming separator is a . but should be configurable
                                var temp1 = PageHeirarchyCombined.Split('.').Take(x).ToArray();
                                mapResource = String.Join(".", temp1);
                            }

                            resultcontentpath = localxmlpath;
                            if (_useCdn)
                                content.Load(_resources.FullWebPath(_resources.IndexOf) + "?timestamp" + DateTime.Now.ToString("yyyyMMddHHmmssffff"));
                            else
                                content.Load(Server.MapPath(_resources.AbsolutePath(_resources.IndexOf)));
                        }
                    }

                    if (!foundXsl)
                    {
                        localxmlpath = String.Join("/", _pageHeirarchy.ToArray(), 0, x);
                        for (int i = _pageHeirarchy.Count; i > 0; i--)
                        {
                            if (foundXsl)
                                break;

                            string resourcePath;
                            resourcePath = String.Format("{0}/{1}.xsl", localxmlpath, _pageHeirarchy[_pageHeirarchy.Count - 1]);

                            foundXsl = _resources.Exists(resourcePath);

                            if (!foundXsl)
                            {
                                if (i == 4)
                                    resourcePath = String.Format("{0}/{1}.{2}.{3}.{4}.xsl", localxmlpath, _pageHeirarchy[0], _pageHeirarchy[1], _pageHeirarchy[2], _pageHeirarchy[3]);
                                else if (i == 3)
                                    resourcePath = String.Format("{0}/{1}.{2}.{3}.xsl", localxmlpath, _pageHeirarchy[0], _pageHeirarchy[1], _pageHeirarchy[2]);
                                else if (i == 2)
                                {
                                    resourcePath = String.Format("{0}/{1}.{2}.xsl", localxmlpath, _pageHeirarchy[0], _pageHeirarchy[1]);
                                    foundXsl = _resources.Exists(resourcePath);
                                    if (!foundXsl)
                                        resourcePath = String.Format("{0}/{2}.xsl", localxmlpath, _pageHeirarchy[0], _pageHeirarchy[1]);
                                }
                                else
                                    resourcePath = String.Format("{0}/{1}.xsl", localxmlpath, _pageHeirarchy[0]);

                                foundXsl = foundXsl || _resources.Exists(resourcePath);
                            }

                        }

                        if (foundXsl)
                        {
                            resultcontentpath = localxmlpath;
                            xslpath = _resources.FullWebPath(_resources.IndexOf) + "?timestamp" + DateTime.Now.ToString("yyyyMMddHHmmssffff");
                            break;
                        }
                    }
                }

                // look for default content in root client projectFlx path considering a default as well
                if (!foundXml || !foundXsl)
                {
                    if(!foundXml)
                        resultcontentpath = localxmlpath = "";

                    string[] resource = new string[] { _pageHeirarchy[0], "default" };
                    foreach (string s in resource)
                    {
                        if (!foundXml && _resources.Exists(String.Format("{0}.xml", s)))
                        {
                            if (_useCdn)
                                content.Load(_resources.FullWebPath(_resources.IndexOf) + "?timestamp" + DateTime.Now.ToString("yyyyMMddHHmmssffff"));
                            else
                                content.Load(Server.MapPath(_resources.AbsolutePath(_resources.IndexOf)));

                            mapResource = s;
                            foundXml = true;
                        }

                        if (foundXml && _resources.Exists(String.Format("{0}.xsl", s)))
                            if (_useCdn)
                                xslpath = _resources.FullWebPath(_resources.IndexOf);
                            else
                                xslpath = Server.MapPath(_resources.AbsolutePath(_resources.IndexOf));
                    }
                }

                if (foundXml)
                {
                    if (String.IsNullOrEmpty(xslpath))
						TMPLT.setXslt(Path.Combine(_projectFlxPath, "Documents/WEB_DOCUMENT_TEMPLATE.xsl"));
					else
						TMPLT.setXslt(xslpath);
                }
                else
                {
                    if (!foundXml && File.Exists(Path.Combine(_projectFlxPath, "content/projectflx.xml")))
                    {
                        content.Load(Path.Combine(_projectFlxPath, "content/projectflx.xml"));
                        TMPLT.setXslt(Path.Combine(_projectFlxPath, "Documents/WEB_DOCUMENT_TEMPLATE.xsl"));
                    }
                }

                // context for the current page
                var context2 = (XmlNode)content.DocumentElement.Clone();
                if (context2 == null)
                    throw new Exception("Failed to load content for the request");

                string xpath;
                _pageHeirarchy.ForEach(pg =>
                {
                    xpath = String.Format("page[translate(@name, 'ABCDEFGHIJKLMNOPQRSTUVWXYZ', 'abcdefghijklmnopqrstuvwxyz')='{0}'] | content[translate(@name,'ABCDEFGHIJKLMNOPQRSTUVWXYZ', 'abcdefghijklmnopqrstuvwxyz')='{0}']", pg);
                    if (context2.SelectSingleNode(xpath) != null)
                        context2 = context2.SelectSingleNode(xpath);
                    else
                        return;
                });

                current = context2;

                // strip child context nodes
                foreach (XmlNode node in current.SelectNodes("content"))
                    current.RemoveChild(node);

                if (_projFlxUseCache)
                {
                    if(!String.IsNullOrEmpty(mapResource))
                    {
                        // TODO: map this
                        MapPage(mapResource);
                        setupCacheKey(CacheKeyEnum.XmlCacheKey, mapResource);
                        setupCacheKey(CacheKeyEnum.XslCacheKey, mapResource);
                        setupCacheKey(CacheKeyEnum.ContextCacheKey, mapResource);
                        setupCacheKey(CacheKeyEnum.PHCacheKey, mapResource);
                        setupCacheKey(CacheKeyEnum.ScriptCacheKey, mapResource);
                        setupCacheKey(CacheKeyEnum.ContentPathKey, mapResource);
                        setupCacheKey(CacheKeyEnum.WaitClosureKey, mapResource);
                    }

                    SaveCache(CacheKeyEnum.PHCacheKey, TMPLT.DOCxml.SelectSingleNode("flx/proj/browser/page/PAGE_HEIRARCHY"), _cacheContentMinutes);
                    SaveCache(CacheKeyEnum.XmlCacheKey, content, _cacheContentMinutes);
                    SaveCache(CacheKeyEnum.ContextCacheKey, current, _cacheContentMinutes);
                    SaveCache(CacheKeyEnum.XslCacheKey, String.IsNullOrEmpty(xslpath) ? "--DEFAULT--" : xslpath, _cacheContentMinutes);
                    SaveCache(CacheKeyEnum.ContentPathKey, resultcontentpath, _cacheContentMinutes);
                }

                return resultcontentpath;
            }
            catch (Exception unhandled)
            {
                var args = new ProjectExceptionArgs("Unhandled Exception Caught", "ProjectFlx.FlxMain", "getXmlResources(XmlDocument, XmlNode)", null, SeverityLevel.Critical, LogLevel.Debug);
                throw new ProjectException(args, unhandled);
            }
        }

        public static Dictionary<string, string> MapPageKeys = new Dictionary<string, string>();
        private void MapPage(string NewPageHeirarchy)
        {
            lock(_lock)
            {
                if (MapPageKeys.Keys.Any(a => a.Equals(PageHeirarchyCombined)))
                    MapPageKeys.Remove(PageHeirarchyCombined);

                MapPageKeys.Add(PageHeirarchyCombined, NewPageHeirarchy);
            }
        }

        /// <summary>
        /// Execute Main code for Site
        /// </summary>
        public virtual void SITE_MAIN()
        {
            try
            {
                Timing.Start("ProjectFlx.FlxMain.SITE_MAIN");
                if (_SiteMap)
                {

                    if (_useCdn)
                    {
                        TMPLT.setXslt(Utility.Paths.CombinePaths(_resources.Host, "ProjectFLX/Documents/WBT_SITE_MAP.xsl"));
                        recurseForSiteMap(_resources);
                    }
                    else
                    {

                        TMPLT.setXslt(Path.Combine(Server.MapPath("/"), "ProjectFLX/Documents/WBT_SITE_MAP.xsl"));
                        DirectoryInfo projectFlx = new DirectoryInfo(Server.MapPath(_clientFlxpath));
                        recurseForSiteMap(projectFlx);
                    }

                    // loop every file

                    // loop every directory

                    if (PageHeirarchyCombined.Equals("sitemapxml"))
                    {
                        Response.Clear();
                        Response.ContentType = "text/xml";
                        TMPLT.AddXslParameter("OUT", "XML");
                        TMPLT.ProcessTemplateExternal();
                        Response.Write(TMPLT.Result);
                        Response.Flush();
                        ClearProcess = true;
                    }
                    return;
                }

                try
                {
                    Timing = Timing.New("ProjectFlx.FlxMain.PAGE");

                    var current = TMPLT.DOCxml.SelectSingleNode("/flx/client");
                    wbtQuery(current);

                    // Page Initialize - overrideable
                    PAGE_INIT();

                    // Main Call (3 of 3) - Call Page Main
                    PAGE_MAIN();

                    // Page Terminate Event - overrideable
                    PAGE_TERMINATE();
                }
                finally
                {
                    Timing = Timing.End("ProjectFlx.FlxMain.PAGE");
                }
            }
            finally
            {
                Timing.Stop("ProjectFlx.FlxMain.SITE_MAIN");
            }
        }

        private void recurseForSiteMap(FileResources resources)
        {
            foreach (var s in resources.collectResources(".xml"))
            {
                TMPLT.AddXML(Utility.Web.getWebResource(Utility.Paths.CombinePaths(_resources.Host, s)));
            }
        }

        private void recurseForSiteMap(DirectoryInfo projectFlx)
        {
            // recurse files
            foreach (FileInfo f in projectFlx.GetFiles("*.xml"))
            {
                using (StreamReader r = new StreamReader(f.FullName))
                {
                    TMPLT.AddXML(r.ReadToEnd());
                }
            }

            // recurse folders
            foreach (DirectoryInfo d in projectFlx.GetDirectories())
                recurseForSiteMap(d);
            
        }

        /// <summary>
        /// Execute Site Termination Code
        /// </summary>
        public virtual void SITE_TERMINATE()
        {
            try
            {
                Timing.Start("ProjectFlx.FlxMain.SITE_TERMINATE");

                TMPLT.AddCommentTag("_useCdn", _useCdn, "FlxWebSite", "SITE_TERMINATE");
                TMPLT.AddCommentTag("_debug", _debug, "FlxWebSite", "SITE_TERMINATE");
                TMPLT.AddCommentTag("_projectFlxPath", _projectFlxPath, "FlxWebSite", "SITE_TERMINATE");
                TMPLT.AddCommentTag("_requestType", _requestType, "FlxWebSite", "SITE_TERMINATE");
                TMPLT.AddCommentTag("_SiteMap", _SiteMap, "FlxWebSite", "SITE_TERMINATE");
                TMPLT.AddCommentTag("_projFlxUseCache", _projFlxUseCache, "FlxWebSite", "SITE_TERMINATE");
                TMPLT.AddCommentTag("_clientFlxpath", _clientFlxpath, "FlxWebSite", "SITE_TERMINATE");
                TMPLT.AddCommentTag("_cdnSite", _cdnSite, "FlxWebSite", "SITE_TERMINATE");
                TMPLT.AddCommentTag("_AuthenticatedUser", _AuthenticatedUser, "FlxWebSite", "SITE_TERMINATE");
                if (_SiteMap)
                    return;

                setupScript();
            }
            finally
            {
                Timing.Stop("ProjectFlx.FlxMain.SITE_TERMINATE");
            }

        }

        private void setupScript()
        {
            // TODO: need global variable set that will eliminate concurrent calls of this

            if (!(_projFlxUseCache && _projFlxUseCacheScript)) return;

            if (CacheContains(CacheKeyEnum.WaitClosureKey))     // waiting on too many compiles
                return;

            Timing.Start("setupScript");

            // TODO: this remains cached even after deleting dependent user file
            if (CacheContains(CacheKeyEnum.ScriptCacheKey))
            {
                var gc = GetCache<closure.ClosureCompiler>(CacheKeyEnum.ScriptCacheKey);
                TMPLT.DOCxml.SelectSingleNode("/flx/proj/browser/page/SCRIPT").RemoveAll();
                TMPLT.AddXML(gc.Xml);
                Timing.End("setupScript");
                return;
            }

            try
            {
                // TODO: closure or no closure

                var list = new List<closure.ClosureCompilerFile>();
                foreach (XmlNode node in TMPLT.DOCxml.SelectNodes("/flx/proj/browser/page/SCRIPT/item"))
                    list.Add(new closure.ClosureCompilerFile(node.InnerText, Server.MapPath("/")));

                var outputs = new List<closure.OutputInfo>();
                outputs.Add(closure.OutputInfo.compiled_code);
                outputs.Add(closure.OutputInfo.errors);
                outputs.Add(closure.OutputInfo.statistics);
                outputs.Add(closure.OutputInfo.warnings);

                if (closure.ClosureCompiler.Instance == null)
                {
                    var gc = closure.ClosureCompiler.NewSealedInstance;
                    try
                    {
                        TMPLT.AddTag("GCCompiler", "Running");
                        gc.Run(
                            list,
                            "projectflx_compiled.js",
                            closure.WarningLevel.VERBOSE,
                            closure.CompilationLevel.SIMPLE_OPTIMIZATIONS,
                            outputs,
                            null,
                            false);

                        if(gc.Xml != null)
                            TMPLT.AddXML(gc.Xml);

                        if (!gc.Success)
                        {
                            if (gc.TooManyCompiles)
                            {
                                SaveCache(CacheKeyEnum.WaitClosureKey, DateTime.UtcNow, 60);       // TODO: consider new timing for this - save longer than normal??
                                TMPLT.AddTag("ClosureCompiler", "Too Many Compiles");
                            }
                            else
                                throw new ProjectException("JS Code Compile Issue");
                        }

                        SaveCache(CacheKeyEnum.ScriptCacheKey, gc, _cacheJscriptMinutes);
                        TMPLT.DOCxml.SelectSingleNode("/flx/proj/browser/page/SCRIPT").RemoveAll();
                    }
                    finally
                    {
                        if (gc != null)
                            gc.Dispose();
                    }
                }
                else
                {
                    TMPLT.AddTag("GCCompiler", "Skipped");
                }
            }
            catch (ProjectException handled)
            {
                TMPLT.AddException(handled);
            }
            catch (Exception unhandled)
            {
                var args = new ProjectExceptionArgs("Unhandled Exception Caught in FlxWebsite setupScript", "ProjectFlx.FlxMain", "SITE_TERMINATE", null, SeverityLevel.Critical, LogLevel.Debug);
                throw new ProjectException(args, unhandled);
            }
            finally
            {
                Timing.End("setupScript");
            }

        }

        protected void ClearCache()
        {
            try
            {
                foreach (var cache in _cache)
                    _cache.Remove(cache.Key);

                if (!String.IsNullOrEmpty(_cacheDependencyPath))
                {
                    var files = Directory.GetFiles(_cacheDependencyPath, "*.cache");
                    foreach (var file in files)
                    {
                        File.Delete(file);
                    }
                }
            }
            catch { }
        }
        protected void ClearCache(String StartsWith)
        {
            foreach (var cache in _cache.Where(w => w.Key.StartsWith(StartsWith)).ToList())
                _cache.Remove(cache.Key);
        }
        protected void ClearCache(Regex RegX)
        {
            foreach (var cache in _cache.Where(w => RegX.IsMatch(w.Key)).ToList())
                _cache.Remove(cache.Key);
        }

        protected T GetCache<T>(CacheKeyEnum Key)
        {
            return GetCache<T>(_cacheKeys[Key]);
        }

        protected string GetCache(CacheKeyEnum Key)
        {
            return GetCache(_cacheKeys[Key]);
        }

        protected string GetCache(String Key)
        {
            if (_cache == null)
                return null;

            try
            {
                // cache
                var obj = _cache[Key];
                return (string)obj;
            }
            catch
            {
                return null;
            }
        }

        protected T GetCache<T>(String Key)
        {
            if (_cache == null)
                return default(T);

            try
            {
                // cache
                var obj = _cache[Key];
                return (T)obj;
            }
            catch
            {
                return default(T);
            }
        }

        protected bool CacheContains(params CacheKeyEnum[] Key)
        {
            var result = true;
            foreach(var k in Key)
            {
                result = CacheContains(_cacheKeys[k]);
                if (!result)
                    break;
            }
            return result;
        }

        protected bool CacheContains(String Key)
        {
            if (_cache == null)
                return false;

            return (_cache[Key] == null) ? false : true;
        }
        
        protected void SaveCache(CacheKeyEnum Key, Object Value, double CacheMinutes)
        {
            SaveCache(_cacheKeys[Key], Value, CacheMinutes);
        }

        private static object _lock = new object();

        protected void SaveCache(string Key, Object Value, double CacheMinutes, bool ForceUseCache = false)
        {
            if (!_projFlxUseCache && !ForceUseCache)
                return;

            lock (_lock)
            {
                if (_cache == null)
                    return;

                var cachedependency = new CacheItemPolicy();
                if (!String.IsNullOrEmpty(this._cacheDependencyPath))
                {
                    var filename = (Key.IndexOf(':') > 0 ? Key.Substring(Key.IndexOf(':')) : Key).Split(Path.GetInvalidFileNameChars());
                    var keypath = string.Concat(filename);
                    if (keypath.Length > 100) keypath = keypath.Substring(0, 100);
                    string depFile = Path.Combine(_cacheDependencyPath, keypath + ".cache");
                    using (StreamWriter writer = new StreamWriter(depFile))
                    {
                        writer.WriteLine(DateTime.Now.ToString("yyyyMMddHHmmssffff"));
                    }
                    List<string> files = new List<string>();
                    files.Add(depFile);
                    cachedependency.AbsoluteExpiration = DateTime.Now.AddMinutes(CacheMinutes);
                    cachedependency.ChangeMonitors.Add(new HostFileChangeMonitor(files));
                }

                var cache = new CacheItem(Key, Value);
                _cache.Set(cache, cachedependency);
            }
        }

        public virtual void PAGE_INIT()
        {
        }

        public virtual void PAGE_MAIN()
        {
        }

        public virtual void HandleUnHandledErrors(Exception e)
        {
            _HandleUnHandledErrors(e);
        }

        protected void _HandleUnHandledErrors(Exception e)
        {
            if (TMPLTUnhandledException != null)
                TMPLTUnhandledException(e);

            // abort when client not connected
            if (!Response.IsClientConnected)
                return;

            Response.Write(@"<html><body>");
            Response.Write("<div style='margin-top:25px; padding:25px; text-align:center; font-family:arial; border:solid 1px #999'>");
            Response.Write("<div>" + e.Message + "</div>");

            Exception innerEx = e.InnerException;

            while (innerEx != null)
            {
                Response.Write("<div style='margin-top:10px; padding-top:5px; font-family:arial;'>");
                Response.Write(innerEx.Message);
                Response.Write(@"</div>");

                innerEx = innerEx.InnerException;
            }

            if (e.StackTrace != null)
            {
                Response.Write("<div style='margin-top:10px; padding-top:5px; font-family:arial;'>");
                Response.Write(e.StackTrace.ToString());
                Response.Write(@"</div>");
            }

            if (_handler.XmlDocument != null)
            {
                XmlNodeList l = _handler.XmlDocument.SelectNodes("ERRORS/ERROR");

                foreach (XmlElement m in l)
                {
                    Response.Write("<div style='margin-top:10px; font-family:arial;'>");
                    Response.Write(string.Concat("Class: ", m.GetAttribute("Class").ToString()));
                    Response.Write(string.Concat("Method: ", m.GetAttribute("Method").ToString()));
                    Response.Write(string.Concat("Severity: ", m.GetAttribute("Severity").ToString()));
                    Response.Write(@"</div>");
                    Response.Write("<div style='margin-top:10px; font-family:arial;'>");
                    Response.Write(string.Concat("Error: ", m.InnerText));
                    Response.Write(@"</div>");
                }
            }

            Response.Write(@"</div></body></html>");
        }

        public virtual void PAGE_TERMINATE()
        {
        }

        public string CurrentPage
        {
            get
            {
                return clsCurrentPage;
            }
            set
            {
                clsCurrentPage = value;
            }
        }
        public string PageName
        {
            get
            {
                return _pageHeirarchy[0];
            }
        }
        public string PageMajorAction
        {
            get
            {
                if(_pageHeirarchy.Count > 1)
                    return _pageHeirarchy[1];
                else 
                    return null;
            }
        }
        public string PageMinorAction
        {
            get
            {
                if (_pageHeirarchy.Count > 2)
                    return _pageHeirarchy[2];
                else
                    return null;
            }
        }
        public string PageSubMinorAction
        {
            get
            {
                if (_pageHeirarchy.Count > 3)
                    return _pageHeirarchy[3];
                else
                    return null;
            }
        }

        public bool ClearProcess
        {
            get
            {
                return clsClearProcess;
            }
            set
            {
                clsClearProcess = value;
            }

        }

        public event FlxTWebSiteExceptions TMPLTUnhandledException;

        public bool IsReusable
        {
            get
            {
                return false;
            }
        }

        public void ProcessRequest(HttpContext context)
        {
            Current = context;
            Server = context.Server;
            Response = context.Response;
            Request = context.Request;
            Session = context.Session;
            Main();
        }

        public string PageHeirarchyCombined
        {
            get
            {
                StringBuilder sb = new StringBuilder();
                int i = 0;
                foreach (string s in _pageHeirarchy)
                {
                    if (!String.IsNullOrEmpty(s))
                    {
                        // CONSIDER: assumes the page delimiter is a . this should be configured by user
                        if (i > 0)
                            sb.Append(".");
                        sb.Append(s);
                    }
                    i++;
                }

                return sb.ToString();
            }
        }

        public bool PageHeirarchyContains(string Value)
        {
            var s =_pageHeirarchy.Find(f => f.Equals(Value.ToLower(), StringComparison.CurrentCultureIgnoreCase));
            return !String.IsNullOrEmpty(s);
        }

        public int PageHeirarchyCount
        {
            get
            {
                return _pageHeirarchy.Count;
            }
        }

        public static string getUserIP()
        {
            var context = HttpContext.Current;
            var forward = context.Request.ServerVariables.Get("HTTP_X_FORWARDED_FOR");
            var ip = context.Request.ServerVariables.Get("REMOTE_ADDR");

            if (!String.IsNullOrEmpty(forward))
            {
                try
                {
                    var a = forward.Split(',');
                    if (a.Length > 0)
                        ip = a[0];
                }
                catch { }
            }

            return ip;
        }

        FileResources _resources = null;
        private bool _projFlxUseCache;
        private bool _projFlxUseCacheScript;
        private double _cacheJscriptMinutes;
        private double _cacheContentMinutes;
        private bool _SiteMap;
        private bool _validUserAgent;
        protected FileResources ProjectFlxResources
        {
            get
            {
                return _resources;
            }
            set
            {
                _resources = value;
            }
        }

        public XmlNamespaceManager NSMGR { get; private set; }
        public string ResourceContentPath { get; private set; }
		protected List<int> PageRoles { get => _pageRoles; }
		protected List<int> UserRoles
		{
			get => _userRoles; set
			{
				_userRoles = value;

				// persist these to Xml
				JObject jobj = new JObject();
				jobj["role"] = JToken.FromObject(_userRoles);

				TMPLT.AddXML(jobj, "user-roles");
			}
		}

        public string CacheDependencyPath { get => _cacheDependencyPath; set => _cacheDependencyPath = value; }

        private List<int> _pageRoles = new List<int>();
		private List<int> _userRoles = new List<int>();

		/// <summary>
		/// Check that user is in stated roles
		/// This is an assertion method and will throw custom message
		/// </summary>
		/// <param name="Role"></param>
		/// <exception cref="Exceptions.InvalidRolesException"></exception>
		protected virtual void AssertRoles()
		{
			// no foul - no roles to assert
			if (PageRoles.Count == 0)
				return;

			// foul - when user not logged on
			if (!LoggedOnUser)
				throw new Exceptions.LogonRequiredException("Page roles require a logged on user");

			// foul - when logged on user has no roles and there are roles to assert
			if (_userRoles.Count == 0)
				throw new Exceptions.InvalidRolesException(String.Format("No roles ror user found.  Roles required for the page, expecting: {0}", String.Join(",", PageRoles.ToArray())));

			bool hasRole = true;		// assume true - for each page role, user must have that role
										// ex. page role: 10000, 100001 
										// user role: 10000,100001,10003 PASS
										// user role: 10000, 10003 FAIL - expecting role 100001
			foreach (var r in PageRoles)
			{
				hasRole = hasRole && (_userRoles.IndexOf(r) > -1);
			};

			if (!hasRole)
				throw new Exceptions.InvalidRolesException(String.Format("User roles do not meet page roles, expecting: {0}", String.Join(",", PageRoles.ToArray())));
		}

		private void getPageRoles()
		{
			var xpath = "/flx/client/page[@name=/flx/client-context/page/@name]/pbt:Roles[not(preceding-sibling::*)]";
			var nodes = TMPLT.DOCxml.SelectNodes(xpath, NSMGR);
			foreach (XmlNode node in nodes)
			{
				_pageRoles.Add(int.Parse(node.InnerText));
			}
		}

	}
}
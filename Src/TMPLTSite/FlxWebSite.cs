using System;
using System.IO;
using System.Text;
using System.Linq;
using System.Configuration;
using System.Collections;
using System.Collections.Generic;
using System.Web;
using System.Web.Caching;
using System.Xml;
using ProjectFlx.Exceptions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Text.RegularExpressions;


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
        protected Cache Cache;
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

        private enum _cacheKeyEnum { XmlCacheKey, ContextCacheKey, XslCacheKey, PHCacheKey, ScriptCacheKey, WaitClosureKey, ContentPathKey };
        private Dictionary<_cacheKeyEnum, String> _cacheKeys = new Dictionary<_cacheKeyEnum, string>();

        protected Utility.TimingDebugger TimingDebugger { get; set; }
        protected Utility.Timing Timing { get; set; }

        public FlxMain()
        {
            NSMGR = new XmlNamespaceManager(new XmlDocument().NameTable);
            NSMGR.AddNamespace("wbt", "myWebTemplater.1.0");
            NSMGR.AddNamespace("sbt", "mySiteTemplater.1.0");
            NSMGR.AddNamespace("pbt", "myPageTemplater.1.0");

            _useCdn = (!String.IsNullOrEmpty(ConfigurationManager.AppSettings["use-cdn"]) && ConfigurationManager.AppSettings["use-cdn"] == "true");
            _useCache = (!String.IsNullOrEmpty(ConfigurationManager.AppSettings["cache-enabled"]) && ConfigurationManager.AppSettings["cache-enabled"] == "true");
            _cacheMinutes = !String.IsNullOrEmpty(ConfigurationManager.AppSettings["cache-expires-minutes"]) ? int.Parse(ConfigurationManager.AppSettings["cache-expires-minutes"]) : 60;

            _cdnSite = ConfigurationManager.AppSettings["project-cdn-path"];
            _clientFlxpath = ConfigurationManager.AppSettings["projectFlxTemplates"];
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

        private void TMPLT_INIT()
        {
            try
            {
                Timing.Start("ProjectFlx.FlxMain.TMPLT_INIT");
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
                if (!(String.IsNullOrEmpty(ConfigurationManager.AppSettings["banned-ips"])))
                {
                    string remoteip = FlxMain.getUserIP();
                    string[] IPs = ConfigurationManager.AppSettings["banned-ips"].Split(' ');
                    foreach (string ip in IPs)
                    {
                        if (remoteip.StartsWith(ip))
                            Response.Redirect(ConfigurationManager.AppSettings["banned-page"]);
                    }
                }

                if (Convert.ToBoolean(ConfigurationManager.AppSettings["site-down"]) == true)
                    Response.Redirect("Index.html");


                if (ConfigurationManager.AppSettings["debug"] != null)
                    _debug = ConfigurationManager.AppSettings["debug"].Equals("true", StringComparison.OrdinalIgnoreCase);

                TMPLT.AddXslParameter("DEBUG", _debug);

                _projectFlxPath = Server.MapPath("/ProjectFLX");

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

                //_cacheKeys.Add(_cacheKeyEnum.XmlCacheKey, "cache:result__content__path");
                string pagecachkey = PageHeirarchyCombined.Replace("/", "__");
                _cacheKeys.Add(_cacheKeyEnum.XmlCacheKey, "cache:xml__" + pagecachkey);
                _cacheKeys.Add(_cacheKeyEnum.ContextCacheKey, "cache:context__" + pagecachkey);
                _cacheKeys.Add(_cacheKeyEnum.XslCacheKey, "cache:xsl__" + pagecachkey);
                _cacheKeys.Add(_cacheKeyEnum.PHCacheKey, "cache:PH__" + pagecachkey);
                _cacheKeys.Add(_cacheKeyEnum.ScriptCacheKey, "cache:script__" + pagecachkey);
                _cacheKeys.Add(_cacheKeyEnum.ContentPathKey, "cache:contentpath__" + pagecachkey);
                _cacheKeys.Add(_cacheKeyEnum.WaitClosureKey, "cache:pausegoogleclosure__" + pagecachkey);
            }
            finally
            {
                Timing.Stop("ProjectFlx.FlxMain.TMPLT_INIT");
            }

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

                try
                {
                    Timing = Timing.New("ProjectFlx.FlxMain.SITE");
                    // SITE Init - overrideable
                    SITE_INIT();

                    // Main Call (2 of 3) - Call Site Main
                    SITE_MAIN();

                    // Site Terminate Event - overrideable
                    SITE_TERMINATE();
                }
                finally
                {
                    Timing = Timing.End("ProjectFlx.FlxMain.SITE");
                }
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

                    TMPLT.AddTag("CacheCount", Cache.Count);
                    TMPLT.AddTag("CacheEffectivePercentagePhysicalMemoryLimit", Cache.EffectivePercentagePhysicalMemoryLimit.ToString());
                    TMPLT.AddTag("CacheEffectivePrivateBytesLimit", Cache.EffectivePrivateBytesLimit.ToString());
                    IDictionaryEnumerator CacheEnum = Cache.GetEnumerator();
                    while (CacheEnum.MoveNext())
                    {
                        TMPLT.AddTag("CacheKey", CacheEnum.Key.ToString());                        
                    }
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
                    Timing.Start("ProjectFlx.FlxMain.TMPLT_TERMINATE.ProcessTemplate");
                    TMPLT.ProcessTemplate();
                    Timing.End("ProjectFlx.FlxMain.TMPLT_TERMINATE.ProcessTemplate");
                    Response.ContentType = "text/html";
                    Response.Write(TMPLT.Result);
                }
            }
			catch(Exception unhandled)
			{
				_exceptionInTerminate = true;
				throw;
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

        protected bool UseCache
        {
            get { return _useCache; }
            set { _useCache = value; }
        }

        protected string CdnSite
        {
            get { return _cdnSite; }
            set { _cdnSite = value; }
        }

        public virtual void SITE_INIT()
        {
            // Temp
            TMPLT.AddTag("BINGO", "RINGO");

            try
            {
                Timing.Start("ProjectFlx.FlxMain.SITE_INIT");

                // bots are bad, block bots
                List<string> whitelist = new List<string>();
                if (ConfigurationManager.AppSettings["bot-white-list"] != null)
                {
                    if (Cache["bot-white-list"] != null)
                    {
                        whitelist = (List<String>)Cache["bot-white-list"];
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
                            var cachedependency = new CacheDependency(filepath);
                            Cache.Insert("bot-white-list", whitelist, cachedependency, DateTime.Now.AddDays(3), System.Web.Caching.Cache.NoSlidingExpiration);
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

                // TEMP
                TMPLT.AddTag("ResourceContentPath", ResourceContentPath);

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

                #region embed required scripts
                foreach (string pickup in paths)
                {
                    if (pickup == "SKIP__RESOURCE")
                        continue;

                    if (_resources.Exists(Utility.Paths.CombinePaths(pickup, "/script/required.txt")))
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
                            line = txtreader.ReadLine();
                        }
                    }
                }
                #endregion

                #region embed inline content xml pbt:javascript and pbt:style
                if (current != null)
                {
                    foreach (XmlNode node in current.SelectNodes("pbt:*", NSMGR))
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
                projsql.setQuery(q.Attributes["query"].Value);

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
            try
            {
                if (!_useCache)
                {
                    try
                    {
                        foreach (var key in _cacheKeys)
                            Cache.Remove(key.Value);
                    }
                    catch { }
                }
                if (_useCache)
                {

                    if (Cache[_cacheKeys[_cacheKeyEnum.XmlCacheKey]] == null)
                    {
                        foreach (var key in _cacheKeys)
                            Cache.Remove(key.Value);
                    }
                    else
                    {
                        var obj = Cache[_cacheKeys[_cacheKeyEnum.XmlCacheKey]];
                        var xm = (XmlDocument)Cache[_cacheKeys[_cacheKeyEnum.XmlCacheKey]];
                        var importnode = content.ImportNode(xm.DocumentElement, true);
                        content.AppendChild(importnode);
                        current = (XmlNode)Cache[_cacheKeys[_cacheKeyEnum.ContextCacheKey]];
                        if (!((string)Cache[_cacheKeys[_cacheKeyEnum.XslCacheKey]] == "--DEFAULT--"))                 // no stylesheet resolved, this falls back to a default stylesheet
                            TMPLT.setXslt((string)Cache[_cacheKeys[_cacheKeyEnum.XslCacheKey]]);

                        if (Cache[_cacheKeys[_cacheKeyEnum.PHCacheKey]] != null)
                        {
                            _pageHeirarchy = new List<string>();
                            TMPLT.ClearBrowserpageItem("PAGE_HEIRARCHY");
                            var browseritems = (XmlNode)Cache[_cacheKeys[_cacheKeyEnum.PHCacheKey]];
                            foreach (XmlNode phnode in browseritems.SelectNodes("item"))
                            {
                                _pageHeirarchy.Add(phnode.InnerText);
                                TMPLT.AddBrowserPageItem("PAGE_HEIRARCHY", phnode.InnerText);
                            }
                        }
                        return (string)Cache[_cacheKeys[_cacheKeyEnum.ContentPathKey]];
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
                        for (int i = 2; i > 0; i--)
                        {
                            string resourcepath;
                            if(i == 2 && _pageHeirarchy.Count > 1)
                                resourcepath = String.Format("{0}/{2}.{1}.xml", localxmlpath, _pageHeirarchy[_pageHeirarchy.Count - 1], _pageHeirarchy[_pageHeirarchy.Count - 2]);
                            else
                                resourcepath = String.Format("{0}/{1}.xml", localxmlpath, _pageHeirarchy[x - 1]);
                            if (_resources.Exists(resourcepath))
                            {
                                resultcontentpath = localxmlpath;
                                if (_useCdn)
                                    content.Load(_resources.FullWebPath(_resources.IndexOf) + "?timestamp" + DateTime.Now.ToString("yyyyMMddHHmmssffff"));
                                else
                                    content.Load(Server.MapPath(_resources.AbsolutePath(_resources.IndexOf)));
                                foundXml = true;
                            }
                        }
                    }

                    if (!foundXsl)
                    {
                        localxmlpath = String.Join("/", _pageHeirarchy.ToArray(), 0, x);
                        for (int i = 2; i > 0; i--)
                        {
                            string resourcepath;
                            if (i == 2 && _pageHeirarchy.Count > 1)
                                resourcepath = String.Format("{0}/{2}.{1}.xsl", localxmlpath, _pageHeirarchy[_pageHeirarchy.Count - 1], _pageHeirarchy[_pageHeirarchy.Count - 2]);
                            else
                                resourcepath = String.Format("{0}/{1}.xsl", localxmlpath, _pageHeirarchy[x - 1]);

                            if (_resources.Exists(resourcepath))
                            {
                                foundXsl = true;
                                resultcontentpath = localxmlpath;
                                xslpath = _resources.FullWebPath(_resources.IndexOf) + "?timestamp" + DateTime.Now.ToString("yyyyMMddHHmmssffff");
                            }

                            if (foundXsl)
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
                            if (s == "default")
                            {
                                resultcontentpath = "default";
                                //_pageHeirarchy = new List<string>();
                                //_pageHeirarchy.Add("default");
                                //TMPLT.ClearBrowserpageItem("PAGE_HEIRARCHY");
                                //TMPLT.AddBrowserPageItem("PAGE_HEIRARCHY", "default");

                                if (_useCache)
                                    Cache.Insert(_cacheKeys[_cacheKeyEnum.PHCacheKey], TMPLT.DOCxml.SelectSingleNode("flx/proj/browser/page/PAGE_HEIRARCHY"));
                            }

                            if (_useCdn)
                                content.Load(_resources.FullWebPath(_resources.IndexOf) + "?timestamp" + DateTime.Now.ToString("yyyyMMddHHmmssffff"));
                            else
                                content.Load(Server.MapPath(_resources.AbsolutePath(_resources.IndexOf)));
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

                if (_useCache)
                {
                    string depFile = Server.MapPath(Path.Combine("/ac", _cacheKeys[_cacheKeyEnum.XmlCacheKey].Substring("cache:".Length) + ".cache"));
                    using (StreamWriter writer = new StreamWriter(depFile))
                    {
                        writer.WriteLine(DateTime.Now.ToString("yyyyMMddHHmmssffff"));
                    }
                    var cachedependency = new CacheDependency(depFile);

                    Cache.Insert(_cacheKeys[_cacheKeyEnum.XmlCacheKey], content, cachedependency, DateTime.Now.AddMinutes(_cacheMinutes), System.Web.Caching.Cache.NoSlidingExpiration);
                    Cache.Insert(_cacheKeys[_cacheKeyEnum.ContextCacheKey], current);
                    Cache.Insert(_cacheKeys[_cacheKeyEnum.XslCacheKey], String.IsNullOrEmpty(xslpath) ? "--DEFAULT--" : xslpath);
                    Cache.Insert(_cacheKeys[_cacheKeyEnum.ContentPathKey], resultcontentpath);
                }

                return resultcontentpath;
            }
            catch (Exception unhandled)
            {
                var args = new ProjectExceptionArgs("Unhandled Exception Caught", "ProjectFlx.FlxMain", "getXmlResources(XmlDocument, XmlNode)", null, SeverityLevel.Critical, LogLevel.Debug);
                throw new ProjectException(args, unhandled);
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
                        TMPLT.ProcessTemplate();
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
                if (_SiteMap)
                    return;

                List<XmlNode> rawscript;

                // TODO: for now skip google closure joiner code
                ////if (_useCache && Cache[_cacheKeys[_cacheKeyEnum.WaitClosureKey]] == null)
                ////{
                ////    // TODO: this remains cached even after deleting dependent user file

                ////    if (Cache[_cacheKeys[_cacheKeyEnum.ScriptCacheKey]] != null)
                ////    {
                ////        rawscript = (List<XmlNode>)Cache[_cacheKeys[_cacheKeyEnum.ScriptCacheKey]];
                ////        TMPLT.DOCxml.SelectSingleNode("/flx/proj/browser/page/SCRIPT").RemoveAll();
                ////        foreach (XmlNode node in rawscript)
                ////            TMPLT.AddBrowserPageItem("RAW_SCRIPT", node.InnerText);
                ////        return;
                ////    }

                ////    try
                ////    {
                ////        StringWriter writer = new StringWriter();
                ////        JsonTextWriter jwriter = new JsonTextWriter(writer);

                ////        jwriter.WriteStartObject();
                ////        jwriter.WritePropertyName("joiner");
                ////        jwriter.WriteStartArray();
                ////        jwriter.WriteStartObject();
                ////        jwriter.WritePropertyName("application");
                ////        jwriter.WriteValue("FLXIncludedScript");
                ////        jwriter.WritePropertyName("compile");
                ////        jwriter.WriteValue(true);
                ////        jwriter.WritePropertyName("version");
                ////        jwriter.WriteValue(1.0d);
                ////        jwriter.WritePropertyName("required");
                ////        jwriter.WriteStartArray();

                ////        if (TMPLT == null || TMPLT.DOCxml == null || TMPLT.DOCxml.DocumentElement == null)
                ////            throw new Exception("TMPLT not properly initialized");

                ////        foreach (XmlNode node in TMPLT.DOCxml.SelectNodes("/flx/proj/browser/page/SCRIPT/item"))
                ////        {
                ////            jwriter.WriteValue(node.InnerText);
                ////        }
                ////        jwriter.WriteEndArray();
                ////        jwriter.WriteEndObject();
                ////        jwriter.WriteEndObject();
                ////        jwriter.Flush();

                ////        var result = Stub.jsJoiner.Joiner.invokeWebApplicationJoiner2(writer.ToString());

                ////        if (result == null)
                ////            throw new Exception("jsJoiner returns null at: Stub.jsJoine.Joiner.invokeWebApplicationJoiner2");

                ////        // write a query to check for server errors
                ////        var query = (from r in result
                ////                     where r.Xml.SelectSingleNode("compilationResult/errors/error | compilationResult/serverErrors/error") != null
                ////                     select r.Xml).ToList();

                ////        if (query == null || query.Count == 0)
                ////        {
                ////            rawscript = new List<XmlNode>();
                ////            // no errors, embed script in page
                ////            XmlNode scriptnode = TMPLT.DOCxml.SelectSingleNode("/flx/proj/browser/page/SCRIPT");
                ////            if (scriptnode != null)
                ////            {
                ////                try
                ////                {
                ////                    query = (from r in result select r.Xml).ToList();

                ////                    foreach (XmlNode node in query)
                ////                    {
                ////                        if (node.SelectSingleNode("compilationResult/serverErrors/error") != null)
                ////                        {
                ////                            if (node.SelectSingleNode("compilationResult/serverErrors/error[@code=\"22\"]") != null)
                ////                                throw new ProjectException("Too many compiles");
                ////                            else
                ////                                throw new Exception(node.SelectSingleNode("compilationResult/serverErrors/error").InnerText);
                ////                        }

                ////                        TMPLT.AddBrowserPageItem("RAW_SCRIPT", node.SelectSingleNode("compilationResult/compiledCode").InnerText);
                ////                        rawscript.Add(node.SelectSingleNode("compilationResult/compiledCode"));
                ////                    }

                ////                    if (_useCache)
                ////                    {
                ////                        Cache.Insert(_cacheKeys[_cacheKeyEnum.ScriptCacheKey], rawscript, null, DateTime.Now.AddMinutes(_cacheMinutes), System.Web.Caching.Cache.NoSlidingExpiration);
                ////                    }

                ////                    scriptnode.RemoveAll();
                ////                }
                ////                catch (ProjectException handled)
                ////                {
                ////                    if (handled.Message == "Too many compiles")
                ////                    {
                ////                        Cache.Insert(_cacheKeys[_cacheKeyEnum.WaitClosureKey], DateTime.Now.ToUniversalTime(), null, DateTime.Now.AddMinutes(15), System.Web.Caching.Cache.NoSlidingExpiration);
                ////                        TMPLT.AddCommentTag("jsCompile", handled.Message);
                ////                    }
                ////                    else
                ////                        TMPLT.AddException(handled);

                ////                }
                ////                catch (Exception unhandled)
                ////                {
                ////                    try
                ////                    {
                ////                        foreach (var gc in result)
                ////                        {
                ////                            TMPLT.AddXML(gc.Xml);
                ////                        }
                ////                    }
                ////                    catch { }

                ////                    throw new Exception("Error compiling jsJoiner code", unhandled);
                ////                }

                ////            }
                ////        }
                ////        else
                ////        {
                ////            // pass errors to our template for later evaluation
                ////            foreach (XmlNode node in query)
                ////            {
                ////                XmlDocument addDoc = new XmlDocument();
                ////                addDoc.InnerXml = node.OuterXml;
                ////                TMPLT.AddXML(addDoc);
                ////            }
                ////        }
                ////    }
                ////    catch (Exception unhandled)
                ////    {
                ////        var args = new ProjectExceptionArgs("Unhandled Exception Caught in FlxWebsite SITE_TERMINATE", "ProjectFlx.FlxMain", "SITE_TERMINATE", null, SeverityLevel.Critical, LogLevel.Debug);
                ////        throw new ProjectException(args, unhandled);
                ////    }
                ////}
                ////else
                ////{
                ////    try
                ////    {
                ////        if (Cache[_cacheKeys[_cacheKeyEnum.WaitClosureKey]] != null)
                ////            TMPLT.AddCommentTag("GoogleClosurePaused", ((DateTime)Cache[_cacheKeys[_cacheKeyEnum.WaitClosureKey]]).ToString("yyyyMMddHHmmssffff"));

                ////        if (Cache[_cacheKeys[_cacheKeyEnum.ScriptCacheKey]] != null)
                ////            Cache.Remove(_cacheKeys[_cacheKeyEnum.ScriptCacheKey]);
                ////    }
                ////    catch { }
                ////}
            }
            finally
            {
                Timing.Stop("ProjectFlx.FlxMain.SITE_TERMINATE");
            }

            

        }

        protected void ClearCache()
        {
            IDictionaryEnumerator CacheEnum = Cache.GetEnumerator();
            while (CacheEnum.MoveNext())
            {
                var key = CacheEnum.Key.ToString();
                Cache.Remove(key);
            }
        }
        protected void ClearCache(String StartsWith)
        {
            IDictionaryEnumerator CacheEnum = Cache.GetEnumerator();
            while (CacheEnum.MoveNext())
            {
                var key = CacheEnum.Key.ToString();
                if (key.StartsWith(StartsWith, StringComparison.InvariantCultureIgnoreCase))
                    Cache.Remove(key);
            }
        }
        protected void ClearCache(Regex RegX)
        {
            IDictionaryEnumerator CacheEnum = Cache.GetEnumerator();
            while (CacheEnum.MoveNext())
            {
                var key = CacheEnum.Key.ToString();
                if (RegX.Match(key).Success)
                    Cache.Remove(key);
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
            Cache = context.Cache;
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
        private bool _useCache;
        private double _cacheMinutes;
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
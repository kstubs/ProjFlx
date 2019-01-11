using System;
using System.IO;
using System.Web;
using System.Xml;
using System.Xml.XPath;
using System.Xml.Xsl;
using System.Text.RegularExpressions;
using ProjectFlx.Exceptions;
using System.Text;

namespace ProjectFlx
{
	/// <summary>
	/// Summary description for myWebTemplater.
	/// </summary>
	public class FlxTemplater : XMLTemplater, IDisposable
	{
		protected XmlDocument clsBrowserVarsXML;
		protected HttpContext httpC;
        //protected XslCompiledTransform _xslt;
        protected xmlTransformerMvp _xslt;
        protected XsltArgumentList _args;
        protected StringWriter _writer;
        private string _domain;

        protected string Domain
        {
            get { return _domain; }
        }
        protected string _subDomain;
        private bool _loaded = false;


		#region Private Methods and Functions

		/// <summary>
		/// Persist browser vars to local browser variable
		/// </summary>
        private void persistBrowserVars()
        {

            //initialize browser vars xml object
            clsBrowserVarsXML = new XmlDocument();
            XmlElement newElm;
            XmlAttribute newAtt;
            int i;

            clsBrowserVarsXML.LoadXml("<browser><page/><formvars/><queryvars/><cookievars/><sessionvars/><servervars/><capable/></browser>");

            HttpRequest requestObj = httpC.Request;

            // append user local time
            DateTime date = DateTime.Now;
            DateTime epoch = new DateTime(1970, 1, 1, 0, 0, 0, 0);
            TimeSpan span = (date - epoch);
            double unixTime = span.TotalSeconds;

            newAtt = clsBrowserVarsXML.CreateAttribute("unix_time");
            newAtt.Value = unixTime.ToString();
            clsBrowserVarsXML.DocumentElement.Attributes.Append(newAtt);

            var utc = DateTime.UtcNow;

            // append server time (utc)
            newAtt = clsBrowserVarsXML.CreateAttribute("utc_time");
            newAtt.Value = utc.ToString("o");
            clsBrowserVarsXML.DocumentElement.Attributes.Append(newAtt);

            // append short time (formatted)
            newAtt = clsBrowserVarsXML.CreateAttribute("utc_time_formatted");
            newAtt.Value = String.Format("{0} {1}", utc.ToShortDateString(), utc.ToShortTimeString());
            clsBrowserVarsXML.DocumentElement.Attributes.Append(newAtt);


            // browser FORM vars
            for (i = 0; i < requestObj.Form.Count; i++)
            {
                if (requestObj.Form.GetKey(i) != null)
                {
                    newElm = clsBrowserVarsXML.CreateElement("element");
                    //newElm.InnerText = HttpUtility.UrlDecode(requestObj.Form.Get(i));
                    newElm.InnerText = requestObj.Form[i];
                    newAtt = clsBrowserVarsXML.CreateAttribute("name");
                    newAtt.InnerText = httpC.Server.HtmlEncode(requestObj.Form.GetKey(i).StartsWith("?") ? requestObj.Form.GetKey(i).Substring(1) : requestObj.Form.GetKey(i));
                    newElm.Attributes.Append(newAtt);
                    clsBrowserVarsXML.DocumentElement.SelectSingleNode("formvars").AppendChild(newElm);
                }
            }
            // browser QUERY vars
            for (i = 0; i < requestObj.QueryString.Count; i++)
            {
                if (requestObj.QueryString.GetKey(i) != null)
                {
                    newElm = clsBrowserVarsXML.CreateElement("element");
                    //newElm.InnerText = HttpUtility.UrlDecode(requestObj.QueryString.Get(i));
                    newElm.InnerText = requestObj.QueryString[i];
                    newAtt = clsBrowserVarsXML.CreateAttribute("name");
                    newAtt.InnerText = httpC.Server.HtmlEncode(requestObj.QueryString.GetKey(i).StartsWith("?") ? requestObj.QueryString.GetKey(i).Substring(1) : requestObj.QueryString.GetKey(i));
                    newElm.Attributes.Append(newAtt);
                    clsBrowserVarsXML.DocumentElement.SelectSingleNode("queryvars").AppendChild(newElm);
                }
            }
            // browser COOKIES
            for (i = 0; i < requestObj.Cookies.Count; i++)
            {
                var cookie = requestObj.Cookies[i];

                newElm = clsBrowserVarsXML.CreateElement("element");
                newElm.SetAttribute("name", cookie.Name);
                newElm.InnerText = httpC.Server.HtmlEncode(httpC.Server.UrlDecode(cookie.Value));
                clsBrowserVarsXML.DocumentElement.SelectSingleNode("cookievars").AppendChild(newElm);
            }

            // http server vars
            foreach(string key in requestObj.ServerVariables)
            {
                newElm = clsBrowserVarsXML.CreateElement("element");
                newElm.InnerText = HttpUtility.UrlDecode(requestObj.ServerVariables[key]);
                newAtt = clsBrowserVarsXML.CreateAttribute("name");
                newAtt.InnerText = httpC.Server.HtmlEncode(key);
                newElm.Attributes.Append(newAtt);
                clsBrowserVarsXML.DocumentElement.SelectSingleNode("servervars").AppendChild(newElm);
            }

            // persist session
            //TODO: loop through session collection

            var bcap = requestObj.Browser;

            // browser capabilities
            string u = requestObj.ServerVariables["HTTP_USER_AGENT"];
            if(!String.IsNullOrEmpty(u))
            {
                clsBrowserVarsXML.DocumentElement.SelectSingleNode("capable").AppendChild(newElement("HTTP_USER_AGEN", u));

                Regex b = new Regex(@"(android|bb\d+|meego).+mobile|avantgo|bada\/|blackberry|blazer|compal|elaine|fennec|hiptop|iemobile|ip(hone|od)|iris|kindle|lge |maemo|midp|mmp|netfront|opera m(ob|in)i|palm( os)?|phone|p(ixi|re)\/|plucker|pocket|psp|series(4|6)0|symbian|treo|up\.(browser|link)|vodafone|wap|windows (ce|phone)|xda|xiino|android|ipad|playbook|silk", RegexOptions.IgnoreCase | RegexOptions.Multiline);
                Regex v = new Regex(@"1207|6310|6590|3gso|4thp|50[1-6]i|770s|802s|a wa|abac|ac(er|oo|s\-)|ai(ko|rn)|al(av|ca|co)|amoi|an(ex|ny|yw)|aptu|ar(ch|go)|as(te|us)|attw|au(di|\-m|r |s )|avan|be(ck|ll|nq)|bi(lb|rd)|bl(ac|az)|br(e|v)w|bumb|bw\-(n|u)|c55\/|capi|ccwa|cdm\-|cell|chtm|cldc|cmd\-|co(mp|nd)|craw|da(it|ll|ng)|dbte|dc\-s|devi|dica|dmob|do(c|p)o|ds(12|\-d)|el(49|ai)|em(l2|ul)|er(ic|k0)|esl8|ez([4-7]0|os|wa|ze)|fetc|fly(\-|_)|g1 u|g560|gene|gf\-5|g\-mo|go(\.w|od)|gr(ad|un)|haie|hcit|hd\-(m|p|t)|hei\-|hi(pt|ta)|hp( i|ip)|hs\-c|ht(c(\-| |_|a|g|p|s|t)|tp)|hu(aw|tc)|i\-(20|go|ma)|i230|iac( |\-|\/)|ibro|idea|ig01|ikom|im1k|inno|ipaq|iris|ja(t|v)a|jbro|jemu|jigs|kddi|keji|kgt( |\/)|klon|kpt |kwc\-|kyo(c|k)|le(no|xi)|lg( g|\/(k|l|u)|50|54|\-[a-w])|libw|lynx|m1\-w|m3ga|m50\/|ma(te|ui|xo)|mc(01|21|ca)|m\-cr|me(rc|ri)|mi(o8|oa|ts)|mmef|mo(01|02|bi|de|do|t(\-| |o|v)|zz)|mt(50|p1|v )|mwbp|mywa|n10[0-2]|n20[2-3]|n30(0|2)|n50(0|2|5)|n7(0(0|1)|10)|ne((c|m)\-|on|tf|wf|wg|wt)|nok(6|i)|nzph|o2im|op(ti|wv)|oran|owg1|p800|pan(a|d|t)|pdxg|pg(13|\-([1-8]|c))|phil|pire|pl(ay|uc)|pn\-2|po(ck|rt|se)|prox|psio|pt\-g|qa\-a|qc(07|12|21|32|60|\-[2-7]|i\-)|qtek|r380|r600|raks|rim9|ro(ve|zo)|s55\/|sa(ge|ma|mm|ms|ny|va)|sc(01|h\-|oo|p\-)|sdk\/|se(c(\-|0|1)|47|mc|nd|ri)|sgh\-|shar|sie(\-|m)|sk\-0|sl(45|id)|sm(al|ar|b3|it|t5)|so(ft|ny)|sp(01|h\-|v\-|v )|sy(01|mb)|t2(18|50)|t6(00|10|18)|ta(gt|lk)|tcl\-|tdg\-|tel(i|m)|tim\-|t\-mo|to(pl|sh)|ts(70|m\-|m3|m5)|tx\-9|up(\.b|g1|si)|utst|v400|v750|veri|vi(rg|te)|vk(40|5[0-3]|\-v)|vm40|voda|vulc|vx(52|53|60|61|70|80|81|83|85|98)|w3c(\-| )|webc|whit|wi(g |nc|nw)|wmlb|wonu|x700|yas\-|your|zeto|zte\-", RegexOptions.IgnoreCase | RegexOptions.Multiline);

                IsMobile = (b.IsMatch(u) || v.IsMatch(u.Substring(0, 4)));
                var elm = clsBrowserVarsXML.SelectSingleNode("/browser/capable");
                elm.AppendChild(newElement("IsMobile", IsMobile));
            }

            string[] caps = {"ActiveXControls", "Adapters", "AOL", "BackgroundSounds", "Adapters", "Beta", "Browser", "CanCombineFormsInDeck", "CanInitiateVoiceCall", 
                                    "CanRenderAfterInputOrSelectElement", "CanRenderEmptySelects", "CanRenderInputAndSelectElementsTogether", "CanRenderMixedSelects", 
                                    "CanRenderOneventAndPrevElementsTogether", "CanRenderPostBackCards", 
                                    "CanRenderSetvarZeroWithMultiSelectionList", "CanSendMail", "CDF", "ClrVersion", "Cookies", "Crawler", "DefaultSubmitButtonLimit", 
                                    "EcmaScriptVersion", "Frames", "GatewayMajorVersion", "GatewayMinorVersion", "GatewayVersion", "HasBackButton", "HidesRightAlignedMultiselectScrollbars", 
                                    "HtmlTextWriter", "Id", "InputType", "IsColor", "IsMobileDevice", "JavaApplets", "JScriptVersion", "MajorVersion", "MaximumHrefLength", "MaximumRenderedPageSize", 
                                    "MaximumSoftkeyLabelLength", "MinorVersion", "MinorVersionString", "MobileDeviceManufacturer", "MobileDeviceModel", "MSDomVersion", "NumberOfSoftkeys", 
                                    "Platform", "PreferredImageMime", "PreferredRenderingMime", "PreferredRenderingType", "PreferredRequestEncoding", "NumberOfSoftkeys", "PreferredResponseEncoding",
                                    "RendersBreakBeforeWmlSelectAndInput", "RendersBreaksAfterHtmlLists", "RendersBreaksAfterWmlAnchor", "RendersBreaksAfterWmlInput", "RendersWmlDoAcceptsInline",
                                    "RendersWmlSelectsAsMenuCards", "RequiredMetaTagNameValue", "RequiresAttributeColonSubstitution", "RequiresContentTypeMetaTag", "RequiresControlStateInSession",
                                    "RequiresDBCSCharacter", "RequiresHtmlAdaptiveErrorReporting", "RequiresLeadingPageBreak", "RequiresNoBreakInFormatting", "RequiresNoBreakInFormatting", "RequiresOutputOptimization",
                                    "RequiresPhoneNumbersAsPlainText", "RequiresSpecialViewStateEncoding", "RequiresUniqueFilePathSuffix", "RequiresUniqueHtmlCheckboxNames", "RequiresUniqueHtmlInputNames",
                                    "RequiresUrlEncodedPostfieldValues", "ScreenBitDepth", "ScreenCharactersHeight", "ScreenCharactersWidth", "ScreenPixelsHeight", "ScreenPixelsWidth",
                                    "SupportsAccesskeyAttribute", "SupportsBodyColor", "SupportsBold", "SupportsCacheControlMetaTag", "SupportsCallback", "SupportsCss", "SupportsDivAlign",
                                    "SupportsDivNoWrap", "SupportsEmptyStringInCookieValue", "SupportsFontColor", "SupportsFontName", "SupportsFontSize", "SupportsImageSubmit", "SupportsIModeSymbols",
                                    "SupportsInputIStyle", "SupportsInputMode", "SupportsItalic", "SupportsJPhoneMultiMediaAttributes", "SupportsJPhoneSymbols", "SupportsQueryStringInFormAction", "SupportsRedirectWithCookie",
                                    "SupportsSelectMultiple", "SupportsUncheck", "SupportsXmlHttp", "Tables", "TagWriter", "Type", "UseOptimizedCacheKey", "VBScript", "Version", "W3CDomVersion", "Win16", "Win32"};

            foreach (var c in caps)
            {
                try
                {
                    clsBrowserVarsXML.DocumentElement.SelectSingleNode("capable").AppendChild(newElement(c, bcap[c]));
                }
                catch
                {
                    clsBrowserVarsXML.DocumentElement.SelectSingleNode("capable").AppendChild(newElement(c, "N/A"));
                }
            }
        }

        private XmlNode newElement(string Name, object Value)
        {
            
            var newElm = clsBrowserVarsXML.CreateElement("element");
            var requestObj = httpC.Request;

            var newAtt = clsBrowserVarsXML.CreateAttribute("name");
            newAtt.Value = Name;
            newElm.Attributes.Append(newAtt);
            newElm.InnerText = Convert.ToString(Value);

            return newElm;
        }

		private void _Clear(TMPLTLookups lookfor)
		{
			XmlNode n;
			string xPathString = null;

			// build xpath string
			switch (lookfor)
			{
				case TMPLTLookups.AnyBrowserVar:
				case TMPLTLookups.Any:
					xPathString = "/flx/proj/browser";
					break;
				case TMPLTLookups.Session:
					xPathString = "/flx/proj/browser/sessionvars";
					break;
				case TMPLTLookups.Cookie:
					xPathString = "/flx/proj/browser/cookievars";
					break;
				case TMPLTLookups.Form:
					xPathString = "/flx/proj/browser/formvars/element";
					break;
				case TMPLTLookups.Querystring:
					xPathString = "/flx/proj/browser/queryvars/element";
					break;
			}

			if (lookfor != TMPLTLookups.AnyBrowserVar)
			{
				n = _xml.SelectSingleNode(xPathString);
				if (n.Name.Equals("cookievars"))
                {
                    foreach (XmlNode n1 in n.SelectNodes("*"))
                    {
                        clearCookie(n1.Attributes["name"].Value);
                    }

					n.RemoveAll();
					n.InnerText = "";
                }
			}

			if (lookfor == TMPLTLookups.AnyBrowserVar)
			{
				xPathString = "/flx/proj/browser/*";
				XmlNodeList l = _xml.SelectNodes(xPathString);
				foreach (XmlNode x in l)
				{
					if (x.Name.Equals("cookievars") || x.Name.Equals("sessionvars")
					|| x.Name.Equals("formvars") || x.Name.Equals("queryvars"))
					{
						foreach (XmlNode x1 in x.SelectNodes("*"))
						{
							clearCookie(x1.Attributes["name"].Value);
						}
						x.RemoveAll();
						x.InnerText = "";
					}
				}
			}


		}

        private void clearCookie(String Name)
        {
            var cookie = HttpContext.Current.Request.Cookies[Name];
            if (cookie != null)
            {
                cookie = new HttpCookie(Name);
                cookie.Domain = _domain;
                cookie.Expires = DateTime.Now.AddDays(-100d);
                HttpContext.Current.Response.Cookies.Add(cookie);
            }
        }
		#endregion

        #region Public Methods and Functions
		public FlxTemplater()
		{
			httpC = HttpContext.Current;
            _domain = getDomain(httpC.Request.Url.GetLeftPart(UriPartial.Authority));
            _subDomain = httpC.Request.Url.GetLeftPart(UriPartial.Scheme);

            _loaded = false;
            _args = new XsltArgumentList();
            _xslt = new xmlTransformerMvp();
            
			persistBrowserVars();
			AddXML("proj",clsBrowserVarsXML);
		}

        private string getDomain(string p)
        {
            Uri u = new Uri(p);
            string host = u.Host;

            if (host.Split('.').Length > 2)
            {
                int lastIndex = host.IndexOf(".");
                return host.Substring(lastIndex + 1); 
            }
            else
            {
                return u.Host;
            }
        }

        public void setXslt(string XsltPath)
        {
            XsltSettings settings = new XsltSettings(true, true);

            _xslt.XSLSource = XsltPath;
            _xslt.AddXslParameter("source.xsl", Path.GetFileName(XsltPath));
            //_xslt.Load(XsltPath, settings, new XmlUrlResolver());
            _loaded = true;
        }

        public bool XsltLoaded
        {
            get
            {
                return _loaded;
            }
        }

		public void AddXslParameter(string name, object value)
		{
            if(value == null)
                return;

            var arg = _args.GetParam(name, "");

            if (arg != null)
                _args.RemoveParam(name, "");

            _args.AddParam(name, "", value);
		}


		public void AddXslParameter(string name, XPathNodeIterator nodeit)
		{
            if (String.IsNullOrEmpty(name) || nodeit == null)
                return;

            var arg = _args.GetParam(name, "");

            if (arg != null)
                _args.RemoveParam(name, "");

            _args.AddParam(name, "", nodeit);
        }

        public void AddXslExtension(string Namespace, Object obj)
        {
            _args.AddExtensionObject(Namespace, obj);   
        }
		
		public virtual void ProcessTemplate()
		{

			if (_xslt == null)
			{
				ProjectExceptionArgs args = new ProjectExceptionArgs("XSL Not Set, ProcessTemplate aborted", "WebTemplater", "ProcessTemplate", null, SeverityLevel.Critical, LogLevel.Event);
                throw new ProjectException(args);
			}

			//initialize xslt global param vars
			string doc_folder = httpC.Request.Path;
			
			//strip application name
			doc_folder = doc_folder.Substring(0,doc_folder.LastIndexOf("/") + 1);

    		//Current Page - DOC_ACTION
            _args.AddParam("DOC_ACTION", "", httpC.Request.Path);

			//Current Folder - DOC_FOLDER
            _args.AddParam("DOC_FOLDER", "", doc_folder);

            using (_writer = new StringWriter())
            {
                //_xslt.Transform(_xml.CreateNavigator(),_args, _writer);
                _xslt.xslTransformer(_xml.CreateNavigator(), _args);
                _xslt.XMLObjectSource = _xml;
                _writer.Write(_xslt.ResultText);
            }
        }

        internal void AddWBTXml(XmlDocument newXML)
        {
            var ns = new XmlNamespaceManager(_xml.NameTable);
            ns.AddNamespace("wbt", "myWebTemplater.1.0");
            var node = _xml.SelectSingleNode("flx/wbt:app", ns);

            if (node == null)
            {
                node = _xml.CreateElement("wbt", "app", ns.LookupNamespace("wbt"));
                _xml.DocumentElement.AppendChild(node);
            }

            var newNode = _xml.ImportNode(newXML.DocumentElement, true);
            node.AppendChild(newNode);
        }
        internal void AddWBTXml(string newXml)
        {
            var xm = new XmlDocument();
            xm.LoadXml(newXml);
            AddWBTXml(xm);
        }

        // create override - add all items to APPLICATION node
        public override void AddXML(XmlDocument newXML)
        {
            base.AddXML("app", newXML);
        }
        // create override - add all items to APPLICATION node
        public void AddXMLNode(XmlNode xmlNode)
        {
            XmlDocument xm = new XmlDocument();
            xm.XmlResolver = new XmlUrlResolver();
            var newNode = xm.ImportNode(xmlNode, true);
            xm.AppendChild(newNode);
            base.AddXML("app", xm);
        }
        // create override - add all items to APPLICATION node
        public void AddXML(String XmlDocumentString)
        {
            XmlDocument xml = new XmlDocument();
            xml.XmlResolver = new XmlUrlResolver();
            xml.LoadXml(XmlDocumentString);
            this.AddXML("app", xml);
        }

        public void AddXML(ProjectFlx.Schema.projectResults ProjectResults)
        {
            foreach (var result in ProjectResults.results)
            {
                XmlDocument doc = new XmlDocument();
                doc.LoadXml(result.Serialize());

                base.AddXML("app", doc);
            }
        }

        public void AddXML(Newtonsoft.Json.Linq.JObject JObject)
        {
            AddXML(Newtonsoft.Json.JsonConvert.DeserializeXmlNode(JObject.ToString()));
        }

        public void AddXML(Newtonsoft.Json.Linq.JObject JObject, String RootTag)
        {
            AddXML(Newtonsoft.Json.JsonConvert.DeserializeXmlNode(JObject.ToString(), RootTag));
        }

        public void AddXMLJson(String Json)
        {
            AddXML(Newtonsoft.Json.JsonConvert.DeserializeXmlNode(Json));
        }

        public void AddXMLJson(String Json, String RootTag)
        {
            AddXML(Newtonsoft.Json.JsonConvert.DeserializeXmlNode(Json, RootTag));
        }

		public void ClearSessionVars()
		{
			_Clear(TMPLTLookups.Session);
		}

		public void ClearCookieVars()
		{
			_Clear(TMPLTLookups.Cookie);
		}

		public void ClearFormVars()
		{
			_Clear(TMPLTLookups.Form);
		}

		public void ClearAllBrowserVars()
		{
			_Clear(TMPLTLookups.AnyBrowserVar);
		}

		/// <summary>
		/// Lookup from TMPLT 
		/// Default look anywhere
		/// </summary>
		/// <param name="lookfor"></param>
		/// <param name="value"></param>
		/// <returns></returns>
		public virtual bool LookUp(string lookfor, ref string value)
		{
			return (_LookUp(lookfor, ref value, TMPLTLookups.Any, clsFindWhenEmpty));
		}

		/// <summary>
		/// Return Browser Vars (ALL) as XMLDocument
		/// </summary>
		/// <returns></returns>
		public XmlDocument BrowserVars
		{

			get
			{

				XmlDocument xmReturn = new XmlDocument();
				XmlNode newNode;

				string xPathString;
				xPathString = "/flx/proj/browser";
				XmlNode n;
				n = _xml.SelectSingleNode(xPathString);

				newNode = xmReturn.ImportNode(n, true);

				xmReturn.AppendChild(newNode);

				return (xmReturn);
			}
		}
		public XmlDocument BrowserFormVars
		{

			get
			{

				XmlDocument xmReturn = new XmlDocument();
				XmlNode newNode;

				string xPathString;
				xPathString = "/flx/proj/browser/formvars";
				XmlNode n;
				n = _xml.SelectSingleNode(xPathString);

				newNode = xmReturn.ImportNode(n, true);

				xmReturn.AppendChild(newNode);

				return (xmReturn);
			}
		}
		public virtual string LookupBrowserVars(string lookfor)
		{
			string value = "";

			_LookUp(lookfor, ref value, TMPLTLookups.AnyBrowserVar, clsFindWhenEmpty);

			return (value);
		}
		/// <summary>
		/// Lookup Browser Vars
		/// </summary>
		/// <param name="lookfor"></param>
		/// <param name="value"></param>
		/// <returns></returns>
		public virtual bool LookupBrowserVars(string lookfor, ref string value)
		{
			return (_LookUp(lookfor, ref value, TMPLTLookups.AnyBrowserVar, clsFindWhenEmpty));
		}

		public virtual bool LookupBrowserVars(string lookfor, ref int value)
		{
			bool success;
			int iVal = 0;
			string sVal = "";
			success = _LookUp(lookfor, ref sVal, TMPLTLookups.AnyBrowserVar, clsFindWhenEmpty);

			//convert to int
			try
			{
                success = int.TryParse(sVal, out iVal);
			}
			catch
			{
				iVal = value;
				success = false;
			}

			value = iVal;
			return (success);

		}

		public virtual string LookupFormVars(string lookfor)
		{
			string value = "";

			_LookUp(lookfor, ref value, TMPLTLookups.Form, clsFindWhenEmpty);

			return (value);
		}
		/// <summary>
		/// Lookup Browser Vars - Look In Form Post
		/// </summary>
		/// <param name="lookfor"></param>
		/// <param name="value"></param>
		/// <returns></returns>
		public virtual bool LookupFormVars(string lookfor, ref string value)
		{
			return (_LookUp(lookfor, ref value, TMPLTLookups.Form, clsFindWhenEmpty));
		}
		public virtual bool LookupFormVars(string lookfor, ref int value)
		{
			bool success;
			int iVal = 0;
			string sVal = "";
			success = _LookUp(lookfor, ref sVal, TMPLTLookups.Form, clsFindWhenEmpty);

			//convert to int
			try
			{
				iVal = Convert.ToInt32(sVal);
			}
			catch
			{
				iVal = 0;
				success = false;
			}

			value = iVal;
			return (success);

		}

		/// <summary>
		/// Lookup Browser Vars - Loo In QueryString
		/// </summary>
		/// <param name="lookfor"></param>
		/// <param name="value"></param>
		/// <returns></returns>
		public virtual bool LookupQueryVars(string lookfor, ref string value)
		{
			return (_LookUp(lookfor, ref value, TMPLTLookups.Querystring, clsFindWhenEmpty));
		}
		public virtual bool LookupQueryVars(string lookfor, ref int value)
		{
			bool success;
			int iVal = 0;
			string sVal = value.ToString();
			success = _LookUp(lookfor, ref sVal, TMPLTLookups.Querystring, clsFindWhenEmpty);

			//convert to int
			try
			{
				iVal = Convert.ToInt32(sVal);
			}
			catch
			{
				iVal = 0;
				success = false;
			}

			value = iVal;
			return (success);

		}

        public virtual string LookupQueryVars(string lookfor)
        {
            string value = "";

            _LookUp(lookfor, ref value, TMPLTLookups.Querystring, clsFindWhenEmpty);

            return (value);
        }

		public virtual string LookupCookieVars(string lookfor)
		{
			string value = "";

			_LookUp(lookfor, ref value, TMPLTLookups.Cookie, clsFindWhenEmpty);
			return (value);
		}
		/// <summary>
		/// Lookup Browser Vars - Look In Cookies
		/// </summary>
		/// <param name="lookfor"></param>
		/// <param name="value"></param>
		/// <returns></returns>
		public virtual bool LookupCookieVars(string lookfor, ref string value)
		{
			return (_LookUp(lookfor, ref value, TMPLTLookups.Cookie, clsFindWhenEmpty));
		}

		public virtual bool LookupCookieVars(string lookfor, ref int value)
		{
			bool success;
			int iVal = 0;
			string sVal = value.ToString();
			success = _LookUp(lookfor, ref sVal, TMPLTLookups.Cookie, clsFindWhenEmpty);

			//convert to int
			try
			{
				iVal = Convert.ToInt32(sVal);
			}
			catch
			{
				iVal = 0;
				success = false;
			}

			value = iVal;
			return (success);

		}

		/// <summary>
		/// Lookup Browser Vars - Look In Session
		/// </summary>
		/// <param name="lookfor"></param>
		/// <param name="value"></param>
		/// <returns></returns>
		public virtual bool LookupSessionsVars(string lookfor, ref string value)
		{
			return (_LookUp(lookfor, ref value, TMPLTLookups.Session, clsFindWhenEmpty));
		}

		public virtual bool LookupSessionsVars(string lookfor, ref int value)
		{
			bool success;
			int iVal = 0;
			string sVal = "";
			success = _LookUp(lookfor, ref sVal, TMPLTLookups.Session, clsFindWhenEmpty);

			//convert to int
			try
			{
				iVal = Convert.ToInt32(sVal);
			}
			catch
			{
				iVal = 0;
				success = false;
			}

			value = iVal;
			return (success);

		}

        /// <summary>
        /// Lookup Browser Vars - Look In Session
        /// </summary>
        /// <param name="lookfor"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public virtual bool LookupServerVars(string lookfor, ref string value)
        {
            return (_LookUp(lookfor, ref value, TMPLTLookups.ServerVars, clsFindWhenEmpty));
        }

        public virtual bool LookupTag(string lookfor, ref string value)
        {
            return (_LookUp(lookfor, ref value, TMPLTLookups.Tag, clsFindWhenEmpty));
        }

		private bool _LookUp(string lookfor, ref string value, TMPLTLookups lookin, bool returnTrueOnEmpty)
		{
			bool lookup_result = false;
			string lookup_value = "";
			string xPathString = "";

			lookup_value = value;

			// build xpath string
			switch (lookin)
			{
                case TMPLTLookups.Session:
                    xPathString = string.Format("/flx/proj/browser/sessionvars/element[translate(@name, 'ABCDEFGHIJKLMNOPQRSTUVWXYZ', 'abcdefghijklmnopqrstuvwxyz')='{0}'][1]", lookfor.ToLower());
                    break;
                case TMPLTLookups.ServerVars:
                    xPathString = string.Format("/flx/proj/browser/servervars/element[translate(@name, 'ABCDEFGHIJKLMNOPQRSTUVWXYZ', 'abcdefghijklmnopqrstuvwxyz')='{0}'][1]", lookfor.ToLower());
                    break;
                case TMPLTLookups.Cookie:
                    xPathString = string.Format("/flx/proj/browser/cookievars/element[translate(@name, 'ABCDEFGHIJKLMNOPQRSTUVWXYZ', 'abcdefghijklmnopqrstuvwxyz')='{0}'][1]", lookfor.ToLower());
					break;
				case TMPLTLookups.Form:
                    xPathString = string.Format("/flx/proj/browser/formvars/element[translate(@name, 'ABCDEFGHIJKLMNOPQRSTUVWXYZ', 'abcdefghijklmnopqrstuvwxyz')='{0}'][1]", lookfor.ToLower());
					break;
				case TMPLTLookups.Querystring:
                    xPathString = string.Format("/flx/proj/browser/queryvars/element[translate(@name, 'ABCDEFGHIJKLMNOPQRSTUVWXYZ', 'abcdefghijklmnopqrstuvwxyz')='{0}'][1]", lookfor.ToLower());
					break;
				case TMPLTLookups.AnyBrowserVar:
                    xPathString = string.Format("/flx/proj/browser/*/element[translate(@name, 'ABCDEFGHIJKLMNOPQRSTUVWXYZ', 'abcdefghijklmnopqrstuvwxyz')='{0}'][1]", lookfor.ToLower());
					break;
                case TMPLTLookups.Tag:
                    xPathString = string.Format("", lookfor);
                    break;
				case TMPLTLookups.Any:
                    xPathString = string.Format("//*[@*[translate(., 'ABCDEFGHIJKLMNOPQRSTUVWXYZ', 'abcdefghijklmnopqrstuvwxyz')='{0}']][1]", lookfor.ToLower());
					break;
			}

			// find it
			XmlNode findElm = _xml.SelectSingleNode(xPathString);

			// determine results for lookup
			if (findElm != null)
			{
				// we found something, if blank:
				// if returnTrueOnEmpty true we should return empty string
				// and return true for the function
				if (findElm.InnerText == "")
				{
					if (returnTrueOnEmpty == true)
						lookup_result = true;
				}
				else
				{
					lookup_result = true;
				}

			}

			// set return value
            if (lookup_result)
                lookup_value = HttpUtility.HtmlDecode(findElm.InnerText);

            // 

			value = lookup_value;

			return (lookup_result);


		}


		/// <summary>
		/// Add Cookie to Response
		/// and add cookie to TMPLT XML for first add - make cookie val
		/// availalble to template right away
		/// </summary>
		/// <param name="Name"></param>
		/// <param name="Value"></param>
        public virtual void AddCookie(string Name, string Value)
        {
            // add cookie to Response
            var cookie = new HttpCookie(Name);
            cookie.Domain = _domain;
            cookie.Value = HttpUtility.UrlEncode(Value);

            httpC.Response.Cookies.Add(cookie);

            XmlElement newElm;
            XmlNode c;
            c = _xml.SelectSingleNode("/flx/proj/browser/cookievars");

            //look for existing cookies var
            newElm = (XmlElement)c.SelectSingleNode(string.Format("element[@name='{0}']", Name));

            if (newElm == null)
            {
                newElm = _xml.CreateElement("element");
                c.AppendChild(newElm);
            }

            newElm.SetAttribute("name", "", Name);
            newElm.InnerText = Value;

        }

        public virtual void AddCookie(String Name, String Value, DateTime Expires, bool Protected = false)
        {
            // add cookie to Response
            var cookie = new HttpCookie(Name);
            cookie.Domain = _domain;
            cookie.Value = HttpUtility.UrlEncode(Value);
            cookie.Expires = Expires;

            if (Protected)
                cookie.HttpOnly = true;

            httpC.Response.Cookies.Add(cookie);

            XmlElement newElm;
            XmlNode c;
            c = _xml.SelectSingleNode("/flx/proj/browser/cookievars");

            //look for existing cookies var
            newElm = (XmlElement)c.SelectSingleNode(string.Format("element[@name='{0}']", Name));

            if (newElm == null)
            {
                newElm = _xml.CreateElement("element");
                c.AppendChild(newElm);
            }

            newElm.SetAttribute("name", "", Name);
            newElm.InnerText = httpC.Server.HtmlEncode(Value);

            // if cookie is meant to be protected then save hash copy of it for verification purposes
            if (Protected)
            {
                byte[] _cookiesalt = Encoding.UTF8.GetBytes("SaltDogz!");

                var val_h = Utility.SimpleHash.ComputeHash(Value, "MD5", _cookiesalt);
                cookie = new HttpCookie(Name + "_h", HttpUtility.UrlEncode(val_h));
                cookie.Domain = Domain;
                cookie.Expires = Expires;
                cookie.HttpOnly = true;
                httpC.Response.Cookies.Add(cookie);
            }

        }

        /// <summary>
        /// Put item in the browser node like Page Name, Page Minor Name, etc..
        /// </summary>
        /// <param name="Name"></param>
        /// <param name="Value"></param>
        public virtual void AddBrowserPageItem(string Name, string Value)
        {
            AddBrowserPageItem(Name, Value, null, null, null);
        }
        public virtual void AddBrowserPageItem(string Name, string Value, string ItemName)
        {
            AddBrowserPageItem(Name, Value, ItemName, null, null);
        }
        public virtual void AddBrowserPageItem(string Name, string Value, string ItemName, string LinkValue)
        {
            AddBrowserPageItem(Name, Value, ItemName, LinkValue, null);
        }
        public virtual void AddBrowserPageItem(string Name, string Value, string ItemName, string LinkValue, string Title)
        {
            if (String.IsNullOrEmpty(Value))
                return;

            XmlElement newElm;
            XmlNode c;
            c = _xml.SelectSingleNode("/flx/proj/browser/page");

            // does the child element exist
            XmlNode child = c.SelectSingleNode(Name);
            if (child == null)
            {
                child = _xml.CreateElement(Name);
                c.AppendChild(child);
            }

            // does the item (by value) exist?  Only add one item 
            XmlNode existingNode = null;
            try
            {
                if (!String.IsNullOrEmpty(Value))
                {
                    if (Value.StartsWith("'") && Value.EndsWith("'"))
                        existingNode = child.SelectSingleNode("item[.=" + Value + "]");
                    else
                        existingNode = child.SelectSingleNode("item[.='" + Value + "']");
                }
            }
            catch (XPathException) { }


            if (existingNode == null)
            {
                newElm = _xml.CreateElement("item");
                if (!String.IsNullOrEmpty(ItemName))
                {
                    var att = _xml.CreateAttribute("name");
                    att.Value = ItemName;
                    newElm.Attributes.Append(att);
                }
                child.AppendChild(newElm);
                if (Name == "RAW_SCRIPT")
                {
                    var cdata = _xml.CreateCDataSection(Value);
                    newElm.AppendChild(cdata);
                } 
                else
                    newElm.InnerText = Value.Trim();

                // keep track of browser action:  name/major/minor/subminor[1..x]
                if (Name == "PAGE_HEIRARCHY")
                {
                    var itemNodes = child.SelectNodes("item");
                    var actionAtt = _xml.CreateAttribute("action");
                    switch (itemNodes.Count)
                    {
                        case 0:         // not sure about this one
                            actionAtt.Value = "-";
                            break;
                        case 1:         // page name
                            actionAtt.Value = "page";
                            break;
                        case 2:         // action major
                            actionAtt.Value = "major";
                            break;
                        case 3:         // action minor
                            actionAtt.Value = "minor";
                            break;
                        default:        // action sub minor [1..x]
                            actionAtt.Value = String.Format("subminor{0}", (itemNodes.Count - 3));
                            break;
                    }
                    newElm.Attributes.Append(actionAtt);

                    if (LinkValue != null)
                    {
                        var linkatt = _xml.CreateAttribute("link");
                        linkatt.Value = LinkValue;
                        newElm.Attributes.Append(linkatt);
                    }

                    if (Title != null)
                    {
                        var titleatt = _xml.CreateAttribute("title");
                        titleatt.Value = Title;
                        newElm.Attributes.Append(titleatt);
                    }

                }
            }
        }

        public virtual void ClearBrowserpageItem(string Name)
        {
            XmlNode c;
            c = _xml.SelectSingleNode("/flx/proj/browser/page");

            // does the child element exist
            XmlNode child = c.SelectSingleNode(Name);
            if (child != null)
                c.RemoveChild(child);

        }
		/// <summary>
		/// Clear a cookie from the list
		/// </summary>
		/// <param name="Name"></param>
		public virtual void ClearCookie(string Name)
		{
            clearCookie(Name);
			XmlNode delNode;
			delNode = _xml.SelectSingleNode(string.Format("/flx/proj/browser/cookievars/element[@name='{0}']", Name));

			if (!(delNode == null))
				_xml.SelectSingleNode("/flx/proj/browser/cookievars").RemoveChild(delNode);

		}

        /// <summary>
        /// Clear an array of cookie vars
        /// </summary>
        /// <param name="Name"></param>
        public virtual void ClearCookie(params string[] Name)
        {
            foreach (string s in Name)
            {
                ClearCookie(s);
            }
        }


		#endregion		

        #region Public Properties

		public String Result
		{
			get 
			{
                return _writer.ToString();
			}			
		}
		#endregion

        public bool isDisposed = false;
        public bool IsMobile;
        public void Dispose()
        {
            if (isDisposed)
                return;

            try
            {
                _xml = null;
                if (_writer != null)
                    _writer.Dispose();
            }
            finally
            {
                isDisposed = true;
            }
            
        }
    }
}

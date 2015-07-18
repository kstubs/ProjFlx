using System;
using System.Web;
using System.Xml;
using System.Xml.XPath;
using ProjectUtilities.Exceptions;

namespace ProjectUtilities.XMLParser
{
	/// <summary>
	/// Summary description for myWebTemplater.
	/// </summary>
	public class WebTemplater : XMLTemplater
	{
		protected XmlDocument clsBrowserVarsXML;
		protected HttpContext httpC;
		protected xmlTransformerMvp T = new xmlTransformerMvp();
		


		/************************************************************
		*	PRIVATE METHODS AND FUNCTIONS
		************************************************************/
		#region Private Methods and Functions

		/// <summary>
		/// Persist browser vars to local browser variable
		/// </summary>
		private void persistBrowserVars() 
		{
			
			try 
			{

				//initialize browser vars xml object
				clsBrowserVarsXML = new XmlDocument();
				XmlElement newElm;
				XmlAttribute newAtt;
				int i;

				clsBrowserVarsXML.LoadXml("<BROWSER><FORMVARS/><QUERYVARS/><COOKIEVARS/><SESSIONVARS/></BROWSER>");

				HttpRequest requestObj = httpC.Request;
			
			
				//persist form vars
				for (i = 0; i < requestObj.Form.Count; i++) 
				{
					newElm = clsBrowserVarsXML.CreateElement("ELEMENT");
					//newElm.InnerText = HttpUtility.UrlDecode(requestObj.Form.Get(i));
                    newElm.InnerText = requestObj.Form[i];
					newAtt = clsBrowserVarsXML.CreateAttribute("name");
					newAtt.InnerText = requestObj.Form.GetKey(i).StartsWith("?") ? requestObj.Form.GetKey(i).Substring(1) : requestObj.Form.GetKey(i);
					newElm.Attributes.Append(newAtt);
					clsBrowserVarsXML.DocumentElement.SelectSingleNode("FORMVARS").AppendChild(newElm);
				}
				//persist query vars
				for (i = 0; i < requestObj.QueryString.Count; i++) 
				{
					newElm = clsBrowserVarsXML.CreateElement("ELEMENT");
					//newElm.InnerText = HttpUtility.UrlDecode(requestObj.QueryString.Get(i));
                    newElm.InnerText = requestObj.QueryString[i];
					newAtt = clsBrowserVarsXML.CreateAttribute("name");
					newAtt.InnerText = requestObj.QueryString.GetKey(i).StartsWith("?") ? requestObj.QueryString.GetKey(i).Substring(1) : requestObj.QueryString.GetKey(i);
					newElm.Attributes.Append(newAtt);
					clsBrowserVarsXML.DocumentElement.SelectSingleNode("QUERYVARS").AppendChild(newElm);
				}
				//persist cookies
				for (i = 0;i < requestObj.Cookies.Count; i++)
				{
					newElm = clsBrowserVarsXML.CreateElement("ELEMENT");
					newElm.InnerText = HttpUtility.UrlDecode(requestObj.Cookies[i].Value);
					newAtt = clsBrowserVarsXML.CreateAttribute("name");
					newAtt.InnerText = requestObj.Cookies[i].Name;
					newElm.Attributes.Append(newAtt);
					clsBrowserVarsXML.DocumentElement.SelectSingleNode("COOKIEVARS").AppendChild(newElm);
				}

				//persist session
				//TODO: loop through session collection

			}
			catch(Exception e) 
			{
				//nothing to do here - HttpContext may not be valid
				Console.Write(e.ToString());
			}
			
			
		}

		private void SetupXSLTDocument() 
		{
			string doc_folder = httpC.Request.Path;
			
			//strip application name
			doc_folder = doc_folder.Substring(0,doc_folder.LastIndexOf("/") + 1);

			//Current Page - DOC_ACTION
			T.AddXslParameter("DOC_ACTION",httpC.Request.Path);

			//Current Folder - DOC_FOLDER
			T.AddXslParameter("DOC_FOLDER", doc_folder);

		}

		private void _Clear(TMPLTLookups lookfor)
		{
			XmlNode n;
			string xPathString = "";

			// build xpath string
			switch (lookfor)
			{
				case TMPLTLookups.Session:
					xPathString = string.Format("/{0}/TMPLT/BROWSER/SESSIONVARS", _rootname);
					break;
				case TMPLTLookups.Cookie:
					xPathString = string.Format("/{0}/TMPLT/BROWSER/COOKIEVARS", _rootname);
					break;
				case TMPLTLookups.Form:
					xPathString = string.Format("/{0}/TMPLT/BROWSER/FORMVARS/ELEMENT", _rootname);
					break;
				case TMPLTLookups.Querystring:
					xPathString = string.Format("/{0}/TMPLT/BROWSER/QUERYVARS/ELEMENT", _rootname);
					break;
			}

			if (lookfor != TMPLTLookups.AnyBrowserVar)
			{
				n = clsTemplaterXMLObj.SelectSingleNode(xPathString);
				n.RemoveAll();
				n.InnerText = "";
			}

			if (lookfor == TMPLTLookups.AnyBrowserVar)
			{
				xPathString = string.Format("/{0}/TMPLT/BROWSER/*", _rootname);
				XmlNodeList l = clsTemplaterXMLObj.SelectNodes(xPathString);
				foreach (XmlNode x in l)
				{
					x.RemoveAll();
					x.InnerText = "";
				}
			}


		}

		#endregion
		/************************************************************
		*	PUBLIC METHODS AND FUNCTIONS
		************************************************************/
		#region Public Methods and Functions
		public WebTemplater()
		{
			httpC = HttpContext.Current;

			persistBrowserVars();
			AddXML("TMPLT",clsBrowserVarsXML);
		}


		public void AddXslParameter(string name, string value)
		{
			T.AddXslParameter(name, value);
		}


		public void AddXslParameter(string name, XPathNodeIterator nodeit)
		{
			T.AddXslParameter(name, nodeit);
		}
		
		public virtual void ProcessTemplate()
		{

			if (XSLSource == "")
			{
				ProjectExceptionArgs args = new ProjectExceptionArgs("XSLSource Not Set, ProcessTemplate aborted", "WebTemplater", "ProcessTemplate", null, SeverityLevel.Critical, LogLevel.Event);
				throw new XMLParserException(args);
			}

			//initialize xslt global param vars
			SetupXSLTDocument();

			T.XMLObjectSource = clsTemplaterXMLObj;

			T.Transform();

		}
		// create override - add all items to APPLICATION node
		public override void AddXML(XmlDocument newXML)
		{
			base.AddXML ("APPLICATION", newXML);
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
				xPathString = string.Format("/{0}/TMPLT/BROWSER", _rootname);
				XmlNode n;
				n = clsTemplaterXMLObj.SelectSingleNode(xPathString);

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
				xPathString = string.Format("/{0}/TMPLT/BROWSER/FORMVARS", _rootname);
				XmlNode n;
				n = clsTemplaterXMLObj.SelectSingleNode(xPathString);

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
				iVal = Convert.ToInt32(sVal);
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
					xPathString = string.Format("/{0}/TMPLT/BROWSER/SESSIONVARS/ELEMENT[@name='{1}'][1]", _rootname, lookfor);
					break;
				case TMPLTLookups.Cookie:
					xPathString = string.Format("/{0}/TMPLT/BROWSER/COOKIEVARS/ELEMENT[@name='{1}'][1]", _rootname, lookfor);
					break;
				case TMPLTLookups.Form:
					xPathString = string.Format("/{0}/TMPLT/BROWSER/FORMVARS/ELEMENT[@name='{1}'][1]", _rootname, lookfor);
					break;
				case TMPLTLookups.Querystring:
					xPathString = string.Format("/{0}/TMPLT/BROWSER/QUERYVARS/ELEMENT[@name='{1}'][1]", _rootname, lookfor);
					break;
				case TMPLTLookups.AnyBrowserVar:
					xPathString = string.Format("/{0}/TMPLT/BROWSER//ELEMENT[@name='{1}'][1]", _rootname, lookfor);
					break;
				case TMPLTLookups.Any:
					xPathString = string.Format("/{0}//*[@*[.='{1}']][1]", _rootname, lookfor);
					break;
			}

			// find it
			XmlNode findElm = clsTemplaterXMLObj.SelectSingleNode(xPathString);

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
				lookup_value = findElm.InnerText;

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
			httpC.Response.Cookies[Name].Value = Value;

			XmlElement newElm;
			XmlNode c;
			c = clsTemplaterXMLObj.SelectSingleNode(string.Format("{0}/TMPLT/BROWSER/COOKIEVARS", _rootname));

			//look for existing cookies var
			newElm = (XmlElement)c.SelectSingleNode(string.Format("ELEMENT[@name='{0}']", Name));

			if (newElm == null)
			{
				newElm = clsTemplaterXMLObj.CreateElement("ELEMENT");
				c.AppendChild(newElm);
			}

			newElm.SetAttribute("name", "", Name);
			newElm.InnerText = Value;


		}

		/// <summary>
		/// Clear a cookie from the list
		/// </summary>
		/// <param name="Name"></param>
		public virtual void ClearCookie(string Name)
		{
			httpC.Response.Cookies[Name].Value = null;
			httpC.Response.Cookies[Name].Expires = DateTime.Now.AddDays(-100);

			XmlNode delNode;
			delNode = clsTemplaterXMLObj.SelectSingleNode(string.Format("{0}/TMPLT/BROWSER/COOKIEVARS/ELEMENT[@name='{1}']", _rootname, Name));

			if (!(delNode == null))
				clsTemplaterXMLObj.SelectSingleNode(string.Format("{0}/TMPLT/BROWSER/COOKIEVARS", _rootname)).RemoveChild(delNode);

		}





		
		#endregion		
		/************************************************************
		*	PUBLIC PROPERTIES
		************************************************************/
		#region Public Properties

		public string Links
		{
			set
			{
				try 
				{
					XmlDocument xmLinks = new XmlDocument();
					xmLinks.Load(value);
					XPathNavigator nav;
					XPathNodeIterator i;
					
					nav = xmLinks.CreateNavigator();
					i = nav.Select("/");

					AddXslParameter("LINKS",i);
					

					
				}
				catch{}


			}
		}



		public string XSLSource
		{
			set 
			{
				T.XSLSource = value;
			}
			get
			{
				return T.XSLSource;
			}
		}
		public String Result
		{
			get 
			{
				return T.ResultText;
			}			
		}
		#endregion





	}
}

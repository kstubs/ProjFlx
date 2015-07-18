using System;
using System.Web;
using System.Xml;
using ProjectUtilities.XMLParser;


namespace ProjectUtilities.WebTemplater
{
	
	/// <summary>
	/// Summary description for myXMLWebTemplater.
	/// </summary>
	public class XMLWebTemplater
	{
		private string clsHTMLResponse = "";
		private XmlDocument classXMLObjectSource;

		// resolve current server object
		private HttpServerUtility _server = HttpContext.Current.Server;
		private HttpResponse _response = HttpContext.Current.Response;
		private HttpRequest _request = HttpContext.Current.Request;

		public XMLWebTemplater()
		{
			XmlDocument xmDocTest = new XmlDocument();
			xmDocTest.Load(@"c:\temp\T.xml");
			

			xmlTransformer T = new xmlTransformer();
			T.XSLSource = _server.MapPath(@"ProjectUtilities\Documents\WEB_DOCUMENT_TEMPLATE.xsl");
			T.XMLObjectSource = xmDocTest;

			T.Transform();
			clsHTMLResponse = T.ResultText;
		}

		public XmlDocument XMLObjectSource
		{
			get
			{
				return(classXMLObjectSource);
			}
			set 
			{
				classXMLObjectSource = value;
			}
		}


		public string Result 
		{
			get 
			{
				return clsHTMLResponse;
			}
		}

		private void test() 
		{
			
		}
	}
}

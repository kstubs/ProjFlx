using System;
using System.IO;
using System.Security;
using System.Security.Policy;
using System.Web;
using System.Xml;
using System.Xml.XPath;
using System.Xml.Xsl;
using ProjectUtilities.Exceptions;

namespace ProjectUtilities.XMLParser
{

	public class xmlTransformer
	{

		/************************************************************
		*	CLASS DECLERATION
		************************************************************/
		#region Class Decleration

		protected string clsClass = "xmlTransformer";
		protected string classXSLObjectSource;
		protected XmlDocument classXMLObjectSource;
		protected FileStream clsFileIO;
		protected HttpResponse clsHTTPResponse;
		protected XmlDocument clsXMLObjectReturn;
		protected string clsTEXTResult = "";
		protected XsltArgumentList clsXSLArgs = new XsltArgumentList();
		#endregion
		/************************************************************
		*	PRIVATE METHODS AND FUNCTIONS
		************************************************************/
		#region Private Methods and Functions
		private void _Transform()
		{

			try
			{

				//validate before we get started
				if (classXSLObjectSource == "")
				{
					ProjectExceptionArgs args = new ProjectExceptionArgs("Stylesheet Not Se", "xmlTransformer", "_Transform", null, SeverityLevel.Critical, LogLevel.Event);
					throw new XMLParserException(args);
				}

				else if (!File.Exists(classXSLObjectSource))
				{
					string errmsg = string.Format("Could not find stylesheet: {0}", classXSLObjectSource);
					ProjectExceptionArgs args = new ProjectExceptionArgs(errmsg, "_Transform", "_Transform", null, SeverityLevel.Critical, LogLevel.Event);
					throw new XMLParserException(args);
				}

				//check for XML Source
				//load up empty if non existent
				if (classXMLObjectSource == null)
				{
					classXMLObjectSource = new XmlDocument();
					classXMLObjectSource.LoadXml(@"<?xml version='1.0'?><ROOT/>");
				}

				//create a navigator object
				XPathNavigator xn;
				xn = classXMLObjectSource.CreateNavigator();

				//run the transformation
				xslTransformer(xn, clsXSLArgs);

			}
			catch (IOException e)
			{
				string errmsg = "FileIO Transformation Error";
				ProjectExceptionArgs args = new ProjectExceptionArgs(errmsg, "xmlTransformer", "_Transform", null, SeverityLevel.Critical, LogLevel.Event);
				throw new XMLParserException(args, e);
			}
			catch (XmlException e)
			{
				string errmsg = "XmlException Transformation Error";
				ProjectExceptionArgs args = new ProjectExceptionArgs(errmsg, "xmlTransformer", "_Transform", null, SeverityLevel.Critical, LogLevel.Event);
				throw new XMLParserException(args, e);
			}
			catch (HttpException e)
			{
				string errmsg = "HttpResponse Transformation Error";
				ProjectExceptionArgs args = new ProjectExceptionArgs(errmsg, "xmlTransformer", "_Transform", null, SeverityLevel.Critical, LogLevel.Event);
				throw new XMLParserException(args, e);
			}
		}


		#endregion
		/************************************************************
		*	PUBLIC METHODS AND FUNCTIONS
		************************************************************/
		#region Public Methods and Functions
		public xmlTransformer()
		{
			
		}
		public virtual XmlDocument XMLObjectSource
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
			

		public virtual  String XSLSource
		{
			get 
			{

				return(classXSLObjectSource);
			}
			set 
			{
				classXSLObjectSource = value;
			}
		}

		public virtual XmlDocument ResultXML
		{
			get
			{
				return clsXMLObjectReturn;

			}
		}

		public virtual string ResultText
		{
			get 
			{
				return clsTEXTResult;
			}
		}

		public void AddXslParameter(string name, string value) 
		{
			clsXSLArgs.AddParam(name,"",value);
			
		}

		public void AddXslParameter(string name, XPathNodeIterator nodeit) 
		{
			clsXSLArgs.AddParam(name,"",nodeit);
		}


		/// <summary>
		/// By default results are string
		/// </summary>
		public void Transform()
		{
			_Transform();

		}		
		/// <summary>
		/// Transform and return results XML
		/// </summary>
		/// <param name="xm"></param>
		public void Transform(out XmlDocument xm)
		{
			clsXMLObjectReturn = new XmlDocument();

			// be sure we have just one output
			clsHTTPResponse = null;
			clsFileIO = null;

			// perform transformation
			_Transform();
			xm = clsXMLObjectReturn;
		}

		/// <summary>
		/// Transform to FILE IO
		/// </summary>
		/// <param name="fileIO"></param>
		public void Transform(FileStream fileIO) 
		{
			// be sure we have just one output
			clsHTTPResponse = null;
			clsXMLObjectReturn = null;

			clsFileIO = fileIO;
			_Transform();

		}
		/// <summary>
		/// Transform to HTTP Response Stream
		/// </summary>
		/// <param name="httpR"></param>
		public void Transform(HttpResponse httpR) 
		{

			// be sure we have just one output
			clsFileIO = null;
			clsXMLObjectReturn = null;
			
			clsHTTPResponse = httpR;

			_Transform();
		}
		public virtual void xslTransformer(XPathNavigator xNav, XsltArgumentList xArgs) 
		{
			//get xsl source
			XslCompiledTransform xslObj = new XslCompiledTransform();
			XsltSettings settings = new XsltSettings(true, true);
			
			
			//default permission set
			//Evidence ev = XmlSecureResolver.CreateEvidenceForUrl(classXSLObjectSource);
			//PermissionSet ps = SecurityManager.ResolvePolicy(ev);

			

			//modify access for specific domain
			//			WebPermission myWebPermission = new WebPermission(NetworkAccess.Connect, 
			//				"http://www.example.com");
			//			myPermissions.SetPermission(myWebPermission);

		
			xslObj.Load(classXSLObjectSource,settings, new XmlUrlResolver());

			StringWriter sWrit = new StringWriter();

			//transform the xml
			if ( !(clsFileIO == null) ) 
			{
				//xslObj.Transform(xNav,xArgs,clsFileIO,xmlSR);
				xslObj.Transform(xNav,xArgs,clsFileIO);
				clsTEXTResult = "FILE SAVED";
			}
			else if ( !(clsHTTPResponse == null) )
			{
				xslObj.Transform(xNav,xArgs,clsHTTPResponse.OutputStream);
				clsTEXTResult = "OUTPUT STREAMED TO HTTP RESPONSE";
			}
			else if ( !(clsXMLObjectReturn == null) )
			{
				xslObj.Transform(xNav,xArgs,sWrit);
				clsXMLObjectReturn.LoadXml(sWrit.ToString());
			}
			else 
			{
				xslObj.Transform(xNav,xArgs,sWrit);

				// default - results to text
				clsTEXTResult = sWrit.ToString();
			}





		}
		#endregion
		




	}
}

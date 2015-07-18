using System;
using System.Xml;
using System.Xml.Xsl;
using System.Xml.XPath;
using System.IO;
using System.Text;
using Mvp.Xml.Common.Xsl;

namespace ProjectUtilities.XMLParser
{
    public class xmlTransformerMvp : xmlTransformer
    {


        public override void xslTransformer(XPathNavigator xNav, XsltArgumentList xArgs)
        {
            XsltSettings settings = new XsltSettings(true, true);
            XmlUrlResolver r = new XmlUrlResolver();
            
            MvpXslTransform xslObj = new MvpXslTransform();
            xslObj.Load(classXSLObjectSource, settings, r);
            XmlInput xm = new XmlInput(xNav);
            XmlOutput xo;

            if (!(clsFileIO == null))
            {
                xo = new XmlOutput(clsFileIO);
                xslObj.Transform(xm, xArgs, xo);
                clsTEXTResult = "FILE SAVED";
            }
            else if (!(clsHTTPResponse == null))
            {
                xo = new XmlOutput(clsHTTPResponse.OutputStream);
                xslObj.Transform(xm, xArgs, xo);
                clsTEXTResult = "OUTPUT STREAMED TO HTTP RESPONSE";
            }
            else if (!(clsXMLObjectReturn == null))
            {
    			StringWriter sWrit = new StringWriter();
                xo = new XmlOutput(sWrit);

                xslObj.Transform(xm, xArgs, xo);
                clsXMLObjectReturn.LoadXml(sWrit.ToString());
            }
            else
            {
                StringWriter sWrit = new StringWriter();
                xo = new XmlOutput(sWrit);

                xslObj.Transform(xm, xArgs, xo);

                // default - results to text
                clsTEXTResult = sWrit.ToString();

            }

        }
    }
}

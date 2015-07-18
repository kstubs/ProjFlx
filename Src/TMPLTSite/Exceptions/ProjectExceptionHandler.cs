using System;
using System.Xml;
using System.Collections.Generic;
using System.Text;

namespace ProjectFlx.Exceptions
{

    public class ProjectExceptionHandler
    {
        private XmlDocument clsXmErrors = null;

        public ProjectExceptionHandler() { }

        void Initialize()
        {
            if (clsXmErrors == null)
            {
                clsXmErrors = new XmlDocument();
                clsXmErrors.LoadXml("<errors/>");
            }

        }

        public void Add(string Message)
        {
            Initialize();

            XmlElement newElm = clsXmErrors.CreateElement("error");
            clsXmErrors.DocumentElement.AppendChild(newElm);

            newElm.InnerText = Message;
            newElm.SetAttribute("severity", SeverityLevel.Fatal.ToString());

        }

        public void Add(ProjectExceptionArgs Args)
        {
            Initialize();

            XmlElement newElm = clsXmErrors.CreateElement("error");
            clsXmErrors.DocumentElement.AppendChild(newElm);

            newElm.SetAttribute("severity", Args.Severity.ToString());
            newElm.SetAttribute("log", Args.Log.ToString());

            if (Args.SourceClass != null)
                newElm.SetAttribute("sourceclass", Args.SourceClass);

            if (Args.SourceMethod != null)
                newElm.SetAttribute("sourcemethod", Args.SourceMethod);

            if (Args.SourceSnippet != null)
                newElm.SetAttribute("sourcesnippet", Args.SourceSnippet);

            if (Args.Message != null)
                newElm.InnerText = Args.Message;

        }

        public XmlDocument XmlDocument
        {
            get
            {
                return (clsXmErrors == null ? null : clsXmErrors);
            }
        }
    }
}

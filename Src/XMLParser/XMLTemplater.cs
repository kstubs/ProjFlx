using System;
using System.Xml;

namespace ProjectUtilities.XMLParser
{
    /// <summary>
    /// Simple class for catching many XML Sources
    /// </summary>
    public class XMLTemplater
    {
        public XmlDocument clsTemplaterXMLObj;

        protected string _rootname = "ROOT";
        protected bool clsFindWhenEmpty = false;

        public XMLTemplater()
        {
            clsTemplaterXMLObj = new XmlDocument();
            clsTemplaterXMLObj.LoadXml(string.Format(@"<?xml version=""1.0""?><{0}/>", _rootname));
        }

        /// <summary>
        /// Pass in name of your own <root/> element
        /// </summary>
        /// <param name="root"></param>
        public XMLTemplater(string rootname)
        {
            clsTemplaterXMLObj = new XmlDocument();
            clsTemplaterXMLObj.LoadXml(string.Format(@"<?xml version=""1.0""?><{0}/>", rootname));
        }

        public XmlDocument DOCxml
        {
            get
            {
                return clsTemplaterXMLObj;
            }
        }

        public bool FindWhenEmpty
        {
            set
            {
                clsFindWhenEmpty = value;
            }
            get
            {
                return clsFindWhenEmpty;
            }
        }
        /// <summary>
        /// Stubbed method for override
        /// </summary>
        /// <param name="newXML"></param>
        public virtual void AddXML(XmlDocument newXML)
        {
            XmlNode newElm = clsTemplaterXMLObj.ImportNode(newXML.DocumentElement, true);
            clsTemplaterXMLObj.DocumentElement.AppendChild(newElm);
        }


        /// <summary>
        /// Public AddXML routine
        /// Adds New Node named XMLSourceName to APPLICATION Node
        /// Along with XML Data in newXML
        /// If XMLSourceName is null, use original XML Source as new Node
        /// </summary>
        /// <param name="XMLSourceName"></param>
        /// <param name="newXML"></param>
        public virtual void AddXML(string XMLSourceName, XmlDocument newXML)
        {

            //TODO:  NEED TRY CATCH HERE
            //SHOULDN'T FAIL ON ATTEMPT TO ADD XML


            XmlElement newElm;
            XmlNode importNode;

            try
            {
                //import passed xml
                importNode = clsTemplaterXMLObj.ImportNode(newXML.DocumentElement, true);
            }
            catch
            {
                importNode = clsTemplaterXMLObj.CreateNode(XmlNodeType.Element, "IMPORT_NODE_FAIL", "");
            }

            //see if element exists
            newElm = (XmlElement)clsTemplaterXMLObj.DocumentElement.SelectSingleNode(XMLSourceName);

            // create the element if non existent
            if (newElm == null)
            {
                newElm = clsTemplaterXMLObj.CreateElement(XMLSourceName);
                clsTemplaterXMLObj.DocumentElement.AppendChild(newElm);
            }

            // append imported xml to xml source
            newElm.AppendChild(importNode);
        }


        //TODO: FUTURE - NOT YET IMPLEMENTED
        /// <summary>
        /// Place all class level xml source in TMPLT node
        /// </summary>
        /// <param name="XMLSourceName"></param>
        /// <param name="newXML"></param>
        private void _AddXML(string NewElementName, string NewAttributeName, string NewAttributeValue, bool AppendLikeElement, XmlDocument newXML)
        {
            XmlElement newElm;

            // Are we creating a new element?
            if (!(NewElementName == null) && AppendLikeElement == false)
            {
                newElm = clsTemplaterXMLObj.CreateElement(NewElementName);
                clsTemplaterXMLObj.DocumentElement.AppendChild(newElm);
            }

            // Are we locating an existing element?
            if (!(NewElementName == null) && AppendLikeElement == true)
            {
                try
                {
                    newElm = (XmlElement)clsTemplaterXMLObj.DocumentElement.SelectSingleNode(NewElementName);
                }
                finally { }
            }


        }

        public void AddTag(string Tag, int Value)
        {
            _AddTag(Tag, Convert.ToString(Value), null, null, false);
        }
        public void AddTag(string Tag, string Value)
        {
            _AddTag(Tag, Value, null, null, false);
        }
        public void AddTag(string Tag, bool Value)
        {
            string sBoolVal = "False";

            if (Value)
                sBoolVal = "True";

            _AddTag(Tag, sBoolVal, null, null, false);
        }

        public void AddCommentTag(string Tag, int Comment)
        {
            _AddTag(Tag, Convert.ToString(Comment), null, null, true);
        }
        public void AddCommentTag(string Tag, string Comment)
        {
            _AddTag(Tag, Comment, null, null, true);
        }

        public void AddCommentTag(string Tag, string Comment, string SourceClass, string SourceMethod)
        {
            _AddTag(Tag, Comment, SourceClass, SourceMethod, true);
        }


        private void _AddTag(string Tag, string Comment, string SourceClass, string SourceMethod, bool CommentOnly)
        {
            XmlElement elmTags = (XmlElement)clsTemplaterXMLObj.DocumentElement.SelectSingleNode("TMPLT/TAGS");
            XmlElement elmTag = clsTemplaterXMLObj.CreateElement("TAG");

            // add Tag
            if (elmTags == null)
            {
                // be sure we have TMPLT element
                XmlElement elmTMPLT = (XmlElement)clsTemplaterXMLObj.DocumentElement.SelectSingleNode("TMPLT");
                if (elmTMPLT == null)
                {
                    elmTMPLT = clsTemplaterXMLObj.CreateElement("TMPLT");
                    clsTemplaterXMLObj.DocumentElement.AppendChild(elmTMPLT);
                }

                elmTags = clsTemplaterXMLObj.CreateElement("TAGS");
                elmTMPLT.AppendChild(elmTags);
            }

            XmlAttribute att = clsTemplaterXMLObj.CreateAttribute("Name");
            att.Value = Tag;
            elmTag.Attributes.Append(att);
            elmTag.InnerText = Comment;

            // add comment attribute = true
            if (CommentOnly == true)
            {
                att = clsTemplaterXMLObj.CreateAttribute("Comment");
                att.Value = "True";
                elmTag.Attributes.Append(att);
            }

            // add Source Class
            if (!(SourceClass == null))
            {
                att = clsTemplaterXMLObj.CreateAttribute("Class");
                att.Value = SourceClass;
                elmTag.Attributes.Append(att);
            }

            // add Source method
            if (!(SourceMethod == null))
            {
                att = clsTemplaterXMLObj.CreateAttribute("Method");
                att.Value = SourceMethod;
                elmTag.Attributes.Append(att);
            }

            elmTags.AppendChild(elmTag);

        }





    }
}
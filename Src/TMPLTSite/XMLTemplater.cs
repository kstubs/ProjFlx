using System;
using System.Xml;

namespace ProjectFlx
{
    /// <summary>
    /// Simple class for catching many XML Sources
    /// </summary>
    public class XMLTemplater
    {
        public XmlDocument _xml;

        protected bool clsFindWhenEmpty = false;

        public XMLTemplater()
        {
            _xml = new XmlDocument();
            _xml.LoadXml("<flx/>");
        }

        public XmlDocument DOCxml
        {
            get
            {
                return _xml;
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
            XmlNode newElm = _xml.ImportNode(newXML.DocumentElement, true);
            _xml.DocumentElement.AppendChild(newElm);
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
                importNode = _xml.ImportNode(newXML.DocumentElement, true);
            }
            catch
            {
                importNode = _xml.CreateNode(XmlNodeType.Element, "IMPORT_NODE_FAIL", "");
            }

            //see if element exists
            newElm = (XmlElement)_xml.DocumentElement.SelectSingleNode(XMLSourceName);

            // create the element if non existent
            if (newElm == null)
            {
                newElm = _xml.CreateElement(XMLSourceName);
                _xml.DocumentElement.AppendChild(newElm);
            }

            // append imported xml to xml source
            newElm.AppendChild(importNode);
        }
        public virtual void AddXML(string XMLSourceName, XmlNode newXML)
        {
            var xm = new XmlDocument();
            var node = xm.ImportNode(newXML, true);
            xm.AppendChild(node);
            AddXML(XMLSourceName, xm);
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
                newElm = _xml.CreateElement(NewElementName);
                _xml.DocumentElement.AppendChild(newElm);
            }

            // Are we locating an existing element?
            if (!(NewElementName == null) && AppendLikeElement == true)
            {
                try
                {
                    newElm = (XmlElement)_xml.DocumentElement.SelectSingleNode(NewElementName);
                }
                finally { }
            }


        }

        public void AddTag(string Tag, int Value)
        {
            _AddTag(Tag, Convert.ToString(Value), null, null, false);
        }
        public void AddTag(string Tag, long Value)
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
        public void AddCommentTag(string Tag, object Comment)
        {
            _AddTag(Tag, Comment, null, null, true);
        }

        public void AddCommentTag(string Tag, object Comment, string SourceClass, string SourceMethod)
        {
            _AddTag(Tag, Comment, SourceClass, SourceMethod, true);
        }

        public void AddException(Exception Exception)
        {
            var ex = Exception;

            while (ex != null)
            {
				if (ex is ProjectFlx.Exceptions.ProjectException)
				{
					// handled exception
					var projex = (ProjectFlx.Exceptions.ProjectException)ex;

					if (projex.Args != null)
					{
						var msg = String.Format("{0}{1}", ex.Message, String.IsNullOrEmpty(projex.Args.SourceSnippet) ? "" : ", Near: " + projex.Args.SourceSnippet);
						_AddTag("ProjectFLX_ERROR",
							   msg,
							   String.IsNullOrEmpty(projex.Args.SourceClass) ? ex.Source : projex.Args.SourceClass,
							   String.IsNullOrEmpty(projex.Args.SourceMethod) ? ex.TargetSite.ToString() : projex.Args.SourceMethod,
							   false,
							   ex);
					}
					else
					{
						_AddTag("ProjectFLX_ERROR", null, null, null, false, ex);
					}
				}
				else
				{
					// unhandled exception
					_AddTag("UNHANDLED_ERROR", ex.Message, ex.Source, (ex.TargetSite == null) ? null : ex.TargetSite.ToString(), false, ex);
				}

                AddTag("STACK_TRACE", ex.StackTrace);
                
                ex = ex.InnerException;
            }
        }

		private void _AddTag(string Tag, object Comment, string SourceClass, string SourceMethod, bool CommentOnly)
		{
			_AddTag(Tag, Comment, SourceClass, SourceMethod, CommentOnly, null);
		}

		private void _AddTag(string Tag, object Comment, string SourceClass, string SourceMethod, bool CommentOnly, Exception Exception)
        {
            try
            {
                XmlElement elmTags = (XmlElement)_xml.DocumentElement.SelectSingleNode("tags");
                if (elmTags == null)
                {
                    elmTags = _xml.CreateElement("tags");
                    _xml.DocumentElement.AppendChild(elmTags);
                }
                XmlElement elmTag = _xml.CreateElement("tag");

                XmlAttribute att = _xml.CreateAttribute("Name");
                att.Value = Tag;
                elmTag.Attributes.Append(att);
                elmTag.InnerText = Convert.ToString(Comment);

                // add comment attribute = true
                if (CommentOnly == true)
                {
                    att = _xml.CreateAttribute("Comment");
                    att.Value = "True";
                    elmTag.Attributes.Append(att);
                }

                // add Source Class
                if (!(SourceClass == null))
                {
                    att = _xml.CreateAttribute("Class");
                    att.Value = SourceClass;
                    elmTag.Attributes.Append(att);
                }

                // add Source method
                if (!(SourceMethod == null))
                {
                    att = _xml.CreateAttribute("Method");
                    att.Value = SourceMethod;
                    elmTag.Attributes.Append(att);
                }

                // source exception
                if (!(Exception == null))
                {
                    att = _xml.CreateAttribute("Exception");
                    att.Value = Exception.GetType().Name;
                    elmTag.Attributes.Append(att);
                }

                elmTags.AppendChild(elmTag);
            }
            catch { }

        }





    }
}
﻿using System;
using System.Xml;
using System.Text;
using System.Text.RegularExpressions;
using System.Collections;
using System.Collections.Generic;
using ProjectFlx.Exceptions;
using ProjectFlx.Schema;
using System.Web.Caching;

namespace ProjectFlx.DB
{
    /// <summary>
    /// DatabaseQuery wrapper
    /// </summary>
    public class XObject : IXObject
    {
        #region private members
        DatabaseQuery _query = null;
        string _projectSqlName = null;
        XmlDocument _xmldocument = null;
        XmlNode _querynode = null;
        XmlDocument _xmregx = null;
        DBParameterFields _fields = new DBParameterFields();


        #endregion
        #region public members

        public ProjectFlx.Utility.TimingCollection Timing { get; set; }

        #region Cache Object
        private Cache _cache { get; set; }

        private string _cachePath;
        private int _hourexpires = 24;

        /// <summary>
        /// Default 24 hour cache
        /// </summary>
        /// <param name="Cache">Syste Web Cache</param>
        public void SetCache(Cache Cache)
        {
            SetCache(Cache, 24, null);
        }
        public void SetCache(Cache Cache, int HoursExpires)
        {
            SetCache(Cache, HoursExpires, null);
        }
        public void SetCache(Cache Cache, String CacheDependencyPath)
        {
            SetCache(Cache, 24, CacheDependencyPath);
        }
        public void SetCache(Cache Cache, int HoursExpires, String CacheDependencyPath)
        {
            _cache = Cache;
            _cachePath = CacheDependencyPath;
            _hourexpires = HoursExpires;
        }
        private bool _cachingEnabled = false;

        [Obsolete("Use Cache Object in ProjectSql.xml")]
        public bool CachingEnabled
        {
            get
            {
                return _cachingEnabled;
            }
            set
            {
                _cachingEnabled = value;
            }
        }
        #endregion

        [Obsolete ("Untested")]
        public XObject(DatabaseQuery Query, IProject Project)
        {
            _query = Query;
            if (Timing == null && Query.Timing != null)
                Timing = Query.Timing;

            _xmldocument = new XmlDocument();

            string serialized = Project.SchemaQuery.Serialize();
            string xml = String.Format("<queries>{0}</queries>", Regex.Replace(serialized, @"<\?xml.+?>", ""));

            _xmldocument.LoadXml(xml);
        }

        public XObject(DatabaseQuery Query, string ProjectSqlPath, string ProjectSqlName)
        {
            _query = Query;
            _query.Project = ProjectSqlName;
            if (Timing == null && Query.Timing != null)
                Timing = Query.Timing;
            _projectSqlName = ProjectSqlName;

            XmlDocument xm = new XmlDocument();

            // CONSIDER: this is loading xml everytime - should be cached
            xm.Load(ProjectSqlPath);

            string xpath = String.Format("/projectSql/child::node()[local-name()='{0}']", _projectSqlName);
            XmlNode node = xm.SelectSingleNode(xpath);

            if (node == null)
                throw new ProjectException(new ProjectExceptionArgs("Invalid ProjectSQL Doc", 
                    "ProjectFlx.DB.XObject, FLX", "XObject(DatabaseQuery Query, string ProjectSqlPath, string ProjectSqlName)",
                    String.Format("string xpath = String.Format(\"/projectSql/child::node()[local-name()='{0}']\"", _projectSqlName), SeverityLevel.Critical, LogLevel.Event));

            _xmldocument = new XmlDocument();
            _xmldocument.LoadXml("<queries/>");

            foreach (XmlNode query in node.ChildNodes)
            {
                var importnode = _xmldocument.ImportNode(query, true);
                _xmldocument.DocumentElement.AppendChild(importnode);
            }

        }

        /// <summary>
        /// Arbitrary label for the queryname like queryname#tag
        /// In SetQuery the name is split and the tag here assigned
        /// </summary>
        public string Tag { get; set; }

        /// <summary>
        /// Sets the query in the SqlProjectDocument
        /// </summary>
        /// <param name="QueryName"></param>
        public void SetQuery(string QueryName)
        {
            try
            {
                var q = QueryName.Split('#');
                var queryname = q[0];
                if (q.Length > 1)
                    this.Tag = q[1];
                _query.Clear();

                string xpath = String.Format("queries/query[@name='{0}']", queryname);

                var tempnode = _xmldocument.SelectSingleNode(xpath);
                if (tempnode == null)
                    throw new Exception("Invalid Query: " + queryname);

                _querynode = tempnode.Clone();

                // Build DB Paramter Fields
                _fields = new DBParameterFields();

                xpath = "parameters/parameter[@lookup][@lookup_value][not(@inout='out')]";
                XmlNodeList nodeparms = _querynode.SelectNodes(xpath);

                foreach (XmlNode node in nodeparms)
                    _fields.Add(new DBParameterField(node.Attributes["name"].Value, node.Attributes["lookup_value"].Value));

                // paging - limit
                xpath = "paging/limit";
                var node2 = _querynode.SelectSingleNode(xpath);
                if (node2 != null)
                {
                    int limit = -1;
                    int.TryParse(node2.InnerText, out limit);
                    _query.Limit = limit;
                }

                
            }
            catch (Exception unhandled)
            {
                ProjectExceptionArgs args = new ProjectExceptionArgs("Unhandled Exception Occured",
                    "mso.Utility.XObject", "SetQuery", null, SeverityLevel.Critical, LogLevel.Event);

                throw new ProjectException(args, unhandled);
            }
        }

        public void SetParameter(string ParamName, object ParamValue, string RegXPatternName)
        {
            if (_xmregx != null)
            {
                string regXPattern = null;

                string xpath = string.Format("root/regx[@name='{0}']", RegXPatternName);
                XmlNode regNode = _xmregx.SelectSingleNode(xpath);

                if (regNode != null)
                {
                    regXPattern = regNode.InnerText;

                    if (!Regex.IsMatch(ParamValue.ToString(), regXPattern, RegexOptions.IgnoreCase))
                        throw new ProjectException("Validation Error for field: " + ParamName);
                }
            }

            _SetParameter(ParamName, ParamValue.ToString());

        }
        private void _SetParameter(string ParamName, string ParamValue)
        {
            try
            {
                if (_querynode == null)
                    throw new Exception("Query not set, call SetQuery first");

                string xpath = String.Format("parameters/parameter[@name='{0}']", ParamName);
                XmlNode node = _querynode.SelectSingleNode(xpath);
                if (node == null)
                    throw new Exception("Parameter not found: " + ParamName);

                if (ParamValue != null)
                    node.InnerText = ParamValue;

            }
            catch (Exception unhandled)
            {

                ProjectExceptionArgs args = new ProjectExceptionArgs
                ("SetParameter fails for ParamName: " + ParamName, "mso.Utility.XObject", "_SetParameter", null, SeverityLevel.Critical, LogLevel.Event);
                throw new ProjectException(args, unhandled);
            }
        }
        public void SetParameter(string ParamName, object ParamValue)
        {
            if (ParamValue == null)
                _SetParameter(ParamName, null);
            else
                _SetParameter(ParamName, ParamValue.ToString());
        }
        public void SetParameter(parameters Parameters)
        {
            foreach (parameter parm in Parameters.parameter)
            {
                SetParameter(parm.name, parm.Text.Flatten());
            }
        }
        /// <summary>
        /// Run the query
        /// </summary>
        public void Query()
        {
            if (Timing != null)
                if (_query.Timing == null)
                    _query.Timing = Timing;

            if (_cache != null)
                _query.SetCache(_cache, _cachePath);

            if (_querynode == null)
                throw new Exception("Invalid operation in DB.XObject.Query, _querynode is NULL");

            _query.Query(_querynode);
        }
        public XmlDocument XmlDocument
        {
            get
            {
                return _query.Result;
            }
        }
        public int RowsAffected
        {
            get
            {
                return _query.RowsAffected;
            }
        }

        public DatabaseQuery DatabaseQuery
        {
            get
            {
                return _query;
            }
            set
            {
                _query = value;
            }
        }
        #endregion

        #region public Properties
        public DBParameterFields ParameterFields
        {
            get
            {
                return _fields;
            }
        }
        #endregion

        public static System.Collections.Specialized.NameValueCollection FillParameters(System.Web.HttpContext httpContext, XObject _xobject)
        {
            System.Collections.Specialized.NameValueCollection namesvalues = new System.Collections.Specialized.NameValueCollection();

            foreach(DBParameterField fld in _xobject.ParameterFields)
            {
                var q = (string[])httpContext.Request.Form.GetValues(fld.AliasName);

                foreach (string v in q)
                    namesvalues.Add(fld.ParameterName, v);                
            }

            return namesvalues;
        }
    }

    public class DBParameterFields : IEnumerable<DBParameterField>
    {
        List<DBParameterField> _fields = new List<DBParameterField>();

        public void Add(DBParameterField Field)
        {
            _fields.Add(Field);
        }

        /// <summary>
        /// Return Stored Procedure Parameter Name for given Alias Name
        /// </summary>
        /// <param name="AliasName">Name of the Alias (might be used on webform)</param>
        /// <returns>Stored Procedure Parameter Name</returns>
        public string this[string AliasName]
        {
            get
            {
                return _fields.Find(delegate(DBParameterField f) { return f.AliasName.Equals(AliasName); }).ParameterName;
            }
        }


        #region IEnumerable<DBParameterField> Members

        public IEnumerator<DBParameterField> GetEnumerator()
        {
            return _fields.GetEnumerator();
        }

        #endregion

        #region IEnumerable Members

        IEnumerator IEnumerable.GetEnumerator()
        {
            return _fields.GetEnumerator();
        }

        #endregion
    }

    /// <summary>
    /// Database Parameter Name match with Friendly Alias Name (might be used on web form)
    /// </summary>
    public struct DBParameterField
    {
        string _parameterName;
        public string ParameterName
        {
            get { return _parameterName; }
            set { _parameterName = value; }
        }

        string _aliasName;
        public string AliasName
        {
            get { return _aliasName; }
            set { _aliasName = value; }
        }

        public DBParameterField(string DBParamName, string AliasName)
        {
            _parameterName = DBParamName;
            _aliasName = AliasName;
        }

        public override bool Equals(object obj)
        {
            if (!(obj is DBParameterField))
                return false;

            var thisobj = (DBParameterField)obj;

            return
                thisobj.AliasName.Equals(this.AliasName) &&
                thisobj.ParameterName.Equals(this.ParameterName);
        }
    }
    

}

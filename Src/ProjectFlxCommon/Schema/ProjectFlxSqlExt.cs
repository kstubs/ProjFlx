﻿using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using System.Xml;
using ProjectFlx.Schema.Extra;
using ProjectFlx.Exceptions;

namespace ProjectFlx.Schema
{
    public partial class projectResults
    {
        /// <summary>
        /// Gets parameter value for named parameter off the default schema object and result
        /// </summary>
        /// <param name="ParameterName">Paramter name to lookup</param>
        /// <returns>String value (or null)</returns>
        public string getParameterValue(string ParameterName)
        {
            parameter parm = results[0].schema[0].parameters.parameter.Find(delegate(parameter p) { return p.name == ParameterName; });
            return Helper.FlattenList(parm.Text);
        }

        /// <summary>
        /// Set parameter value for named parameter
        /// </summary>
        /// <param name="ParameterName"></param>
        /// <param name="ParameterValue"></param>
        public void setParameterValue(string ParameterName, string ParameterValue)
        {
            parameter parm = results[0].schema[0].parameters.parameter.Find(delegate(parameter p) { return p.name == ParameterName; });
            parm.Text = new List<string>();
            if (parm != null)
                parm.Text.Add(ParameterValue);
        }

        /// <summary>
        /// Clears parameter value for named parameter.  Useful to clear sensitive parameter values like passwords
        /// </summary>
        /// <param name="ParameterName"></param>
        public void clearParameterValue(string ParameterName)
        {
            parameter parm = results[0].schema[0].parameters.parameter.Find(delegate(parameter p) { return p.name == ParameterName; });
            if (parm != null)
                parm.Text = new List<string>();
        }

    }

    public partial class parameters
    {
        public void AddParameter(String Name, String Value)
        {
            parameter parm;
            if (this.parameter == null)
                this.parameter = new List<parameter>();

            this.parameter.Add(parm = new parameter() { name = Name, type=fieldType.text });
            parm.Text = new List<string>();
            parm.Text.Add(Value);
        }

        public parameters Clone()
        {
            var text = this.Serialize();
            return Deserialize(text);
        }
    }

    public partial class SchemaQueryType
    {
        /// <summary>
        /// Get a SchemaQueryType for quickly building a query to execute
        /// </summary>
        /// <param name="QueryName"></param>
        /// <param name="Action"></param>
        /// <param name="CommandName"></param>
        /// <returns></returns>
        public static SchemaQueryType Create(string QueryName, actionType CommandType, string CommandName, string CommandText = null)
        {
            var query = new ProjectFlx.Schema.SchemaQueryType();
            query.name = QueryName;
            query.command = new ProjectFlx.Schema.command();
            query.command.name.Text.Add(CommandName);
            query.command.action = CommandType;

            if (!String.IsNullOrEmpty(CommandText))
            {
                query.command.text = new mixedContent();
                query.command.text.Text = new List<string>();
                query.command.text.Text.Add(CommandText);
                query.command.type = new commandType();
                query.command.type = commandType.Select;
            }

            query.fields = new List<ProjectFlx.Schema.field>();
            query.parameters = new ProjectFlx.Schema.parameters();

            return query;
        }

        public static SchemaQueryType Create(string QueryName, actionType CommandType, string CommandName, string CommandText, XmlNodeList Fields)
        {
            var query = Create(QueryName, CommandType, CommandName, CommandText);
            foreach (XmlNode node in Fields)
            {

                fieldType fieldtype = (fieldType)Enum.Parse(typeof(fieldType), node.Attributes["type"].Value);
                query.fields.Add(new field()
                {
                    name = node.Attributes["name"].Value,
                    type = fieldtype
                });
            }

            return query;

        }

        public parameter AddParameter(string Name, object Value)
        {
            return this.AddParameter(Name, Value, fieldType.text);
        }
        /// <summary>
        /// Adds a new Parameter to the Parameter list
        /// guarantees the object and list are created
        /// and return the newly created parameter back for 
        /// convenience of setting additional parameters
        /// </summary>
        /// <param name="Name"></param>
        /// <param name="Value"></param>
        /// <param name="FieldType"></param>
        /// <returns></returns>
        public parameter AddParameter(string Name, object Value, fieldType FieldType)
        {
            return AddParameter(Name, Value, FieldType, inoutType.@in);
        }

        public parameter AddParameter(string Name, object Value, fieldType FieldType, inoutType InOut)
        {
            return AddParameter(Name, Value, FieldType, InOut, 0);
        }

        public parameter AddParameter(string Name, object Value, fieldType FieldType, inoutType InOut, int Size = 0)
        {
            if (this.parameters == null)
                this.parameters = new parameters();

            if (this.parameters.parameter == null)
                this.parameters.parameter = new List<parameter>();

            var parm = new parameter() { name = Name, type = FieldType, inout = InOut };

            var temp = this.parameters.parameter.Where(a => a.name == Name).FirstOrDefault();
            if (temp != null)
                this.parameters.parameter.Remove(temp);

            this.parameters.parameter.Add(parm);
            parm.Text = new List<string>();

            if(Size != 0) {
                parm.size = Size;
                parm.sizeSpecified = true;
            }

            if (Value != null)
                parm.Text.Add(Value.ToString());

            return parm;

        }

        public XmlNode QueryNode
        {
            get
            {
                XmlDocument xml = new XmlDocument();
                xml.LoadXml(this.Serialize());

                return xml.SelectSingleNode("/query");
            }
        }
    }

    public partial class SchemaType
    {
        public static SchemaType GetSchemaType(string ProjSqlPath, string Name)
        {
            var xm = new XmlDocument();
            xm.Load(ProjSqlPath);
            var xpath = string.Format("/*/*[local-name()='{0}']", Name);
            var node = xm.SelectSingleNode(xpath);

            var schemanode = xm.CreateElement("schema");
            foreach (XmlNode node2 in node.SelectNodes("query"))
            {
                schemanode.AppendChild(node2);
            }

            return SchemaType.Deserialize(schemanode.OuterXml);
        }
    }

    public partial class result
    {
        /// <summary>
        /// Finds first row in result with matching Key and Value
        /// </summary>
        /// <param name="Key"></param>
        /// <param name="Value"></param>
        /// <returns></returns>
        public row FindRow(String Key, String Value)
        {
            var row = this.rowField.Find(r => r.AnyAttr.LookupValue(Key).Equals(Value));
            return row;
        }
        public row FindRow(String Key, int Value)
        {
            var row = this.rowField.Find(r => r.AnyAttr.LookupValue(Key).Equals(Value.ToString()));
            return row;
        }

        /// <summary>
        /// First all rows in result with matching Key and Value
        /// </summary>
        /// <param name="Key"></param>
        /// <param name="Value"></param>
        /// <returns></returns>
        public List<row> FindAllRows(String Key, String Value)
        {
            var rows = this.rowField.FindAll(r => r.AnyAttr.LookupValue(Key).Equals(Value));
            return rows;
        }
        public List<row> FindAllRows(String Key, int Value)
        {
            var rows = this.rowField.FindAll(r => r.AnyAttr.LookupValue(Key).Equals(Value));
            return rows;
        }
    }

    public partial class subresult
    {
        /// <summary>
        /// Finds first row in result with matching Key and Value
        /// </summary>
        /// <param name="Key"></param>
        /// <param name="Value"></param>
        /// <returns></returns>
        public row FindRow(String Key, String Value)
        {
            var row = this.rowField.Find(r => r.AnyAttr.LookupValue(Key).Equals(Value));
            return row;
        }
        public row FindRow(String Key, int Value)
        {
            var row = this.rowField.Find(r => r.AnyAttr.LookupValue(Key).Equals(Value.ToString()));
            return row;
        }

        /// <summary>
        /// First all rows in result with matching Key and Value
        /// </summary>
        /// <param name="Key"></param>
        /// <param name="Value"></param>
        /// <returns></returns>
        public List<row> FindAllRows(String Key, String Value)
        {
            var rows = this.rowField.FindAll(r => r.AnyAttr.LookupValue(Key).Equals(Value));
            return rows;
        }
        public List<row> FindAllRows(String Key, int Value)
        {
            var rows = this.rowField.FindAll(r => r.AnyAttr.LookupValue(Key).Equals(Value));
            return rows;
        }
    }



    namespace Extra
    {
        public static class Extensions
        {

            /// <summary>
            /// Assumes first row in result set and
            /// And returns value for given attribute by name
            /// </summary>
            /// <param name="Attributes"></param>
            /// <param name="Name"></param>
            /// <returns></returns>
            public static string LookupValue(this result Result, string Name)
            {
                try
                {
                    if (Result.row != null && Result.row.Count == 0)
                        return String.Empty;

                    var row = Result.row[0];

                    if (row == null) return String.Empty;
                    return row.AnyAttr.LookupValue(Name);
                }
                catch { return string.Empty; }
            }

            /// <summary>
            /// Return value for given attribute by name
            /// </summary>
            /// <param name="Row"></param>
            /// <param name="Name"></param>
            /// <returns></returns>
            public static string LookupValue(this row Row, string Name)
            {
                if (Row == null) return String.Empty;
                return Row.AnyAttr.LookupValue(Name);
            }
            /// <summary>
            /// Return <T> for given attribute by name
            /// </summary>
            /// <param name="Row"></param>
            /// <param name="Name"></param>
            /// <returns></returns>
            public static T LookupValue<T>(this row Row, string Name)
            {
                return (T)Convert.ChangeType(Row.AnyAttr.LookupValue(Name), typeof(T));
            }
            /// <summary>
            /// Return value for give attribute by name
            /// </summary>
            /// <param name="Attributes"></param>
            /// <param name="Name"></param>
            /// <returns></returns>
            public static string LookupValue(this List<XmlAttribute> Attributes, string Name)
            {
                try
                {
                    var x = Attributes.Find(a => a.Name.Equals(Name));
                    return x == null ? string.Empty : x.Value.ToString();
                }
                catch { return string.Empty; }
            }

            public static XmlAttribute GetAttribute(this row Row, string Name)
            {
                return Row.AnyAttr.FirstOrDefault(a => a.Name == Name);
            }

            /// <summary>
            /// Parameter by Name
            /// </summary>
            /// <param name="Parameters"></param>
            /// <param name="Name"></param>
            /// <returns></returns>
            public static parameter Lookup(this parameters Parameters, string Name)
            {
                return Parameters.parameter.FirstOrDefault(p => { return p.name.Equals(Name); });
            }

            /// <summary>
            /// Parameter value by Name
            /// </summary>
            /// <param name="Parameters"></param>
            /// <param name="Name"></param>
            /// <returns></returns>
            public static string LookupValue(this parameters Parameters, string Name)
            {
                var parm = Parameters.parameter.FirstOrDefault(p => { return p.name.Equals(Name); });
                if(parm == null) return string.Empty;
                return parm.Value();
            }

            public static string Value(this parameter Parameter)
            {
                return Parameter.Text != null && Parameter.Text.Count > 0 ? Parameter.Text.Flatten() : "";
            }

            /// <summary>
            /// Find All projectResults by Name
            /// </summary>
            /// <param name="Results"></param>
            /// <param name="Name"></param>
            /// <returns></returns>
            public static List<results> FindAll(this projectResults Results, string Name)
            {
                return Results.results.FindAll(r => { return r.name.Equals(Name); });
            }

            public static result Lookup(this List<results> Results, String Name)
            {
                var results = Results.Where(w => w.name == Name).LastOrDefault();
                if (results == null) return null;
                else
                    return results.result;
            }

            public static result Lookup(this projectResults Results, String Name)
            {
                return Results.results.Lookup(Name);
            }

            public static bool HasRows(this result result)
            {
                if (result == null) return false;
                if (result.row == null) return false;
                if (result.row.Count == 0) return false;

                return true;
            }

            public static bool HasRows(this projectResults Results, String Name)
            {
                return HasRows(Results.results.Lookup(Name));
            }

            public static result Lookup(this projectResults Results, String Name, String ParameterName, String ParameterValue)
            {
                var temp1 = Results.results.Where((r) => { return r.name.Equals(Name); }).FirstOrDefault();
                var temp2 = temp1.schema.FirstOrDefault().parameters.parameter.Find((f) => { return f.name.Equals(ParameterName); });
                var temp3 = temp1.schema.FirstOrDefault().parameters.Lookup(ParameterName);

                var result = Results.results.Where((r) => {
                    return r.name.Equals(Name) &&
                    r.schema[0].parameters.Lookup(ParameterName).Value().Equals(ParameterValue); }).FirstOrDefault();

                if (result == null)
                    return null;
                else
                    return result.result;
            }

            public static parameters getParameters(this projectResults Results, String ResultsName)
            {
                var result = Results.results.Find((f) => { return f.name == ResultsName; });
                if (result == null)
                    return null;

                return result.schema[0].parameters;
            }

            public static List<field> getFields(this projectResults Results, String ResultsName)
            {
                var result = Results.results.Find((f) => { return f.name == ResultsName; });
                if (result == null)
                    return null;

                return result.schema[0].fields;
            }
        }

        public class commonProj : ProjectFlx.DB.IProject
        {
            private XmlDocument _xm;
            private ProjectFlx.Schema.SchemaQueryType _query = null;
            private XmlNode _catalog = null;
            private string _project = null;
            private XmlDocument _regx = null;

            public commonProj(String ProjectSqlPath)
            {
                _xm = new XmlDocument();
                if(Regex.Match(ProjectSqlPath, "^http(s)?://").Success)
                {
                    _xm.Load(ProjectSqlPath);
                }
                else
                {
                    var ProjSql = Schema.projectSql.LoadFromFile(ProjectSqlPath);
                    _xm.LoadXml(ProjSql.Serialize());
                }

                var xpath = @"projectSql/*[1]";
                _catalog = _xm.SelectSingleNode(xpath);
                _project = _catalog.LocalName;
            }

            public commonProj(String ProjectSqlPath, String Catalog)
            {
                var ProjSql = Schema.projectSql.LoadFromFile(ProjectSqlPath);

                _xm = new XmlDocument();
                _xm.LoadXml(ProjSql.Serialize());

                var xpath = String.Format(@"projectSql/*[local-name()='{0}']", Catalog);
                _catalog = _xm.SelectSingleNode(xpath);
                if (_catalog == null)
                    throw new Exception(String.Format("Project Not Found: {0}", Catalog));
                _project = _catalog.LocalName;
            } 

            public commonProj(ProjectFlx.Schema.projectSql ProjSql, String Catalog)
            {
                _xm = new XmlDocument();
                _xm.LoadXml(ProjSql.Serialize());

                var xpath = String.Format(@"projectSql/*[local-name()='{0}']", Catalog);
                _catalog = _xm.SelectSingleNode(xpath);
                if (_catalog == null)
                    throw new Exception(String.Format("Project Not Found: {0}", Catalog));
                _project = Catalog;
            }

            public void setProject(string Catalog)
            {
                var xpath = String.Format(@"projectSql/*[local-name()='{0}']", Catalog);
                _catalog = _xm.SelectSingleNode(xpath);
                if (_catalog == null)
                    throw new Exception(String.Format("Project Not Found: {0}", Catalog));
                _project = _catalog.LocalName;
            }

            public void setRegX(String ValidationRegXPath)
            {
                _regx = new XmlDocument();
                _regx.Load(ValidationRegXPath);
            }

            public ProjectFlx.Schema.SchemaQueryType SchemaQuery
            {
                get
                {
                    var retval = ProjectFlx.Schema.SchemaQueryType.Deserialize(_query.Serialize());
                    return retval;
                }
            }

            public void clearParameters()
            {
                _query.parameters = new ProjectFlx.Schema.parameters();
            }

            public void fillParms(object someObject)
            {
                fillParms(someObject, true);
            }

            public void fillParms(object someObject, bool BlankIsNull = true)
            {
                var nv = (NameValueCollection)someObject;

                for (int i = 0; i < nv.Keys.Count; i++)
                {
                    var val = nv[nv.Keys[i]];
                    if (BlankIsNull && String.IsNullOrEmpty(val))
                        continue;

                    if (_query.parameters.parameter.Exists(p => { return p.name == nv.Keys[i]; }))
                        this.setParameter(nv.Keys[i], HttpContext.Current.Server.HtmlEncode(val));
                }
            }

            public void fillParms()
            {
                throw new NotImplementedException();
            }

            public void setParameter(string Name, object Value)
            {
                setParameter(Name, Value, inoutType.@in);
            }

            public void setParameter(string Name, object Value, Schema.inoutType InOut)
            {
                setParameter(Name, Value, fieldType.text, InOut);
            }

            public void setParameter(string Name, object Value, Schema.fieldType FieldType, Schema.inoutType InOut)
            {
                setParameter(Name, Value, FieldType, InOut, 0);
            }

            public void setParameter(string Name, object Value, Schema.fieldType FieldType, Schema.inoutType InOut, int Size = 0)
            {
                string val = String.Empty;

                if (Value != null)
                    val = Value.ToString();
                else
                {
                    // default values
                    switch (FieldType)
                    {
                        case fieldType.@decimal:
                        case fieldType.@int:
                            val = "0";
                            break;
                        case fieldType.json:
                        case fieldType.tryjson:
                            val = "{}";
                            break;
                        case fieldType.text:
                            val = "";
                            break;
                        default:
                            var msg = String.Format("Value missing for parameter: {0}", Name);
                            throw new Exception(msg);
                    }
                }

                var q = _query.parameters.parameter.FirstOrDefault(p => { return p.name == Name; });
                if (q != null)
                {
                    q.Text = new List<string>();                    
                    q.Text.Add(val);                    
                    if(Size != 0)
                    {
                        q.size = Size;
                        q.sizeSpecified = true;
                    }
                }
                else
                {
                    if(Size != 0)
                        _query.AddParameter(Name, val, FieldType, InOut, Size);
                    else
                        _query.AddParameter(Name, val, FieldType, InOut);
                }
            }


            public void setQuery(string ProjQueryName)
            {                
                var xpath = String.Format("query[@name='{0}']", ProjQueryName);
                _query = new SchemaQueryType();
                var node = _catalog.SelectSingleNode(xpath);
                if (node == null)
                    throw new Exception("setQuery failed.  Query Not Found: " + ProjQueryName);
                _query = ProjectFlx.Schema.SchemaQueryType.Deserialize(node.OuterXml);
                _query.project = _project;
            }

            public void setQuery(SchemaQueryType ProjQuery)
            {
                this._query = ProjQuery;
                this._query.project = this._project;
            }

            public XmlNode ProjSqlNode
            {
                get
                {
                    if (_xm == null || _xm.DocumentElement == null)
                        return null;

                    return (XmlNode)_xm.DocumentElement;
                }
            }

            public string Tag { get; set; }
            public int Limit { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
            public int CurrentPage { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

            public void checkInputParms()
            {
                var format_exception_required = "Value for input: {0} is required.";
                var format_exception_regx = "Value for input: {0} does not match expected pattern.  Check your projSql settings.";

                foreach(var parm in _query.parameters.parameter)
                {
                    string regxName = null;
                    var val = Helper.FlattenList(parm.Text);
                    var field_name = String.IsNullOrEmpty(parm.display) ? parm.name : parm.display;

                    // is required
                    if (String.IsNullOrEmpty(val) && parm.required)
                        throw new ProjectException(String.Format(format_exception_required, field_name));

                    // match regx
                    if(!String.IsNullOrEmpty(val) && parm.regx != null && _regx != null && !String.IsNullOrEmpty(regxName = Helper.FlattenList(parm.regx)))
                    {
                        var xpath = String.Format("root/regx[@name='{0}']", regxName);
                        var patternNode = _regx.SelectSingleNode(xpath);

                        if (patternNode != null)
                        {
                            var pattern = patternNode.InnerText;
                            if (!Regex.Match(val, pattern).Success)
                                throw new ProjectException(String.Format(format_exception_regx, parm.name));
                        }
                    }
                }
            }
        }
    }
}

﻿using System;
using System.Xml;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using ProjectFlx;
using ProjectFlx.DB;
using System.Text.RegularExpressions;
using System.Web.Caching;
using ProjectFlx.Schema;

namespace ProjectFlx.DB.SchemaBased
{
    public class DatabaseQuery : IDisposable, IDatabaseQuery
    {
        DatabaseConnection _database;
        ProjectFlx.Schema.projectResults _Projresults;
        private SqlCommand _command;

        #region Cache Object
        private Cache _cache { get; set; }

        private string _cachePath;
        private int _cacheMinutes = 60 * 24;

        /// <summary>
        /// Default 24 hour cache
        /// </summary>
        /// <param name="Cache">Syste Web Cache</param>
        public void SetCache(Cache Cache)
        {
            SetCache(Cache, 24, null);
        }
        public void SetCache(Cache Cache, int MinuteExpires)
        {
            SetCache(Cache, MinuteExpires, null);
        }
        public void SetCache(Cache Cache, String CacheDependencyPath)
        {
            SetCache(Cache, 24, CacheDependencyPath);
        }
        public void SetCache(Cache Cache, int MinuteExpires, String CacheDependencyPath)
        {
            _cache = Cache;
            _cachePath = CacheDependencyPath;
            _cacheMinutes = MinuteExpires;
        }
        public void ClearCache(string Key)
        {
            _cache.Remove(Key);
        }
        private bool _cachingEnabled = false;
		private int _scalar = 0;

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

        public DatabaseQuery(DatabaseConnection Connection, ProjectFlx.Schema.projectResults ProjectResults)
        {
            _database = Connection;
            _Projresults = ProjectResults;
        }

        public void Query(ProjectFlx.DB.IProject ProjectSchemaQueryObject)
        {
            _Query(ProjectSchemaQueryObject.SchemaQuery, false);
        }

        public void Query(ProjectFlx.DB.IProject ProjectSchemaQueryObject, bool IgnoreResults)
        {
            _Query(ProjectSchemaQueryObject.SchemaQuery, IgnoreResults);
        }

        public void Query(ProjectFlx.Schema.SchemaQueryType SchemaQuery)
        {
            _Query(SchemaQuery, false);
        }

        public void Query(ProjectFlx.Schema.SchemaQueryType SchemaQuery, bool IngoreResults)
        {
            _Query(SchemaQuery, IngoreResults);
        }
        public ProjectFlx.Schema.projectResults ProjectResults
        {
            get
            {
                return _Projresults;
            }
        }
        void _Query(ProjectFlx.Schema.SchemaQueryType query, bool IgnoreResults)
        {
            string timingToken = String.Format("ProjectFlxDB.DB.SchemaBase.DatabaseQuery {0}.{1}", query.project, query.name);
            if (Timing != null)
                Timing.Start(timingToken);

            try
            {

                if (query.paging == null)
                    query.paging = new ProjectFlx.Schema.paging();

                ProjectFlx.Schema.results rslts;
                _Projresults.results.Add(rslts = new ProjectFlx.Schema.results());
                rslts.schema.Add(query);
                rslts.name = query.name;

                if (_database.State != ConnectionState.Open)
                {
                    _database.InitializeConnection();
                    _database.Open();
                }

                InitializeCommand();

                // command timeout
                if (query.scripttimeoutSpecified)
                    _command.CommandTimeout = query.scripttimeout;

                switch (query.command.type)
                {
                    case ProjectFlx.Schema.commandType.StoredProcedure:
                        _command.CommandText = ProjectFlx.Schema.Helper.FlattenList(query.command.name.Text);
                        _command.CommandType = System.Data.CommandType.StoredProcedure;
                        break;
                    case ProjectFlx.Schema.commandType.Select:
                        _command.CommandText = ProjectFlx.Schema.Helper.FlattenList(query.command.text.Text);
                        _command.CommandType = System.Data.CommandType.Text;
                        break;
                }

                _command.Parameters.Clear();    // be sure to clear parms in case _comman reused
                foreach (ProjectFlx.Schema.parameter parm in query.parameters.parameter)
                {
                    // short circuit 
                    if (query.command.type == Schema.commandType.Select)
                    {
                        string replace = String.Format("[{0}]", parm.name);
                        _command.CommandText = _command.CommandText.Replace(replace, ProjectFlx.Schema.Helper.FlattenList(parm.Text));
                        continue;
                    }

                    // guarantee that we setup variable for out param types
                    bool isoutparm = parm.inout == ProjectFlx.Schema.inoutType.inout || parm.inout == ProjectFlx.Schema.inoutType.@out;

                    // assume null parameter value if collection is length of 0
                    // see _fillParmsWeb for implementation details on 
                    // passing null and empty strings
                    if (!parm.blankIsNull && parm.Text.Count == 0)
                        parm.Text.Add("");

                    if (parm.Text.Count == 0 && !isoutparm)
                        continue;

                    var value = ProjectFlx.Schema.Helper.FlattenList(parm.Text);

                    if (value != null || isoutparm)
                    {
                        SqlParameter inoutparm;

                        if (parm.type == ProjectFlx.Schema.fieldType.date)
                        {
                            string dtValue = null;
                            if (value.EndsWith(" GMT"))
                                dtValue = value.Substring(0, value.LastIndexOf(" GMT"));
                            if (value.EndsWith(" UTC"))
                                dtValue = value.Substring(0, value.LastIndexOf(" UTC"));
                            if (dtValue == null)
                                dtValue = value;

                            var dt = DateTime.Parse("1970-1-1 01:01:01");
                            DateTime.TryParse(dtValue, out dt);

                            if (dt.ToString("d").Equals("1/1/1970") && dt.ToString("t").Equals("1:1 AM"))
                                throw new Exception("Could not parse date: " + value);

                            inoutparm = _command.Parameters.AddWithValue(parm.name, dt);
                        }
                        else if (parm.type == Schema.fieldType.json)
                        {
                            inoutparm = _command.Parameters.AddWithValue(parm.name, value);
                        }
                        else
                            inoutparm = _command.Parameters.AddWithValue(parm.name, value);


                        switch (parm.inout)
                        {
                            case ProjectFlx.Schema.inoutType.inout:
                                inoutparm.Direction = ParameterDirection.InputOutput;
                                inoutparm.DbType = getDBTypeForSchemaParm(parm);
                                break;
                            case ProjectFlx.Schema.inoutType.@out:
                                inoutparm.Direction = ParameterDirection.Output;
                                inoutparm.DbType = getDBTypeForSchemaParm(parm);
                                break;
                        }

                        // enforce size for inout params (text only) 
                        if (parm.type == ProjectFlx.Schema.fieldType.text && isoutparm)
                        {
                            if (parm.size > 0)
                                inoutparm.Size = parm.size;
                            else
                                throw new Exception(String.Format("Expecting parameter size for parameter {0} in query {1}", parm.name, query.name));
                        }

                        // validate json text type
                        if (parm.type == Schema.fieldType.json || parm.type == Schema.fieldType.tryjson)
                        {
                            try
                            {
                                var jsonObj = Newtonsoft.Json.Linq.JObject.Parse(Schema.Helper.FlattenList(parm.Text));
                            }
                            catch (Exception handled)
                            {
                                throw new Exception("Invalid Json for Parameter: " + parm.name, handled);
                            }
                        }
                    }
                }

                int result = 0;

                switch (query.command.action)
                {
                    case ProjectFlx.Schema.actionType.NonQuery:
                        result = _command.ExecuteNonQuery();
                        if (IgnoreResults == true) return;
                        // populate output parameter values
                        foreach (SqlParameter parm in _command.Parameters)
                        {
                            if (parm.Direction == ParameterDirection.InputOutput || parm.Direction == ParameterDirection.Output)
                            {
                                ProjectFlx.Schema.parameter outboundParm = query.parameters.parameter.Find(delegate (ProjectFlx.Schema.parameter g) { return g.name == parm.ParameterName; });
                                if (outboundParm != null)
                                {
                                    outboundParm.Text = new List<string>();
                                    outboundParm.Text.Add(Convert.ToString(parm.Value));
                                }

                            }
                        }

                        // set sorm sort of result
                        break;
                    case ProjectFlx.Schema.actionType.Result:

                        var cachekey = cacheKeyHelper(query);
                        var cacheresult = GetCache(cachekey);
                        if (cacheresult != null && CachingEnabled)
                        {
                            rslts.result = cacheresult;
                        }
                        else
                        {
                            using (SqlDataReader reader = _command.ExecuteReader())
                            {
                                if (IgnoreResults == true) return;

                                readerToResult(reader, rslts.result, query.fields, query.paging);

                                if (_cache != null && _cachingEnabled)
                                {
                                    var key = cacheKeyHelper(query);
                                    SaveCache(key, rslts.result);
                                }


                                // include sub results (StoredProcedure returns more than one Result Set)
                                while (reader.NextResult())
                                {
                                    if (_cache != null && _cachingEnabled)
                                        throw new Exception("Caching not supported for Suquery Queries");

                                    if (query.subquery != null)
                                    {
                                        readerToResult(reader, rslts.subresult, query.subquery.fields, query.paging);
                                    }
                                }
                            }
                        }
                        break;
                    case ProjectFlx.Schema.actionType.Scalar:
                        Object objresult = _command.ExecuteScalar();
						int scalar = 0;
						int.TryParse(objresult == null ? "0" : objresult.ToString(), out scalar);
						_scalar = scalar;
                        if (IgnoreResults == true) return;
                        var r = new ProjectFlx.Schema.result();
                        var i = new ProjectFlx.Schema.row();
                        XmlDocument xm = new XmlDocument();
                        XmlAttribute att = xm.CreateAttribute("Scalar");
						att.Value = _scalar.ToString();
                        i.AnyAttr.Add(att);
                        r.row.Add(i);
						_Projresults.results.Last().result = r;

                        // populate output parameter values
                        foreach (SqlParameter parm in _command.Parameters)
                        {
                            if (parm.Direction == ParameterDirection.InputOutput || parm.Direction == ParameterDirection.Output)
                            {
                                ProjectFlx.Schema.parameter outboundParm = query.parameters.parameter.Find(delegate (ProjectFlx.Schema.parameter g) { return g.name == parm.ParameterName; });
                                if (outboundParm != null)
                                {
                                    outboundParm.Text = new List<string>();
                                    outboundParm.Text.Add(Convert.ToString(parm.Value));
                                }

                            }
                        }

                        break;
                }

            }
            finally
            {
                if (Timing != null)
                    Timing.Stop(timingToken);
            }
        }


        public void InitializeCommand()
        {
            //_rowsaffected = 0;

            // make the command
            if (_command == null)
            {
                _command = new SqlCommand();
                _command.CommandType = CommandType.Text;

                // set timeouts
                //int commandTimeout = (ConfigurationManager.AppSettings["SqlCommandTimeout"] != null) ? Convert.ToInt32(ConfigurationManager.AppSettings["SqlCommandTimeout"]) : 25;
                //_command.CommandTimeout = commandTimeout;

                _command.Connection = _database.Connection;

                if (_database.WithTransaction)
                    _command.Transaction = _database.Transaction;
            }

        }


        private DbType getDBTypeForSchemaParm(ProjectFlx.Schema.parameter parm)
        {
            switch (parm.type)
            {
                case ProjectFlx.Schema.fieldType.date:
                    return DbType.Date;
                case ProjectFlx.Schema.fieldType.@decimal:
                    return DbType.Decimal;
                case ProjectFlx.Schema.fieldType.@int:
                    return DbType.Int32;
                case ProjectFlx.Schema.fieldType.text:
                    return DbType.String;
            }

            throw new Exception("Invalid Schema DBType for SQL DBType");
        }

        private void readerToResult(SqlDataReader reader, Object Result, List<ProjectFlx.Schema.field> Fields, ProjectFlx.Schema.paging Paging)
        {

            int currentpage = Paging.pages.current == 0 ? 1 : Paging.pages.current;

            // TODO: fill available pages to dropdown

            switch (Paging.direction)
            {
                case ProjectFlx.Schema.pagingDirectionType.next:
                    currentpage++;
                    break;
                case ProjectFlx.Schema.pagingDirectionType.previous:
                    currentpage--;
                    break;
                case ProjectFlx.Schema.pagingDirectionType.top:
                    currentpage = 1;
                    break;
                case ProjectFlx.Schema.pagingDirectionType.last:
                    currentpage = 99;      // TODO: last page
                    break;
            }
            if (currentpage < 1)
                currentpage = 1;

            Paging.pages.current = currentpage;

            int resultfrom = ((currentpage * Paging.limit) - Paging.limit) + 1;
            int resultto = resultfrom + Paging.limit;

            // keep copy of previous page results incase resultsfrom exceeds total pages
            // return last know page
            ProjectFlx.Schema.result prevresult = new ProjectFlx.Schema.result();

            int readercount = 0;
            while (reader.Read())
            {

                // enforce paging results
                readercount++;
                if ((readercount >= resultfrom && readercount < (resultto)) || Paging.limit.Equals(-1))
                {

                    ProjectFlx.Schema.row r;
                    if (Result.GetType().Equals(typeof(ProjectFlx.Schema.subresult)))
                        ((ProjectFlx.Schema.subresult)Result).row.Add(r = new ProjectFlx.Schema.row());
                    else
                        ((ProjectFlx.Schema.result)Result).row.Add(r = new ProjectFlx.Schema.row());

                    XmlDocument xm = new XmlDocument();
                    List<XmlElement> innerNodes = new List<XmlElement>();

                    // empty fieldset by default grab all
                    if (Fields.Count == 0)
                    {
                        for (int fld = 0; fld < reader.FieldCount; fld++)
                        {
                            string fname = String.Format("field_{0:d3}", fld);
                            XmlAttribute att = xm.CreateAttribute(fname);
                            att.Value = reader[fld].ToString();
                            r.AnyAttr.Add(att);
                        }
                    }


                    foreach (ProjectFlx.Schema.field f in Fields)
                    {

                        XmlAttribute att = xm.CreateAttribute(f.name);
                        if (f.type == Schema.fieldType.json || f.type == Schema.fieldType.tryjson)
                        {
                            string json = reader[f.name].ToString().TrimEnd();
                            try
                            {
                                if (String.IsNullOrEmpty(json))
                                    json = "{}";
                                else
                                    Newtonsoft.Json.Linq.JObject.Parse(json);

                                string jsonDetails = String.Format("{{\"{0}\":{1}}}", f.name, json);
                                var innerXml = Newtonsoft.Json.JsonConvert.DeserializeXmlNode(jsonDetails);
                                innerNodes.Add((XmlElement)innerXml.DocumentElement);

                            }
                            catch
                            {
                                if (f.type == Schema.fieldType.tryjson)
                                {
                                    json = reader[f.name].ToString().TrimEnd();
                                }
                                else
                                {
                                    json = "{\"error\":\"" + "Invalid Json Object field: " + f.name + "\"}";
                                }
                            }
                            att.Value = json;
                        }
                        else
                        {
                            try
                            {
                                string val = null;
                                if (f.encode.HasValue())
                                {
                                    val = reader[f.encode].ToString();
                                    val = System.Web.HttpUtility.UrlEncode(val.TrimEnd()).Replace("+", "%20");
                                }
                                else if (f.regx_field.HasValue())
                                {
                                    val = reader[f.regx_field].ToString();
                                    val = Regex.Replace(val, Schema.Helper.FlattenList(f.regx), f.regx_replace);
                                }
                                else
                                    val = reader[f.name].ToString();


                                if (val != null)
                                    att.Value = safeXmlCharacters(val);
                            }
                            catch (IndexOutOfRangeException handled)
                            {
                                att.Value = "-field not found-";
                            }
                        }

                        r.AnyAttr.Add(att);
                    }

                    // append inner nodes (usually from json field conversion)
                    for (int i = 0; i < innerNodes.Count; i++)
                        r.Any.Add(innerNodes[i]);
                }

                // save previous page results incase resultfrom > readercount, then return last page results
                int prevresultfrom = resultfrom - Paging.limit;
                int prevresultto = resultto - Paging.limit;

                if (prevresultfrom > 0)
                    if ((readercount >= prevresultfrom && readercount < (prevresultto)) && Paging.limit > 0)
                    {

                        ProjectFlx.Schema.row r;
                        prevresult.row.Add(r = new ProjectFlx.Schema.row());
                        XmlDocument xm = new XmlDocument();

                        foreach (ProjectFlx.Schema.field f in Fields)
                        {

                            XmlAttribute att = xm.CreateAttribute(f.name);
                            if (f.encode != null)
                                att.Value = System.Web.HttpUtility.UrlEncode(reader[f.encode].ToString().TrimEnd());
                            else
                                att.Value = reader[f.name].ToString().TrimEnd();

                            r.AnyAttr.Add(att);
                        }
                    }

            }

            // check to see if we are trying to read records beyond recordset and 
            // return last page results if we are
            if (resultfrom > readercount && Paging.limit != -1)
            {
                currentpage--;
                if (currentpage > 0)
                    Paging.pages.current = currentpage;

                // copy previous results to Result
                ProjectFlx.Schema.row r;
                foreach (ProjectFlx.Schema.row r2 in prevresult.row)
                {
                    if (Result.GetType().Equals(typeof(ProjectFlx.Schema.subresult)))
                        ((ProjectFlx.Schema.subresult)Result).row.Add(r = new ProjectFlx.Schema.row());
                    else
                        ((ProjectFlx.Schema.result)Result).row.Add(r = new ProjectFlx.Schema.row());

                    foreach (XmlAttribute att in r2.AnyAttr)
                        r.AnyAttr.Add(att);

                }
            }

            // get pages available for reader
            int pagescount = (readercount / Paging.limit) + 1;
            Paging.pages.totalpages = pagescount;
            Paging.pages.totalrecords = readercount;

            for (int x = 1; x <= pagescount; x++)
            {
                ProjectFlx.Schema.page pg = new ProjectFlx.Schema.page();
                pg.value = x;
                pg.from = ((Paging.limit * x) - Paging.limit) + 1;

                // upper limit page to value is difference of total reader count
                if (x.Equals(pagescount))
                    pg.to = readercount;
                else
                    pg.to = (Paging.limit * x);

                Paging.pages.page.Add(pg);
            }

        }

        private string safeXmlCharacters(string val)
        {
            string re = @"[\x00-\x08\x0B\x0C\x0E-\x1F\x26]";
            return Regex.Replace(val, re, "");
        }

        public bool ResultEquals(Object Value)
        {
            try
            {
                return ResultValue().Equals(Value.ToString());
            }
            catch { return false; }
        }

        public String ResultValue()
        {
            try
            {

                if (_Projresults == null || _Projresults.results == null)
                    return null;

                if (_Projresults.results.Count == 0)
                    return null;

                // we are interested in a single static result
                if (_Projresults.results[0].result == null ||
                    _Projresults.results[0].result.row == null ||
                    _Projresults.results[0].result.row.Count == 0 ||
                    _Projresults.results[0].result.row.Count > 1)
                    return null;

                if (_Projresults.results[0].result.row[0].AnyAttr.Count == 0)
                    return null;

                var att = _Projresults.results[0].result.row[0].AnyAttr[0];
                return att.ToString();
            }
            catch
            {
                return null;
            }
        }

        public bool ResultGreaterThan(Object Value)
        {
            try
            {
                float[] vals = new float[2] { 0, 0 };

                float.TryParse(ResultValue(), out vals[0]);
                float.TryParse(Value.ToString(), out vals[1]);

                return vals[0] > vals[1];
            }
            catch { return false; }
        }

        public bool ResultLessThan(Object Value)
        {
            try
            {
                float[] vals = new float[2] { 0, 0 };

                float.TryParse(ResultValue(), out vals[0]);
                float.TryParse(Value.ToString(), out vals[1]);

                return vals[0] < vals[1];
            }
            catch { return false; }
        }

        private string cacheKeyHelper(SchemaQueryType query)
        {
            var keybuilder = new StringBuilder();
            keybuilder.Append(query.name);
            foreach (var parm in query.parameters.parameter)
            {
                if (parm.Text.Count > 0)
                    keybuilder.Append(parm.Text.Flatten());
            }

            return keybuilder.ToString();
        }

        private result GetCache(String Key)
        {
            if (_cache == null || _cachingEnabled == false)
                return null;

            try
            {
                // cache
                var obj = _cache[Key];
                return (result)obj;
            }
            catch
            {
                return null;
            }
        }

        private void SaveCache(string Key, result result)
        {
            if (_cache == null)
                return;

            CacheDependency cachedependency = null;
            if (!String.IsNullOrEmpty(this._cachePath))
            {
                string depFile = Path.Combine(_cachePath, System.IO.Path.GetRandomFileName() + ".cache");
                using (StreamWriter writer = new StreamWriter(depFile))
                {
                    writer.WriteLine(DateTime.Now.ToString("yyyyMMddHHmmssffff"));
                }
                cachedependency = new CacheDependency(depFile);
            }
            _cache.Insert(Key, result, cachedependency, DateTime.Now.AddMinutes(_cacheMinutes), System.Web.Caching.Cache.NoSlidingExpiration);
        }
        //public XmlDocument RegXValidationDocument
        //{
        //    get
        //    {
        //        return _xmRegX;
        //    }
        //    set
        //    {
        //        _xmRegX = value;
        //    }
        //}

        #region IDisposable Members

        public void Dispose()
        {
            try
            {
                if(_database != null && _database.Connection.State == ConnectionState.Open)
                    _database.Connection.Close();
                _database.Dispose();
            }
            catch { }
        }

        #endregion

        public Utility.Timing Timing { get; set; }
		public int Scalar { get => _scalar; }
	}

    public class XObject : ProjectFlx.DB.IProject
    {
        private Schema.SchemaType _schema = null;
        private Schema.SchemaQueryType _schemaQuery = null;

        public XObject(Schema.SchemaType Schema)
        {
            _schema = Schema;
			if (_schema.query.Count == 1)
				_schemaQuery = _schema.query[0];
        }

        public void setQuery(object ProjQuery)
        {
            throw new NotImplementedException();
        }

        public void setQuery(string ProjQueryName)
        {
            var q = _schema.query.Find(x=>x.name.Equals(ProjQueryName));

            if(q == null)
                throw new ProjectFlx.Exceptions.ProjectException(new Exceptions.ProjectExceptionArgs("Project Query Not Found " + ProjQueryName, Exceptions.SeverityLevel.Critical));

            _schemaQuery = q;
        }

        public void setParameter(String Name, object Value)
        {
            if (_schemaQuery.parameters == null || _schemaQuery.parameters.parameter == null)
                return;

            var p = _schemaQuery.parameters.parameter.Find(x => x.name.Equals(Name));
            if (p != null)
                p.Text.Add(Value.ToString());

        }

        public void clearParameters()
        {
            _schemaQuery.parameters = new Schema.parameters();
            _schemaQuery.parameters.parameter = new List<Schema.parameter>();
        }

        public void fillParms()
        {
            throw new NotImplementedException();
        }

        public void fillParms(object someObject)
        {
            throw new NotImplementedException();
        }

        public Schema.SchemaQueryType SchemaQuery
        {
            get 
            {
                if (_schemaQuery == null)
                    throw new ProjectFlx.Exceptions.ProjectException(new Exceptions.ProjectExceptionArgs("SchemaQuery Not Set", Exceptions.SeverityLevel.Critical));

                return _schemaQuery;
            }
        }

    }

}

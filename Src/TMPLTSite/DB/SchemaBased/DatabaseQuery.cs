using System;
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


namespace ProjectFlx.DB.SchemaBased
{
    public class DatabaseQuery : IDisposable
    {
        DatabaseConnection _database;
        ProjectFlx.Schema.projectResults _Projresults;

        public DatabaseQuery(DatabaseConnection Connection, ProjectFlx.Schema.projectResults ProjectResults)
        {
            _database = Connection;
            _Projresults = ProjectResults;
        }

        public void Query(ProjectFlx.DB.IProject ProjectSchemaQueryObject)
        {
            _Query(ProjectSchemaQueryObject.SchemaQuery);
        }

        public void Query(ProjectFlx.Schema.SchemaQueryType SchemaQuery)
        {
            _Query(SchemaQuery);
        }
        public ProjectFlx.Schema.projectResults ProjectResults
        {
            get
            {
                return _Projresults;
            }
        }
        void _Query(ProjectFlx.Schema.SchemaQueryType query)
        {
            if(query.paging == null)
                query.paging = new ProjectFlx.Schema.paging();

            ProjectFlx.Schema.results rslts;
            _Projresults.results.Add(rslts = new ProjectFlx.Schema.results());
            rslts.schema.Add(query);
            rslts.name = query.name;

            SqlCommand cmd = new SqlCommand();

            if (_database.State != ConnectionState.Open)
            {
                _database.InitializeConnection();
                _database.Open();
            }

            cmd.Connection = _database.Connection;
            
            switch (query.command.type)
            {
                case ProjectFlx.Schema.commandType.StoredProcedure:
                    cmd.CommandText = ProjectFlx.Schema.Helper.FlattenList(query.command.name.Text);
                    cmd.CommandType = System.Data.CommandType.StoredProcedure;
                    break;
                case ProjectFlx.Schema.commandType.Select:
                    cmd.CommandText = ProjectFlx.Schema.Helper.FlattenList(query.command.text.Text);
                    cmd.CommandType = System.Data.CommandType.Text;
                    break;
            }

            foreach (ProjectFlx.Schema.parameter parm in query.parameters.parameter)
            {
                // short circuit 
                if (query.command.type == Schema.commandType.Select)
                {
                    string replace = String.Format("[{0}]", parm.name);
                    cmd.CommandText = cmd.CommandText.Replace(replace, ProjectFlx.Schema.Helper.FlattenList(parm.Text));
                    continue;
                }

                // guarantee that we setup variable for out param types
                bool isoutparm = parm.inout == ProjectFlx.Schema.inoutType.inout || parm.inout == ProjectFlx.Schema.inoutType.@out;

                // assume null parameter value if collection is length of 0
                // see _fillParmsWeb for implementation details on 
                // passing null and empty strings
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

                        inoutparm = cmd.Parameters.AddWithValue(parm.name, dt);
                    }
                    else 
                        inoutparm = cmd.Parameters.AddWithValue(parm.name, value);


                    switch(parm.inout)
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
                    if (parm.type == Schema.fieldType.json)
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
                    result = cmd.ExecuteNonQuery();

                    // populate output parameter values
                    foreach(SqlParameter parm in cmd.Parameters)
                    {
                        if (parm.Direction == ParameterDirection.InputOutput || parm.Direction == ParameterDirection.Output)
                        {
                            ProjectFlx.Schema.parameter outboundParm = query.parameters.parameter.Find(delegate(ProjectFlx.Schema.parameter g) { return g.name == parm.ParameterName; });
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

                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {                        
                        readerToResult(reader, rslts.result, query.fields, query.paging);

                        // include sub results (StoredProcedure returns more than one Result Set)
                        while (reader.NextResult())
                        {
                            if (query.subquery != null)
                            {
                                _Projresults.results.Add(rslts = new ProjectFlx.Schema.results());
                                readerToResult(reader, rslts.result, query.subquery.fields, query.paging);
                            }
                        }
                    }
                    break;
                case ProjectFlx.Schema.actionType.Scalar:
                    Object objresult = cmd.ExecuteScalar();
                    var r = new ProjectFlx.Schema.result();
                    var i = new ProjectFlx.Schema.row();
                    XmlDocument xm = new XmlDocument();
                    XmlAttribute att = xm.CreateAttribute("Scalar");
                    att.Value = objresult.ToString();
                    i.AnyAttr.Add(att);
                    r.row.Add(i);
                    _Projresults.results[0].result = r;
                    break;
            }

        }

        private DbType getDBTypeForSchemaParm(ProjectFlx.Schema.parameter parm)
        {
            switch(parm.type)
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

        private void readerToResult(SqlDataReader reader, ProjectFlx.Schema.result Result, List<ProjectFlx.Schema.field> Fields, ProjectFlx.Schema.paging Paging)
        {
            
            int currentpage =  Paging.pages.current == 0 ? 1 : Paging.pages.current;

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
                    Result.row.Add(r = new ProjectFlx.Schema.row());
                    XmlDocument xm = new XmlDocument();
                    List<XmlElement> innerNodes = new List<XmlElement>();

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
                                    val = Regex.Replace(val,Schema.Helper.FlattenList(f.regx), f.regx_replace);
                                }
                                else
                                    val = reader[f.name].ToString();


                                if (val != null)
                                    att.Value = val;
                            }
                            catch (IndexOutOfRangeException handled)
                            {
                                throw new ProjectFlx.Exceptions.ProjectException(new Exceptions.ProjectExceptionArgs("A critical error has occured.  Please notify MSO.  We are sorry for the inconvenience.", "ProjectFlx.DB.SchemaBased.DatabaseQuery", "readerToResult(SqlDataReader, ProjectFlx.Schema.result, List<ProjectFlx.Schema.field>, ProjectFlx.Schema.paging", "att.Value = System.Web.HttpUtility.UrlEncode(reader[f.encode].ToString().TrimEnd()).Replace(" + ", \"%20\");", Exceptions.SeverityLevel.Critical, Exceptions.LogLevel.Event), handled);
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
                    Result.row.Add(r = new ProjectFlx.Schema.row());
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
    }

    public class XObject : ProjectFlx.DB.IProject
    {
        private Schema.SchemaType _schema = null;
        private Schema.SchemaQueryType _schemaQuery = null;

        public XObject(Schema.SchemaType Schema)
        {
            _schema = Schema;
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

using ProjectFlx.DB;
using ProjectFlx.Exceptions;
using ProjectFlx.Schema;
using ProjectFlx.Schema.Extra;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Web;

namespace ProjectFlx
{

    public class Queries : IQueries
    {
        public HttpContext Current;
        public HttpServerUtility Server;
        Utility.TimingCollection _timing;

        /// <summary>
        /// Set this to override default connection string setting
        /// </summary>
        public string ConnectionString = null;

        public Queries() { }
        public Queries(HttpContext Context, HttpServerUtility Server) : this(Context, Server, default(Utility.TimingCollection)) { }
        public Queries(HttpContext Context, HttpServerUtility Server, Utility.TimingCollection Timing)
        {
            this.Current = Context;
            this.Server = Server;
            this._timing = Timing;
        }

        public class FieldNameCollection : List<FieldName>
        {
            public void Add(string Name, ProjectFlx.Schema.fieldType Type)
            {
                this.Add(new FieldName()
                {
                    Name = Name,
                    Type = Type
                });
            }
        }
        public class FieldName
        {
            public string Name { get; set; }
            public ProjectFlx.Schema.fieldType Type { get; set; }
        }
        public class QueryPromise
        {
            public bool OneOrMore { get; set; }
            public int Rows { get; set; }
            public string Message { get; set; }
        }

        private QueryPromise _promise = null;
        private int _scalar;

        public int TimeOut { get; set; }

        public void ExecuteQuery(string Project, string QueryName)
        {
            ExecuteQuery(Project, QueryName, null);
        }
        public void ExecuteQuery(string Project, string QueryName, NameValueCollection Parms)
        {
            _promise = null;
            var results = new ProjectFlx.Schema.projectResults();
            ExecuteQuery(null, Project, QueryName, results, Parms);
        }
        public void ExecuteQuery(string Project, string QueryName, QueryPromise Promise, NameValueCollection Parms)
        {
            _promise = Promise;
            var results = new ProjectFlx.Schema.projectResults();
            ExecuteQuery(null, Project, QueryName, results, Parms);
        }
        public void ExecuteQuery(string Project, string QueryName, projectResults Results, NameValueCollection Parms)
        {
            _promise = null;
            ExecuteQuery(null, Project, QueryName, Results, Parms);
        }
        public void ExecuteQuery(string Project, string QueryName, projectResults Results, QueryPromise Promise, NameValueCollection Parms)
        {
            _promise = Promise;
            var results = new ProjectFlx.Schema.projectResults();
            ExecuteQuery(null, Project, QueryName, results, Parms);
        }

        public void ExecuteQuery(ProjectFlx.FlxTemplater TMPLT, string Project, string QueryName)
        {
            _promise = null;
            var results = new ProjectFlx.Schema.projectResults();
            ExecuteQuery(TMPLT, Project, QueryName, results, null);
        }
        public void ExecuteQuery(ProjectFlx.FlxTemplater TMPLT, string Project, string QueryName, NameValueCollection Parms)
        {
            _promise = null;
            var results = new ProjectFlx.Schema.projectResults();
            ExecuteQuery(TMPLT, Project, QueryName, results, Parms);
        }
        public void ExecuteQuery(ProjectFlx.FlxTemplater TMPLT, string Project, string QueryName, QueryPromise Promise, NameValueCollection Parms)
        {
            _promise = Promise;
            var results = new ProjectFlx.Schema.projectResults();
            ExecuteQuery(TMPLT, Project, QueryName, results, Parms);
        }
        public void ExecuteQuery(ProjectFlx.FlxTemplater TMPLT, string Project, string QueryName, ProjectFlx.Schema.projectResults Results, NameValueCollection Parms)
        {
            _promise = _promise ?? null;
            var x = new NameValueCollection();

            var QName = QueryName.Split('#');
            var queryname = QName[0];

            using (var db = new DatabaseConnection(ConnectionString))
            using (var dq = new ProjectFlx.DB.SchemaBased.DatabaseQuery(db, Results))
            {
                dq.Timing = _timing;
                var schema = ProjectFlx.Schema.SchemaType.GetSchemaType(Server.MapPath("/ProjectSQL.xml"), Project);
                var xobj = new ProjectFlx.DB.SchemaBased.XObject(schema);

                if(QName.Length==2)
                    xobj.Tag = QName[1];

                xobj.setQuery(queryname);
                ProjectFlx.DB.SchemaBased.XObject.FillParameters(Current, xobj, Parms);
                ProjectFlx.DB.SchemaBased.XObject.FillPaging(Current, xobj, Parms);
                dq.Query(xobj);
                var result = Results.Lookup(queryname);
                
                if (_promise != null)
                {
                    if (_promise.OneOrMore)
                    {
                        if (result.row.Count == 0)
                            throw new QueryPromiseException($"Promise failed for query {queryname}, expecting One Or More row(s), none found.");
                        return;
                    }
                    if (!result.row.Count.Equals(_promise.Rows))
                        throw new QueryPromiseException($"Promise failed for query {queryname}, expecting {_promise.Rows} row(s), actual {result.row.Count} row(s) found.");
                }
                this._scalar = dq.Scalar;

                if (TMPLT != null)
                    TMPLT.AddXML(Results);        
            }
        }
        public void ExecuteQuery(FlxTemplater TMPLT, string Project, string QueryName, projectResults Results, QueryPromise Promise, NameValueCollection Parms)
        {
            _promise = Promise;
            ExecuteQuery(TMPLT, Project, QueryName, Results, Parms);
        }


        public void ExecuteRawQuery(string Project, string QueryName, string Select)
        {
            throw new System.NotImplementedException();
        }
        public void ExecuteRawQuery(string Project, string QueryName, string Select, FieldNameCollection Fields)
        {
            ExecuteRawQuery(null, Project, QueryName, Select, default(projectResults), Fields, default(NameValueCollection));
        }
        public void ExecuteRawQuery(string Project, string QueryName, string Select, projectResults Result, NameValueCollection Parms)
        {
            ExecuteRawQuery(null, Project, QueryName, Select, Result, default(FieldNameCollection), Parms);
        }
        public void ExecuteRawQuery(string Project, string QueryName, string Select, projectResults Result, FieldNameCollection Fields, NameValueCollection Parms)
        {
            ExecuteRawQuery(null, Project, QueryName, Select, Result, Fields, Parms);
        }



        public void ExecuteRawQuery(ProjectFlx.FlxTemplater TMPLT, string Project, string QueryName, string Select)
        {
            ExecuteRawQuery(TMPLT, Project, QueryName, Select, null, null, null);
        }
        public void ExecuteRawQuery(ProjectFlx.FlxTemplater TMPLT, string Project, string QueryName, string Select, FieldNameCollection Fields)
        {
            ExecuteRawQuery(TMPLT, Project, QueryName, Select, null, Fields, null);
        }
        public void ExecuteRawQuery(ProjectFlx.FlxTemplater TMPLT, string Project, string QueryName, string Select, ProjectFlx.Schema.projectResults Result, NameValueCollection Parms)
        {
            ExecuteRawQuery(TMPLT, Project, QueryName, Select, Result, null, Parms);
        }
        public void ExecuteRawQuery(ProjectFlx.FlxTemplater TMPLT, string Project, string QueryName, string Select, ProjectFlx.Schema.projectResults Result, FieldNameCollection Fields, NameValueCollection Parms)
        {
            var QName = QueryName.Split('#');
            var queryname = QName[0];

            var result = Result ?? new ProjectFlx.Schema.projectResults();
            using (var db = new DatabaseConnection())
            using (var dq = new ProjectFlx.DB.SchemaBased.DatabaseQuery(db, result))
            {
                dq.Timing = _timing;
                if (QName.Length == 2)
                    dq.Tag = QName[1];

                string sql = Select;
                if (Parms != null)
                {
                    foreach (var key in Parms.AllKeys)
                    {
                        var skey = $"[{key}]";
                        sql = sql.Replace(skey, Parms[key].Replace("'", "''"));
                    }
                }
                var schema = ProjectFlx.Schema.SchemaQueryType.Create(queryname, ProjectFlx.Schema.actionType.Result, queryname, sql);
                if (Fields != null)
                {
                    schema.fields = new List<ProjectFlx.Schema.field>();
                    foreach (var field in Fields)
                        schema.fields.Add(new ProjectFlx.Schema.field()
                        {
                            name = field.Name,
                            type = field.Type
                        });
                }

                dq.Query(schema);

                this._scalar = dq.Scalar;
            }

            TMPLT.AddXML(result);
        }


        public virtual ProjectFlx.Schema.result ExecuteQuery2(string Project, string QueryName, NameValueCollection Parms)
        {
            _promise = null;
            return ExecuteQuery2(Project, QueryName, null, Parms);
        }
        public virtual ProjectFlx.Schema.result ExecuteQuery2(string Project, string QueryName, QueryPromise Promise, NameValueCollection Parms)
        {
            var projresult = new ProjectFlx.Schema.projectResults();
            ExecuteQuery(Project, QueryName, projresult, Parms);

            var QName = QueryName.Split('#');
            var queryname = QName[0];

            var result = projresult.Lookup(queryname);
            if (_promise != null)
            {
                if (_promise.OneOrMore)
                {
                    if (result.row.Count == 0)
                        throw new QueryPromiseException($"Promise failed for query {queryname}, expecting One Or More row(s), none found.");
                }
                if (!result.row.Count.Equals(_promise.Rows))
                    throw new QueryPromiseException($"Promise failed for query {queryname}, expecting {_promise.Rows} row(s), actual {result.row.Count} row(s) found.");
            }

            return result;
        }



        public virtual string ExecuteQueryJson(string Project, string QueryName, NameValueCollection Parms)
        {
            _promise = null;
            var result = new ProjectFlx.Schema.projectResults();
            ExecuteQuery(Project, QueryName, result, Parms);

            return ProjectFlx.Schema.Helper.schemaQueryJsonBuilder.getJsonString(result);
        }

        public int ExecuteScalar(string Project, string QueryName, NameValueCollection Parms)
        {
            _promise = null;
            ExecuteQuery(Project, QueryName, Parms);
            return this._scalar;
        }

        public int ExecuteNonQuery(string Project, string QueryName, NameValueCollection Parms)
        {
            throw new System.NotImplementedException();
        }
    }
}

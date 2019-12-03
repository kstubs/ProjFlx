using System;
using System.IO;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using System.Data;
using System.Data.SqlClient;
using System.Configuration;
using ProjectFlx;
using ProjectFlx.Exceptions;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System.Web.Caching;
using System.Xml.Linq;

namespace ProjectFlx.DB
{
    internal struct ResultPages
    {
        List<ResultPaging> _pages;
        string _queryName;
        string _hash;
        int _currentPage;
        int _totalPages;
        int _totalItems;
        int _itemPerPageCount;

        internal ResultPages(string QueryName, string Hash, int CurrentPage, int ItemsPerPage)
        {
            _queryName = QueryName;
            _hash = Hash;
            _currentPage = CurrentPage;
            _totalPages = 0;
            _totalItems = 0;
            _itemPerPageCount = ItemsPerPage;

            _pages = new List<ResultPaging>();
        }
        internal void Add(ResultPaging Page)
        {
            _pages.Add(Page);
            _totalPages++;
            _totalItems += Page.ItemCount;
        }
        internal string Serialize()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendFormat(@"<pages name=""{0}"" total_pages=""{1}"" total_items=""{2}"" items_per_page=""{3}"" current_page=""{4}"" hash=""{5}"" >", _queryName, _totalPages, _totalItems, _itemPerPageCount, _currentPage, _hash);

            foreach (ResultPaging page in _pages)
            {
                sb.Append(page.Serialize());
            }

            sb.Append(@"</pages>");

            return sb.ToString();
        }
    }
    internal struct ResultPaging
    {
        int _pageNumber;
        int _itemsCount;
        string _pageOfDescription;

        internal ResultPaging(int CurrentPage, int ItemCount)
        {
            _pageNumber = CurrentPage;
            _itemsCount = ItemCount;
            _pageOfDescription = String.Format("Page {0}", CurrentPage);

        }

        internal int ItemCount
        {
            get
            {
                return _itemsCount;
            }
        }

        internal string Serialize()
        {
            string xmlpattern = @"<page value=""{0}"" item_count=""{1}"">{2}</page>";
            return String.Format(xmlpattern, _pageNumber, _itemsCount, _pageOfDescription);
        }
    }
    public class DatabaseConnection : IDisposable
    {
        #region private members
        private string _connectionString = null;
        private SqlConnection _connection = null;
        private SqlTransaction _trans = null;
        private bool _withTrx;
        private bool _disposed;

        public SqlTransaction Transaction
        {
            get { return _trans; }
            set { _trans = value; }
        }

        public void InitializeConnection()
        {
            // determine connection string
            if (string.IsNullOrEmpty(_connectionString))
            {
                string connectionName = ConfigurationManager.AppSettings["conn-name"];
                if (String.IsNullOrEmpty(connectionName))
                    throw new ProjectException(new ProjectExceptionArgs("Expecting conn-name appsetting for to resolve connectionString", "ProjectFlx.DB", "DatabaseConnection", "string connectionName = ConfigurationManager.AppSettings[\"conn-name\"]", SeverityLevel.Fatal, LogLevel.Event));
                var data = ConfigurationManager.ConnectionStrings[connectionName];

                if (data == null)
                    throw new Exception("DB ConnectionString not found for key: " + connectionName);

                _connectionString = ConfigurationManager.ConnectionStrings[connectionName].ConnectionString;
            }

            if(_connection == null)
                _connection = new SqlConnection(_connectionString);
        }
        #endregion

        #region protected members
        public SqlConnection Connection
        {
            get
            {
                return _connection;
            }
        
        }
        public DatabaseConnection()
        {
            InitializeConnection();
        }
        public DatabaseConnection(string ConnectionString)
        {
            _connectionString = ConnectionString;
            InitializeConnection();
        }
        public bool WithTransaction
        {
            get
            {
                return _withTrx;
            }
            set
            {
                _withTrx = value;
            }
        } 
        public DatabaseConnection(string ConnectionString, bool WithTransaction)
        {
            _connectionString = ConnectionString;
            _withTrx = WithTransaction;
            InitializeConnection();
        }

        public ConnectionState State
        {
            get
            {
                return _connection.State;
            }
        }
        public void Open()
        {
            int tries = 3;
            while (tries > 0)
            {
                try
                {
                    _connection.Open();
                    tries = 0;
                }
                catch(Exception unhandled)
                {
                    tries--;
                    if (tries <= 0)
                        throw unhandled;

                    System.Threading.Thread.Sleep(200);
                }

            }

            if (WithTransaction)
                _trans = _connection.BeginTransaction(IsolationLevel.RepeatableRead);
        }

        public void CommitTransaction()
        {
            if (_connection != null && _connection.State == ConnectionState.Open && _trans != null)
            {
                _trans.Commit();
                _trans = null;
            }
        }
        public void RollBackTransaction()
        {
            if (_connection != null && _connection.State == ConnectionState.Open && _trans.Connection != null)
            {
                _trans.Rollback();
                _trans = null;
            }
        }
        public void Close()
        {
            try
            {

                if (_trans != null && _trans.Connection != null)
                {
                    RollBackTransaction();
                    throw new Exception("Warning, transaction rolled back.  Required, call Close override with commit transaction boolean when Transaction flag for connection is true.");
                }
            }
            finally
            {
                this.Dispose();
            }

        }
        public void Close(Boolean Commit)
        {

            try
            {
                if (_trans != null && _trans.Connection != null)
                {
                    if (Commit)
                    {
                        _trans.Commit();
                        _trans = null;
                    }
                    else
                    {
                        RollBackTransaction();
                    }
                }
            }
            catch { }
            finally
            {
                this.Dispose();
            }
        }
        #endregion


        #region IDisposable Members

        public void Dispose()
        {
            if (_disposed)
                return;

            try
            {
                if (_trans != null)
                {
                    if (_trans != null && _trans.Connection != null)
                    {
                        _trans.Rollback();
                        _trans = null;
                        throw new Exception("Warning, transaction rolled back.  Required, call Close override with commit transaction boolean when Transaction flag for connection is true.");
                    }
                }
            }
            catch(Exception unhandled)
            {
                // TODO: log error here to system
                System.Diagnostics.Debugger.Log(0, "Unhandled Exception", unhandled.Message);
            }
            finally
            {
                if (_connection != null)
                {
                    _connection.Close();
                    _connection.Dispose();
                }
                _disposed = true;
            }
        }

        #endregion
    }

    public class DatabaseQuery : IDatabaseQueryPaging, IDatabaseQuery, IDisposable
    {
        #region members
        string _sqlProjName = null;
        DatabaseConnection _database = null;
        private SqlCommand _command = null;
        XmlDocument _xmRegX = null;
        XmlDocument _xmresult = new XmlDocument();
        XmlNode _querynode = null;
        int _rowsaffected = 0;
        int _scalar = 0;
        int _lastInsertID = 0;
        bool _results = false;
        ProjectExceptionHandler _handler = new ProjectExceptionHandler();

        ResultPages _resultPages;

        int _pagingLimit = 5000;
        int _currentPage = 1;
        DatabaseQueryPagingDirection _pagingDirection = DatabaseQueryPagingDirection.None;
        #endregion

        #region private methods
        private void AddParameter(string name, object value, string valueType, string inout, int size)
        {
            _command.CommandType = CommandType.StoredProcedure;
            SqlParameter parameter = null;
            ProjectExceptionArgs args = null;

            switch (valueType)
            {
                case ("int"):
                    parameter = _command.Parameters.Add(name, SqlDbType.Int);
                    try
                    {
                        parameter.Value = Convert.ToInt32(value);
                    }
                    catch (FormatException)
                    {
                        args = new ProjectExceptionArgs(
                           String.Format("Invalid parameter value for: {0} - expecting valid number", name), SeverityLevel.Format);
                        _handler.Add(args);
                    }
                    break;
                case ("date"):
                    parameter = _command.Parameters.Add(name, SqlDbType.DateTime);
                    try
                    {
                        parameter.Value = Convert.ToDateTime(value);
                    }
                    catch (FormatException)
                    {
                        args = new ProjectExceptionArgs(
                           String.Format("Invalid parameter value for: {0} - expecting a valid Date", name), SeverityLevel.Format);
                        _handler.Add(args);
                    }
                    break;
                case ("varchar"):
                    parameter = _command.Parameters.Add(name, SqlDbType.VarChar);
                    try
                    {
                        parameter.Value = Convert.ToString(value);
                        parameter.Size = (size <= 0) ? (-1) : size;
                    }
                    catch (FormatException)
                    {
                        args = new ProjectExceptionArgs(
                           String.Format("Invalid parameter value for: {0} - expecting a valid String", name), SeverityLevel.Format);
                        _handler.Add(args);
                    }
                    break;
                case ("text"):
                default:
                    try
                    {
                        if (size <= 0)
                        {
                            parameter = _command.Parameters.Add(name, SqlDbType.VarChar);
                            parameter.Size = -1;
                        }
                        else
                        {
                            parameter = _command.Parameters.Add(name, SqlDbType.Text);
                            parameter.Size = (size <= 0) ? (-1) : size;
                        }
                        parameter.Value = Convert.ToString(value);
                    }
                    catch (InvalidCastException)
                    {
                        args = new ProjectExceptionArgs(
                           String.Format("Invalid parameter value for: {0} - expecting a valid String", name), SeverityLevel.Format);
                        _handler.Add(args);
                    }
                    break;
            }

            // parameter direction
            if (inout == "inout")
                parameter.Direction = ParameterDirection.InputOutput;
            if (inout == "out")
                parameter.Direction = ParameterDirection.Output;

        }
        public void InitializeCommand()
        {
            _rowsaffected = 0;

            // make the command
            if (_command == null)
            {
                _command = _database.Connection.CreateCommand();
                _command.CommandType = CommandType.Text;

                // set timeouts
                int commandTimeout = (ConfigurationManager.AppSettings["SqlCommandTimeout"] != null) ? Convert.ToInt32(ConfigurationManager.AppSettings["SqlCommandTimeout"]) : 25;
                _command.CommandTimeout = commandTimeout;

                if (_database.WithTransaction)
                    _command.Transaction = _database.Transaction;
            }

            if (_command.Connection.State == ConnectionState.Closed )
                _command.Connection.Open();

        }
        private void BuildResults(SqlDataReader dr, int subqueryIndex = 0)
        {
            // paging
            switch (_pagingDirection)
            {
                case DatabaseQueryPagingDirection.Top:
                    _currentPage = 1;
                    break;
                case DatabaseQueryPagingDirection.Next:
                    _currentPage++;
                    break;
                case DatabaseQueryPagingDirection.Previous:
                    _currentPage--;
                    if (_currentPage < 1)
                        _currentPage = 1;
                    break;
            }

            int startread = (_currentPage * _pagingLimit) - _pagingLimit;
            int endread = startread + _pagingLimit;

            //build result node
            MemoryStream stream = new MemoryStream();
            XmlTextWriter w = new XmlTextWriter(stream, Encoding.UTF8);

            XmlNodeList fieldNodes;

            // begin writing the xml result
            w.WriteStartElement("result");

            try
            {

                //field values for results
                if (subqueryIndex > 0)
                    fieldNodes = _querynode.SelectNodes(String.Format(@"subquery[{0}]/fields/field", subqueryIndex));
                else
                    fieldNodes = _querynode.SelectNodes(@"fields/field");

                //add rows to result node
                int currentrow = 0;

                _resultPages = getNewResultPages(_querynode);
                int pagecount = 1;
                int inpagecount = 0;
                while (dr.Read())
                {
                    bool flginpage = false;

                    if ((currentrow >= (startread) && currentrow < endread) || _pagingLimit == -1)
                        flginpage = true;

                    #region in page write results
                    if (flginpage)
                    {
                        w.WriteStartElement("row");

                        // if we come accross Json fields, they are translated to Xml and added as child to row node
                        // keep track of them and add them after all attributes have been processed
                        List<string> innerXml = new List<string>();

                        //attributes (fields)
                        if (fieldNodes.Count > 0)
                        {
                            foreach (XmlElement m in fieldNodes)
                            {
                                try
                                {
                                    // validate json type
                                    if (m.GetAttribute("type") == "json" || m.GetAttribute("type") == "tryjson")
                                    {
                                        try
                                        {

                                            var val = dr[m.GetAttribute("name")].ToString();
                                            string json = null;
                                            if (string.IsNullOrEmpty(val))
                                            {
                                                json = "{}";
                                            }
                                            else
                                            {
                                                var jsoObj = Newtonsoft.Json.Linq.JObject.Parse(val);
                                                json = val;
                                            }
                                            w.WriteAttributeString(m.GetAttribute("name").ToString(), json);
                                        }
                                        catch (IndexOutOfRangeException handled)
                                        {
                                            throw handled;
                                        }
                                        catch (Exception unhandled)
                                        {
                                            w.WriteAttributeString(m.GetAttribute("name").ToString(), "{\"error\":\"" + unhandled.Message + "\"}");
                                        }
                                    }
                                    else
                                    {
                                        string val = dr[m.GetAttribute("name")].ToString();
                                        if (!(m.HasAttribute("encode") || m.HasAttribute("regx")))
                                            val = dr[m.GetAttribute("name")].ToString().Trim();

                                        if (m.HasAttribute("encode"))
                                        {
                                            val = System.Web.HttpUtility.UrlEncode(dr[m.GetAttribute("encode")].ToString().TrimEnd()).Replace("+", "%20"); ;
                                        }
                                        if (m.HasAttribute("regx") && m.HasAttribute("replace") && m.HasAttribute("field"))
                                        {
                                            val = dr[m.GetAttribute("field")].ToString().Trim();
                                            val = Regex.Replace(val, m.GetAttribute("regex").ToString(), m.GetAttribute("replace").ToString());
                                        }
                                        w.WriteAttributeString(m.GetAttribute("name").ToString(), String.IsNullOrEmpty(val) ? "" : safeXmlCharacters(val.ToString().Trim()));
                                    }


                                    if (m.GetAttribute("type") == "json")
                                    {
                                        string xml = null;
                                        try
                                        {

                                            var details = dr[m.GetAttribute("name")].ToString();

                                            // try to parse it


                                            string jsonDetails = String.Format("{{\"{0}\":{1}}}", m.GetAttribute("name"), details);
                                            xml = JsonConvert.DeserializeXmlNode(jsonDetails).OuterXml;
                                            if (xml.StartsWith("<?"))
                                                xml = xml.Substring(xml.IndexOf("?>") + 2);
                                        }
                                        catch (JsonReaderException) { }
                                        catch (JsonSerializationException) { }
                                        finally
                                        {
                                            if (xml != null)
                                                innerXml.Add(xml);
                                        }
                                    }
                                }
                                catch (IndexOutOfRangeException)
                                {
                                    w.WriteAttributeString(m.GetAttribute("name").ToString(), "#field not found#");
                                }
                            }
                        }
                        else
                        {
                            for (int i = 0; i < dr.FieldCount; i++)
                            {
                                w.WriteAttributeString(dr.GetName(i), dr[i].ToString().Trim());
                            }
                        }

                        // add inner xml 
                        foreach (String s in innerXml)
                            w.WriteRaw(s);

                        w.WriteEndElement();
                    }
                    #endregion


                    _rowsaffected = currentrow++;
                    inpagecount++;
                    if (inpagecount >= _pagingLimit)
                    {
                        _resultPages.Add(new ResultPaging(pagecount++, inpagecount));
                        inpagecount = 0;
                    }

                }

                // get last result for _resultPage
                if (inpagecount > 0)
                    _resultPages.Add(new ResultPaging(pagecount++, inpagecount));

                _results = true;

                // reset a couple of paging items
                _currentPage = 1;
                _pagingDirection = DatabaseQueryPagingDirection.None;
            }
            catch (IndexOutOfRangeException ie)
            {
                string errmsg = string.Format("One or more invalid Field or Parameters for QueryName: {0}", _querynode.Attributes["name"].InnerText);
                ProjectExceptionArgs args = new ProjectExceptionArgs(errmsg, "Database.cs", "BuildResults", null, SeverityLevel.Fatal, LogLevel.Event);
                throw new ProjectException(args, ie);
            }
            catch (SqlException se)
            {
                string errmsg = string.Format("ExecuteReader Error For QueryName: {0}", _querynode.Attributes["name"].InnerText);
                ProjectExceptionArgs args = new ProjectExceptionArgs(errmsg, "Database.cs", "BuildResults", null, SeverityLevel.Fatal, LogLevel.Event);
                throw new ProjectException(args, se);
            }

            //end result node
            w.WriteEndElement();
            w.Flush();

            // include sub results (StoredProcedure returns more than one Result Set)
            while (dr.NextResult())
            {
                subqueryIndex++;
                BuildResults(dr, subqueryIndex);
            }


            //add stream xml to return xml
            XmlDocument xmStreamObj = new XmlDocument();
            stream.Seek(0, SeekOrigin.Begin);
            xmStreamObj.Load(stream);

            pushToTree(xmStreamObj, subqueryIndex);

        }

        private void pushToTree(XmlDocument xmStreamObj, int subqueryIndex)
        {
            //import result xml to original xml obj
            XmlNode import = _xmresult.ImportNode(xmStreamObj.DocumentElement, true);
            XmlNode elm;
            if (subqueryIndex > 0 && _xmresult.SelectSingleNode("results/subquery") == null)
                elm = _xmresult.SelectSingleNode("results").AppendChild(_xmresult.CreateElement("subquery"));
            else
                elm = _xmresult.SelectSingleNode("results");

            elm.AppendChild(import);


            // cache builder
            if (_cache != null && CachingEnabled)
            {
                string key = cacheKeyHelper(_xmresult);
                SaveCache(key, import);
            }
        }

        private string cacheKeyHelper(XmlNode Node)
        {
            var keybuilder = new StringBuilder();
            var queryNode = Node.SelectSingleNode("descendant-or-self::query");
            keybuilder.Append(queryNode.Attributes["name"].Value);
            var pars = Node.SelectNodes("descendant::parameter");
            foreach (XmlNode node in pars)
            {
                if (!String.IsNullOrWhiteSpace(node.InnerText))
                    keybuilder.Append(node.InnerText.Trim());
            }

            return keybuilder.ToString();
        }

        private void pushToTree(XmlNode cachedBuilder)
        {
            XmlNode import = _xmresult.ImportNode(cachedBuilder, true);
            XmlNode elm = _xmresult.SelectSingleNode("results");
            elm.AppendChild(import);
        }


        private string safeXmlCharacters(string val)
        {
            string re = @"[\x00-\x08\x0B\x0C\x0E-\x1F\x26]";
            return Regex.Replace(val, re, "");
        }

        private ResultPages getNewResultPages(XmlNode _querynode)
        {
            string queryname;
            string queryhash;

            queryname = _querynode.Attributes["name"].InnerText;
            byte[] saltbyte = Encoding.UTF8.GetBytes("mso1234");   // for salting form submission
            queryhash = Utility.SimpleHash.ComputeHash(_querynode.OuterXml, "HASH256", saltbyte);
            return new ResultPages(queryname, queryhash, _currentPage, _pagingLimit);
        }
        private bool _isValid
        {
            get
            {
                return (_handler.XmlDocument == null ? true : false);
            }
        }
        private bool validInput(XmlNode node)
        {
            bool result = true;
            bool required = false;

            XmlElement m = (XmlElement)node;

            // exceptions - no regular expression check
            if (m.GetAttribute("validation") == "")
                return (true);

            // for each validation scenario in validations (space delimited list
            string[] aValid = m.GetAttribute("validation").Split(' ');
            List<string> validations = new List<string>(aValid);
            string srequired = validations.Find(delegate (String s) { return s == "required"; });
            bool isrequired = Convert.ToBoolean(String.IsNullOrEmpty(srequired) ? false : srequired.Equals("true") || srequired.Equals("True"));

            foreach (string validation in validations)
            {
                if (validation.StartsWith("regx:"))
                {
                    if (!m.IsEmpty || !string.IsNullOrEmpty(m.InnerText))
                    {
                        //load validation xml obj library
                        if (_xmRegX == null)
                            throw new Exception("RegX input validation fails, RegX library not loaded");

                        // TODO: pass back friendly title for item
                        if (!validInputTestRegX(m.InnerText, validation.Split(':')[1]))
                        {
                            if (!isrequired && m.InnerText.Length == 0)
                                result = true;
                            else
                            {
                                result = false;
                                ProjectExceptionArgs args = new ProjectExceptionArgs(String.Format("Invalid Format for item: {0}", String.IsNullOrEmpty(m.GetAttribute("title")) ? m.GetAttribute("name") : m.GetAttribute("title")), SeverityLevel.Format);
                                _handler.Add(args);
                            }
                        }
                    }
                }
                else
                {
                    switch (validation)
                    {
                        case "required":
                            // expecting a value for innertext
                            required = true;
                            if (m.IsEmpty)
                            {
                                result = false;
                                ProjectExceptionArgs args = new ProjectExceptionArgs(String.Format("A value is required: {0}", String.IsNullOrEmpty(m.GetAttribute("title")) ? m.GetAttribute("name") : m.GetAttribute("title")), SeverityLevel.Format);
                                _handler.Add(args);
                            }
                            break;
                    }

                }

            }

            // test the object type for consistency with object value
            if (required && result)
                switch (m.GetAttribute("type"))
                {
                    case "int":
                        if (!(validInputTestNumber(m.InnerText)))
                        {
                            result = false;
                            ProjectExceptionArgs args = new ProjectExceptionArgs(String.Format("Format Exception, expecting a valid number for: {0}", String.IsNullOrEmpty(m.GetAttribute("title")) ? m.GetAttribute("name") : m.GetAttribute("title")), SeverityLevel.Format);
                            _handler.Add(args);
                        }
                        break;
                    case "text":
                    case "varchar":
                        if (!(validInputTestCharacters(m.InnerText, m.GetAttribute("size"))))
                        {
                            result = false;
                            ProjectExceptionArgs args = new ProjectExceptionArgs(String.Format("Format Exception, too many characters for: {0}, expecting: {1}", String.IsNullOrEmpty(m.GetAttribute("title")) ? m.GetAttribute("name") : m.GetAttribute("title"), m.GetAttribute("size")), SeverityLevel.Format);
                            _handler.Add(args);
                        }
                        break;
                    case "date":
                        // expecting a value that can parse as a date
                        if (!(validInputTestDate(m.InnerText)))
                        {
                            result = false;
                            ProjectExceptionArgs args = new ProjectExceptionArgs(String.Format("Format Exception, expecting a valid date for: {0}", String.IsNullOrEmpty(m.GetAttribute("title")) ? m.GetAttribute("name") : m.GetAttribute("title"), m.GetAttribute("size")), SeverityLevel.Format);
                            _handler.Add(args);
                        }
                        break;
                }

            return result;
        }
        private bool validInputTestCharacters(string Value, string Size)
        {
            bool result = false;

            if (String.IsNullOrEmpty(Size))
                return true;

            int size = Convert.ToInt32(Size);

            if (Value.Length <= size)
                result = true;

            return result;
        }
        private bool validInputTestNumber(string Value)
        {
            bool result = false;

            try
            {
                int x = Int32.Parse(Value);
                result = true;  // we only get this far of Parse is successfull
            }
            catch { }

            return result;
        }
        private bool validInputTestDate(string Value)
        {
            bool result = false;
            try
            {
                DateTime dt = DateTime.Parse(Value);
                result = true;  // we only get here if DateTime Parse is successfull
            }
            catch { }

            return result;
        }
        private bool validInputTestRegX(string Subject, string RegX)
        {
            XmlNode reg_ex = _xmRegX.SelectSingleNode(string.Format("root/regx[@name='{0}']", RegX));
            if (reg_ex == null)
                throw new Exception("Validation regular expression missing: " + RegX);
            string _pattern = reg_ex.InnerText;
            Match _match = Regex.Match(Subject, _pattern, RegexOptions.IgnoreCase);
            return (_match.Success);
        }

        #endregion

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


        #region constructors
        public DatabaseQuery(DatabaseConnection Connection)
        {
            _database = Connection;
        }
        #endregion

        #region public methods
        public void Query(XmlNode Query)
        {
            // command type
            var xpath = "command/type";
            var elm = (XmlElement)Query.SelectSingleNode(xpath);
            var CommandTypeName = elm.InnerText;

            xpath = "cache";
            var cachenode = (XmlElement)Query.SelectSingleNode(xpath);
            _cachingEnabled = false;
            if(_cache != null && cachenode != null)
            {
                _cachingEnabled = bool.Parse(cachenode.SelectSingleNode("enabled").InnerText);
                _cacheMinutes = int.Parse(cachenode.SelectSingleNode("minutes").InnerText);
            }

            // command action
            xpath = "command/action";
            elm = (XmlElement)Query.SelectSingleNode(xpath);
            var CommandAction = elm.InnerText;

            string timingToken = "Anonymous Query";
            if (Timing != null)
            {
                if (Query.Attributes["name"] != null)
                {
                    var queryname = Query.Attributes["name"].Value;
                    timingToken = String.Format("ProjectFlxDB.DB.DatabaseQuery {0}", queryname);
                }
                Timing.Start(timingToken);
            }

            string _commandtext = null;
            
            if (CommandAction == "Result")
            {
                string cachekey = cacheKeyHelper(Query);
                var cachedBuilder = GetCache(cachekey);
                if (cachedBuilder != null && _cachingEnabled)
                {
                    SetupResults(_xmresult, Query);
                    pushToTree(cachedBuilder);
                    if (Timing != null)
                        Timing.End(timingToken);
                    return;
                }
            }

            InitializeCommand();

            // command timeout
            if (Query.Attributes["script-timeout"] != null)
                _command.CommandTimeout = int.Parse(Query.Attributes["script-timeout"].Value);

            _querynode = Query;

            try
            {

                switch (CommandTypeName)
                {
                    case "StoredProcedure":
                        _command.Parameters.Clear();
                        _command.CommandType = CommandType.StoredProcedure;
                        break;
                    case "Select":
                    case "SQL":
                        _command.CommandType = CommandType.Text;
                        break;
                }

                // command text
                switch (_command.CommandType)
                {
                    case (CommandType.Text):
                        xpath = "command/text";
                        elm = (XmlElement)Query.SelectSingleNode(xpath);
                        _commandtext = Regex.Replace(elm.InnerText, @"^[ \t]+", "", RegexOptions.Multiline | RegexOptions.IgnorePatternWhitespace);
                        _commandtext = Regex.Replace(_commandtext, @"(\r\n)", " ", RegexOptions.Multiline | RegexOptions.IgnorePatternWhitespace);
                        break;
                    default:
                        xpath = "command/name";
                        elm = (XmlElement)Query.SelectSingleNode(xpath);
                        _commandtext = elm.InnerText;
                        _command.Parameters.Clear();
                        break;
                }

                // command parameters - in only
                xpath = "parameters/parameter[not(@inout='out')]";
                XmlNodeList nodes = Query.SelectNodes(xpath);
                foreach (XmlNode node in nodes)
                {
                    // validate input
                    if (validInput(node))
                    {

                        if (_command.CommandType == CommandType.Text)
                        {
                            string replaceparam = "[" + node.Attributes["name"].Value + "]";
                            _commandtext = _commandtext.Replace(replaceparam, node.InnerText);
                        }
                        else
                        {
                            elm = (XmlElement)node;
                            if (!elm.IsEmpty)
                            {
                                int size = 0;
                                if (!String.IsNullOrEmpty(elm.GetAttribute("size")))
                                    size = Convert.ToInt32(elm.GetAttribute("size"));

                                AddParameter(elm.GetAttribute("name"), elm.InnerText, elm.GetAttribute("type"), elm.GetAttribute("inout"), size);
                            }
                        }

                    }
                }

                // did all input values validate?
                if (!_isValid)
                    throw new ProjectException("Invalid Formatting Exception", _handler);

                // command parameters - output only (are required)
                xpath = "parameters/parameter[@inout='out']";
                nodes = Query.SelectNodes(xpath);
                foreach (XmlNode node in nodes)
                {
                    if (_command.CommandType == CommandType.Text)
                    {
                        string replaceparam = "[" + node.Attributes["name"].Value + "]";
                        _commandtext.Replace(replaceparam, node.InnerText);
                    }
                    else
                    {
                        elm = (XmlElement)node;
                        int size = 0;
                        if (!String.IsNullOrEmpty(elm.GetAttribute("size")))
                            size = Convert.ToInt32(elm.GetAttribute("size"));

                        AddParameter(elm.GetAttribute("name"), elm.InnerText, elm.GetAttribute("type"), elm.GetAttribute("inout"), size);
                    }

                }

                _command.CommandText = _commandtext;

                // prepare result xml document
                XmlNode importnode;
                XmlNode newElm;
                SetupResults(_xmresult, Query);

                // execute query
                int scalar = 0;
                int rows = 0;

                switch (CommandAction)
                {
                    case ("Scalar"):
                        var obj = _command.ExecuteScalar();
                        int.TryParse(obj == null ? "0" : obj.ToString(), out scalar);
                        _scalar = scalar;
                        if (scalar > 0)
                            rows = 1;
                        _rowsaffected = rows;

                        // set result in xml 
                        elm = (XmlElement)_xmresult.SelectSingleNode("results");
                        newElm = _xmresult.CreateElement("result");
                        newElm.InnerText = Convert.ToString(_rowsaffected);
                        elm.AppendChild(newElm);

                        break;
                    case ("Result"):
                        // TODO: enable caching
                        //var cachekey = cacheKeyHelper(_xmresult);
                        //var cachedBuilder = GetCache(cachekey);
                        //if (cachedBuilder != null && _cachingEnabled)
                        //{
                        //    pushToTree(cachedBuilder);
                        //}
                        //else
                        //{
                            using (SqlDataReader dr = _command.ExecuteReader())
                            {
                                try
                                {
                                    BuildResults(dr);
                                }
                                finally
                                {
                                    dr.Close();
                                }
                            }
                        //}
                        break;
                    case ("NonQuery"):

                    default:
                        rows = _command.ExecuteNonQuery();
                        _rowsaffected = rows;

                        // set result in xml 
                        elm = (XmlElement)_xmresult.SelectSingleNode("results");
                        newElm = _xmresult.CreateElement("result");
                        newElm.InnerText = Convert.ToString(_rowsaffected);
                        elm.AppendChild(newElm);

                        break;
                }

                // set output parameter results on result xml document
                xpath = "results/schema/query/parameters/parameter[@inout='out' or @inout='inout']";
                nodes = _xmresult.SelectNodes(xpath);
                foreach (XmlNode node in nodes)
                {
                    node.InnerText = _command.Parameters[node.Attributes["name"].InnerText].Value.ToString();
                }
            }
            catch (SqlException handledSql)
            {
                if (handledSql.Message.Contains(" duplicate key "))
                {
                    if (QuiteUniqueConstraints == false)
                        throw handledSql;
                }
                else
                    throw handledSql;
            }
            catch (Exception unhandled)
            {
                ProjectExceptionArgs args = new ProjectExceptionArgs("Sorry, we handled an exception.  The problem has been sent to MSO Admin", "mso.Utility.DB.DatabaseQuery", "Query " + _commandtext, null, SeverityLevel.Critical, LogLevel.Event);
                throw new ProjectException(args, unhandled);
            }
            finally
            {
                if (Timing != null)
                    Timing.Stop(timingToken);
            }
        }

        private void SetupResults(XmlDocument xmresult, XmlNode Query)
        {
            /*
            _xmresult = new XmlDocument();
            _xmresult.LoadXml("<results><schema/></results>");
            _xmresult.DocumentElement.SetAttribute("name", Query.Attributes["name"].Value);
            if (!String.IsNullOrEmpty(_sqlProjName))
                _xmresult.DocumentElement.SetAttribute("ProjectSqlFile", _sqlProjName);
            importnode = _xmresult.ImportNode(Query, true);
            _xmresult.SelectSingleNode("results/schema").AppendChild(importnode);
            */

            if (_xmresult == null || _xmresult.DocumentElement == null)
            {
                _xmresult = new XmlDocument();
                _xmresult.LoadXml("<results><schema/></results>");

                var schemanode = _xmresult.SelectSingleNode("results/schema");
                var importnode = _xmresult.ImportNode(Query, true);

                _xmresult.DocumentElement.SetAttribute("name", Query.Attributes["name"].Value);
                _xmresult.SelectSingleNode("results/schema").AppendChild(importnode);
                
                // TODO: needed to support caching?
                //foreach(XmlNode node in Query.SelectNodes("child::*"))
                //{
                //    var newnode = _xmresult.ImportNode(node, true);
                //    schemanode.AppendChild(newnode);
                //}

                if (!String.IsNullOrEmpty(_sqlProjName))
                    _xmresult.DocumentElement.SetAttribute("ProjectSqlFile", _sqlProjName);

                return;
            }

            // TODO: support existing element - supports caching?
            if (xmresult.DocumentElement != null)
            {
                _xmresult.DocumentElement.SetAttribute("name", Query.Attributes["name"].Value);
                if (!String.IsNullOrEmpty(_sqlProjName))
                    _xmresult.DocumentElement.SetAttribute("ProjectSqlFile", _sqlProjName);
            }
        }

        private XmlNode GetCache(String Key)
        {
            if (_cache == null || CachingEnabled == false)
                return null;

            try
            {
                // cache
                var obj = _cache[Key];
                return (XmlNode)obj;
            }
            catch
            {
                return null;
            }
        }

        // TODO: replicate this for Schemabase queries
        // TODO: drive caching off of Xml decleration
        // TODO: consider an AWS caching service
        private void SaveCache(string Key, XmlNode BuilderDoc)
        {
            if (_cache == null)
                return;

            CacheDependency cachedependency = null;
            if (!String.IsNullOrEmpty(this._cachePath))
            {
                //string depFile = Path.Combine(_cachePath, System.IO.Path.GetRandomFileName() + ".cache");
                var keypath = string.Concat(Key.Split(Path.GetInvalidFileNameChars()));
                if (keypath.Length > 100) keypath = keypath.Substring(0, 100);

                string depFile = Path.Combine(_cachePath, keypath + ".cache");
                using (StreamWriter writer = new StreamWriter(depFile))
                {
                    writer.WriteLine(DateTime.Now.ToString("yyyyMMddHHmmssffff"));
                }
                cachedependency = new CacheDependency(depFile);
            }
            _cache.Insert(Key, BuilderDoc, cachedependency, DateTime.Now.AddMinutes(_cacheMinutes), System.Web.Caching.Cache.NoSlidingExpiration);
        }

        public void Clear()
        {
            _pagingLimit = -1;
            _scalar = 0;
            _rowsaffected = 0;
            _results = false;
            _xmresult = null;
        }

        public void Query(Schema.SchemaQueryType QueryType)
        {
            XmlDocument xml = new XmlDocument();
            xml.LoadXml(QueryType.Serialize());
            Query((XmlNode)xml.DocumentElement);
        }
        #endregion

        #region public properties
        public XmlDocument RegXValidationDocument
        {
            get
            {
                return _xmRegX;
            }
            set
            {
                _xmRegX = value;
            }
        }
        public XmlDocument Result
        {
            get
            {
                return _xmresult;
            }
        }
        public XDocument ResultXDoc
        {
            get
            {
                return XDocument.Load(new XmlNodeReader(_xmresult));
            }
        }
        public XmlDocument PagingNode
        {
            get
            {
                if (!_results)
                    throw new Exception("No results built.  Call Query method first");

                XmlDocument xm = new XmlDocument();
                xm.LoadXml(_resultPages.Serialize());

                return xm;

            }
        }
        public ProjectExceptionHandler ProjectExceptionHandler
        {
            get
            {
                return _handler;
            }
        }
        public int RowsAffected
        {
            get
            {
                return _rowsaffected;
            }
        }
        public int Scalar
        {
            get
            {
                return _scalar;
            }
        }
        public int LastInsertID
        {
            get
            {
                return _lastInsertID;
            }
        }
        public int CurrentPage
        {
            get
            {
                return _currentPage;
            }
            set
            {
                _currentPage = value;
            }
        }
        public string SqlProjectName
        {
            get
            {
                return _sqlProjName;
            }
            set
            {
                _sqlProjName = value;
            }
        }
        #endregion

        #region IDatabaseQueryPaging Members

        public void PageMove(DatabaseQueryPagingDirection PagingDirection)
        {
            _pagingDirection = PagingDirection;
        }

        public int Limit
        {
            get
            {
                return _pagingLimit;
            }
            set
            {
                _pagingLimit = value;
            }
        }

        #endregion

        private bool _uniqueContraintsFlag = false;
        /// <summary>
        /// Snuff Unique Contraint Errors - default true
        /// </summary>
        public bool QuiteUniqueConstraints
        {
            get
            {
                return _uniqueContraintsFlag;
            }
            set
            {
                _uniqueContraintsFlag = value;
            }
        }
        bool disposed = false;

        public void Dispose()
        {
            if (this.disposed)
                return;

            if(_command != null)
                _command.Dispose();
        }
        public Utility.Timing Timing { get; set; }
    }

    // TODO: fill paging web from web post or other

    //    public static DatabaseQueryPagingDirection getPageDirectionFromString(string Value)
    //{
    //    DatabaseQueryPagingDirection d = new DatabaseQueryPagingDirection();
    //    switch (Value)
    //    {
    //        case "top":
    //            d = DatabaseQueryPagingDirection.Top;
    //            break;
    //        case "last":
    //            d = DatabaseQueryPagingDirection.Last;
    //            break;
    //        case "next":
    //            d = DatabaseQueryPagingDirection.Next;
    //            break;
    //        case "previous":
    //            d = DatabaseQueryPagingDirection.Previous;
    //            break;
    //        default:
    //            d = DatabaseQueryPagingDirection.None;
    //            break;
    //    }

    //    return d;
    //}


    //    protected void fillPagingWeb()
    //{
    //    HttpContext _current = HttpContext.Current;
    //    if (_current == null)
    //        return;

    //    // limit
    //    string qobj = _current.Request["limit"];
    //    int iobj = -1;
    //    int.TryParse(qobj, out iobj);

    //    //TODO: figure out why paging element is not persisting to json object
    //    // need paging object on client side


    //    if (iobj > 0)
    //        _schemaQuery.paging.limit = iobj;

    //    // current page
    //    iobj = 1;
    //    qobj = _current.Request["page"];
    //    int.TryParse(qobj, out iobj);

    //    if (iobj == 0)
    //        iobj = 1;

    //    _schemaQuery.paging.pages.current = iobj;

    //    // direction
    //    qobj = _current.Request["direction"];

    //    if (qobj != null)
    //    {
    //        Schema.pagingDirectionType pagingtype = (ProjSchema.pagingDirectionType)Enum.Parse(typeof(ProjSchema.pagingDirectionType), qobj);
    //        _schemaQuery.paging.direction = pagingtype;

    //    }

    //}

}

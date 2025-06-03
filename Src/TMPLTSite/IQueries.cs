using ProjectFlx.Schema;
using System.Collections.Specialized;

namespace ProjectFlx
{
    public interface IQueries
    {

        void ExecuteQuery(string Project, string QueryName);
        void ExecuteQuery(string Project, string QueryName, NameValueCollection Parms);
        void ExecuteQuery(string Project, string QueryName, projectResults Results, NameValueCollection Parms);
        void ExecuteQuery(string Project, string QueryName, Queries.QueryPromise Promise, NameValueCollection Parms);
        void ExecuteQuery(string Project, string QueryName, projectResults Results, Queries.QueryPromise Promise, NameValueCollection Parms);

        void ExecuteQuery(FlxTemplater TMPLT, string Project, string QueryName);
        void ExecuteQuery(FlxTemplater TMPLT, string Project, string QueryName, NameValueCollection Parms);
        void ExecuteQuery(FlxTemplater TMPLT, string Project, string QueryName, projectResults Results, NameValueCollection Parms);
        void ExecuteQuery(FlxTemplater TMPLT, string Project, string QueryName, Queries.QueryPromise Promise, NameValueCollection Parms);
        void ExecuteQuery(FlxTemplater TMPLT, string Project, string QueryName, projectResults Results, Queries.QueryPromise Promise, NameValueCollection Parms);

        void ExecuteRawQuery(string Project, string QueryName, string Select);
        void ExecuteRawQuery(string Project, string QueryName, string Select, Queries.FieldNameCollection Fields);
        void ExecuteRawQuery(string Project, string QueryName, string Select, projectResults Result, NameValueCollection Parms);
        void ExecuteRawQuery(string Project, string QueryName, string Select, projectResults Result, Queries.FieldNameCollection Fields, NameValueCollection Parms);

        void ExecuteRawQuery(FlxTemplater TMPLT, string Project, string QueryName, string Select);
        void ExecuteRawQuery(FlxTemplater TMPLT, string Project, string QueryName, string Select, Queries.FieldNameCollection Fields);
        void ExecuteRawQuery(FlxTemplater TMPLT, string Project, string QueryName, string Select, projectResults Result, NameValueCollection Parms);
        void ExecuteRawQuery(FlxTemplater TMPLT, string Project, string QueryName, string Select, projectResults Result, Queries.FieldNameCollection Fields, NameValueCollection Parms);

        result ExecuteQuery2(string Project, string QueryName, NameValueCollection Parms);
        result ExecuteQuery2(string Project, string QueryName, Queries.QueryPromise Promise, NameValueCollection Parms);

        string ExecuteQueryJson(string Project, string QueryName, NameValueCollection Parms);

        int ExecuteScalar(string Project, string QueryName, NameValueCollection Parms);

        int ExecuteNonQuery(string Project, string QueryName, NameValueCollection Parms);
    }
}
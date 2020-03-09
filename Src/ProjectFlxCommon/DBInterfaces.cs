using ProjectFlx.Schema;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace ProjectFlx.DB
{
    public enum DatabaseQueryPagingDirection { Top, Next, Previous, Last, None }

    public interface IDatabaseQueryPaging
    {
        void PageMove(DatabaseQueryPagingDirection PagingDirection);
        int Limit { get; set; }
        int CurrentPage { get; set; }
        XmlDocument PagingNode { get; }
    }

    public interface IDatabaseQuery
    {
        int LastInsertID { get; }
        int RowsAffected { get; }
    }

    public interface IProjectValidator
    {
        void Validate(string SchemaQuerySourceName);
        string QueryName { get; }
    }

    public interface IProjects
    {
        string Json(string QueryName, SchemaBased.IDatabaseQuery DB);
        string JsonFieldParms(String QueryName);
        void SetValidator(IProjectValidator Validator);
    }

    public interface IProject
    {
        void setQuery(String ProjQueryName);
        void setParameter(String Name, Object Value);
        void clearParameters();
        void fillParms();
        void fillParms(Object someObject);

        ProjectFlx.Schema.SchemaQueryType SchemaQuery { get; }
    }


    namespace SchemaBased
    {
        public interface IDatabaseQuery
        {
            projectResults ProjectResults { get; }

            void Query(IProject ProjectSchemaQueryObject);
            void Query(IProject ProjectSchemaQueryObject, bool IgnoreResults);
            void Query(SchemaQueryType SchemaQuery);
            void Query(SchemaQueryType SchemaQuery, bool IngoreResults);
        }
    }
}

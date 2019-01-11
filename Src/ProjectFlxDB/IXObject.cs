using ProjectFlx.Schema;

namespace ProjectFlx.DB
{
    public interface IXObject
    {
        void Query();
        void SetParameter(parameters Parameters);
        void SetParameter(string ParamName, object ParamValue);
        void SetParameter(string ParamName, object ParamValue, string RegXPatternName);
        void SetQuery(string QueryName);
    }
}
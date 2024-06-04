namespace ProjectFlx
{
    /// <summary>
    /// Custom encode a field coming out of the database
    /// In ProjectSql.xml add encode_custom field and set value to your custom field process routine
    /// </summary>
    public interface ICustomEncoder
    {
        string Process(string ProcessorName, string Value);
    }
}
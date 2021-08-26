using System;
using System.IO;
using System.Xml;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using ProjectFlx.Schema;
using System.Web;
using System.Net;
using ProjectFlx.Schema.Extra;

namespace ProjectFlx.Schema
{
    public static class Helper
    {
        public static string FlattenList(List<string> list)
        {
            var val = String.Join(" ", list.ToArray());
            var current = HttpContext.Current;
            if (current != null)
                return current.Server.HtmlDecode(val);
            else
                return val;
        }

        public static class schemaQueryXml
        {
            public static Utility.TimingCollection Timing { get; set; }

            public static void getProjectGrouped(projectResults ProjectResults, XmlWriter XWriter, params string[] Grouped)
            {
                try
                {
                    if (Timing != null)
                        Timing.Start("getResultGrouped");

                    foreach (var results in ProjectResults.results)
                    {
                        getResultGrouped(results, XWriter, Grouped);
                    }
                }
                finally
                {
                    Timing.Stop("getResultGrouped");
                }
            }

            public static void getResultGrouped_Original(results Results, XmlWriter XWriter, params string[] Grouped)
            {
                XWriter.WriteStartElement("results");
                XWriter.WriteAttributeString("name", Results.name);
                XWriter.WriteAttributeString("ProjectSqlFile", Results.ProjectSqlFile);
                XWriter.WriteStartElement("schema");
                XWriter.WriteRaw(Helper.StripXmlDecleration(Results.schema[0].Serialize()));
                XWriter.WriteEndElement();
                XWriter.WriteStartElement("result");

                var groupingValues = new Dictionary<string, string>();
                foreach(var s in Grouped)
                    groupingValues.Add(s.Trim(), null);
                int depth;

                // check for differences - open element if null
                for (int x = 0; x < groupingValues.Count; x++)
                {
                    foreach (var row in Results.result.row)
                    {

                        depth = x;
                        string key = groupingValues.Keys.ElementAt(x);
                        if (groupingValues[key] == null)
                        {
                            XWriter.WriteStartElement(key);
                            XWriter.WriteAttributeString("value", row.AnyAttr.LookupValue(key));
                            groupingValues[key] = row.AnyAttr.LookupValue(key);
                            continue;
                        }

                        if (!groupingValues[key].Equals(row.AnyAttr.LookupValue(key)))
                        {
                            // close elements
                            for (int j = x; j < groupingValues.Count; j++)
                                XWriter.WriteEndElement();

                            // open elements
                            for (int k = x; k < groupingValues.Count; k++)
                            {
                                key = groupingValues.Keys.ElementAt(k);
                                XWriter.WriteStartElement(key);
                                XWriter.WriteAttributeString("value", row.AnyAttr.LookupValue(key));
                                groupingValues[key] = row.AnyAttr.LookupValue(key);
                            }
                        }
                        getRowGrouped(row, XWriter);
                    }
                }
                // close last grouping items
                for(int x = 0; x < groupingValues.Count; x++)
                    XWriter.WriteEndElement();

                XWriter.WriteEndElement();
                XWriter.WriteEndElement();
            }

            internal static void Temp_WriteFile(string Msg, params object[] parms)
            {
                try
                {
                    var msg = String.Format("{0}\n", Msg);
                    File.AppendAllText(@"e:\temp\Temp_WriteFile.txt", String.Format(msg, parms));
                }
                catch { }
            }

            internal static void getSubGroups(results Results, List<Grouped> Groups, Grouped Parent, int Depth, params string[] Grouped)
            {
                var subrow = Results.result.row;

                var parent = Parent;
                while (parent != null)
                {
                    subrow = subrow.Where(w => w.AnyAttr.LookupValue(parent.Name) == parent.Value).ToList();
                    parent = parent.Parent;
                }

                foreach (var row in subrow)
                {
                    var name = Grouped[0];
                    var val = row.AnyAttr.LookupValue(name);

                    if (!Groups.Any(a => a.Name.Equals(name) && a.Value.Equals(val)))
                    {
                        Schema.Grouped g;
                        Groups.Add(g = new Schema.Grouped()
                        {
                            Depth = Depth,
                            Name = name,
                            Value = val,
                            Parent = Parent         // TODO: is this correct parent?
                        });

                        // Exhaust Grouped to last item - then add rows
                        //      Group1>Group2>Group3
                        //      Group2>Group3
                        //      Group3
                        //        row
                        //        row ...
                        if (Grouped.Length == 1)
                        {
                            if (g.Row == null)
                                g.Row = new List<row>();
                            g.Row.Add(row);
                        }
                    }
                }

                // drill down for each group
                if (Grouped.Length > 1)
                {
                    Depth++;
                    foreach (var group in Groups)
                    {
                        group.Groups = new List<Schema.Grouped>();
                        getSubGroups(Results, group.Groups, group, Depth, Grouped.Skip(1).ToArray());
                    }
                }
            }

            public static void getResultGrouped(results Results, XmlWriter XWriter, params string[] Grouped)
            {
                if (Timing != null)
                    Timing.Start("getResultGrouped");

                try
                {
                    /*
                     * 1. organize results by grouped
                     * 2. 
                     */
                    var list = new List<Grouped>();

                    //var groupingValues = new Dictionary<string, string>();

                    getSubGroups(Results, list, null, 0, Grouped);

                    list.Sort();

                    XWriter.WriteStartElement("results");
                    XWriter.WriteAttributeString("name", Results.name);
                    XWriter.WriteAttributeString("ProjectSqlFile", Results.ProjectSqlFile);
                    XWriter.WriteStartElement("schema");
                    XWriter.WriteRaw(Helper.StripXmlDecleration(Results.schema[0].Serialize()));
                    XWriter.WriteEndElement();
                    XWriter.WriteStartElement("result");

                    if (Timing != null)
                        Timing.Start("writeResultsGrouped");

                    writeResultsGrouped(XWriter, list);

                    if (Timing != null)
                        Timing.Stop("writeResultsGrouped");


                    XWriter.WriteEndElement();
                    XWriter.WriteEndElement();
                }
                finally
                {
                    if(Timing != null)
                        Timing.Stop("getResultGrouped");
                }
            }

            static int writeResultsGroupedCounter = 1;
            private static void writeResultsGrouped(XmlWriter XWriter, List<Grouped> list)
            {
                foreach (var item in list)
                {
                    XWriter.WriteStartElement(item.Name);
                    XWriter.WriteAttributeString("value", item.Value);

                    if (item.Row != null)
                        foreach (var row in item.Row)
                            getRowGrouped(row, XWriter);

                    if (item.Groups != null)
                        writeResultsGrouped(XWriter, item.Groups);

                    XWriter.WriteEndElement();
                }
            }

            public static void getRowGrouped(row Row, XmlWriter XWriter)
            {
                XWriter.WriteStartElement("row");
                for (int x = 0; x < Row.AnyAttr.Count; x++ )
                {
                    XWriter.WriteAttributeString(Row.AnyAttr[x].Name, Row.AnyAttr[x].Value.ToString());
                }

                if (Row.Any != null)
                {
                    foreach (var elm in Row.Any)
                    {
                        XWriter.WriteRaw(elm.OuterXml);
                    }
                }

                XWriter.WriteEndElement();
            }
        }

        public static class schemaQueryJsonBuilder
        {
            public static string getJsonString(projectResults ProjectResults)
            {

                StringWriter sw = new StringWriter();
                JsonTextWriter json = new JsonTextWriter(sw);

                json.WriteStartObject();
                json.WritePropertyName("results");
                json.WriteStartArray();


                //TODO context projResults needs to update for multiple query operation
                foreach (results rslts in ProjectResults.results)
                {
                    json.WriteStartObject();
                    // ProjectID and QueryID
                    json.WritePropertyName("ProjectID");

                    // get projectid, for subjhow
                    json.WriteValue((rslts.schema.Count > 0 ) ? rslts.schema[0].project : ProjectResults.results[0].schema[0].project);
                    json.WritePropertyName("QueryID");
                    json.WriteValue((rslts.schema.Count > 0 ) ? rslts.schema[0].name : String.Format("{0}.subquery",  ProjectResults.results[0].schema[0].name));

                    if (rslts.schema.Count > 0)
                    {
                        // parameters
                        json.WritePropertyName("parameters");
                        getParameterJsonStringArray(json, rslts.schema[0].parameters);

                        // paging
                        json.WritePropertyName("paging");

                        string pagingstring = JsonConvert.SerializeObject(rslts.schema[0].paging);
                        json.WriteRawValue(pagingstring);
                    }

                    // fields
                    json.WritePropertyName("fields");
                    getFieldJsonStringArray(json, (rslts.schema.Count > 0) ? rslts.schema[0].fields : ProjectResults.results[0].schema[0].fields);

                    // results
                    json.WritePropertyName("result");
                    getRowJsonStringArray(json, rslts.result, (rslts.schema.Count > 0) ? rslts.schema[0].fields : ProjectResults.results[0].schema[0].fields);

                    // TODO: add sub results
                    if(rslts.subresult != null)
                    {
                        json.WritePropertyName("subquery");
                        json.WriteStartObject();
                        json.WritePropertyName("result");
                        getRowJsonStringArray(json, rslts.subresult, (rslts.schema.Count > 0) ? rslts.schema[0].subquery.fields : ProjectResults.results[0].schema[0].subquery.fields);
                        json.WriteEndObject();
                    }

                    json.WriteEndObject();
                }
                json.WriteEndArray();
                json.WriteEndObject();

                json.Flush();
                sw.Flush();

                return sw.ToString();
            }

            private static void getParameterJsonStringArray(JsonTextWriter json, parameters parameters)
            {


                json.WriteStartObject();

                foreach (parameter schemaParm in parameters.parameter)
                {

                    json.WritePropertyName(schemaParm.name);
                    json.WriteValue(Helper.FlattenList(schemaParm.Text));

                }
                json.WriteEndObject();

            }

            private static void getRows(JsonTextWriter json, List<row> row, List<field> fields)
            {
                foreach (row schemaRow in row)
                {

                    json.WriteStartObject();

                    foreach (XmlAttribute att in schemaRow.AnyAttr)
                    {
                        json.WritePropertyName(att.Name);
                        var parm = fields.Find(p => p.name.Equals(att.Name));

                        // write value as is
                        if (parm == null)
                        {
                            json.WriteValue(att.Value.Trim());
                            continue;
                        }

                        // evaluate value for field type
                        switch (parm.type)
                        {
                            case fieldType.json:
                                json.WriteRawValue(att.Value);
                                break;
                            case fieldType.tryjson:
                                try
                                {
                                    Newtonsoft.Json.Linq.JObject.Parse(att.Value);
                                    json.WriteRawValue(att.Value);
                                }
                                catch
                                {
                                    json.WriteValue(att.Value.Trim());
                                }
                                break;
                            case fieldType.date:
                                var dt = new DateTime(1970, 1, 1);
                                if (DateTime.TryParse(att.Value, out dt) &&
                                    dt.ToString("d").Equals("1/1/1970"))
                                        json.WriteValue(att.Value);
                                    else
                                        json.WriteValue(dt.ToString("d"));
                                break;
                            case fieldType.datetime:
                                var dttime = new DateTime(1970, 1, 1);
                                if(DateTime.TryParse(att.Value, out dttime) &&
                                    dttime.ToString("d").Equals("1/1/1970"))
                                        json.WriteValue(att.Value);
                                    else
                                        json.WriteValue(dttime.ToString("MM-dd-yyyy HH':'mm':'ss tt"));
                                break;
                            default:
                                json.WriteValue(att.Value.Trim());
                                break;
                        }

                    }

                    json.WriteEndObject();
                }
            }

            public static void getRowJsonStringArray(JsonTextWriter json, subresult Result, List<field> fields)
            {
                json.WriteStartObject();
                json.WritePropertyName("row");

                json.WriteStartArray();
                getRows(json, Result.row, fields);

                json.WriteEndArray();
                json.WriteEndObject();
            }

            public static void getRowJsonStringArray(JsonTextWriter json, result Result, List<field> fields)
            {
                json.WriteStartObject();
                json.WritePropertyName("row");

                json.WriteStartArray();
                getRows(json, Result.row, fields);

                json.WriteEndArray();
                json.WriteEndObject();
            }

            public static void getFieldJsonStringArray(JsonTextWriter json, List<field> Fields)
            {
                json.WriteStartArray();

                foreach (field field in Fields)
                {

                    json.WriteStartObject();

                    json.WritePropertyName("field");
                    json.WriteValue(field.name.Trim());

                    json.WritePropertyName(TrimValue(field.name));
                    json.WriteValue(TrimValue(field.display));

                    json.WritePropertyName("type");
                    json.WriteValue(field.type.ToString());

                    json.WritePropertyName("display");
                    json.WriteValue(TrimValue(field.display));

                    json.WritePropertyName("ForView");
                    json.WriteValue(field.ForView);

                    json.WritePropertyName("ForUpdate");
                    json.WriteValue(field.ForUpdate);

                    json.WriteEndObject();
                }

                json.WriteEndArray();

            }

            public static string getJsonErrorString(String ProjectID, String QueryName, String Message)
            {
                using (StringWriter t = new StringWriter())
                {
                    Newtonsoft.Json.JsonTextWriter writer = new JsonTextWriter(t);
                    writer.WriteStartObject();
                    writer.WritePropertyName("results");
                    writer.WriteStartArray();
                    writer.WriteStartObject();
                    writer.WritePropertyName("ProjectID");
                    writer.WriteValue(ProjectID);
                    writer.WritePropertyName("QueryID");
                    writer.WriteValue(QueryName);
                    writer.WritePropertyName("error");
                    writer.WriteValue(Message);
                    writer.WriteEndObject();
                    writer.WriteEndArray();
                    writer.WriteEndObject();
                    writer.Flush();

                    return t.ToString();
                }
            }
            public static string TrimValue(Object Value)
            {
                if (Value == null)
                    return null;

                return Value.ToString().Trim();

            }

        }

        /// <summary>
        /// Convert parameter list to json object
        /// </summary>
        public static string parameterFieldMapJson(parameters parms, string ProjectID, string QueryID)
        {
            StringWriter sw = new StringWriter();
            JsonTextWriter json = new JsonTextWriter(sw);

            json.WriteStartObject();
            json.WritePropertyName("results");
            json.WriteStartArray();
            json.WriteStartObject();
            // ProjectID and QueryID
            json.WritePropertyName("ProjectID");
            json.WriteValue(ProjectID);
            json.WritePropertyName("QueryID");
            json.WriteValue(QueryID);

            json.WritePropertyName("parameters");
            json.WriteRawValue(JsonConvert.SerializeObject(parms));

            json.WriteEndObject();
            json.WriteEndArray();
            json.WriteEndObject();
    
            json.Flush();
            sw.Flush();

            return sw.ToString();


        }

        public static string StripXmlDecleration(String Value)
        {
            if (Value.StartsWith("<?"))
                return Value.Substring(Value.IndexOf("?>") + 2);

            return Value;
        }


    }

    internal class Grouped : IComparable<Grouped>
    {
        internal int Depth { get; set; }
        internal string Name { get; set; }
        internal string Value { get; set; }
        internal List<row> Row { get; set; }
        internal List<Grouped> Groups { get; set; }
        internal Grouped Parent { get; set; }

        public int CompareTo(Grouped other)
        {
            try
            {
                if (Depth != other.Depth)
                    return Depth.CompareTo(other.Depth);

                // int
                int tempInt = 0;
                if (int.TryParse(Value, out tempInt))
                    return tempInt.CompareTo(int.Parse(other.Value));

                // float
                float tempFloat = 0;
                if (float.TryParse(Value, out tempFloat))
                    return tempFloat.CompareTo(float.Parse(other.Value));

                // double
                double tempDouble = 0;
                if (double.TryParse(Value, out tempDouble))
                    return tempDouble.CompareTo(double.Parse(other.Value));

                if (Value != null)
                    return Value.ToString().CompareTo(other.Value.ToString());
            }
            catch { }

            return 0;
        }
    }
}

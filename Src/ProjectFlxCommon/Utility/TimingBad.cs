using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;

namespace ProjectFlx.Utility
{
    
    [XmlRoot("TimingObjType")]
    public class TimingType
    {
        [XmlElement]
        public string Name { get; set; }
        [XmlIgnore]
        public TimeSpan TimeSpanStart { get; set; }
        [XmlIgnore]
        public TimeSpan TimeSpanEnd { get; set; }
        /// <summary>
        /// Total Execution Time in Milliseconds
        /// </summary>
        [XmlElement]
        public Double ExecutionTime { get; set; }
    }

    public class Timing
    {
        List<TimingType> _list;

        public Timing()
        {
            List = new List<TimingType>();
        }

        public string Name { get; set; }

        public Timing Parent { get; set; }
        
        public void Start(String Name)
        {
            List.Add(new TimingType() { Name = Name, TimeSpanStart = new TimeSpan(DateTime.Now.Ticks) });
        }

        public void Stop(String Name)
        {
            var timing = List.Where(t => t.Name == Name).FirstOrDefault();
            if (timing == null)
                List.Add(new TimingType() { Name = Name, TimeSpanStart = new TimeSpan(DateTime.Now.Ticks) });
            else
            {
                timing.TimeSpanEnd = new TimeSpan(DateTime.Now.Ticks);
                timing.ExecutionTime = timing.TimeSpanEnd.Subtract(timing.TimeSpanStart).TotalMilliseconds;
            }
        }

        public double ExecutionTime(String Name)
        {
            double result = 0;
            var timing = List.Where(t => t.Name == Name).FirstOrDefault();
            if (timing != null)
            {
                result = timing.ExecutionTime;
            }

            return result;
        }

        public List<TimingDebugger> TimingDebugger { get; set; }

        [XmlElement("Timing")]
        public List<TimingType> List { get => _list; set => _list = value; }

        public Timing New(string Name)
        {
            if (this.TimingDebugger == null)
                this.TimingDebugger = new List<TimingDebugger>();

            var timingdebugger = new TimingDebugger();
            this.TimingDebugger.Add(timingdebugger);

            return timingdebugger.New(Name);
        }

        /*        public static void Serialize(Newtonsoft.Json.JsonWriter Writer, Timing Timing)
                {

                    Writer.WritePropertyName("Timing");
                    Writer.WriteStartArray();
                    foreach (TimingType timing in Timing)
                    {
                        if (timing.Name == Timing.Name)
                            continue;
                        Writer.WriteStartObject();
                        Writer.WritePropertyName("Name");
                        Writer.WriteValue(timing.Name);
                        Writer.WritePropertyName("ExecutionTime");
                        Writer.WriteValue(timing.ExecutionTime);
                        Writer.WriteEndObject();
                    }
                    Writer.WriteEndArray();

                    if(Timing.TimingDebugger != null)
                    {
                        foreach (var timedebugger in Timing.TimingDebugger)
                            ProjectFlx.Utility.TimingDebugger.Serialize(Writer, timedebugger);
                    }

                    // TODO: child
                    //if (TimingCollectionObj.Child != null)
                    //{
                    //    var timing2 = TimingCollectionObj.Child.Where(tt => tt.Name == TimingCollectionObj.Child.Name).First();
                    //    Writer.WritePropertyName("TimingGroup");
                    //    Writer.WriteStartObject();
                    //    Writer.WritePropertyName("Name");
                    //    Writer.WriteValue(TimingCollectionObj.Child.Name);
                    //    Writer.WritePropertyName("ExecutionTime");
                    //    Writer.WriteValue(timing2.ExecutionTime);
                    //    Timing.Serialize(Writer, TimingCollectionObj.Child);
                    //    Writer.WriteEndObject();
                    //}

                }*/

    }

    [XmlRoot("FLXTiming")]
    public class TimingDebugger : IEnumerable<Timing>
    {
        private List<Timing> _list;

        public TimingDebugger()
        {
            _list = new List<Timing>();
        }

        public Timing New(string Name)
        {
            _list.Add(new Timing()
            {
                Name = Name
            });

            var timing = _list.Last();
            timing.Start(Name);
            return timing;
        }
        
        /// <summary>
        /// Execution Time greater than (in Milliseconds)
        /// </summary>
        /// <param name="ExecutionTime"></param>
        /// <returns></returns>
        public bool TotalExecutionGreaterThan(double ExecutionTime)
        {
            bool isGreater = false;

            foreach(var timing in _list)
            {
                isGreater = isGreater || timing.List.Any(t => t.ExecutionTime > ExecutionTime);
            }

            return isGreater;
        }

        public void Add(Timing Timing)
        {
            _list.Add(Timing);
        }

        public IEnumerator<Timing> GetEnumerator()
        {
            return ((IEnumerable<Timing>)_list).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable)_list).GetEnumerator();
        }

        //public static void Serialize(Newtonsoft.Json.JsonWriter Writer, TimingDebugger TimingDebuggerObj)
        //{
        //    Writer.WritePropertyName("TimingGroup");
        //    Writer.WriteStartArray();
        //    foreach (var t in TimingDebuggerObj)
        //    {
        //        var timing = t.Where(tt => tt.Name == t.Name).First();
        //        Writer.WriteStartObject();
        //        Writer.WritePropertyName("Name");
        //        Writer.WriteValue(t.Name);
        //        Writer.WritePropertyName("ExecutionTime");
        //        Writer.WriteValue(timing.ExecutionTime);
        //        Timing.Serialize(Writer, t);
        //        Writer.WriteEndObject();
        //    }
        //    Writer.WriteEndArray();
        //}


        /*        public static string SerializeJSON(TimingDebugger TimingDebuggerObj)
                {
                    var writer = new StringWriter();
                    var jwriter = new Newtonsoft.Json.JsonTextWriter(writer);
                    jwriter.WriteStartObject();

                    Serialize(jwriter, TimingDebuggerObj);
                    jwriter.WriteEndObject();

                    jwriter.Flush();
                    writer.Flush();

                    return writer.ToString();
                }*/

        public static XmlDocument Serialize(TimingDebugger TimingDebuggerObj)
        {
            var mgr = new XmlSerializerNamespaces();
            mgr.Add(String.Empty, string.Empty);

            using (var m = new MemoryStream())
            {
                var serializer = new XmlSerializer(typeof(TimingDebugger));
                serializer.Serialize(m, TimingDebuggerObj, mgr);
                m.Flush();
                m.Position = 0;

                var xml = new XmlDocument();
                xml.Load(m);

                return xml;
            }
        }
    }
}

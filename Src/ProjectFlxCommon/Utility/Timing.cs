using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProjectFlx.Utility
{
    public class TimingType
    {
        public string Name { get; set; }
        public TimeSpan TimeSpanStart { get; set; }
        public TimeSpan TimeSpanEnd { get; set; }
        /// <summary>
        /// Total Execution Time in Milliseconds
        /// </summary>
        public Double ExecutionTime { get; set; }
    }

    public class Timing : IEnumerable<TimingType>
    {
        List<TimingType> _list;

        public Timing()
        {
            _list = new System.Collections.Generic.List<TimingType>();
        }

        public string Name { get; set; }

        public Timing Parent { get; set; }

        public Timing Child { get; set; }

        public IEnumerator<TimingType> GetEnumerator()
        {
            return _list.GetEnumerator();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return _list.GetEnumerator();
        }

        public void Start(String Name)
        {
            _list.Add(new TimingType() { Name = Name, TimeSpanStart = new TimeSpan(DateTime.Now.Ticks) });
        }
        public void Stop(String Name)
        {
            var timing = _list.Where(t => t.Name == Name).FirstOrDefault();
            if (timing == null)
                _list.Add(new TimingType() { Name = Name, TimeSpanStart = new TimeSpan(DateTime.Now.Ticks) });
            else
            {
                timing.TimeSpanEnd = new TimeSpan(DateTime.Now.Ticks);
                timing.ExecutionTime = timing.TimeSpanEnd.Subtract(timing.TimeSpanStart).TotalMilliseconds;
            }
        }

        public double ExecutionTime(String Name)
        {
            double result = 0;
            var timing = _list.Where(t => t.Name == Name).FirstOrDefault();
            if (timing != null)
            {
                result = timing.ExecutionTime;
            }

            return result;
        }

        public Timing End(String Name)
        {
            Stop(Name);
            return Parent;
        }
        public Timing New(String Name)
        {
            var child = this.Child = new Timing()
            {
                Name = Name,
                Parent = this
            };
            child.Start(Name);
            return child;
        }

        public static void Serialize(Newtonsoft.Json.JsonWriter Writer, Timing TimingCollectionObj)
        {

            Writer.WritePropertyName("Timing");
            Writer.WriteStartArray();
            foreach (TimingType timing in TimingCollectionObj)
            {
                if (timing.Name == TimingCollectionObj.Name)
                    continue;
                Writer.WriteStartObject();
                Writer.WritePropertyName("Name");
                Writer.WriteValue(timing.Name);
                Writer.WritePropertyName("ExecutionTime");
                Writer.WriteValue(timing.ExecutionTime);
                Writer.WriteEndObject();
            }
            Writer.WriteEndArray();
            if (TimingCollectionObj.Child != null)
            {
                var timing2 = TimingCollectionObj.Child.Where(tt => tt.Name == TimingCollectionObj.Child.Name).First();
                Writer.WritePropertyName("TimingGroup");
                Writer.WriteStartObject();
                Writer.WritePropertyName("Name");
                Writer.WriteValue(TimingCollectionObj.Child.Name);
                Writer.WritePropertyName("ExecutionTime");
                Writer.WriteValue(timing2.ExecutionTime);
                Timing.Serialize(Writer, TimingCollectionObj.Child);
                Writer.WriteEndObject();
            }
        }

    }

    public class TimingDebugger : IEnumerable<Timing>
    {
        List<Timing> _list;

        public TimingDebugger()
        {
            _list = new List<Timing>();
        }

        public System.Collections.Generic.IEnumerator<Timing> GetEnumerator()
        {
            return _list.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return _list.GetEnumerator();
        }

        public Timing New(string TimingCollectionName)
        {
            var timing = new Timing()
            {
                Name = TimingCollectionName
            };
            timing.Start(TimingCollectionName);
            _list.Add(timing);

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

            foreach(var timing in this)
            {
                isGreater = isGreater || timing.Where(t => t.ExecutionTime > ExecutionTime).Any();
            }

            return isGreater;
        }

        public static void Serialize(Newtonsoft.Json.JsonTextWriter Writer, TimingDebugger TimingDebuggerObj)
        {
            Writer.WritePropertyName("TimingGroup");
            Writer.WriteStartArray();
            foreach (var t in TimingDebuggerObj)
            {
                var timing = t.Where(tt => tt.Name == t.Name).First();
                Writer.WriteStartObject();
                Writer.WritePropertyName("Name");
                Writer.WriteValue(t.Name);
                Writer.WritePropertyName("ExecutionTime");
                Writer.WriteValue(timing.ExecutionTime);
                Timing.Serialize(Writer, t);
                Writer.WriteEndObject();
            }
            Writer.WriteEndArray();
        }

        public static string Serialize(TimingDebugger TimingDebuggerObj)
        {
            var writer = new StringWriter();
            var jwriter = new Newtonsoft.Json.JsonTextWriter(writer);
            jwriter.WriteStartObject();

            Serialize(jwriter, TimingDebuggerObj);
            jwriter.WriteEndObject();

            jwriter.Flush();
            writer.Flush();
            
            return writer.ToString();
        }
    }
}

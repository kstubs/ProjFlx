using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;
using System.Xml.Serialization;

namespace ProjectFlx.Utility
{

    public class TimingType
    {
        public string Name { get; set; }
        [XmlIgnore]
        public TimeSpan TimeSpanStart { get; set; }

        [XmlIgnore]
        public TimeSpan TimeSpanEnd { get; set; }
        public double ExecutionTime { get; set; }

        [XmlArray("MetaData")]
        [XmlArrayItem("Item")]
        public List<MetaData> MetaData { get; set; }
        public void AddMetaData(string Name, Object Value)
        {
            if (this.MetaData == null)
                this.MetaData = new List<MetaData>();

            this.MetaData.Add(new Utility.MetaData() { Name = Name, Value = Value?.ToString() ?? String.Empty });
        }


    }
    public class TimingCollection
    {
        List<TimingType> _list;
        List<TimingCollection> _collection;

        public TimingCollection()
        {
            _list = new List<TimingType>();
        }

        [XmlAttribute]
        public String Name { get; set; }

        [XmlAttribute]
        public double ExecutionTime { get; set; }

        public TimingType Start(string Name)
        {
            _list.Add(new TimingType()
            {
                Name = Name,
                TimeSpanStart = new TimeSpan(DateTime.Now.Ticks)
            });

            if (String.IsNullOrEmpty(this.Name))
                this.Name = Name;

            return _list.Last();
        }

        public void Stop()
        {
            Stop(this.Name);
        }
        public void Stop(string Name)
        {
            var timing = _list.FirstOrDefault(f => f.Name == Name);
            if (timing != null)
            {
                timing.TimeSpanEnd = new TimeSpan(DateTime.Now.Ticks);
                timing.ExecutionTime = timing.TimeSpanEnd.Subtract(timing.TimeSpanStart).TotalMilliseconds;
            }

            this.ExecutionTime = _list.Sum(f => f.ExecutionTime);
        }

        public TimingCollection New(string Name)
        {
            if (_collection == null)
                _collection = new List<TimingCollection>();

            var timing = new TimingCollection();
            _collection.Add(timing);
            timing.Start(Name);
            return timing;
        }

        [XmlElement("Timing")]
        public List<TimingType> Timing
        {
            get
            {
                return _list;
            }
            internal set
            {
                _list = value;
            }
        }

        [XmlElement("TimingGroup")]
        public List<TimingCollection> TimingCollectionGroup
        {
            get
            {
                return _collection;
            }
            internal set
            {
                _collection = value;
            }
        }

    }

    public class TimingDebugger
    {
        [XmlElement("TimingGroup")]
        public List<TimingCollection> List
        {
            get; set;
        }

        public TimingDebugger()
        {
            List = new List<TimingCollection>();
        }
        public TimingCollection New(string Name)
        {
            var item = new TimingCollection();
            item.Start(Name);
            List.Add(item);
            return List.Last();
        }

        [XmlArray("MetaData")]
        [XmlArrayItem("Item")]
        public List<MetaData> MetaData { get; set; }
        public void AddMetaData(string Name, Object Value)
        {
            if(this.MetaData == null)
                this.MetaData = new List<MetaData>();

            this.MetaData.Add(new Utility.MetaData() { Name = Name, Value = Value?.ToString() ?? String.Empty });
        }

        public double ExecutionTime
        {
            get
            {
                return this.List.Sum(x => x.ExecutionTime);
            }
        }
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

    public class MetaData
    {
        public string Name { get; set; }
        public string Value { get; set; }
    }
}

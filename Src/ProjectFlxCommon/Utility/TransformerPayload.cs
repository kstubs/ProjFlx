using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProjectFlx.Utility
{
    //[ProtoContract]
    //class TransformerPayload
    //{
    //    [ProtoMember(1)]
    //    public string Name { get; set; }
    //}

    [Serializable]
    public class PayloadNameValue
    {
        public string Name { get; set; }
        public object Value { get; set; }
    }

    [Serializable]
    public class TransformPayload
    {
        List<PayloadNameValue> _items;

        public TransformPayload()
        {
            _items = new List<PayloadNameValue>();
        }
        public List<PayloadNameValue> Items { get => _items; set => _items = value; }

        public void Add(string Name, object Value)
        {
            _items.Add(new PayloadNameValue()
            {
                Name = Name,
                Value = Value
            });
        }

        public object this[string Name]
        {
            get
            {
                var item = _items.FirstOrDefault(f => f.Name.Equals(Name));
                return item.Value;
            }
        }

        public T GetItem<T>(String Name)
        {
            var item = this[Name];
            if (item == null)
                return default(T);

            return (T)item;
        }

        public string GetItem(String Name)
        {
            var item = this[Name];
            if (item == null)
                return default(string);

            return Convert.ToString(item);
        }
    }
}

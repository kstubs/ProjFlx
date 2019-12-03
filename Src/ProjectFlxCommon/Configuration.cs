using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public class App
{
    private static Config _config = null;

    public static Config Config
    {
        get
        {
            if (_config == null)
                _config = new Config();

            return _config;
        }
    }
}

public class AppDictionary
{
    public string Name { get; set; }
    public object Value { get; set; }
}

public class AppDictionaryCollection : IEnumerable<AppDictionary>
{
    List<AppDictionary> _list;

    public AppDictionaryCollection()
    {
        _list = new List<AppDictionary>();
    }

    public IEnumerator<AppDictionary> GetEnumerator()
    {
        return _list.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        throw new NotImplementedException();
    }

    public object this[string Name]
    {
        get
        {
            var item = _list.FirstOrDefault(l => l.Name.Equals(Name));
            return item?.Value;
        }
    }

    public bool Contains(String Name)
    {
        lock (this._list)
        {
            return _list.ToList().Any(a => a.Name.Equals(Name));
        }
    }

    public int Count
    {
        get
        {
            return _list.Count;
        }
    }

    public void Add(String Name, Object Value)
    {
        lock (this._list)
        {
            this._list.Add(new AppDictionary()
            {
                Name = Name,
                Value = Value
            });
        }
    }

    internal void Remove(string Name)
    {
        lock (this._list)
        {
            var item = _list.FirstOrDefault(l => l.Name.Equals(Name));

            if (item != null)
                _list.Remove(item);
        }
    }

    public void Add(Object Value)
    {
        throw new NotImplementedException();
    }
}

public class Config
{
    AppDictionaryCollection _appsettings = null;
    public Config()
    {
        _appsettings = new AppDictionaryCollection();
    }

    public AppDictionaryCollection AppSettings
    {
        get
        {
            return _appsettings;
        }
    }

    private static Object _lock = new Object();
    public bool Initialized
    {
        get
        {
            var result = _appsettings != null
                            && _appsettings.Count > 1
                                && _appsettings.Contains("app_settings_cached");

            if (result)
            {
                int timeout = 0;
                if (!int.TryParse(this["app_settings_cache_timeout_seconds"], out timeout))
                    timeout = 30;   // 0 = no timeout, but buffer to 5 seconds to allow multiple internal fetches to grab cache one time

                var totSeconds = DateTime.UtcNow.Subtract(Convert.ToDateTime(_appsettings["app_settings_cached"])).Duration().TotalSeconds;
                if (totSeconds > timeout)
                {
                    System.Diagnostics.Debug.WriteLine("ProjectFlxCommon app_settings_cached Initialized FALSE after cache expired [{0}]", totSeconds);
                    result = false;
                }
                else
                    System.Diagnostics.Debug.WriteLine("ProjectFlxCommon app_settings_cached Initialized TRUE");
            }

            return result;
        }
    }

    public string this[string Name]
    {
        get
        {
            if (!Contains(Name))
                return string.Empty;

            return _appsettings[Name].ToString();
        }
    }

    public bool Contains(String Key)
    {
        return _appsettings.Contains(Key);
    }

    public void Add(string Name, object Value)
    {
        lock (_lock)
        {
            if (_appsettings.Contains(Name))
                _appsettings.Remove(Name);

            _appsettings.Add(Name, Value);
        }
    }

    public void Setup(AppDictionaryCollection Collection)
    {
        lock (_lock)
        {
            _appsettings = Collection;
            Setup();
        }
    
    }

    public void Setup()
    {
        if (Initialized) return;

        lock (_lock)
        {

            var appsettings = System.Configuration.ConfigurationManager.AppSettings;
            foreach (string key in System.Configuration.ConfigurationManager.AppSettings)
            {
                if (!Contains(key))
                    _appsettings.Add(key, System.Configuration.ConfigurationManager.AppSettings[key]);
            }
            if (_appsettings.Contains("app_settings_cached"))
                _appsettings.Remove("app_settings_cached");

            _appsettings.Add("app_settings_cached", DateTime.UtcNow);
        }
    }

    public int GetValue(string Property, int DefaultValue)
    {
        var val = this[Property];
        if (!String.IsNullOrEmpty(val))
            int.TryParse(val, out DefaultValue);

        return DefaultValue;
    }

    public bool GetValue(string Property, bool DefaultValue)
    {
        var val = this[Property];
        if (!String.IsNullOrEmpty(val))
            bool.TryParse(val, out DefaultValue);

        return DefaultValue;
    }
}


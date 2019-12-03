using System;
using System.IO;
using System.Xml;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;
using Newtonsoft.Json;

namespace ProjectFlx.Utility.Closure
{
    public class Joiner
    {
        XmlDocument _compilerXml = null;
        public struct JoinerFile : IComparable<JoinerFile>
        {
            string _File;
            int _order;

            public string File
            {
                get { return _File; }
            }

            public JoinerFile(String FileName, int Order)
            {
                _File = FileName;
                _order = Order;
            }

            #region IComparable<JoinerFile> Members

            public int CompareTo(JoinerFile other)
            {
                return this._order.CompareTo(other._order);
            }

            #endregion
        }
        ClosureCompiler _gc = null;
        private string _scriptName;
        private List<OutputInfo> _oinfos = new List<OutputInfo>();
        private WarningLevel _warningLevel = WarningLevel.DEFAULT;

        public WarningLevel WarningLevel
        {
            get { return _warningLevel; }
            set { _warningLevel = value; }
        }

        private JoinerFiles _files = new JoinerFiles();
        private CompilationLevel _compilation = new CompilationLevel();
        private bool _prettyPrint;

        public bool PrettyPrint
        {
            get { return _prettyPrint; }
            set { _prettyPrint = value; }
        }

        public CompilationLevel Compilation
        {
            get { return _compilation; }
            set { _compilation = value; }
        }

        public class JoinerFiles : IEnumerable<JoinerFile>
        {
            List<JoinerFile> _jFiles = new List<JoinerFile>();

            public void Sort()
            {
                _jFiles.Sort();
            }

            public void Add(String FileName)
            {
                _jFiles.Add(new JoinerFile(FileName, _jFiles.Count));
            }

            #region IEnumerable<JoinerFile> Members

            public IEnumerator<JoinerFile> GetEnumerator()
            {
                return _jFiles.GetEnumerator();
            }

            System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
            {
                return _jFiles.GetEnumerator();
            }

            #endregion

            public static List<ClosureCompilerFile> ListOfFiles(JoinerFiles JoinerFiles)
            {
                var files = new List<ClosureCompilerFile>();

                JoinerFiles.Sort();
                foreach (JoinerFile j in JoinerFiles)
                    files.Add(new ClosureCompilerFile(j.File, null));       // if using this will need to pass in root path

                return files;
            }

        }

        public Joiner(JoinerFiles Files, String ScriptName)
        {

            _files = Files;
            _scriptName = ScriptName;
            // TODO: these become web config items

            _oinfos = new List<OutputInfo>();
            _oinfos.Add(OutputInfo.compiled_code);
            _oinfos.Add(OutputInfo.errors);
            _oinfos.Add(OutputInfo.statistics);
            _oinfos.Add(OutputInfo.warnings);

        }

        public void Run()
        {
            _gc = new ClosureCompiler();

            _gc.Run(
                JoinerFiles.ListOfFiles(_files),
                _scriptName,
                _warningLevel,
                _compilation,
                _oinfos,
                null,
                _prettyPrint);
        }


        /// <summary>
        /// Save Compiled Script to Disk
        /// </summary>
        /// <param name="FileName"></param>
        public void SaveToFile(String FileName)
        {
            using (StreamWriter writer = new StreamWriter(FileName))
            {
                writer.Write(_gc.Script);
                writer.Flush();
            }
        }

        #region factory methods
        /// <summary>
        /// Looks for Application Joiner configuration file
        /// in virtual path, loads each required javascript
        /// into Joiner File, compiles with GC and saves
        /// to application compiled subfolder returning relative
        /// path to the new resource
        /// </summary>
        /// <param name="Application">Virtual Path or Actual path to Joiner aplication configuration</param>
        /// <remarks>Expects configuration file of naming: _&lt;virtual_folder_name&gt;.json</remarks>
        /// <returns></returns>
        public static List<gcCompiledFile> invokeWebApplicationJoiner(string Application)
        {
            List<gcCompiledFile> result = new List<gcCompiledFile>();

            var Server = HttpContext.Current.Server;
            var path = Server.MapPath(Application);
            Uri uri = new Uri(path);
            var localpath = uri.LocalPath.Substring(0, uri.LocalPath.LastIndexOf("\\"));

            string config = path;

            using (var reader = new StreamReader(config))
            {
                var joiner = (Newtonsoft.Json.Linq.JObject)Newtonsoft.Json.Linq.JToken.ReadFrom(new JsonTextReader(reader));
                //if (!(bool)obj["compile"])
                //    return null;

                var jobjects = (Newtonsoft.Json.Linq.JArray)joiner["joiner"];

                foreach (var obj in jobjects)
                {
                    var relpath = Application.Substring(0, Application.LastIndexOf('.'));
                    var version = ((int)((float)obj["version"] * 1000)).ToString("D8");
                    var name = (string)obj["application"];
                    var jsfile = Path.Combine(relpath, String.Format("__{0}_{1}.js", name, version));

                    // create joine files collection
                    var jfiles = new JoinerFiles();

                    var jsoutfile = Server.MapPath(jsfile);
                    var outuri = new Uri(jsoutfile);
                    var outpath = outuri.LocalPath.Substring(0, outuri.LocalPath.LastIndexOf("\\"));

                    var dinfo = new DirectoryInfo(outpath);
                    if (!dinfo.Exists)
                        dinfo.Create();

                    var finfo = new FileInfo(jsoutfile);

                    // chached?
                    if (finfo.Exists)
                    {
                        XmlDocument xm = new XmlDocument();
                        xm.LoadXml(string.Format("<compilationResult><compiledCode>{0}</compiledCode></compilationResult>", jsfile));
                        result.Add(new gcCompiledFile(name, jsfile, version, xm));
                    }
                    else
                        using (StreamWriter writer = new StreamWriter(jsoutfile))
                        {
                            string scriptBase = Server.MapPath((string)obj["scriptBase"]);
                            var scripts = obj["required"].Children();
                            foreach (string s in scripts)
                                jfiles.Add(Path.Combine(scriptBase, s.Replace("/", "\\")));

                            // run GC and compile  javascript code
                            var j = new Joiner(jfiles, "");
                            j.Run();

                            var scriptnode = j.Xml.SelectSingleNode("/compilationResult/compiledCode");
                            if (scriptnode != null)
                            {
                                writer.Write(scriptnode.InnerText);
                                scriptnode.InnerText = jsfile;
                            }
                            result.Add(new gcCompiledFile(name, jsfile, version, j.Xml));
                            writer.Flush();
                        }
                }

                return result;

            }
        }
        #endregion

        public XmlDocument Xml
        {
            get
            {
                return _gc.Xml;
            }
        }

    }

    public struct gcCompiledFile
    {
        string _name;
        string _path;
        string _version;
        XmlDocument _xml;

        public XmlDocument Xml
        {
            get { return _xml; }
            set { _xml = value; }
        }

        public gcCompiledFile(string Name, string Path, string Version, XmlDocument Xml)
        {
            _name = Name;
            _path = Path;
            _version = Version;
            _xml = Xml;
        }

        public string Name
        {
            get { return _name; }
            set { _name = value; }
        }

        public string Path
        {
            get { return _path; }
            set { _path = value; }
        }

        public string Version
        {
            get { return _version; }
            set { _version = value; }
        }
    }
}
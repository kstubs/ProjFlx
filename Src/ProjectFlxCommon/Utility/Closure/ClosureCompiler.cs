using System;
using System.IO;
using System.Net;
using System.Xml;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ProjectFlx.Utility.Closure
{
    public enum RequestType { js_code, code_url };
    public enum OutputFormat { xml, json, text};
    public enum CompilationLevel {WHITESPACE_ONLY, SIMPLE_OPTIMIZATIONS, ADVANCED_OPTIMIZATIONS };
    public enum OutputInfo { compiled_code, warnings, errors, statistics };
    public enum WarningLevel { QUIET, DEFAULT, VERBOSE };

    public class ClosureCompilerFile
    {
        public ClosureCompilerFile(string UriPath, string RootPath)
        {
            if (Uri.IsWellFormedUriString(UriPath, UriKind.Relative))
            {
                var path = UriPath.Replace("/", "\\");
                if (path.StartsWith("\\"))
                    this.JSFile = new Uri(Path.Combine(RootPath, UriPath.Substring(1, UriPath.Length - 1)));
                else
                    this.JSFile = new Uri(Path.Combine(RootPath, UriPath));
            }
            else
                this.JSFile = new Uri(UriPath);
        }
        public Uri JSFile { get; set; }
    }
    public sealed class ClosureCompiler : IDisposable
    {
        public static ClosureCompiler Instance = null;
        private static readonly object _lock = new object();

        public ClosureCompiler() { }

        public static ClosureCompiler NewSealedInstance
        {
            get
            {
                lock (_lock)
                {
                    if (Instance == null)
                    {
                        Instance = new ClosureCompiler();
                        return Instance;
                    }
                }
                return Instance;
            }
        }

        XmlDocument _xml = new XmlDocument();

        public string _outFileName = null;
        public OutputFormat _outFormat = OutputFormat.text;

        /// <summary>
        /// Runs Google's Closure Compiler returning compiled JavaScript code
        /// </summary>
        /// <param name="files">List of files to compile</param>
        /// <param name="temp_file" remarks="CCApi:output_file_name">Closure Compiler cached script copy (avail 1hr)</param>
        /// <param name="WarningLevel" remarks="CCApi:warning_level">Warning Level, see Enumeration</param>
        /// <param name="CompilationLevel" remarks="CCApi:compilation_level">Degree of Compression, see Enumeration</param>
        /// <param name="OutputInfos" remarks="CCApi:output_info">Level of output, see Enumeration</param>
        /// <param name="ExternsJS" remarks="CCApi:js_externs">Declares function name or other symbols</param>
        /// <param name="Pretty" remarks="CCApi:pretty_print">Collapse whitespace or print with tabs and line returns</param>
        public void Run(List<ClosureCompilerFile> files, string temp_file, WarningLevel WarningLevel, CompilationLevel CompilationLevel, List<OutputInfo> OutputInfos, string ExternsJS, bool Pretty)
        {
            _outFormat = OutputFormat.xml;

            StringBuilder parms = new StringBuilder();
            parms.Append(String.Format(@"compilation_level={0}", CompilationLevel.ToString()));            
            parms.Append(String.Format(@"&output_format={0}", _outFormat.ToString().ToLower()));
            parms.Append(@"&output_file_name=" + temp_file);
            parms.Append(String.Format(@"&warning_level={0}", WarningLevel.ToString().ToLower()));
            if (Pretty)
                parms.Append(@"&formatting=pretty_print");
            parms.Append(@"&formatting=print_input_delimiter");

            foreach(OutputInfo oi in OutputInfos )
                parms.Append(String.Format(@"&output_info={0}", oi.ToString()));

            if (!String.IsNullOrEmpty(ExternsJS))
            {
                parms.Append(@"&js_externs=");
                using (StreamReader reader = new StreamReader(ExternsJS))
                {
                    parms.Append(System.Web.HttpUtility.UrlEncode(reader.ReadToEnd()));
                }

            }

            // embed local script
            foreach (var file in files.Where(f => f.JSFile.Host.Equals(String.Empty)))
            {
                parms.Append(@"&js_code=");
                using (StreamReader reader = new StreamReader(file.JSFile.AbsolutePath))
                {
                    StringBuilder bldr = new StringBuilder();
                    string line = null;
                    while (!reader.EndOfStream)
                    {
                        line = reader.ReadLine();
                        if (line.EndsWith("\\"))
                            bldr.Append(line.Substring(0, line.Length - 1));
                        else
                            bldr.AppendLine(line);
                    }
                    parms.Append(System.Web.HttpUtility.UrlEncode(bldr.ToString()));
                }
            }

            foreach (var file in files.Where(f => !f.JSFile.Host.Equals(String.Empty)))
            {
                parms.Append(@"&code_url=");
                parms.Append(file.JSFile.AbsoluteUri);
            }

            MemoryStream m = new MemoryStream();

            using (StreamWriter f = new StreamWriter(m))
            {
                try
                {
                    RunPost(parms.ToString(), f);
                    m.Seek(0, SeekOrigin.Begin);
                    _xml.Load(m);

                    XmlNodeList testNodes = _xml.SelectNodes("compilationResult/errors/error");
                    //if (testNodes.Count > 0)
                    //    _errors = true;
                }
                catch(Exception unhandled)
                {
                    var args = new ProjectFlx.Exceptions.ProjectExceptionArgs("Google Closure Exception Caught");
                    throw new ProjectFlx.Exceptions.ProjectException(args, unhandled); ;
                }

            }
        }

        void RunPost(String Parms, StreamWriter Out)
        {
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create("https://closure-compiler.appspot.com/compile");
            request.Method = "POST";
            request.ContentType = "application/x-www-form-urlencoded";

            request.ContentLength = getBytesLength(Parms);

            StreamWriter postStream = new StreamWriter(request.GetRequestStream(), System.Text.Encoding.ASCII);
            postStream.Write(Parms);
            postStream.Close();

            HttpWebResponse response = (HttpWebResponse)request.GetResponse();
            StreamReader stIn = new StreamReader(response.GetResponseStream());
            Out.Write(stIn.ReadToEnd());
            Out.Flush();
            stIn.Close();

            response.Close();

        }

        long getBytesLength(string Parms)
        {
            ASCIIEncoding encoding = new ASCIIEncoding();
            byte[] data = encoding.GetBytes(Parms.ToString());

            return data.Length;

        }

        public XmlDocument Xml
        {
            get
            {
                return _xml;
            }
        }

        public string Script
        {
            get
            {
                string script = null;
                try
                { 
                    XmlNode node = this.Xml.SelectSingleNode("/compilationResult/compiledCode");
                    script = node.InnerText;                
                }
                catch(Exception unhandled)
                {
                    script = unhandled.Message;
                }

                return script;
            }
        }

        public bool Success
        {
            get
            {
                if (this.Xml == null)
                    return false;

                XmlNode node = this.Xml.SelectSingleNode("/compilationResult/compiledCode");
                return node != null && !String.IsNullOrEmpty(node.InnerText);
            }
        }

        public bool TooManyCompiles
        {
            get
            {
                if (this.Xml == null)
                    return false;

                XmlNode node = this.Xml.SelectSingleNode("/compilationResult/serverErrors/error[@code='22']");
                return node != null;
            }
        }

        public string getCompiledCode()
        {
            var scriptnode = this.Xml.SelectSingleNode("/compilationResult/compiledCode");
            return scriptnode?.InnerText;
        }

        public void Dispose()
        {
            Instance = null;
        }
    }
}

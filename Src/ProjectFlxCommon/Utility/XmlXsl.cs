using net.sf.saxon.trace;   // TimingTraceListener
using net.sf.saxon.lib;     // StandardLogger
using java.io;              // PrintStream
using Saxon.Api;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Xml;
using System.Xml.Xsl;

namespace ProjectFlx.Utility
{
    public class XmlXsl
    {
        private static Processor Processor;
        private static readonly Dictionary<string, XsltExecutable> XsltCache = new Dictionary<string, XsltExecutable>();
        private static readonly object ProcessorLock = new object();
        private static readonly object CacheLock = new object();
        private static bool IsTracingEnabled =>
            App.Config.GetValue("saxon_trace_enabled", false) ||
            System.Diagnostics.Debugger.IsAttached; // Auto-enable in debugger


        private static DateTime _lastResetTime = DateTime.MinValue;
        private static readonly TimeSpan ResetCooldown = TimeSpan.FromMinutes(5); // 5-minute cooldown

        public static string TransformXmlSaxon(XmlDocument xmlDocument, String XslUri, Dictionary<string, object> Args = null)
        {
            var xsluri = XslUri?.Split('?').FirstOrDefault() ?? String.Empty;

            using (MemoryStream m = new MemoryStream())
            {
                TransformXmlSaxon(xmlDocument, xsluri, m, Args);
                m.Flush();
                m.Position = 0;

                using (var reader = new StreamReader(m))
                    return reader.ReadToEnd();
            }
        }

        public static void TransformXmlSaxon(XmlDocument xmlDocument, string XslUri, MemoryStream r, Dictionary<string, object> Args = null)
        {
            var xsluri = XslUri?.Split('?').FirstOrDefault() ?? String.Empty;
            bool tracingEnabled = System.Diagnostics.Debugger.IsAttached ||
                      App.Config.GetValue("saxon_trace_enabled", false);
            var errorList = new System.Collections.Generic.List<Saxon.Api.XmlProcessingError>();


            // 2 tries - make sure we have the correct Processor
            for (int attempt = 1; attempt <= 2; attempt++)
            {
                GetProcessor(App.Config.GetValue("reset_saxon_cache", false));

                try
                {
                    // Get or compile XsltExecutable
                    XsltExecutable executable;
                    lock (CacheLock)
                    {
                        if (!XsltCache.TryGetValue(xsluri, out executable))
                        {
                            if (XsltCache.Count >= 100) // Limit cache size (~50 MB at 500 KB/XSLT)
                                XsltCache.Clear();
                            var compiler = GetCompiler();
                            compiler.SetErrorList(errorList);

                            if (tracingEnabled)
                                compiler.Implementation.setCompileWithTracing(true);

                            compiler.BaseUri = new Uri(XslUri);
                            executable = compiler.Compile(new Uri(xsluri));
                            XsltCache[xsluri] = executable;
                        }
                    }

                    var transformer = executable.Load();

                    // Convert XmlDocument to Saxon's XdmNode
                    using (var xmlStream = new MemoryStream())
                    {
                        xmlDocument.Save(xmlStream);
                        xmlStream.Seek(0, SeekOrigin.Begin);
                        var docBuilder = Processor.NewDocumentBuilder();
                        docBuilder.BaseUri = new Uri(xsluri);
                        var saxonInput = docBuilder.Build(xmlStream);

                        // Set up transformation
                        transformer.InitialContextNode = saxonInput;

                        // Handle XsltArgumentList parameters
                        if (Args != null)
                        {
                            var args = DicToSaxonParms(Args);
                            foreach (var arg in args)
                                transformer.SetParameter(arg.Key, arg.Value);
                        }

                        var serializer = Processor.NewSerializer(r);

                        if (tracingEnabled)
                        {
                            // Disable optimization to populate TimingTraceListener report
                            Processor.Implementation.setConfigurationProperty(
                                net.sf.saxon.lib.FeatureKeys.OPTIMIZATION_LEVEL, "0");  // string "0"

                            var controller = transformer.Implementation;

                            //var traceListener = new net.sf.saxon.trace.XSLTTraceListener();
                            var traceListener = new net.sf.saxon.trace.TimingTraceListener();

                            var logPath = GetTraceLogPath();

                            var printStream = new java.io.PrintStream(logPath);
                            var logger = new net.sf.saxon.lib.StandardLogger(printStream);

                            traceListener.setOutputDestination(logger);

                            controller.addTraceListener(traceListener);

                            System.Diagnostics.Debug.WriteLine($"Saxon 10.9 profiling trace enabled → {logPath}");
                            transformer.SetParameter(new QName("saxon-trace"), new XdmAtomicValue(logPath));
                        }

                        transformer.Run(serializer);
                    }

                    r.Flush();
                    r.Seek(0, SeekOrigin.Begin);
                    return;
                }
                catch (Saxon.Api.StaticError)
                {
                    foreach (var err in errorList)
                    {
                        var msg = $"XSLT Compile Error: {err.Message}\n" +
                                  $"Line: {err.LineNumber}, Column: {err.ColumnNumber}\n" +
                                  $"File: {err.ModuleUri}";

                        System.Diagnostics.Debug.WriteLine(msg);
                    }

                    throw;
                }
                catch (Saxon.Api.DynamicError ex)
                {
                    var msg = new System.Text.StringBuilder();

                    msg.AppendLine("=== Saxon DynamicError ===");
                    msg.AppendLine($"Message      : {ex.Message}");
                    msg.AppendLine($"Error Code   : {ex.ErrorCode}");
                    msg.AppendLine($"Line Number  : {ex.LineNumber}");
                    msg.AppendLine($"Module/File  : {ex.ModuleUri}");
                    msg.AppendLine($"Is Type Error: {ex.IsTypeError}");

                    msg.AppendLine("Stack Trace:");
                    msg.AppendLine(ex.StackTrace ?? "None");

                    if (ex.InnerException != null)
                    {
                        msg.AppendLine("Inner Exception:");
                        msg.AppendLine($"  Message: {ex.InnerException.Message}");
                        msg.AppendLine($"  Type   : {ex.InnerException.GetType().Name}");
                    }

                    msg.AppendLine("==========================");

                    System.Diagnostics.Debug.WriteLine(msg.ToString());
                }
                catch (Exception handled) when (IsConfigurationMismatch(handled))
                {
                    if (attempt == 2)
                        throw;

                    Thread.Sleep(100);
                }
            }
        }


        /// <summary>
        /// Transform Xml and Xsl using Saxon
        /// XsltArgumentList kept for backwards compatibility
        /// </summary>
        /// <param name="xmlDocument"></param>
        /// <param name="XslUri"></param>
        /// <param name="Args"></param>
        /// <returns></returns>
        public static string TransformXmlSaxon(XmlDocument xmlDocument, String XslUri, XsltArgumentList Args)
        {
            var xsluri = XslUri?.Split('?').FirstOrDefault() ?? String.Empty;

            using (MemoryStream m = new MemoryStream())
            {
                TransformXmlSaxon(xmlDocument, xsluri, m, Args);
                m.Flush();
                m.Position = 0;

                using (var reader = new StreamReader(m))
                    return reader.ReadToEnd();
            }
        }

        /// <summary>
        /// Transform Xml and Xsl using Saxon
        /// XsltArgumentList kept for backwards compatibility
        /// </summary>
        /// <param name="xmlDocument"></param>
        /// <param name="XslUri"></param>
        /// <param name="r"></param>
        /// <param name="Args"></param>
        /// <returns></returns>
        public static void TransformXmlSaxon(XmlDocument xmlDocument, string XslUri, MemoryStream r, XsltArgumentList Args)
        {
            var xsluri = XslUri?.Split('?').FirstOrDefault() ?? String.Empty;
            bool tracingEnabled = System.Diagnostics.Debugger.IsAttached ||
                      App.Config.GetValue("saxon_trace_enabled", false);
            var errorList = new System.Collections.Generic.List<Saxon.Api.XmlProcessingError>();

            // 2 tries - make sure we have the correct Processor
            for (int attempt = 1; attempt <= 2; attempt++)
            {
                GetProcessor(App.Config.GetValue("reset_saxon_cache", false));

                try
                {
                    // Get or compile XsltExecutable
                    XsltExecutable executable;
                    lock (CacheLock)
                    {
                        if (!XsltCache.TryGetValue(xsluri, out executable))
                        {
                            if (XsltCache.Count >= 100) // Limit cache size (~50 MB at 500 KB/XSLT)
                                XsltCache.Clear();
                            var compiler = GetCompiler();
                            compiler.SetErrorList(errorList);

                            if (tracingEnabled)
                                compiler.Implementation.setCompileWithTracing(true);

                            compiler.BaseUri = new Uri(XslUri);
                            executable = compiler.Compile(new Uri(xsluri));
                            XsltCache[xsluri] = executable;
                        }
                    }

                    var transformer = executable.Load();

                    // Convert XmlDocument to Saxon's XdmNode
                    using (var xmlStream = new MemoryStream())
                    {
                        xmlDocument.Save(xmlStream);
                        xmlStream.Seek(0, SeekOrigin.Begin);
                        var docBuilder = Processor.NewDocumentBuilder();
                        //docBuilder.BaseUri = new Uri(Path.GetDirectoryName(XslUri)); // Set base URI for resolving includes
                        docBuilder.BaseUri = new Uri(xsluri);
                        var Input = docBuilder.Build(xmlStream);

                        // Set up transformation
                        transformer.InitialContextNode = Input;

                        // Handle XsltArgumentList parameters
                        if (Args != null)
                        {
                            var argsField = typeof(XsltArgumentList).GetField("_arguments", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                            if (argsField != null)
                            {
                                var argsDict = (Hashtable)argsField.GetValue(Args);
                                foreach (DictionaryEntry arg in argsDict)
                                {
                                    string val = arg.Value?.ToString() ?? "";
                                    transformer.SetParameter(new Saxon.Api.QName((string)arg.Key), new Saxon.Api.XdmAtomicValue(val));
                                }
                            }
                        }

                        var serializer = Processor.NewSerializer(r);

                        if (tracingEnabled)
                        {
                            // Disable optimization to populate TimingTraceListener report
                            Processor.Implementation.setConfigurationProperty(
                                net.sf.saxon.lib.FeatureKeys.OPTIMIZATION_LEVEL, "0");  // string "0"

                            var controller = transformer.Implementation;

                            //var traceListener = new net.sf.saxon.trace.XSLTTraceListener();
                            var traceListener = new net.sf.saxon.trace.TimingTraceListener();

                            var logPath = GetTraceLogPath();

                            var printStream = new java.io.PrintStream(logPath);
                            var logger = new net.sf.saxon.lib.StandardLogger(printStream);

                            traceListener.setOutputDestination(logger);

                            controller.addTraceListener(traceListener);

                            System.Diagnostics.Debug.WriteLine($"Saxon 10.9 profiling trace enabled → {logPath}");
                            transformer.SetParameter(new QName("saxon-trace"), new XdmAtomicValue(logPath));
                        }

                        transformer.Run(serializer);
                    }

                    r.Flush();
                    r.Seek(0, SeekOrigin.Begin);
                    return;
                }
                catch(Saxon.Api.StaticError)
                {
                    foreach (var err in errorList)
                    {
                        var msg = $"XSLT Compile Error: {err.Message}\n" +
                                  $"Line: {err.LineNumber}, Column: {err.ColumnNumber}\n" +
                                  $"File: {err.ModuleUri}";

                        System.Diagnostics.Debug.WriteLine(msg);
                    }

                    throw;
                }
                catch (Exception handled) when (IsConfigurationMismatch(handled))
                {
                    if (attempt == 2)
                        throw;

                    Thread.Sleep(100);
                }
            }
        }

        public static void ClearXsltCache(string xslUri = null)
        {
            lock (CacheLock)
            {
                if (xslUri == null)
                {
                    XsltCache.Clear();
                }
                else
                {
                    XsltCache.Remove(xslUri);
                }
            }
        }

        private static Dictionary<Saxon.Api.QName, Saxon.Api.XdmValue> DicToSaxonParms(Dictionary<string, object> Args)
        {
            var saxonParams = new Dictionary<QName, XdmValue>();

            foreach (var key in Args.Keys)
            {
                var qname = new QName(key);
                var obj = Args[key];
                var XdmValue = ConvertToXdmValue(obj);
                saxonParams.Add(qname, XdmValue);
            }

            return saxonParams;
        }

        private static Saxon.Api.XdmValue ConvertToXdmValue(object value)
        {
            if (value == null)
            {
                // Handle null as an empty sequence
                return XdmEmptySequence.INSTANCE;
            }

            switch (value)
            {
                case bool boolValue:
                    // Convert boolean to xs:boolean using hard-coded "true" or "false"
                    return new XdmAtomicValue(boolValue ? "true" : "false", XdmAtomicType.BOOLEAN);
                case XmlDocument xmlDocument:
                    var docBuilder = Processor.NewDocumentBuilder();
                    return docBuilder.Build(xmlDocument);
                case XmlNode xmlNode:
                    var nodeBuilder = Processor.NewDocumentBuilder();
                    return nodeBuilder.Build(xmlNode);
                default:
                    // Convert all other types to xs:string
                    return new XdmAtomicValue(value.ToString(), XdmAtomicType.STRING);
            }

        }

        private static void GetProcessor(bool resetRequested)
            {
                lock (ProcessorLock)
                {
                    bool inCooldown = (DateTime.UtcNow - _lastResetTime) < ResetCooldown;
                    bool shouldReset = resetRequested && !inCooldown;
                    if (Processor == null || shouldReset)
                    {
                        Processor = new Processor(false);
                        ClearXsltCache();
                        _lastResetTime = DateTime.UtcNow;
                    }

                System.Diagnostics.Debug.WriteLine("Saxon Version: " + Processor.ProductVersion);
                    System.Diagnostics.Debug.WriteLine("Saxon Edition: " + Processor.ProductTitle);
            }
        }

        private static XsltCompiler GetCompiler()
        {
            return Processor.NewXsltCompiler();
        }

        private static bool IsConfigurationMismatch(Exception ex)
        {
            // Matches common Configuration mismatch messages
            string[] patterns = new[]
            {
            "belongs to the wrong Configuration",
            "Node belongs to a different Configuration",
            "Cannot use a node from one Configuration in another Configuration",
            "Configuration mismatch in stylesheet execution",
            "Invalid Configuration for transformation",
            "incompatible Configuration"
            };

            foreach (var pattern in patterns)
            {
                if (Regex.IsMatch(ex.Message, pattern, RegexOptions.IgnoreCase))
                    return true;
            }
            return false;
        }


        private static string GetTraceLogPath()
        {
            // Adjust this to match how you normally write to /ac
            // Example options — pick the one that works in your app:

            // Option 1: Using Server.MapPath if this is ASP.NET Web Forms/MVC
            // return System.Web.Hosting.HostingEnvironment.MapPath("~/ac/saxon-trace-" + DateTime.Now.ToString("yyyyMMdd-HHmmss") + ".txt");

            // Option 2: Using HttpContext (works in most ASP.NET)
            // return HttpContext.Current.Server.MapPath("~/ac/saxon-trace-" + DateTime.Now.ToString("yyyyMMdd-HHmmss") + ".txt");

            // Option 3: Hardcoded relative or absolute path (common in modern ASP.NET Core or if you know the root)
            var rootPath = AppDomain.CurrentDomain.BaseDirectory; // Usually the bin folder
            var acPath = Path.Combine(rootPath, "ac"); // Goes up one level to project root, then into /ac
            Directory.CreateDirectory(acPath); // Ensure folder exists

            return Path.Combine(acPath, $"saxon-trace-{DateTime.Now:yyyyMMdd-HHmmss}-{Guid.NewGuid():N}.html");
        }
    }
}

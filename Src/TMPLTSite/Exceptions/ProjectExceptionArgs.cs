using System;
using System.Collections.Generic;
using System.Text;

namespace ProjectFlx.Exceptions
{
    public class ProjectExceptionArgs : EventArgs
    {
        private string _message = null;
        private string _sourceclass = null;
        private string _sourcemethod = null;
        private string _sourcecodesnippet = null;
        private SeverityLevel _severitylevel = SeverityLevel.Information;
        private LogLevel _loglevel = LogLevel.Debug;

        public ProjectExceptionArgs(string Messsage)
        {
            this._message = Messsage;

            // minimal defaults ..
            this._severitylevel = SeverityLevel.Information;
            this._loglevel = LogLevel.Event;
        }

        public ProjectExceptionArgs(string Messsage, SeverityLevel Severity)
        {
            this._message = Messsage;

            // minimal defaults ..
            this._severitylevel = Severity;
            this._loglevel = LogLevel.Event;
        }

        public ProjectExceptionArgs(string Message, string SourceClass, string SourceMethod, string SourceSnippet, SeverityLevel Severity, LogLevel Log)
        {
            this._message = Message;
            this._sourceclass = SourceClass;
            this._sourcemethod = SourceMethod;
            this._sourcecodesnippet = SourceSnippet;
            this._severitylevel = Severity;
            this._loglevel = Log;
        }

        public string Message { get { return _message; } }
        public string SourceClass { get { return _sourceclass; } }
        public string SourceMethod { get { return _sourcemethod; } }
        public string SourceSnippet { get { return _sourcecodesnippet; } }
        public SeverityLevel Severity { get { return _severitylevel; } }
        public LogLevel Log { get { return _loglevel; } }


    }
}

using System;
using System.Xml;
using System.Collections.Generic;
using System.Text;

namespace ProjectFlx.Exceptions
{
    public class ProjectException : SystemException
    {
        ProjectExceptionArgs _args;
        ProjectExceptionHandler _handler = null;

		public ProjectException() { }

        public ProjectException(string Message) : base(Message) {
            _args = new ProjectExceptionArgs(Message);
        }
        public ProjectException(ProjectExceptionArgs Args) : base(Args.Message)
        {
            _args = Args;
        }
        public ProjectException(string Message, ProjectExceptionHandler Handler)
            : base(Message)
        {
            _handler = Handler;
        }
        public ProjectException(string Message, Exception inner) : base(Message, inner)
        {
            _args = new ProjectExceptionArgs(Message);
        }
        public ProjectException(ProjectExceptionArgs Args, Exception inner) : base(Args.Message, inner)
        {
            _args = Args;
        }

        public ProjectExceptionArgs Args
        {
            get
            {
                return _args;
            }
        }
        public XmlDocument HandledExceptions
        {
            get
            {
                if (_handler == null)
                    return null;

                return  _handler.XmlDocument;
            }
        }
    }
}

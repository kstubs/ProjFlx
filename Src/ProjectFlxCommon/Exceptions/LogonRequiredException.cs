using System;

namespace ProjectFlx.Exceptions
{
    public class LogonRequiredException : ProjectException
    {
        public LogonRequiredException(String Message) : base(Message) { }
    }
}

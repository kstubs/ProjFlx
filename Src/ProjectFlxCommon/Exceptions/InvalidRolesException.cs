using System;

namespace ProjectFlx.Exceptions
{
    public class InvalidRolesException : ProjectException
    {
        public InvalidRolesException(String Message) : base(Message) { }
    }
}

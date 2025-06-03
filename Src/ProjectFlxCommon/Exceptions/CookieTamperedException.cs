using System;

namespace ProjectFlx.Exceptions
{
    public class CookieTamperedException : ProjectException
    {
        public CookieTamperedException() { }
        public CookieTamperedException(String Message) : base(Message) { }
    }
}

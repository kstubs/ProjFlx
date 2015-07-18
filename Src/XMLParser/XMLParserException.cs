using System;
using ProjectUtilities.Exceptions;


namespace ProjectUtilities.XMLParser
{
    class XMLParserException : ProjectException
    {
        public XMLParserException(string ErrorMessage) : base(ErrorMessage) { }
        public XMLParserException(string ErrorMessage, Exception inner) : base(ErrorMessage, inner) { }
        public XMLParserException(ProjectExceptionArgs Args) : base(Args) { }
        public XMLParserException(ProjectExceptionArgs Args, Exception inner) : base(Args, inner) { }

    }
}

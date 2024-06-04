﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProjectFlx.Exceptions
{
    public class QueryPromiseException : ProjectException
    {
        public QueryPromiseException() { }
        public QueryPromiseException(string message) : base(message) { }
    }
}

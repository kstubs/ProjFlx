﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProjectFlx.Exceptions
{
    public class CookieTamperedException : ProjectException
    {
        public CookieTamperedException() { }
        public CookieTamperedException(String Message) : base(Message) { }
    }
}

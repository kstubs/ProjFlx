using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProjectFlx.Exceptions
{
	public class LogonRequiredException : ProjectException
	{
		public LogonRequiredException(String Message) : base(Message) { }
	}
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProjectFlx.Exceptions
{
	public class InvalidRolesException : ProjectException
	{
		public InvalidRolesException(String Message) : base(Message) {}
	}
}

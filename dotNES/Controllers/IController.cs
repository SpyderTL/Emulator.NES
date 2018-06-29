using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace dotNES.Controllers
{
	interface IController
	{
		void Update(INESController controller);
	}
}

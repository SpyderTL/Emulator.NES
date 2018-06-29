using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SharpDX.DirectInput;

namespace dotNES.Controllers
{
	public class KeyboardController : IController
	{
		private static DirectInput Context;
		private static Keyboard Keyboard;
		private static KeyboardState State;

		public KeyboardController()
		{
			if (Context == null)
			{
				Context = new DirectInput();
				Keyboard = new Keyboard(Context);
				Keyboard.Acquire();
				State = new KeyboardState();
			}
		}

		public void Update(INESController controller)
		{
			var nesController = controller as NES001Controller;

			if (nesController != null)
			{
				Keyboard.GetCurrentState(ref State);

				if (State.IsPressed(Key.A))
					nesController.State |= NES001Controller.Buttons.A;

				if (State.IsPressed(Key.S))
					nesController.State |= NES001Controller.Buttons.B;

				if (State.IsPressed(Key.RightShift))
					nesController.State |= NES001Controller.Buttons.Select;

				if (State.IsPressed(Key.Return))
					nesController.State |= NES001Controller.Buttons.Start;

				if (State.IsPressed(Key.Up))
					nesController.State |= NES001Controller.Buttons.Up;

				if (State.IsPressed(Key.Down))
					nesController.State |= NES001Controller.Buttons.Down;

				if (State.IsPressed(Key.Left))
					nesController.State |= NES001Controller.Buttons.Left;

				if (State.IsPressed(Key.Right))
					nesController.State |= NES001Controller.Buttons.Right;
			}
		}
	}
}

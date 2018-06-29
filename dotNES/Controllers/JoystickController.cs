using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SharpDX.DirectInput;

namespace dotNES.Controllers
{
	public class JoystickController : IController
	{
		private static DirectInput Context;
		private static Joystick Joystick;
		private static JoystickState State;

		public JoystickController(Guid deviceGuid)
		{
			if (Context == null)
				Context = new DirectInput();

			Joystick = new Joystick(Context, deviceGuid);
			Joystick.Acquire();
			State = new JoystickState();
		}

		public void Update(INESController controller)
		{
			var nesController = controller as NES001Controller;

			if (nesController != null)
			{
				Joystick.GetCurrentState(ref State);

				if (State.Buttons[0])
					nesController.State |= NES001Controller.Buttons.A;

				if (State.Buttons[1])
					nesController.State |= NES001Controller.Buttons.B;

				if (State.Buttons[6])
					nesController.State |= NES001Controller.Buttons.Select;

				if (State.Buttons[7])
					nesController.State |= NES001Controller.Buttons.Start;

				if (State.PointOfViewControllers[0] < 0)
					return;

				if (State.PointOfViewControllers[0] <= 4500 ||
					State.PointOfViewControllers[0] >= 31500)
					nesController.State |= NES001Controller.Buttons.Up;

				if (State.PointOfViewControllers[0] >= 13500 &&
					State.PointOfViewControllers[0] <= 22500)
					nesController.State |= NES001Controller.Buttons.Down;

				if (State.PointOfViewControllers[0] >= 22500 &&
					State.PointOfViewControllers[0] <= 31500)
					nesController.State |= NES001Controller.Buttons.Left;

				if (State.PointOfViewControllers[0] >= 4500 &&
					State.PointOfViewControllers[0] <= 13500)
					nesController.State |= NES001Controller.Buttons.Right;
			}
		}

		public static IList<DeviceInstance> GetJoysticks()
		{
			if (Context == null)
				Context = new DirectInput();

			return Context.GetDevices(DeviceType.Gamepad, DeviceEnumerationFlags.AttachedOnly);
		}
	}
}

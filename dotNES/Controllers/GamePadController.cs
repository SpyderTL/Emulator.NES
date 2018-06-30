using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SharpDX.DirectInput;

namespace dotNES.Controllers
{
	public class GamePadController : IController
	{
		private static DirectInput context;
		private Joystick joystick;
		private JoystickState state;

		public GamePadController(Guid deviceGuid)
		{
			if (context == null)
				context = new DirectInput();

			joystick = new Joystick(context, deviceGuid);
			joystick.Acquire();
			state = new JoystickState();
		}

		public void Update(INESController controller)
		{
			var nesController = controller as NES001Controller;

			if (nesController != null)
			{
				joystick.GetCurrentState(ref state);

				if (state.Buttons[0])
					nesController.State |= NES001Controller.Buttons.B;

				if (state.Buttons[1])
					nesController.State |= NES001Controller.Buttons.A;

				if (state.Buttons[6])
					nesController.State |= NES001Controller.Buttons.Select;

				if (state.Buttons[7])
					nesController.State |= NES001Controller.Buttons.Start;

				if (state.X > 0xc000)
					nesController.State |= NES001Controller.Buttons.Right;

				if (state.X < 0x4000)
					nesController.State |= NES001Controller.Buttons.Left;

				if (state.Y > 0xc000)
					nesController.State |= NES001Controller.Buttons.Down;

				if (state.Y < 0x4000)
					nesController.State |= NES001Controller.Buttons.Up;

				if (state.PointOfViewControllers[0] < 0)
					return;

				if (state.PointOfViewControllers[0] <= 4500 ||
					state.PointOfViewControllers[0] >= 31500)
					nesController.State |= NES001Controller.Buttons.Up;

				if (state.PointOfViewControllers[0] >= 13500 &&
					state.PointOfViewControllers[0] <= 22500)
					nesController.State |= NES001Controller.Buttons.Down;

				if (state.PointOfViewControllers[0] >= 22500 &&
					state.PointOfViewControllers[0] <= 31500)
					nesController.State |= NES001Controller.Buttons.Left;

				if (state.PointOfViewControllers[0] >= 4500 &&
					state.PointOfViewControllers[0] <= 13500)
					nesController.State |= NES001Controller.Buttons.Right;
			}
		}

		public static IList<DeviceInstance> GetJoysticks()
		{
			if (context == null)
				context = new DirectInput();

			return context.GetDevices(DeviceType.Gamepad, DeviceEnumerationFlags.AttachedOnly);
		}
	}
}

using UnityEngine;

namespace Kirurobo
{
	public class InputProxy
	{
		public static Vector3 mousePosition => GetMousePosition();

		public static bool GetKeyUp(string key)
		{
			return Input.GetKeyUp(key);
		}

		private static Vector3 GetMousePosition()
		{
			return Input.mousePosition;
		}

		public static bool GetMouseButton(int button)
		{
			return Input.GetMouseButton(button);
		}

		public static bool GetMouseButtonDown(int button)
		{
			return Input.GetMouseButtonDown(button);
		}

		public static bool GetMouseButtonUp(int button)
		{
			return Input.GetMouseButtonUp(button);
		}
	}
}

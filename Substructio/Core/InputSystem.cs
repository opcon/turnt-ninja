using System.Collections.Generic;
using OpenTK;
using OpenTK.Input;

namespace Substructio.Core
{
	public static class InputSystem
	{
		#region Member Variables

		#endregion

		#region Properties

		public static List<Key> CurrentKeys = new List<Key>();
		public static List<Key> NewKeys = new List<Key>();
		public static List<char> PressedChars = new List<char>();
		public static List<MouseButton> LastButtons = new List<MouseButton>();
		public static List<MouseButton> CurrentButtons = new List<MouseButton>();
		public static List<MouseButton> PressedButtons = new List<MouseButton>();
		public static List<MouseButton> UnHandledButtons = new List<MouseButton>();
		public static float MouseWheelDelta;
		public static Vector2 MouseDelta;
		public static Vector2 MousePreviousXY, MouseXY;

	    public static bool Focused = false;

		#endregion

		#region Constructors

		#endregion

		#region Public Methods

		public static void KeyPressed(OpenTK.KeyPressEventArgs e)
		{
			if (Focused)
				PressedChars.Add(e.KeyChar);
		}

		public static void KeyDown(KeyboardKeyEventArgs e)
		{
			if (Focused) {

				if (!CurrentKeys.Contains(e.Key)) {
					CurrentKeys.Add(e.Key);
				}
				if (!NewKeys.Contains(e.Key)) {
					NewKeys.Add(e.Key);
				} 
			}
		}

		public static void KeyUp(KeyboardKeyEventArgs e)
		{
			if (Focused) {
				if (CurrentKeys.Contains(e.Key)) {
					CurrentKeys.Remove(e.Key);
				} 
			}
		}

		public static void MouseDown(MouseButtonEventArgs e)
		{
			if (Focused) {
				if (!CurrentButtons.Contains(e.Button)) {
					CurrentButtons.Add(e.Button);
				}
				if (!UnHandledButtons.Contains(e.Button)) {
					UnHandledButtons.Add(e.Button);
				}
				if (!PressedButtons.Contains(e.Button)) {
					PressedButtons.Add(e.Button);
				} 
			}
		}

		public static void MouseUp(MouseButtonEventArgs e)
		{
			if (Focused) {
				if (CurrentButtons.Contains(e.Button)) {
					CurrentButtons.Remove(e.Button);
				} 
			}
		}

		public static bool IsKeyDown(Key k)
		{
			return CurrentKeys.Contains(k);
		}

		public static bool IsMouseButtonClicked(MouseButton button)
		{
			return Mouse.GetState().IsButtonDown(button);
		}

		public static void Update()
		{
			MouseWheelDelta = 0;
			MousePreviousXY = MouseXY;
			MouseXY = new Vector2(Mouse.GetState().X * 0.5f, -Mouse.GetState().Y * 0.5f);
			MouseDelta = Vector2.Subtract(MouseXY, MousePreviousXY);
			PressedChars.Clear();
			PressedButtons.Clear();
			UnHandledButtons.Clear();
			NewKeys.Clear();
			//LastButtons = new List<MouseButton>(CurrentButtons);
			//CurrentButtons.Clear();
		}

		#endregion

		#region Private Methods

		#endregion

		public static void MouseWheelChanged(MouseWheelEventArgs e)
		{
			MouseWheelDelta += -e.DeltaPrecise;
		}

		public static void MouseMoved(MouseMoveEventArgs e)
		{
			//MouseDelta += new Vector2(e.XDelta, -e.YDelta);
			//Console.WriteLine(MouseDelta.ToString());
		}
	}
}
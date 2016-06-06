using System;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;

namespace TreeShare.Utils
{
	/// <summary>
	/// Utility class used to manage the console.
	/// </summary>
	public static class ConsoleManager
	{
		/// <summary>
		/// Used to manipulate the console window.
		/// </summary>
		/// <returns>Window handle.</returns>
		[DllImport("kernel32.dll")]
		static extern IntPtr GetConsoleWindow();

		/// <summary>
		/// Used to manipulate visibility of the console window.
		/// </summary>
		/// <param name="hWnd">Handle of the window.</param>
		/// <param name="nCmdShow">New status of the window.</param>
		/// <returns>True if successful, false otherwise.</returns>
		[DllImport("user32.dll")]
		static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

		/// <summary>
		/// Shows the console window.
		/// </summary>
		public static void ShowConsole()
		{
			ShowWindow(GetConsoleWindow(), 5);
		}

		/// <summary>
		/// Hides the console window. 
		/// </summary>
		public static void HideConsole()
		{
			ShowWindow(GetConsoleWindow(), 0);
		}

		/// <summary>
		/// Hides the console window for a given
		/// amount of time (returns once the
		/// window is visible again).
		/// </summary>
		/// <param name="ms">Time (in miliseconds) to hide for.</param>
		public static void HideSleepShow(int ms)
		{
			HideConsole();
			Thread.Sleep(ms);
			ShowConsole();
		}

		/// <summary>
		/// Gets a password from the console, using stars
		/// to hide the input.
		/// </summary>
		/// <returns>User's password.</returns>
		public static string GetPassword()
		{
			var res = new StringBuilder();
			while(true)
			{
				var key = Console.ReadKey(true);
				if(key.Key == ConsoleKey.Enter)
				{
					Console.WriteLine();
					break;
				}
				else if(key.Key == ConsoleKey.Backspace)
				{
					if(res.Length > 0)
					{
						res.Remove(res.Length - 1, 1);
						Console.Write("\b \b");
					}
				}
				else
				{
					res.Append(key.KeyChar);
					Console.Write("*");
				}
			}
			return res.ToString();
		}
	}
}

using System;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;

namespace TreeShare.Utils
{
	/// <summary>
	/// 
	/// </summary>
	public static class ConsoleManager
	{
		/// <summary>
		/// 
		/// </summary>
		/// <returns></returns>
		[DllImport("kernel32.dll")]
		static extern IntPtr GetConsoleWindow();

		/// <summary>
		/// 
		/// </summary>
		/// <param name="hWnd"></param>
		/// <param name="nCmdShow"></param>
		/// <returns></returns>
		[DllImport("user32.dll")]
		static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

		/// <summary>
		/// 
		/// </summary>
		public static void ShowConsole()
		{
			ShowWindow(GetConsoleWindow(), 5);
		}

		/// <summary>
		/// 
		/// </summary>
		public static void HideConsole()
		{
			ShowWindow(GetConsoleWindow(), 0);
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="ms"></param>
		public static void HideSleepShow(int ms)
		{
			HideConsole();
			Thread.Sleep(ms);
			ShowConsole();
		}

		/// <summary>
		/// 
		/// </summary>
		/// <returns></returns>
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

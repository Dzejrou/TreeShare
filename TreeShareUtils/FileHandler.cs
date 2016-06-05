using System;
using System.IO;

namespace TreeShare.Utils
{
	public static class FileHelper
	{
		/// <summary>
		/// 
		/// </summary>
		/// <param name="tmp"></param>
		/// <param name="real"></param>
		public static void Move(string tmp, string real)
		{
			bool tmpExists = File.Exists(tmp);
			if(File.Exists(real) && tmpExists)
				File.Delete(real);

			if(tmpExists)
				File.Move(tmp, real);
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="file"></param>
		public static void Delete(string file)
		{
			if(File.Exists(file))
				File.Delete(file);
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="file"></param>
		/// <returns></returns>
		public static DateTime Create(string file)
		{
			Directory.CreateDirectory(Path.GetDirectoryName(file));
			File.Create(file).Close(); // Will not necessarily work with it, so close it.
			return File.GetCreationTime(file);
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="file"></param>
		/// <returns></returns>
		public static bool FileInUse(string file)
		{
			FileStream reader = null;
			try
			{
				reader = File.Open(file, FileMode.Open, FileAccess.Read, FileShare.None);
			}
			catch(IOException)
			{
				return true;
			}
			finally
			{
				if(reader != null)
					reader.Close();
			}
			return false;
		}
	}
}

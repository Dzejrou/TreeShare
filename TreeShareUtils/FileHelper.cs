using System;
using System.IO;

namespace TreeShare.Utils
{
	/// <summary>
	/// Utility class that handles file manipulation.
	/// </summary>
	public static class FileHelper
	{
		/// <summary>
		/// Moves a file, overwriting any other file standing
		/// in the way.
		/// </summary>
		/// <param name="tmp">Original path to the file being moved.</param>
		/// <param name="real">Target path of the file beind moved.</param>
		public static void Move(string tmp, string real)
		{
			bool tmpExists = File.Exists(tmp);
			if(File.Exists(real) && tmpExists)
				File.Delete(real);

			if(tmpExists)
				File.Move(tmp, real);
		}

		/// <summary>
		/// Deletes a file if it exists.
		/// </summary>
		/// <param name="file">Path to the file.</param>
		public static void Delete(string file)
		{
			if(File.Exists(file))
				File.Delete(file);
		}

		/// <summary>
		/// Creates a new file (and any missing directory
		/// in its path) and returns its time of creation.
		/// </summary>
		/// <param name="file">Path to the file.</param>
		/// <returns>Time of creation or DataTime.MaxValue if creation didn't take place.</returns>
		public static DateTime Create(string file)
		{
			try
			{
				string dir;
				if((dir = Path.GetDirectoryName(file)) != "" && dir != null)
					Directory.CreateDirectory(Path.GetDirectoryName(file));
			}
			catch(ArgumentException)
			{
				// Invalid path.
				return DateTime.MaxValue;
			}
			
			File.Create(file).Close(); // Will not necessarily work with it, so close it.
			return File.GetCreationTime(file);
		}

		/// <summary>
		/// Checks if a file is being used by another process (or
		/// another thread within this process).
		/// </summary>
		/// <param name="file">Path to the file.</param>
		/// <returns>True if the files is used, false otherwise.</returns>
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

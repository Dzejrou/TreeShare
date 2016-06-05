using System;

namespace TreeShare.DB
{
	/// <summary>
	/// 
	/// </summary>
	public sealed class ClientDatabase : SerializableDatabase
	{
		/// <summary>
		/// 
		/// </summary>
		private DatabaseTable<File> files = new DatabaseTable<File>();

		/// <summary>
		/// 
		/// </summary>
		public readonly string FileSaveFile = "files.xml";

		/// <summary>
		/// 
		/// </summary>
		/// <returns></returns>
		public DatabaseTable<File> GetFiles()
		{
			return files;
		}

		/// <summary>
		/// 
		/// </summary>
		public override void Save()
		{
			SaveDB(FileSaveFile, files.GetDictionary());
		}

		/// <summary>
		/// 
		/// </summary>
		public override void Load()
		{
			LoadDB(FileSaveFile, files.GetDictionary());
		}
	}
}


namespace TreeShare.DB
{
	/// <summary>
	/// Specialization of the database, used by the client.
	/// Keeps track of files only as groups and users are
	/// irrelevant to the client.
	/// </summary>
	public sealed class ClientDatabase : SerializableDatabase
	{
		/// <summary>
		/// Table containing the files.
		/// </summary>
		private DatabaseTable<File> files = new DatabaseTable<File>();

		/// <summary>
		/// Serialization target for the file table.
		/// </summary>
		public readonly string FileSaveFile = "files.xml";

		/// <summary>
		/// Getter for the file table.
		/// </summary>
		/// <returns>The file table.</returns>
		public DatabaseTable<File> GetFiles()
		{
			return files;
		}

		/// <summary>
		/// Saves the current state of the database using XML.
		/// </summary>
		public override void Save()
		{
			SaveDB(FileSaveFile, files.GetDictionary());
		}

		/// <summary>
		/// Loads the database from an XML file.
		/// </summary>
		public override void Load()
		{
			LoadDB(FileSaveFile, files.GetDictionary());
		}
	}
}


namespace TreeShare.DB
{
	/// <summary>
	/// Base class for all database rows.
	/// </summary>
	public abstract class DatabaseItem
	{
		/// <summary>
		/// Name identifier of the row, used for indexing.
		/// </summary>
		public string Name { get; set; }
	}
}

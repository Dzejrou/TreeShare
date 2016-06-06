using System;
using System.Collections;
using System.Collections.Generic;

namespace TreeShare.DB
{
	/// <summary>
	/// Represents a single table in the TreeShare database.
	/// </summary>
	/// <typeparam name="T">Type of the row.</typeparam>
	public sealed class DatabaseTable<T> : IEnumerable<T> where T : DatabaseItem
	{
		/// <summary>
		/// Underlying container.
		/// </summary>
		private Dictionary<string, T> db = new Dictionary<string, T>();

		/// <summary>
		/// Allows bracket indexing.
		/// </summary>
		/// <param name="name">Name of the row (and the associated entity).</param>
		/// <returns>The requested table or null if not found.</returns>
		public T this[string name]
		{
			get
			{
				return db[name];
			}

			set
			{
				db[name] = value;
			}
		}

		/// <summary>
		/// Returns the number of rows in the table.
		/// </summary>
		public int Count
		{
			get
			{
				return db.Keys.Count;
			}
		}

		/// <summary>
		/// Returns the underlying dictionary which can be used
		/// for serialization.
		/// </summary>
		/// <returns></returns>
		public Dictionary<string, T> GetDictionary()
		{
			return db;
		}

		/// <summary>
		/// Adds a new row to the table.
		/// </summary>
		/// <param name="item">New DatabaseItem instance to add.</param>
		public void Add(T item)
		{
			db.Add(item.Name, item);
		}

		/// <summary>
		/// Removes an item from the table (found by reference).
		/// </summary>
		/// <param name="item">The DatabaseItem to remove.</param>
		public void Remove(T item)
		{
			db.Remove(item.Name);
		}

		/// <summary>
		/// Removes an item from the table (found by name).
		/// </summary>
		/// <param name="name">Name of the row.</param>
		public void Remove(string name)
		{
			db.Remove(name);
		}

		/// <summary>
		/// Tries to retrieve a row (specified by name) from
		/// the table.
		/// </summary>
		/// <param name="name">Name of the row.</param>
		/// <param name="res">Found result, will be assigned to.</param>
		/// <returns>If true, the target has been found and is safe to use.</returns>
		public bool TryGet(string name, out T res)
		{
			return db.TryGetValue(name, out res);
		}

		/// <summary>
		/// Applies an action to every row of the database.
		/// </summary>
		/// <param name="action">Action to apply.</param>
		public void ForEach(Action<T> action)
		{
			foreach(var item in this)
				action(item);
		}

		/// <summary>
		/// Checks if a row (specified by name) is in the
		/// table.
		/// </summary>
		/// <param name="name">Name of the row.</param>
		/// <returns>True if the row is in the table, false otherwise.</returns>
		public bool Contains(string name)
		{
			return db.ContainsKey(name);
		}

		/// <summary>
		/// Returns the enumerator which can be used to iterate
		/// over all rows of the table.
		/// </summary>
		/// <returns>Enumerator.</returns>
		public IEnumerator<T> GetEnumerator()
		{
			foreach(var item in db.Values)
				yield return item;
		}

		/// <summary>
		/// Returns the enumerator which can be used to iterate
		/// over all rows of the table.
		/// </summary>
		/// <returns>Enumerator.</returns>
		IEnumerator IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}
	}
}

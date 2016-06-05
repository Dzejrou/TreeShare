using System;
using System.Collections;
using System.Collections.Generic;

namespace TreeShare.DB
{
	/// <summary>
	/// 
	/// </summary>
	/// <typeparam name="T"></typeparam>
	public sealed class DatabaseTable<T> : IEnumerable<T> where T : DatabaseItem
	{
		/// <summary>
		/// 
		/// </summary>
		private Dictionary<string, T> db = new Dictionary<string, T>();

		/// <summary>
		/// 
		/// </summary>
		/// <param name="name"></param>
		/// <returns></returns>
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
		/// 
		/// </summary>
		public int Count
		{
			get
			{
				return db.Keys.Count;
			}
		}

		/// <summary>
		/// 
		/// </summary>
		/// <returns></returns>
		public Dictionary<string, T> GetDictionary()
		{
			return db;
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="item"></param>
		public void Add(T item)
		{
			db.Add(item.Name, item);
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="item"></param>
		public void Remove(T item)
		{
			db.Remove(item.Name);
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="name"></param>
		public void Remove(string name)
		{
			db.Remove(name);
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="name"></param>
		/// <param name="res"></param>
		/// <returns></returns>
		public bool TryGet(string name, out T res)
		{
			return db.TryGetValue(name, out res);
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="action"></param>
		public void ForEach(Action<T> action)
		{
			foreach(var item in this)
				action(item);
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="name"></param>
		/// <returns></returns>
		public bool Contains(string name)
		{
			return db.ContainsKey(name);
		}

		/// <summary>
		/// 
		/// </summary>
		/// <returns></returns>
		public IEnumerator<T> GetEnumerator()
		{
			foreach(var item in db.Values)
				yield return item;
		}

		/// <summary>
		/// 
		/// </summary>
		/// <returns></returns>
		IEnumerator IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}
	}
}

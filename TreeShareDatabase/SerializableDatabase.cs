using System.Collections.Generic;
using System.IO;
using System.Linq;
using System;
using System.Xml.Serialization;
using System.Xml;

namespace TreeShare.DB
{
	/// <summary>
	/// 
	/// </summary>
	public abstract class SerializableDatabase
	{
		/// <summary>
		/// 
		/// </summary>
		public abstract void Save();

		/// <summary>
		/// 
		/// </summary>
		public abstract void Load();

		/// <summary>
		/// 
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="file"></param>
		/// <param name="db"></param>
		protected void SaveDB<T>(string file, Dictionary<string, T> db)
		{
			var serializer = new XmlSerializer(typeof(T[]));
			var items = db.Values.ToArray<T>();

			try
			{
				var settings = new XmlWriterSettings { OmitXmlDeclaration = true, CheckCharacters = false, Indent = true };

				using(var stream = System.IO.File.Open(file, FileMode.Create))
				using(var writer = XmlWriter.Create(stream, settings))
				{
					serializer.Serialize(writer, items);
				}
			}
			catch(IOException e)
			{
				Console.WriteLine("Cannot serialize DB: {0}", e.Message);
			}
		}

		/// <summary>
		/// 
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="file"></param>
		/// <param name="db"></param>
		protected void LoadDB<T>(string file, Dictionary<string, T> db) where T : DatabaseItem
		{
			var serializer = new XmlSerializer(typeof(T[]));
			T[] items = { };

			try
			{
				var settings = new XmlReaderSettings { CheckCharacters = false };

				using(var stream = System.IO.File.OpenRead(file))
				using(var reader = XmlReader.Create(stream, settings))
				{
					items = (T[])serializer.Deserialize(reader);
				}
			}
			catch(IOException e)
			{
				Console.WriteLine("Cannot deserialize DB: {0}", e.Message);
			}

			foreach(var item in items)
				db.Add(item.Name, item);
		}
	}
}

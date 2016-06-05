using System;
using System.Collections.Generic;
using System.Xml.Serialization;

namespace TreeShare.DB
{
	/// <summary>
	/// 
	/// </summary>
	[Serializable]
	[XmlRoot(ElementName = "Group")]
	public sealed class Group : DatabaseItem
	{
		/// <summary>
		/// 
		/// </summary>
		[XmlArray("Users"), XmlArrayItem(Type = typeof(string), ElementName = "User")]
		public List<string> Users { get; private set; }

		/// <summary>
		/// 
		/// </summary>
		[XmlElement(ElementName = "CanCreateFiles")]
		public bool CanCreateFiles { get; set; }

		/// <summary>
		/// 
		/// </summary>
		[XmlElement(ElementName = "DefaultAccessRight")]
		public AccessRight DefaultAccessRight { get; set; }

		#region Constructors
		/// <summary>
		/// 
		/// </summary>
		public Group() : this("Unknown")
		{ /* DUMMY BODY */ }

		/// <summary>
		/// 
		/// </summary>
		/// <param name="n"></param>
		public Group(string n)
		{
			Name = n;
			Users = new List<string>();
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="n"></param>
		/// <param name="u"></param>
		public Group(string n, List<string> u)
		{
			Name = n;
			Users = u;
		}
		#endregion

		/// <summary>
		/// 
		/// </summary>
		/// <param name="u"></param>
		public void AddUser(User u)
		{
			Users.Add(u.Name);
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="u"></param>
		public void RemoveUser(User u)
		{
			Users.Remove(u.Name);
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="u"></param>
		/// <returns></returns>
		public bool HasUser(User u)
		{
			return Users.Contains(u.Name);
		}
	}
}

using System;
using System.Collections.Generic;
using System.Xml.Serialization;

namespace TreeShare.DB
{
	/// <summary>
	/// Database row representing a group of users.
	/// </summary>
	[Serializable]
	[XmlRoot(ElementName = "Group")]
	public sealed class Group : DatabaseItem
	{
		/// <summary>
		/// List of users in the group.
		/// </summary>
		[XmlArray("Users"), XmlArrayItem(Type = typeof(string), ElementName = "User")]
		public List<string> Users { get; private set; }

		/// <summary>
		/// True if the group's members can create new files.
		/// </summary>
		[XmlElement(ElementName = "CanCreateFiles")]
		public bool CanCreateFiles { get; set; }

		/// <summary>
		/// Default right this group has to new files.
		/// </summary>
		[XmlElement(ElementName = "DefaultAccessRight")]
		public AccessRight DefaultAccessRight { get; set; }

		#region Constructors
		/// <summary>
		/// Default constructor used for XML serialization.
		/// </summary>
		public Group() : this("Unknown")
		{ /* DUMMY BODY */ }

		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="n">Name of the group.</param>
		public Group(string n)
		{
			Name = n;
			Users = new List<string>();
		}

		/// <summary>
		/// 'Copy constructor.' Initializes the group
		/// with a name and an already existing set of members.
		/// </summary>
		/// <param name="n">Name of the group.</param>
		/// <param name="u">List of the group's members.</param>
		public Group(string n, List<string> u)
		{
			Name = n;
			Users = u;
		}
		#endregion

		/// <summary>
		/// Adds a new user to the group.
		/// </summary>
		/// <param name="u">User to be added.</param>
		public void AddUser(User u)
		{
			Users.Add(u.Name);
		}

		/// <summary>
		/// Removes a user from the group.
		/// </summary>
		/// <param name="u">User to remove.</param>
		public void RemoveUser(User u)
		{
			Users.Remove(u.Name);
		}

		/// <summary>
		/// Checks if a given user is a member of this true.
		/// </summary>
		/// <param name="u">User to check.</param>
		/// <returns>True if the user belongs to this group, false otherwise.</returns>
		public bool HasUser(User u)
		{
			return Users.Contains(u.Name);
		}
	}
}

using System;
using System.Collections.Generic;
using System.Xml.Serialization;

namespace TreeShare.DB
{
	/// <summary>
	/// Database row representing a tracked file.
	/// </summary>
	[Serializable]
	[XmlRoot(ElementName = "File")]
	public sealed class File : DatabaseItem
	{
		/// <summary>
		/// List of access right for different groups.
		/// </summary>
		[XmlArray("AccessInfo"), XmlArrayItem(Type = typeof(AccessInfo), ElementName = "Access")]
		public List<AccessInfo> Access { get; set; }

		/// <summary>
		/// Time of last modification.
		/// </summary>
		[XmlElement(ElementName = "DateModified")]
		public DateTime DateModified { get; set; }

		/// <summary>
		/// Default constructor used for XML serialization.
		/// </summary>
		public File() : this("Unknown")
		{ /* DUMMY BODY */ }

		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="p">Path to the file.</param>
		public File(string p)
		{
			Access = new List<AccessInfo>();
			Name = p;
		}

		/// <summary>
		/// Adds a given right to a group.
		/// </summary>
		/// <param name="g">Name of the group.</param>
		/// <param name="r">Right to add.</param>
		public void AddRight(Group g, AccessRight r)
		{
			var info = FindAccessInfo(g);
			if(info != null)
				info.AddRight(r);
			else
				Access.Add(new AccessInfo { Group = g, Right = r, GroupDummy = g.Name });
		}

		/// <summary>
		/// Removes a given right from a group.
		/// </summary>
		/// <param name="g">Name of the group</param>
		/// <param name="r">Right to add.</param>
		public void RemoveRight(Group g, AccessRight r)
		{
			var info = FindAccessInfo(g);
			if(info != null)
				info.RemoveRight(r);
		}

		/// <summary>
		/// Sets a given right as the only right of a given group.
		/// </summary>
		/// <param name="g">Name of the group.</param>
		/// <param name="r">Right to set.</param>
		public void SetRight(Group g, AccessRight r)
		{
			var info = FindAccessInfo(g);
			if(info != null)
				info.Right = r;

		}

		/// <summary>
		/// Tests if a group has a given right to this file.
		/// </summary>
		/// <param name="g">Name of the group.</param>
		/// <param name="r">Right to test.</param>
		/// <returns>True if the group is authorized, false otherwise.</returns>
		public bool Test(Group g, AccessRight r)
		{
			var info = FindAccessInfo(g);
			if(info != null)
				return info.Test(r);
			else
				return false;
		}

		/// <summary>
		/// Checks if this file is older than a given time point.
		/// </summary>
		/// <param name="d">DateTime to compare against.</param>
		/// <returns>True if this file is older, false otherwise.</returns>
		public bool OlderThan(DateTime d)
		{
			return DateModified < d;
		}

		/// <summary>
		/// Updates the modification time of this file.
		/// </summary>
		/// <param name="d">New modification time.</param>
		public void Update(DateTime d)
		{
			DateModified = d;
		}

		/// <summary>
		/// Finds Right-Group pair in the list of access rights.
		/// </summary>
		/// <param name="g"></param>
		/// <returns></returns>
		private AccessInfo FindAccessInfo(Group g)
		{
			foreach(var info in Access)
			{
				if(info.Group == g)
					return info;
			}
			return null;
		}
	}
}

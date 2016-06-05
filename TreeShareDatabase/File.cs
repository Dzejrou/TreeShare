using System;
using System.Collections.Generic;
using System.Xml.Serialization;

namespace TreeShare.DB
{
	/// <summary>
	/// 
	/// </summary>
	[Serializable]
	[XmlRoot(ElementName = "File")]
	public sealed class File : DatabaseItem
	{
		/// <summary>
		/// 
		/// </summary>
		[XmlArray("AccessInfo"), XmlArrayItem(Type = typeof(AccessInfo), ElementName = "Access")]
		public List<AccessInfo> Access { get; set; }

		/// <summary>
		/// 
		/// </summary>
		[XmlElement(ElementName = "DateModified")]
		public DateTime DateModified { get; set; }

		/// <summary>
		/// 
		/// </summary>
		public File() : this("Unknown")
		{ /* DUMMY BODY */ }

		/// <summary>
		/// 
		/// </summary>
		/// <param name="p"></param>
		public File(string p)
		{
			Access = new List<AccessInfo>();
			Name = p;
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="g"></param>
		/// <param name="r"></param>
		public void AddRight(Group g, AccessRight r)
		{
			var info = FindAccessInfo(g);
			if(info != null)
				info.AddRight(r);
			else
				Access.Add(new AccessInfo { Group = g, Right = r});
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="g"></param>
		/// <param name="r"></param>
		public void RemoveRight(Group g, AccessRight r)
		{
			var info = FindAccessInfo(g);
			if(info != null)
				info.RemoveRight(r);
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="g"></param>
		/// <param name="r"></param>
		public void SetRight(Group g, AccessRight r)
		{
			var info = FindAccessInfo(g);
			if(info != null)
				info.Right = r;

		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="g"></param>
		/// <param name="r"></param>
		/// <returns></returns>
		public bool Test(Group g, AccessRight r)
		{
			var info = FindAccessInfo(g);
			if(info != null)
				return info.Test(r);
			else
				return false;
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="d"></param>
		/// <returns></returns>
		public bool OlderThan(DateTime d)
		{
			return DateModified < d;
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="d"></param>
		public void Update(DateTime d)
		{
			DateModified = d;
		}

		/// <summary>
		/// 
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

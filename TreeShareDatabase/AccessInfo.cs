using System;
using System.Xml.Serialization;

namespace TreeShare.DB
{
	/// <summary>
	/// 
	/// </summary>
	[Flags]
	public enum AccessRight
	{
		/// <summary>
		/// 
		/// </summary>
		NONE = 0,

		/// <summary>
		/// 
		/// </summary>
		WRITE = 1,

		/// <summary>
		/// 
		/// </summary>
		READ = 2,

		/// <summary>
		/// 
		/// </summary>
		READ_WRITE = 3
	}

	/// <summary>
	/// 
	/// </summary>
	[Serializable]
	[XmlRoot(ElementName = "AccessInfo")]
	public sealed class AccessInfo
	{
		/// <summary>
		/// 
		/// </summary>
		[XmlIgnore]
		public Group Group { get; set; }

		/// <summary>
		/// 
		/// </summary>
		[XmlElement(ElementName = "Group")]
		public string GroupDummy { get; set; }

		/// <summary>
		/// 
		/// </summary>
		[XmlElement(ElementName = "Right")]
		public AccessRight Right { get; set; }

		/// <summary>
		/// 
		/// </summary>
		/// <param name="r"></param>
		public void AddRight(AccessRight r)
		{
			Right |= r;
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="r"></param>
		public void RemoveRight(AccessRight r)
		{
			Right &= ~r;
		}
		
		/// <summary>
		/// 
		/// </summary>
		/// <param name="r"></param>
		/// <returns></returns>
		public bool Test(AccessRight r)
		{
			return (Right & r) != AccessRight.NONE;
		}
	}

	/// <summary>
	/// 
	/// </summary>
	public static class AccessRightHelper
	{
		/// <summary>
		/// 
		/// </summary>
		/// <param name="right"></param>
		/// <returns></returns>
		public static AccessRight Parse(string right)
		{
			AccessRight tmp;
			if(!Enum.TryParse(right, out tmp))
				return AccessRight.NONE;
			else
				return tmp;
		}
	}
}

using System;
using System.Xml.Serialization;

namespace TreeShare.DB
{
	/// <summary>
	/// Enum representing invidiual access rights that
	/// can be used as flags (unix like rights).
	/// </summary>
	[Flags]
	public enum AccessRight
	{
		/// <summary>
		/// No access right.
		/// </summary>
		NONE = 0,

		/// <summary>
		/// Write access right.
		/// </summary>
		WRITE = 1,

		/// <summary>
		/// Read access right.
		/// </summary>
		READ = 2,

		/// <summary>
		/// Read/Write access right.
		/// </summary>
		READ_WRITE = 3
	}

	/// <summary>
	/// Group-AccessRight pair used to authorize
	/// access to file by members of a group.
	/// </summary>
	[Serializable]
	[XmlRoot(ElementName = "AccessInfo")]
	public sealed class AccessInfo
	{
		/// <summary>
		/// Group owning this right.
		/// </summary>
		[XmlIgnore]
		public Group Group { get; set; }

		/// <summary>
		/// String proxy used for serialization,
		/// this avoids serialization of groups for each
		/// of their access right.
		/// </summary>
		[XmlElement(ElementName = "Group")]
		public string GroupDummy { get; set; }

		/// <summary>
		/// The right the group has to the file.
		/// </summary>
		[XmlElement(ElementName = "Right")]
		public AccessRight Right { get; set; }

		/// <summary>
		/// Adds a new right.
		/// </summary>
		/// <param name="r">Right to add.</param>
		public void AddRight(AccessRight r)
		{
			Right |= r;
		}

		/// <summary>
		/// Removes a right.
		/// </summary>
		/// <param name="r">Right to remove.</param>
		public void RemoveRight(AccessRight r)
		{
			Right &= ~r;
		}
		
		/// <summary>
		/// Tests access for a given operation.
		/// </summary>
		/// <param name="r">Operation represented by required right.</param>
		/// <returns>True if authorized, false otherwise.</returns>
		public bool Test(AccessRight r)
		{
			return (Right & r) != AccessRight.NONE;
		}
	}

	/// <summary>
	/// Utility class used to parse rights.
	/// (Used to console I/O on the server.)
	/// </summary>
	public static class AccessRightHelper
	{
		/// <summary>
		/// Parses an AccessRight from a string.
		/// </summary>
		/// <param name="right">String to parse.</param>
		/// <returns>Resulting right or NONE if the string is invalid.</returns>
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

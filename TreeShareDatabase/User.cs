using System;
using System.Net;
using System.Xml.Serialization;
using TreeShare.Utils;

namespace TreeShare.DB
{
	/// <summary>
	/// Database row representing a user.
	/// </summary>
	[Serializable]
	[XmlRoot(ElementName = "User")]
	public sealed class User : DatabaseItem
	{
		/// <summary>
		/// Hash of the user's password (with salt).
		/// </summary>
		[XmlElement(ElementName = "Password")]
		public string PasswordHash { get; set; }

		/// <summary>
		/// Group this user belongs to.
		/// </summary>
		[XmlElement(ElementName = "Group")]
		public string Group { get; set; }

		/// <summary>
		/// Port this user listens on for server
		/// announcements.
		/// </summary>
		[XmlIgnore]
		public int ListenPort { get; set; }

		/// <summary>
		/// Address the user listens on for server
		/// announcements.
		/// </summary>
		[XmlIgnore]
		public IPAddress Address { get; set; }

		/*
		 * Currently not used, need to decide if keeping the IP addresses
		 * isn't a security risk (enemy would know where to listen for passwords).

		/// <summary>
		/// Proxy property allowing XML serialize of the IP Address.
		/// (IPAddress type is not serializable as it does not have default constructor.)
		/// </summary>
		[XmlElement(ElementName = "IPAddress")]
		public string AddressDummy
		{ // This is a hack allowing XML serialization.
			get
			{
				return Address.ToString();
			}
			set
			{
				Address = string.IsNullOrEmpty(value) ? null : IPAddress.Parse(value);
			}
		}
		*/

		/// <summary>
		/// Default constructor, used for XML serialization.
		/// </summary>
		public User()
		{
			Name = "Unknown";
			PasswordHash = "Unknown";
			Group = "Unknown";
			ListenPort = -1;
		}

		/// <summary>
		/// Checks if a given password matches this user's password.
		/// </summary>
		/// <param name="pass">Password hash (without salt).</param>
		/// <returns>True if the passwords match, false otherwise.</returns>
		public bool Authenticate(string pass)
		{
			return PasswordHash == GetSaltedHash(Name, pass);
		}

		/// <summary>
		/// Returns the salted version of a user password.
		/// </summary>
		/// <param name="name">Name of the user.</param>
		/// <param name="pass">Hash of the user's password (without salt).</param>
		/// <returns></returns>
		public static string GetSaltedHash(string name, string pass)
		{
			pass = string.Concat(name, pass);
			return Hasher.CreatePasswordHash(pass);
		}
	}
}

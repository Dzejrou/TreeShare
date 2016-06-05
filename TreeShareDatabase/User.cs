using System;
using System.Net;
using System.Xml.Serialization;
using TreeShare.Utils;

namespace TreeShare.DB
{
	/// <summary>
	/// 
	/// </summary>
	[Serializable]
	[XmlRoot(ElementName = "User")]
	public sealed class User : DatabaseItem
	{
		/// <summary>
		/// 
		/// </summary>
		[XmlElement(ElementName = "Password")]
		public string PasswordHash { get; set; }

		/// <summary>
		/// 
		/// </summary>
		[XmlElement(ElementName = "Group")]
		public string Group { get; set; }

		/// <summary>
		/// 
		/// </summary>
		[XmlIgnore]
		public int ListenPort { get; set; }

		/// <summary>
		/// 
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
		/// 
		/// </summary>
		public User()
		{
			Name = "Unknown";
			PasswordHash = "Unknown";
			Group = "Unknown";
			ListenPort = -1;
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="pass"></param>
		/// <returns></returns>
		public bool Authenticate(string pass)
		{
			return PasswordHash == GetSaltedHash(Name, pass);
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="name"></param>
		/// <param name="pass"></param>
		/// <returns></returns>
		public static string GetSaltedHash(string name, string pass)
		{
			pass = string.Concat(name, pass);
			return Hasher.CreatePasswordHash(pass);
		}
	}
}

using System.Security;
using System.Security.Cryptography;
using System.Text;

namespace TreeShare.Utils
{
	/// <summary>
	/// Utility class used to hash passwords.
	/// </summary>
	public static class Hasher
	{
		/// <summary>
		/// Generates a hash for a password represented as a
		/// string.
		/// </summary>
		/// <param name="pass">Password to hash.</param>
		/// <returns>Hash of the password.</returns>
		public static string CreatePasswordHash(string pass)
		{
			byte[] bytes = Encoding.ASCII.GetBytes(pass);
			bytes = new SHA256Managed().ComputeHash(bytes);
			return Encoding.ASCII.GetString(bytes);
		}

		/// <summary>
		/// Generates a hash for a password represented as a
		/// SecureString.
		/// Note: No safe method found so far (all found
		/// require unsafe code), so it's does not offer
		/// any protection.
		/// </summary>
		/// <param name="pass">Password to hash.</param>
		/// <returns>Hash of the password.</returns>
		public static string CreatePasswordHash(SecureString pass)
		{ // TODO: Ask about this? The ToString() call reveals it :/
			byte[] bytes = Encoding.ASCII.GetBytes(pass.ToString());
			bytes = new SHA256Managed().ComputeHash(bytes);
			return Encoding.ASCII.GetString(bytes);
		}
	}
}

using System.Security;
using System.Security.Cryptography;
using System.Text;

namespace TreeShare.Utils
{
	/// <summary>
	/// 
	/// </summary>
	public static class Hasher
	{
		/// <summary>
		/// 
		/// </summary>
		/// <param name="pass"></param>
		/// <returns></returns>
		public static string CreatePasswordHash(string pass)
		{
			byte[] bytes = Encoding.ASCII.GetBytes(pass);
			bytes = new SHA256Managed().ComputeHash(bytes);
			return Encoding.ASCII.GetString(bytes);
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="pass"></param>
		/// <returns></returns>
		public static string CreatePasswordHash(SecureString pass)
		{ // TODO: Ask about this? The ToString() call reveals it :/
			byte[] bytes = Encoding.ASCII.GetBytes(pass.ToString());
			bytes = new SHA256Managed().ComputeHash(bytes);
			return Encoding.ASCII.GetString(bytes);
		}
	}
}

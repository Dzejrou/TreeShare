using System;

namespace TreeShare.Network
{
	/// <summary>
	/// Enum used to direct the network communication
	/// between the server and its clients.
	/// </summary>
	public enum Protocol
	{
		NONE,
		AUTHENTICATE,
		REGISTER,
		SUCCESS,
		FAIL,
		FILE_CREATED,
		FILE_CHANGED,
		FILE_DELETED,
		TRANSMISSION_END,
		REQUEST_FILE_CONTENTS,
		REQUEST_INITIAL_INFO,
		NEW_CONNECTION
	}

	/// <summary>
	/// Utility class used to parse protocol messages.
	/// </summary>
	public static class ProtocolHelper
	{
		/// <summary>
		/// Parses a protocol token from a string.
		/// </summary>
		/// <param name="msg">String to parse.</param>
		/// <returns>Resulting protocol or NONE if the string is invalid.</returns>
		public static Protocol ExtractProtocol(string msg)
		{
			Protocol tmp;
			if(msg == null || !Enum.TryParse(msg, out tmp))
				return Protocol.NONE;
			else
				return tmp;
		}
	}
}

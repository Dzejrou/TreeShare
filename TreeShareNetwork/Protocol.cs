using System;

namespace TreeShare.Network
{
	/// <summary>
	/// Enum used to direct the network communication
	/// between the server and its clients.
	/// </summary>
	public enum Protocol
	{
		/// <summary>
		/// Dummy message.
		/// </summary>
		NONE,

		/// <summary>
		/// Request for authentication sent from the client.
		/// </summary>
		AUTHENTICATE,

		/// <summary>
		/// Request for registration sent from the client.
		/// </summary>
		REGISTER,

		/// <summary>
		/// Positive acknowldgement.
		/// </summary>
		SUCCESS,

		/// <summary>
		/// Negative acknowledgement.
		/// </summary>
		FAIL,

		/// <summary>
		/// Notifies that a new file has been created.
		/// </summary>
		FILE_CREATED,

		/// <summary>
		/// Notifies that a file has been changed.
		/// </summary>
		FILE_CHANGED,

		/// <summary>
		/// Notifies that a file has been deleted.
		/// </summary>
		FILE_DELETED,

		/// <summary>
		/// Marks end of the current communication or
		/// sub communication (like file transfer).
		/// </summary>
		TRANSMISSION_END,
		
		/// <summary>
		/// Marks a request for the contents of a file.
		/// </summary>
		REQUEST_FILE_CONTENTS,

		/// <summary>
		/// Marks a request for information about all
		/// files that are available for reading to the
		/// sending client.
		/// </summary>
		REQUEST_INITIAL_INFO,

		/// <summary>
		/// Notifies about a new connection and incoming
		/// client listen port.
		/// </summary>
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

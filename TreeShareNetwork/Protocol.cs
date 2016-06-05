
using System;

namespace TreeShare.Network
{
	/// <summary>
	/// 
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
		SERVER_SHUTDOWN,
		CLIENT_SHUTDOWN,
		RIGHT_TO_CREATE_FILES_ADDED,
		REQUEST_FILE_CONTENTS,
		REQUEST_INITIAL_INFO,
		NEW_CONNECTION
	}

	/// <summary>
	/// 
	/// </summary>
	public static class ProtocolHelper
	{
		/// <summary>
		/// 
		/// </summary>
		/// <param name="msg"></param>
		/// <returns></returns>
		public static Protocol ExtractProtocol(string msg)
		{
			Protocol tmp;
			if(!Enum.TryParse(msg, out tmp))
				return Protocol.NONE;
			else
				return tmp;
		}
	}
}

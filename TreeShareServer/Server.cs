#define debug
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using TreeShare.DB;
using TreeShare.Network;
using TreeShare.Utils;

namespace TreeShare
{
	/// <summary>
	/// 
	/// </summary>
	sealed class Server
	{
		/// <summary>
		/// 
		/// </summary>
		public ServerDatabase db = new ServerDatabase();

		/// <summary>
		/// 
		/// </summary>
		private string serverAddress;

		/// <summary>
		/// 
		/// </summary>
		private int serverPort;

		/// <summary>
		/// 
		/// </summary>
		private int portLow;

		/// <summary>
		/// 
		/// </summary>
		private int portHigh;

		/// <summary>
		/// 
		/// </summary>
		private TcpListener tcpListener;

		/// <summary>
		/// 
		/// </summary>
		private List<int> portsInUse;

		/// <summary>
		/// 
		/// </summary>
		private int running = 1;

		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="a">Address the server listens on.</param>
		/// <param name="p">Port the server listens on.</param>
		/// <param name="l">Lower bound of the port pool.</param>
		/// <param name="h">Higher bound of the port pool.</param>
		public Server(string a, int p, int l, int h)
		{
			serverAddress = a;
			serverPort = p;
			portLow = l;
			portHigh = h;
			portsInUse = new List<int>();

			IPAddress addr;
			if(!IPAddress.TryParse(serverAddress, out addr))
				throw new ArgumentException("Wrong server address: {0}", serverAddress);
			tcpListener = new TcpListener(addr, serverPort);
		}

		/// <summary>
		/// 
		/// </summary>
		public void Start()
		{
			Console.WriteLine("[INFO] Loading DB.");
			db.Load();
			EnsureDefaultGroup();
			tcpListener.Start();

			Thread t1 = new Thread(HandleConsoleIO);
			t1.Start();
			Thread t2 = new Thread(AcceptConnection);
			t2.Start();

			var saveDBTimer = new Timer(_ => db.Save(), null, 5000, 5000);

			/*
			 * Note: Using integer to allow atomic modification
			 *       with Interlocked, otherwise locking a control
			 *       variable would be necessary.
			 * Note: Why does VS2015 register /** and warn about it? -.-
			 */
			while(Interlocked.CompareExchange(ref running, 0, 0) == 1)
				Thread.Sleep(500);

			tcpListener.Stop();
			t1.Join();

			Console.WriteLine("[INFO] Saving DB.");
			db.Save();
		}

		/// <summary>
		/// 
		/// </summary>
		private void AcceptConnection()
		{
			try
			{
				while(true)
				{
					Socket s = tcpListener.AcceptSocket();
					Task.Run(() => HandleConnection(s));
				}
			}
			catch(SocketException)
			{ /* Application is exiting. */ }
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="s"></param>
		private void HandleConnection(Socket s)
		{
			int newPort = 0;
			try
			{
				// Resend the client to another port.
				using(var writer = new StreamWriter(new NetworkStream(s)))
				{
					writer.AutoFlush = true;

					newPort = GetNewPort();
					writer.WriteLine(newPort);
					var tmpListener = new TcpListener(IPAddress.Parse(serverAddress), newPort);
					tmpListener.Start();

					// Just to be sure, close the original after establishing a new connection.
					var tmpSocket = tmpListener.AcceptSocket();
					tmpListener.Stop();
					s.Close();

					s = tmpSocket;
				}

				using(var stream = new NetworkStream(s))
				using(var reader = new StreamReader(stream))
				using(var writer = new StreamWriter(stream))
				{
					writer.AutoFlush = true;
					//reader.BaseStream.ReadTimeout = 10000;
					string msg;
					User user = null;

					bool running = true;
					while(running)
					{
						msg = reader.ReadLine();
						Protocol protocol = ProtocolHelper.ExtractProtocol(msg);
						Console.WriteLine("[TOKEN] " + protocol);

						switch(protocol)
						{
							case Protocol.AUTHENTICATE:
								user = HandleUserAuthentication(s, reader, writer);
								if(user == null)
									running = false;
								break;
							case Protocol.REGISTER:
								user = HandleUserRegistration(s, reader, writer);
								if(user == null)
									running = false;
								break;
							case Protocol.FILE_CHANGED:
							case Protocol.FILE_CREATED:
							case Protocol.FILE_DELETED:
								HandleFileUpdate(s, reader, writer, protocol, user);
								break;
							case Protocol.REQUEST_INITIAL_INFO:
								if(!SendInitialInfo(writer, user))
									running = false;
								break;
							case Protocol.REQUEST_FILE_CONTENTS:
								HandleFileRequest(reader, writer, user);
								break;
							case Protocol.TRANSMISSION_END:
								running = false;
								break;
							case Protocol.NEW_CONNECTION:
								GetClientListenPort(reader, user);
								break;
						}
					}
				}
			}
			catch(IOException e)
			{ /* This should be timeout. */
#if debug
				Console.WriteLine("[Debug] Timeout. {0}", e.Message);
#endif
			}
			finally
			{
				ReleasePort(newPort);
				s.Close();
			}
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="reader"></param>
		/// <param name="user"></param>
		private void GetClientListenPort(StreamReader reader, User user)
		{
			if(user == null)
				return;
			string port = reader.ReadLine();
			int tmp;
			if(int.TryParse(port, out tmp))
				user.ListenPort = tmp;
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="socket"></param>
		/// <param name="reader"></param>
		/// <param name="writer"></param>
		/// <param name="operation"></param>
		/// <param name="user"></param>
		/// <returns></returns>
		private void HandleFileUpdate(Socket socket, StreamReader reader, StreamWriter writer, Protocol operation, User user)
		{
			Group group;
			var groups = db.GetGroups();
			if(user == null || !groups.TryGet(user.Group, out group))
				return;

			string fileName = reader.ReadLine();

			if(fileName == null)
				return;
			var files = db.GetFiles();
			DB.File file = null;

			if(!AuthorizeFileAccess(fileName, out file, operation, group))
			{
				writer.WriteLine(Protocol.FAIL);
				return;
			}
			else
				writer.WriteLine(Protocol.SUCCESS);
			fileName = null;

			if(operation == Protocol.FILE_DELETED)
				DeleteFile(file);
			else if(!WriteContentsToFile(file, operation, reader))
				return;

			InformAll(user, operation, file.Name);
			file.DateModified = DateTime.Now; // So the clients don't all refresh the file contents.
			return;
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="file"></param>
		/// <returns></returns>
		private bool DeleteFile(DB.File file)
		{
			lock(file)
			{
				try
				{
					BackupFile(file.Name);
					System.IO.File.Delete(file.Name);
					if(db.GetFiles().Contains(file.Name))
						db.GetFiles().Remove(file);
					return true;
				}
				catch(IOException)
				{
					return false;
				}
			}
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="file"></param>
		/// <param name="operation"></param>
		/// <param name="reader"></param>
		/// <returns></returns>
		private bool WriteContentsToFile(DB.File file, Protocol operation, StreamReader reader)
		{
			lock(file)
			{
				if(operation == Protocol.FILE_CHANGED)
					BackupFile(file.Name);
				string tmpFile = file.Name + ".tmp";
				try
				{
					string line;
					using(var stream = System.IO.File.OpenWrite(tmpFile))
					using(var fileWriter = new StreamWriter(stream))
					{
						while((line = reader.ReadLine()) != null && line != "TRANSMISSION_END")
							fileWriter.WriteLine(line);
					}

					if(line != "TRANSMISSION_END")
					{
						FileHelper.Delete(tmpFile);
						Console.WriteLine("[ERROR] Wrong file transmission end token.");
						return false;
					}
					else
						FileHelper.Move(tmpFile, file.Name);
				}
				catch(IOException e)
				{ // Most probably time out.
					try
					{
						Console.WriteLine("[ERROR] Cannot write to file: {0}", e.Message);
						FileHelper.Delete(tmpFile);
					}
					catch(IOException)
					{
						// TODO:
					}
					return false;
				}
			}
			return true;
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="name"></param>
		/// <param name="file"></param>
		/// <param name="type"></param>
		/// <param name="group"></param>
		/// <returns></returns>
		private bool AuthorizeFileAccess(string name, out DB.File file, Protocol type, Group group)
		{
			switch(type)
			{
				case Protocol.FILE_CHANGED:
				case Protocol.FILE_DELETED:
					return db.GetFiles().TryGet(name, out file) && file.Test(group, AccessRight.WRITE);
				case Protocol.FILE_CREATED:
					if(group.CanCreateFiles && !db.GetFiles().Contains(name))
					{
						Directory.CreateDirectory(Path.GetDirectoryName(name));
						file = db.CreateNewFile(name, group);
						return true;
					}
					else
					{
						file = null;
						return false;
					}
				case Protocol.REQUEST_FILE_CONTENTS:
					if(!db.GetFiles().TryGet(name, out file))
						return false;
					else
						return file.Test(group, AccessRight.READ);
				default:
					file = null;
					return false;
			}
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="ignored"></param>
		/// <param name="message"></param>
		/// <param name="fileName"></param>
		private void InformAll(User ignored, Protocol message, string fileName)
		{
			if(message != Protocol.FILE_CHANGED && message != Protocol.FILE_CREATED && message != Protocol.FILE_DELETED)
				return;

			DB.File file;
			if(!db.GetFiles().TryGet(fileName, out file) && message != Protocol.FILE_DELETED)
				return;

			foreach(var user in db.GetUsers())
			{
				if(user == ignored || user.Address == null)
					continue;

				if(message != Protocol.FILE_DELETED)
				{
					Group group;
					if(!db.GetGroups().TryGet(user.Group, out group))
						continue;

					if(!file.Test(group, AccessRight.READ))
						continue;
				}

				TcpClient client = null;
				try
				{
					client = new TcpClient(user.Address.ToString(), user.ListenPort);
					using(var stream = client.GetStream())
					using(var writer = new StreamWriter(stream))
					using(var reader = new StreamReader(stream))
					{
						writer.AutoFlush = true;
						writer.WriteLine(message);

						if(message != Protocol.FILE_DELETED)
						{
							using(var fileReader = new StreamReader(System.IO.File.OpenRead(fileName)))
								SendFileContents(writer, fileName);
						}
						else // SendFileContents already sent T_E.
						{
							writer.WriteLine(fileName);
							writer.WriteLine(Protocol.TRANSMISSION_END);
						}
					}
				}
				catch(IOException e)
				{
					// TODO:
					Console.WriteLine("Hah? {0}", e);
				}
				finally
				{
					if(client != null)
						client.Close();
				}
			}
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="writer"></param>
		/// <param name="name"></param>
		private bool SendFileContents(StreamWriter writer, string name)
		{
			try
			{
				using(var reader = new StreamReader(System.IO.File.OpenRead(name)))
				{
					string line;
					writer.WriteLine(name);
					while((line = reader.ReadLine()) != null)
						writer.WriteLine(line);
					writer.WriteLine(Protocol.TRANSMISSION_END);
				}
				return true;
			}
			catch(IOException)
			{
				try
				{
					writer.WriteLine(Protocol.FAIL);
					return true;
				}
				catch(IOException)
				{
					return false;
				}
			}
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="path"></param>
		private void BackupFile(string path)
		{
			if(!db.GetFiles().Contains(path) || !System.IO.File.Exists(path))
				return;
			string bckp = "backup\\" + path + ".bckp_" + DateTime.Now.ToString("yyy-MM-dd_hh_mm_ss");
			string dir = Path.GetDirectoryName(bckp);
			Directory.CreateDirectory(dir);

			System.IO.File.Copy(path, bckp);
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="socket"></param>
		/// <param name="reader"></param>
		/// <param name="writer"></param>
		/// <returns></returns>
		private User HandleUserRegistration(Socket socket, StreamReader reader, StreamWriter writer)
		{
			string name = reader.ReadLine();
			string password = reader.ReadLine();

			var users = db.GetUsers();
			if(users.Contains(name))
			{
				writer.WriteLine(Protocol.FAIL);
				return null;
			}
			else
			{
				var user = new User();
				var endPoint = ((IPEndPoint)socket.RemoteEndPoint);
				user.Address = endPoint.Address;
				user.Name = name;
				user.PasswordHash = User.GetSaltedHash(name, password);

				users.Add(user);
				db.MoveUserToGroup(name, "default");
				writer.WriteLine(Protocol.SUCCESS);

				return user;
			}
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="name"></param>
		/// <param name="password"></param>
		/// <param name="user"></param>
		/// <returns></returns>
		private bool AuthenticateUser(string name, string password, out User user)
		{
			var users = db.GetUsers();
			if(!users.TryGet(name, out user))
				return false;
			else
				return user.Authenticate(password);
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="socket"></param>
		/// <param name="reader"></param>
		/// <param name="writer"></param>
		/// <returns></returns>
		private User HandleUserAuthentication(Socket socket, StreamReader reader, StreamWriter writer)
		{
			string name = reader.ReadLine();
			string password = reader.ReadLine();

			var users = db.GetUsers();
			User user;
			if(!AuthenticateUser(name, password, out user))
			{
				writer.WriteLine(Protocol.FAIL);
				return null;
			}
			else
			{
				writer.WriteLine(Protocol.SUCCESS);
				user.Address = ((IPEndPoint)socket.RemoteEndPoint).Address;

				return user;
			}
		}

		/// <summary>
		/// 
		/// </summary>
		private void HandleConsoleIO()
		{
			string command;
			string[] tokens;
			while(true)
			{
				command = Console.ReadLine();
				tokens = command.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
				if(tokens.Length < 1)
					Console.WriteLine("[ERROR] No command entered.");
				else
				{
					switch(tokens[0].ToLower())
					{
						case "exit":
							Interlocked.Decrement(ref running);
							return;
						case "hide":
							int ms;
							if(!int.TryParse(tokens[1], out ms))
								Console.WriteLine("[ERROR] {0} is not a valid amount of seconds.", tokens[1]);
							else if(ms > 0)
								ConsoleManager.HideSleepShow(ms * 1000);
							else
								ConsoleManager.HideConsole();
							break;
						case "group-create":
							bool canCreateFiles;
							if(tokens.Length < 4)
								Console.WriteLine("[ERROR] Not enough arguments. Usage: group-create <group-name> <default-right> <bool-can-create-files>");
							else if(!bool.TryParse(tokens[3], out canCreateFiles))
								Console.WriteLine("[ERROR] {0} is not a valid boolean value.");
							else
								db.CreateNewGroup(tokens[1], AccessRightHelper.Parse(tokens[2]), canCreateFiles);
							break;
						case "group-add":
							if(tokens.Length < 3)
								Console.WriteLine("[ERROR] Not enough arguments. Usage: group-add <user> <target-group>.");
							else
								db.MoveUserToGroup(tokens[1], tokens[2]);
							break;
						case "user-add":
							if(tokens.Length < 2)
								Console.WriteLine("[ERROR] Not enough arguments. Usage: user-add <user>.");
							else
								db.CreateNewUser(tokens[1]);
							break;
						case "file-add":
							if(tokens.Length < 2)
								Console.WriteLine("[ERROR] Not enough arguments. Usage: file-add <path>.");
							else if(tokens[1].Contains(".."))
								Console.WriteLine("[ERROR] Invalid path, '..' is not allowed to avoid file system pollution.");
							else
								db.CreateNewFile(tokens[1], null);
							InformAll(null, Protocol.FILE_CREATED, tokens[1]);
							db.Save();
							break;
						case "add-right":
							if(tokens.Length < 4)
							{
								Console.WriteLine("[ERROR] Not enough arguments. Usage: add-right <path> <group> <righ>");
								Console.WriteLine("[RIGHTS] {0} {1} {2} {3}", AccessRight.NONE, AccessRight.READ, AccessRight.WRITE, AccessRight.READ_WRITE);
							}
							else
								db.AddRightToGroup(tokens[1], tokens[2], AccessRightHelper.Parse(tokens[3]));
							break;
						case "remove-right":
							if(tokens.Length < 4)
							{
								Console.WriteLine("[ERROR] Not enough arguments. Usage: remove-right <path> <group> <righ>");
								Console.WriteLine("[RIGHTS] {0} {1} {2} {3}", AccessRight.NONE, AccessRight.READ, AccessRight.WRITE, AccessRight.READ_WRITE);
							}
							else
								db.RemoveRightFromGroup(tokens[1], tokens[2], AccessRightHelper.Parse(tokens[3]));
							break;
						case "save-db":
							db.Save();
							break;
						case "load-db":
							db.Load();
							break;
						default:
							Console.WriteLine("[ERROR] Unknown command: {0}", tokens[0]);
							break;
					}
				}
			}
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="reader"></param>
		/// <param name="writer"></param>
		/// <param name="user"></param>
		/// <returns></returns>
		private bool HandleFileRequest(StreamReader reader, StreamWriter writer, User user)
		{
			string fileName = reader.ReadLine();

			Group group;
			DB.File file;
			if(user == null ||
			   !db.GetGroups().TryGet(user.Group, out group) ||
			   !AuthorizeFileAccess(fileName, out file, Protocol.REQUEST_FILE_CONTENTS, group))
			{
				Console.WriteLine("[INFO] Access to {0} not authorized.", fileName);
				writer.WriteLine(Protocol.FAIL);
				return true;
			}
			else
				writer.WriteLine(Protocol.SUCCESS);
			Console.WriteLine("[INFO] Access to {0} authorized.", fileName);
			
			lock(file)
				return SendFileContents(writer, fileName);
		}

		/// <summary>
		/// Returns a new port from the port pool.
		/// </summary>
		/// <returns>
		/// New port that can be used for communication with a client.
		/// -1 if no ports are available.
		/// </returns>
		private int GetNewPort()
		{
			lock(portsInUse)
			{
				for(int i = portLow; i <= portHigh; ++i)
				{
					if(!portsInUse.Contains(i))
					{
						portsInUse.Add(i);
						return i;
					}
				}
			}
			return -1;
		}

		/// <summary>
		/// Returns a used port back to the available port pool.
		/// </summary>
		/// <param name="p">Port to be released.</param>
		private void ReleasePort(int p)
		{
			lock(portsInUse)
			{
				portsInUse.Remove(p);
			}
		}

		/// <summary>
		/// 
		/// </summary>
		private void EnsureDefaultGroup()
		{
			var groups = db.GetGroups();
			if(!groups.Contains("default"))
			{
				var group = new Group("default");
				group.CanCreateFiles = false;

				groups.Add(group);
				db.AddAccessRightToSubDirectory(group, AccessRight.READ, "");

				var users = db.GetUsers();
				foreach(var user in users)
				{ // Fixes broken user-group relationships.
					if(user.Group == "default")
						group.AddUser(user);
				}
			}
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="writer"></param>
		/// <param name="user"></param>
		/// <returns>True if the server should expect additional information sent by the client, false otherwise.</returns>
		private bool SendInitialInfo(StreamWriter writer, User user)
		{
			Group group;
			if(!db.GetGroups().TryGet(user.Group, out group))
			{
				writer.WriteLine(Protocol.TRANSMISSION_END);
				return true;
			}
			else
				writer.WriteLine(Protocol.SUCCESS);

			try
			{
				foreach(var file in db.GetFiles())
				{
					if(!file.Test(group, AccessRight.READ))
						continue;
					Console.WriteLine("Sending info about file: {0}", file.Name);
					writer.WriteLine(file.Name);
					writer.WriteLine(file.DateModified);
				}
				writer.WriteLine(Protocol.TRANSMISSION_END);
			}
			catch(IOException)
			{ // Connection closed.
				return false;
			}
			return true;
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="args"></param>
		static void Main(string[] args)
		{
			int port = 0, low = 0, high = 0;
			if(args.Length != 4 || !int.TryParse(args[1], out port) 
			|| !int.TryParse(args[2], out low) || !int.TryParse(args[3], out high))
			{
				Console.WriteLine("[INFO] Usage: TreeShareServer <address> <port> <low> <high>");

			}

			try
			{
				var server = new Server(args[0], port, low, high);
				server.Start();
			}
			catch(ArgumentException e)
			{
				Console.WriteLine("[ERROR] Argument exception: {0}", e.Message);
			}
		}
	}
}

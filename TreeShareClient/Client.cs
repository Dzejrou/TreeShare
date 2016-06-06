using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using TreeShare.DB;
using TreeShare.Network;
using TreeShare.Utils;

namespace TreeShare
{
	/// <summary>
	/// Represents a client that synchronizes it's tracked
	/// directory tree with the TreeShare server.
	/// </summary>
	sealed class Client
	{
		/// <summary>
		/// Name of the tracked directory name.
		/// </summary>
		public string TrackedDirectory { get; set; }

		/// <summary>
		/// Period (in seconds) between file checks.
		/// </summary>
		public int CheckPeriod = 10;

		/// <summary>
		/// Database that stores file info.
		/// </summary>
		private ClientDatabase db = new ClientDatabase();

		/// <summary>
		/// Timer used to periodically check all files.
		/// </summary>
		private Timer checkTimer;

		/// <summary>
		/// List of files that have changed since the last check.
		/// </summary>
		private List<string> filesChanged = new List<string>();

		/// <summary>
		/// List of files that have been created since the last check.
		/// </summary>
		private List<string> filesCreated = new List<string>();

		/// <summary>
		/// List of files that have been deleted since the last check
		/// </summary>
		private List<string> filesDeleted = new List<string>();

		/// <summary>
		/// List of files that have been checked in the last check.
		/// </summary>
		private List<string> filesChecked = new List<string>();

		/// <summary>
		/// File containing client configuration info.
		/// </summary>
		private readonly string configFile = "client.conf";

		/// <summary>
		/// File containing user login credentials.
		/// </summary>
		private readonly string userData = "user.conf";

		/// <summary>
		/// File containing ignored file endings.
		/// </summary>
		private readonly string ignoredEndingsFile = "ignored.conf";

		/// <summary>
		/// Address that the server listens on.
		/// </summary>
		private string serverAddress;

		/// <summary>
		/// Port that the server listens on.
		/// </summary>
		private int serverPort;

		/// <summary>
		/// Port that the client listens on.
		/// </summary>
		private int listenPort;

		/// <summary>
		/// Listener used to accept connections from the server.
		/// </summary>
		private TcpListener tcpListener;

		/// <summary>
		/// If true, the userData file will be ignored and console
		/// login will be required.
		/// </summary>
		private bool forceManualAuthentization;

		/// <summary>
		/// True if this client can create new files.
		/// </summary>
		private bool canCreateFiles;

		/// <summary>
		/// If true, the console will be hidden.
		/// </summary>
		private bool daemonize;

		/// <summary>
		/// Name of the user.
		/// </summary>
		private string name;

		/// <summary>
		/// Hash of the users password.
		/// </summary>
		private string passwordHash;

		/// <summary>
		/// List of ignored file name endings.
		/// </summary>
		private List<string> ignoredNameEndings;

		/// <summary>
		/// Prefix that should be prepended to the TrackedDirectory
		/// variable (used to track sub trees).
		/// </summary>
		private string subTreePrefix;

		/// <summary>
		/// Encoding used for communication with the server.
		/// </summary>
		private readonly Encoding encoding = Encoding.Default;

		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="a">Address of the server.</param>
		/// <param name="p">Port of the server.</param>
		public Client(string a, int p)
		{
			serverAddress = a;
			serverPort = p;
			forceManualAuthentization = false;
			canCreateFiles = true;
			daemonize = false;
			ignoredNameEndings = new List<string>();
			subTreePrefix = "";
		}

		/// <summary>
		/// Starts the client and causes it to periodically
		/// check files and listen to the server announcements.
		/// </summary>
		public void Start()
		{
			if(!Authenticate())
			{
				Console.WriteLine("[ERROR] Could not log into the server, exiting.");
				return;
			}
			else
				Console.WriteLine("[INFO] Login successful.");

			// Hide the console if needed.
			if(daemonize)
				ConsoleManager.HideConsole();

			if(!System.IO.File.Exists(db.FileSaveFile))
				CheckDirectory(TrackedDirectory);
			else
				db.Load();

			tcpListener = new TcpListener(IPAddress.Any, listenPort);
			tcpListener.Start();
			PerformInitialRequest();
			checkTimer = new Timer(PerformCheck, null, CheckPeriod, CheckPeriod);

			try
			{
				while(true)
				{ // Wait for updates from the server.
					Socket s = tcpListener.AcceptSocket();
					HandleServerNotice(s);
					s.Close();
				}
			}
			catch(SocketException e)
			{
				Console.WriteLine("[ERROR] Incoming communication failed: {0}", e.Message);
			}
			finally
			{ // Just a hack to ensure the port is released on exit.
				if(tcpListener != null)
					tcpListener.Stop();
			}
		}

		/// <summary>
		/// Handles a notification from the server (mostly
		/// regardin file changes).
		/// </summary>
		/// <param name="s">Socket used for the communication.</param>
		private void HandleServerNotice(Socket s)
		{
			try
			{
				using(var stream = new NetworkStream(s))
				using(var reader = new StreamReader(stream, encoding))
				using(var writer = new StreamWriter(stream, encoding))
				{
					writer.AutoFlush = true;
					//reader.BaseStream.ReadTimeout = 10000;

					var message = ProtocolHelper.ExtractProtocol(reader.ReadLine());
					string name = reader.ReadLine();
					if(name.StartsWith(subTreePrefix + TrackedDirectory + @"\"))
					{
						name = name.Substring(subTreePrefix.Length);

						if(message != Protocol.FILE_DELETED)
						{
							writer.WriteLine(Protocol.SUCCESS);
						}
						else
							reader.ReadLine(); // T_E token, can be ignored here.
						Console.WriteLine("ACCEPTED {0}, does start with {1}", name, subTreePrefix + TrackedDirectory + @"\");
					}
					else
					{
						if(message != Protocol.FILE_DELETED)
						{
							writer.WriteLine(Protocol.FAIL);
						}
						else
							reader.ReadLine(); // T_E token, can be ignored here.
						writer.WriteLine(Protocol.TRANSMISSION_END);
						Console.WriteLine("DENIED {0}, does not start with {1}", name, subTreePrefix + TrackedDirectory + @"\");
						return;
					}

					if(message == Protocol.NONE || name == null)
						return;

					DB.File file = null;
					if(message == Protocol.FILE_CREATED ||
					  (name != null && !db.GetFiles().TryGet(name, out file) && message != Protocol.FILE_DELETED))
					{
						file = new DB.File(name);
						file.DateModified = FileHelper.Create(file.Name);
						if(file.DateModified != DateTime.MaxValue)
							db.GetFiles().Add(file);
					}

					if(message == Protocol.FILE_DELETED)
					{
						FileHelper.Delete(name);
						if(file != null)
						{
							db.GetFiles().Remove(file);
						}
						reader.ReadLine(); // T_E token, can be ignored here.
					}
					else
					{
						lock (file)
						{
							string tmpName = name + ".tmp";
							try
							{
								string line;
								using(var fstream = System.IO.File.OpenWrite(tmpName))
								using(var fileWriter = new StreamWriter(fstream, encoding))
								{
									while((line = reader.ReadLine()) != null && line != "TRANSMISSION_END")
										fileWriter.Write(line);
									fileWriter.BaseStream.SetLength(fileWriter.BaseStream.Position);

									if(line != "TRANSMISSION_END")
									{
										// TODO:
										Console.WriteLine("NO TRANSMISSION END!");
									}
								}

								FileHelper.Move(tmpName, file.Name);
								file.DateModified = System.IO.File.GetLastWriteTime(file.Name);
							}
							catch(IOException e)
							{
								Console.WriteLine(e);
								// TODO:
								FileHelper.Delete(tmpName);
							}
						}
					}
				}
			}
			catch(IOException e)
			{
				// TODO:
				Console.WriteLine("HUH? {0}", e.Message);
			}
			finally
			{
				db.Save();
			}
		}

		/// <summary>
		/// Performs initial authentication either from config file, or
		/// (if it's missing or manual authentication is forced) from
		/// the console (and saves info on success).
		/// </summary>
		/// <returns>True if authentication succeeded, false otherwise.</returns>
		private bool Authenticate()
		{
			bool register = false;
			bool consoleAuthentication = false;
			if(forceManualAuthentization || !System.IO.File.Exists(userData))
			{ // Console authentication.
				consoleAuthentication = true;
				register = GetCredentialsFromConsole(out name, out passwordHash);
			}
			else
			{ // Saved authentication.
				using(var stream = System.IO.File.OpenRead(userData))
				using(var reader = new StreamReader(stream))
				{
					name = reader.ReadLine();
					passwordHash = reader.ReadLine();
				}
			}

			bool authenticated = false; // TODO: Handle Socket Exception below.
			using(var client = ConnectToServer())
			using(var stream = client.GetStream())
			using(var writer = new StreamWriter(stream, encoding))
			using(var reader = new StreamReader(stream, encoding))
			{ // Actual authentication communication.
				writer.AutoFlush = true;
				writer.WriteLine(register ? Protocol.REGISTER : Protocol.AUTHENTICATE);
				writer.WriteLine(name);
				writer.WriteLine(passwordHash);

				Protocol response = ProtocolHelper.ExtractProtocol(reader.ReadLine());
				authenticated = (response == Protocol.SUCCESS);

				if(authenticated)
				{
					writer.WriteLine(Protocol.NEW_CONNECTION);
					writer.WriteLine(listenPort);
					writer.WriteLine(Protocol.TRANSMISSION_END);
				}
			}

			if(consoleAuthentication && authenticated)
			{ // Save the credentials for later use.
				using(var stream = System.IO.File.Open(userData, FileMode.Create))
				using(var writer = new StreamWriter(stream, encoding))
				{
					writer.WriteLine(name);
					writer.WriteLine(passwordHash);
					writer.WriteLine(listenPort);
				}
			}

			return authenticated;
		}

		/// <summary>
		///	Asks the user (via console) if he wants to register or login,
		///	then prompts for login/registration credentials.
		/// </summary>
		/// <param name="name">User name, will be assigned to.</param>
		/// <param name="password">Password, will be assigned to.</param>
		/// <returns>True if registration is needed, false otherwise.</returns>
		private bool GetCredentialsFromConsole(out string name, out string password)
		{
			string input;
			bool correctInput = false;
			bool register = false;
			name = null;
			password = null;

			while(!correctInput)
			{
				Console.WriteLine("[ERROR] User data not found, choose an option:\n\t[1] Login\n\t[2] Register");
				Console.Write("[>] ");
				input = Console.ReadLine();
				switch(input)
				{
					case "1":
					case "[1]":
						register = false;
						correctInput = true;
						break;
					case "2":
					case "[2]":
						register = true;
						correctInput = true;
						break;
					default:
						Console.WriteLine("[ERROR] Wrong input, please try again.\n");
						continue;
				}

				Console.Write("[USERNAME] ");
				name = Console.ReadLine();
				Console.Write("[PASSWORD] ");
				password = ConsoleManager.GetPassword();
				password = Hasher.CreatePasswordHash(password);
			}
			return register;
		}

		/// <summary>
		/// Checks the tracked directory for changes and
		/// notifies the server if necessary.
		/// </summary>
		/// <param name="state">State required by the timer, not used.</param>
		public void PerformCheck(object state)
		{
			CheckDirectory(TrackedDirectory);
			InformServer();
			db.Save();
		}

		/// <summary>
		/// Checks a given directory for file changes.
		/// </summary>
		/// <param name="dir">Path to the directory.</param>
		public void CheckDirectory(string dir)
		{
			try
			{
				Directory.CreateDirectory(dir);
				foreach(var f in Directory.GetFiles(dir))
					HandleFile(f);

				foreach(var d in Directory.GetDirectories(dir))
				{
					foreach(var f in Directory.GetFiles(d))
						HandleFile(f);
					CheckDirectory(d);
				}
			}
			catch(IOException e)
			{
				Console.WriteLine("[ERROR] IOException during directory check: {0}", e.Message);
			}
		}

		/// <summary>
		/// Checks a single file and handles changes if
		/// any occured (modification, creation).
		/// </summary>
		/// <param name="file">Path to the file.</param>
		private void HandleFile(string file)
		{
			// Without this, used files would be treated as deleted.
			if(FileHelper.FileInUse(file))
			{
				filesChecked.Add(file);
				return;
			}

			if(!System.IO.File.Exists(file) || FileHelper.FileInUse(file) ||
				file.EndsWith(".tmp") || filesChecked.Contains(file) || Ignored(file))
				return;
			filesChecked.Add(file);

			DB.File tmp = null;
			var files = db.GetFiles();
			var date = System.IO.File.GetLastWriteTime(file);
			if(!files.TryGet(file, out tmp) && canCreateFiles)
			{
				Console.WriteLine("[INFO] New file found: {0}", file);
				var f = new DB.File();
				f.Name = file;
				f.DateModified = date;
				f.Access = null;
				files.Add(f);
				filesCreated.Add(file);
			}
			else if(tmp != null && tmp.OlderThan(date))
			{
				Console.WriteLine("[INFO] Newer version of {0} found.", file);
				tmp.Update(date);
				filesChanged.Add(file);
			}
		}

		/// <summary>
		/// Clears all file lists (changed, deleted etc.).
		/// </summary>
		private void ClearLists()
		{
			lock(filesCreated)
				filesCreated.Clear();
			lock(filesChanged)
				filesChanged.Clear();
			lock(filesDeleted)
				filesDeleted.Clear();
			lock(filesChecked)
				filesChecked.Clear();
		}

		/// <summary>
		/// Informs server about all changes found during the last
		/// directory checks.
		/// </summary>
		private void InformServer()
		{
			if((filesCreated.Count == 0 || !canCreateFiles) &&
			   filesChecked.Count == db.GetFiles().Count && filesChanged.Count == 0)
			{
				// If checked == db files, we still need to clear the checked files otherwise deletes would be ignored
				// next time we check.
				lock(filesChecked)
				{
					if(filesChecked.Count > 0)
						filesChecked.Clear();
				}
				return;
			}

			try
			{
				var client = ConnectToServer();
				var files = db.GetFiles();
				using(var stream = client.GetStream())
				using(var writer = new StreamWriter(stream, encoding))
				using(var reader = new StreamReader(stream, encoding))
				{
					writer.AutoFlush = true;

					// Authentication of this session.
					writer.WriteLine(Protocol.AUTHENTICATE);
					writer.WriteLine(name);
					writer.WriteLine(passwordHash);
					if(ProtocolHelper.ExtractProtocol(reader.ReadLine()) != Protocol.SUCCESS)
					{
						writer.WriteLine(Protocol.TRANSMISSION_END);
						Console.WriteLine("[ERROR] Refused because of wrong credentials.");
						ClearLists();
						return;
					}

					lock(filesCreated)
					{
						bool removeCreatedFiles = false;
						foreach(var file in filesCreated)
						{
							writer.WriteLine(Protocol.FILE_CREATED);
							writer.WriteLine(subTreePrefix + file);
							if(ProtocolHelper.ExtractProtocol(reader.ReadLine()) == Protocol.SUCCESS)
								SendFileContents(writer, file);
							else
							{
								writer.WriteLine(Protocol.TRANSMISSION_END);
								canCreateFiles = false;
								removeCreatedFiles = true;
								break;
							}
						}
						if(removeCreatedFiles)
						{ // This removes those recently tracked and canCreateFiles == false will prevent new tracking.
							foreach(var file in filesCreated)
								files.Remove(file);
						}
						filesCreated.Clear();
					}

					lock(filesChanged)
					{
						foreach(var file in filesChanged)
						{
							writer.WriteLine(Protocol.FILE_CHANGED);
							writer.WriteLine(subTreePrefix + file);
							if(ProtocolHelper.ExtractProtocol(reader.ReadLine()) == Protocol.SUCCESS)
								SendFileContents(writer, file);
							else
								files.Remove(file); // Leave it physically on the disk, but do not track it.
						}
						filesChanged.Clear();
					}

					lock(filesDeleted)
					{
						filesDeleted = db.GetFiles().GetDictionary().Keys.ToList().Except(filesChecked).ToList();

						lock (filesChecked)
						{
							filesChecked.Clear();
						}
						foreach(var file in filesDeleted)
						{
							writer.WriteLine(Protocol.FILE_DELETED);
							writer.WriteLine(subTreePrefix + file);
							db.GetFiles().Remove(file);

							/*
							 * Note: This is either SUCCESS or FAIL (depending of authorization).
							 *       Currently is ignored, assuming the user just wants to get rid of the file,
							 *       but it may be useful to add a config option to request the data of deleted
							 *       files to recover them if the deletion fails on the server.
							 */
							reader.ReadLine();
						}
						filesDeleted.Clear();
					}

					writer.WriteLine(Protocol.TRANSMISSION_END);
				}
			}
			catch(IOException e)
			{
				Console.WriteLine("[ERROR] Cannot inform the server: {0}", e.Message);
				ClearLists();
			}
			finally
			{
				db.Save();
			}
		}

		/// <summary>
		/// Sends the contents of a file to the server.
		/// </summary>
		/// <param name="writer">StreamWriter used to send the file.</param>
		/// <param name="file">Path to the file.</param>
		private void SendFileContents(StreamWriter writer, string file)
		{
			try
			{
				using(var stream = System.IO.File.OpenRead(file))
				using(var reader = new StreamReader(stream, encoding))
				{
					string line;
					while((line = reader.ReadLine()) != null)
						if(line != "") writer.WriteLine(line);
					writer.WriteLine(Protocol.TRANSMISSION_END);
				}
			}
			catch(IOException e)
			{
				Console.WriteLine("[ERROR] Cannot send file contents: {0}", e.Message);
			}
		}

		/// <summary>
		/// Requests (and reads if authorized) contents of a given file from
		/// the server.
		/// </summary>
		/// <param name="reader">StreamReader used to read authorization result and the contents of the file.</param>
		/// <param name="writer">StreamWriter used to send the request.</param>
		/// <param name="file">Path to the file.</param>
		private void RequestFileContents(StreamReader reader, StreamWriter writer, string file)
		{
			writer.WriteLine(Protocol.REQUEST_FILE_CONTENTS);
			writer.WriteLine(subTreePrefix + file);
			string line = reader.ReadLine();
			string tmpFile = file + ".tmp";

			if(ProtocolHelper.ExtractProtocol(line) == Protocol.SUCCESS)
			{
				DB.File tmp = null;
				if(!db.GetFiles().TryGet(file, out tmp))
				{
					tmp = new DB.File(file);
					db.GetFiles().Add(tmp);
				}

				try
				{
					reader.ReadLine(); // File name resent, here we can discard it.
					writer.WriteLine(Protocol.SUCCESS);

					Directory.CreateDirectory(Path.GetDirectoryName(file));

					using(var stream = System.IO.File.OpenWrite(tmpFile))
					using(var fileWriter = new StreamWriter(stream, encoding))
					{
						while((line = reader.ReadLine()) != null && line != "TRANSMISSION_END" && line != "FAIL")
							fileWriter.WriteLine(line);
					}

					FileHelper.Move(tmpFile, file);
					tmp.DateModified = System.IO.File.GetLastWriteTime(file);
				}
				catch(SocketException)
				{ // This should be connection interruption or time out.
					FileHelper.Delete(tmpFile);
				}
				catch(IOException)
				{ // Target file in use or nonexistent.
					FileHelper.Delete(tmpFile);
				}
			}
		}

		/// <summary>
		/// Checks if a file is ignored (based on its name's ending).
		/// </summary>
		/// <param name="file">Path to the file.</param>
		/// <returns>True if the file is ignored, false otherwise.</returns>
		private bool Ignored(string file)
		{
			foreach(var ending in ignoredNameEndings)
			{
				if(file.EndsWith(ending))
					return true;
			}
			return false;
		}

		/// <summary>
		/// Loads the configuration for this client. See documentation
		/// for the config options.
		/// </summary>
		/// <returns>True if the load was successful, false otherwise.</returns>
		public bool LoadConfig()
		{
			try
			{
				using(var stream = System.IO.File.OpenRead(configFile))
				using(var reader = new StreamReader(stream))
				{
					string line = null;
					while((line = reader.ReadLine()) != null)
					{
						var tokens = line.Split(new char[] { '=' }, StringSplitOptions.RemoveEmptyEntries);

						if(tokens.Length != 2)
							return false;
						switch(tokens[0])
						{
							case "TrackedDirectory":
								TrackedDirectory = tokens[1];
								break;
							case "CheckPeriod":
								if(!int.TryParse(tokens[1], out CheckPeriod))
									return false;
								break;
							case "ListenPort":
								if(!int.TryParse(tokens[1], out listenPort))
									return false;
								break;
							case "ForceManualAuthentization":
								if(tokens[1] == "1") // Avoid parsing.
									forceManualAuthentization = true;
								break;
							case "Daemonize":
								if(tokens[1] == "1")
									daemonize = true;
								break;
							case "SubTreePrefix":
								subTreePrefix = tokens[1];
								break;
							default:
								Console.WriteLine("[ERROR] Invalid config option detected: {0}", tokens[0]);
								break;
						}
					}
				}

				using(var stream = System.IO.File.OpenRead(ignoredEndingsFile))
				using(var reader = new StreamReader(stream, encoding))
				{
					string line = null;
					while((line = reader.ReadLine()) != null)
						ignoredNameEndings.Add(line);
				}
			}
			catch(IOException e)
			{
				Console.WriteLine("[ERROR] IOException during config load: {0}", e.Message);
				return false;
			}
			return true;
		}

		/// <summary>
		/// Asks the server for information (modification time) about all files
		/// this client has read access to and requests contents if necessary.
		/// </summary>
		private void PerformInitialRequest()
		{
			var filesToRequest = new List<string>();
			var filesOnServer = new List<string>();
			try
			{
				var client = ConnectToServer();
				using(var stream = client.GetStream())
				using(var reader = new StreamReader(stream, encoding))
				using(var writer = new StreamWriter(stream, encoding))
				{
					writer.AutoFlush = true;
					reader.BaseStream.ReadTimeout = 10000;
					string line;
					string fileName;
					DB.File file;
					Protocol message;

					writer.WriteLine(Protocol.AUTHENTICATE);
					writer.WriteLine(name);
					writer.WriteLine(passwordHash);
					line = reader.ReadLine();
					message = ProtocolHelper.ExtractProtocol(line);
					if(message != Protocol.SUCCESS)
						return;

					writer.WriteLine(Protocol.REQUEST_INITIAL_INFO);
					line = reader.ReadLine();
					message = ProtocolHelper.ExtractProtocol(line);

					if(message == Protocol.SUCCESS)
					{
						var files = db.GetFiles();
						while(true)
						{
							fileName = reader.ReadLine();

							if(fileName.StartsWith(subTreePrefix + TrackedDirectory + @"\"))
								fileName = fileName.Substring(subTreePrefix.Length);
							else if(fileName == "TRANSMISSION_END")
								break;
							else
							{
								reader.ReadLine(); // Skip modification time.
								continue;
							}
							filesOnServer.Add(fileName);

							line = reader.ReadLine();

							if(!files.TryGet(fileName, out file) || file.OlderThan(DateTime.Parse(line)))
								filesToRequest.Add(fileName);
						}

						foreach(var f in filesToRequest)
							RequestFileContents(reader, writer, f);
					}
					writer.WriteLine(Protocol.TRANSMISSION_END);
					client.Close();

					var filesToDelete = db.GetFiles().GetDictionary().Keys.ToList().Except(filesOnServer);
					foreach(var f in filesToDelete)
					{
						db.GetFiles().Remove(f);
						FileHelper.Delete(f);
					}
				}
			}
			catch(SocketException)
			{ /* Time out. */ }
			catch(IOException)
			{ /* File probably already deleted as RequestFileContents does not throw. */ }
			finally
			{
				db.Save();
			}
		}

		/// <summary>
		/// Connects to server and receives new port from it. Immediately
		/// after connects to the new assigned port.
		/// </summary>
		/// <returns>TcpClient that can be used for communication with the client.</returns>
		private TcpClient ConnectToServer()
		{
			var client = new TcpClient(serverAddress, serverPort);
			using(var reader = new StreamReader(client.GetStream(), encoding))
			{
				string msg = reader.ReadLine(); // New port.
				int newPort;
				if(!int.TryParse(msg, out newPort))
					throw new Exception("Server didn't sent new port!");

				var tmp = new TcpClient(serverAddress, newPort);
				client.Close();
				client = tmp;

			}
			return client;
		}

		/// <summary>
		/// Entry point of the program. Initializes and
		/// starts the client.
		/// Usage: TreeShareClient [ServerAddress] [ServerPort]
		/// </summary>
		/// <param name="args">Command line arguments.</param>
		static void Main(string[] args)
		{
			int port;
			if(args.Length != 2 || !int.TryParse(args[1], out port))
			{
				Console.WriteLine("Usage: TreeShareClient <ServerAddress> <ServerPort>");
				return;
			}

			var client = new Client(args[0], port);
			if(!client.LoadConfig())
			{
				Console.WriteLine("[ERROR] Could not load config, exiting.");
				return;
			}

			try
			{
				client.Start();
			}
			catch(SocketException)
			{
				Console.WriteLine("[ERROR] Cannot connect to a server at {0}:{1}", args[0], port);
			}
		}
	}
}

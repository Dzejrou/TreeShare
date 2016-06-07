--------------------------------------------------------------------------------
------------------------ TreeShare FileSharing Program  ------------------------
------------------------          By Dzejrou            ------------------------
--------------------------------------------------------------------------------
Table of contents:
	1. About
        2. User's documentation
		2.1 Server
		2.2 Client
	3. Developer's documentation
		3.1 Database
		3.2 Server
		3.3 Client
		3.4 Protocol

------------
- 1. About -
------------
	TreeShare is a file sharing program inspired by Dropbox. It allows
	its users to set up a server keeping central file repository which
	they can connect to via clients. Each client can 'mount' the top
	directory of the tracked directory tree or any other sub directory
	in the hierarchy. Any updates to the files (such as creation,
	modification and deletion) will then be distributed through the
	server to all other clients, which mirror the changes.

	Individual clients can be assigned to groups, which can have set
	access rights to different files, allowing the prevention of unwanted
	modification from individuals (i.e. one man groups) or entire groups.

---------------------------
- 2. User's documentation -
---------------------------
- 2.1 Server --------------
---------------------------
	Directory Layout:
	The server contains a directory called 'dir' within the same directory
	as the executable, this directory (and his sub directories) get shared
	with the clients. A second directory, called 'backup' gets created by
	the server and stores a backup of any file that gets changed or
	deleted (mimicking the hierarchy in 'dir').
	
	Startup:
	The server can be run from the command line using the following
	command 'TreeShareServer.exe <addr> <port> <port-lb> <port-ub>'
	with arguments:
		<addr> Address the server listens on.
		<port> Port the server listens on.
		<port-lb> Lower bound of the port pool.
		<port-ub> Upper bound of the port pool.

	Port pool:
	The port pool is a closed interval [port-lb, port-ub], values of which
	will get used to redirect clients to (allowing 1 client/port
	communication). This interval should have high enough numbers to not
	interfere with the 'well-known ports' range of 0-1023 and any other
	commonly used port numbers outside this range. (The port pool
	should be above ten thousand.)

	Groups:
	Every group can have multiple users that share access rights to files.
	When a file is created, for every group the group's default access
	right is assigned to it. When a new user is created, they are
	assigned to the group 'default', that has a default right of NONE
	and cannot create files. The user can then be reassigned to a
	different group from the console by the admin.

	Access rights:
	The possible rights a group can have to any given file are NONE, WRITE,
	READ and READ_WRITE (which behave like flags, so adding WRITE to a
	group that only has READ will result in a READ_WRITE access righ).
	When adding/removing a right through the console, these have to be
	spelled correctly for the server to recognise them.
	
	Console I/O:
	After launch, the console window can be used to pass commands directly
	to the server and with their help manipulate files, users or groups.
	The administrator can use following commands (sans the | ):
	(Note: <path> is relative to the executable, so it starts
	       with 'dir\'.)
	
	| exit
	Saves the database and shuts down the server.

	| hide <time>
	Hides the console for <time> seconds. (<seconds> <= 0 will cause
	the console to disappear for the rest of the session.)

	| group-create <name> <right> <create>
	Creates a new group with the name <name>, default access right to
	files <right> and if <create> is 'true', the ability to create
	new files.

	| group-add <user> <group>
	Adds (or moves) a user with the name <user> to a group with the name
	<group>.

	| user-add <name>
	Creates a new user with the name <name> in the 'default' group.

	| file-add <path>
	Adds a new (already existing, but untracked) file to the databse and
	informs all users about its creation.

	| add-right <path> <group> <right>
	Adds a new right <right> to the group <group> for the file located
	at <path>.

	| remove-right <path> <group> <right>
	Remoes a right <right> from the group <group> for the file located at
	<path>.

	| file-inform <path>
	Informs all clients about a change in the file located at <path>.

	| file-delete <path>
	Deletes the file located at <path> and removes it form the database,
	then informs all clients about its deletion.

	| save-db
	Saves the current state of the database.

	| load-db
	Loads a saved state of the database.

---------------------------
- 2.2 Client --------------
---------------------------
	Configuration:
	The client loads its configuration on startup from the file
	'client.conf', this file can contain entries in the form of
	<key>=<value> which can have keys (in no particular order):
	(sans the | )

	| TrackedDirectory
	The value for this option is the name of the directory that is being
	synchronized (this can be the root 'dir' directory or any of its
	sub directories, but to mount a sub directory, the option
	SubTreePrefix has to be specified, see below).
	(Note: This value is without quotes as used for 'dir' above.)

	| SubTreePrefix
	This prefix gets prepended to files when communicating with the
	and has to be used if we only mount a sub directory (if
	TrackedDirectory is 'dir' without the quotes).
	Example: If we mount dir\subdir1\subdir2 then TrackedDirectory
	         is 'subdir2' and SubTreePrefix is 'dir\subdir1\' without
		 the quotes.

	| CheckPeriod
	The value for this option is the number of miliseconds between
	the periodic checks that are performed to find changes in the
	synchronized files.

	| ListenPort
	The value for this option determines which port does this client
	listen on for server announcements (sent to the server automatically,
	this allows multiple clients on one machine).

	| ForceManualAuthentization
	If the value for this option is equal to '1' (sans the quotes),
	the client will require the user to enter his login credentials
	through the console regardless of the fact if it finds these
	credentials in the user configuration file (generated after
	registration).

	| Daemonize
	If the value for this option is 1, the client will hide the console
	window upon startup and will run as a daemon.
	Note: This option does not work with manual authentization, so should
	      not be used on first run of the client application (which
	      in most cases requires registration through the console) or
	      when the ForceManualAuthentization option is set.

	Directory layout:
	In the directory where the TreeShareClient.exe file is located,
	a directory with the name equal to the value of the TrackedDirectory
	option will be created once the client is run. This directory
	will contain all the synchronized files.

	Ignored file endings:
	In the file 'ignored.conf', the user can specify which file name endings
	will be ignored by the client (one ending per line). To ignore an
	entire file name, simply write it as an ending, since the files
	are checked if they end with the strings contained in this file during
	checks.

	Startup:
	The client can be run from the command line using the command
	'TreeShareClient.exe <addr> <port>' with arguments:
		<addr> Address of the server.
		<port> Port the server listens on.
	Upon the start of the application, it will check for a 'user.conf'
	file (which contains user name and a hashed password). If this file
	is not found, the user will be prompted to either register or
	login and for their credentials (which then get saved into the
	'user.conf' file). This prompt will also appear whenever the
	ForceManualAuthentization option is set in the client config
	file.

---------------------------
- 3. Dev's documentation --
---------------------------
	Note: Due to the nature of this application only a brief description
	      is given here about the individual server and client classes,
	      while rest of their documentation can be found in the auto
	      generated documentation.
	      This is because the main part of the program that needs
	      to be documented is the communication protocol, which has
	      been moved to a separate part (3.4).

---------------------------
- 3.1 Database ------------
---------------------------
	The database holds information about files, users and groups
	(ServerDatabase) or about files (ClientDatabase) and provides
	auxiliary functions for data management.
	Both of these database classes inherit from the abstract class
	SerializableDatabase, which provides a function to serialize
	a DatabaseTable instance and two abstract functions for saving
	and loading, which the derived database classes override and fill
	with serializing steps (for their different tables).
	More information about the functions of these and other classes
	as well as about their public and private fields can be found
	in the auto generated documentation.

---------------------------
- 3.2 Server --------------
---------------------------
	The server has two primary threads (+ the main thread, which sleeps
	until a shutdown has been initiated from the console). One of these
	threads serves for console I/O, through which the user can issue
	commands such as user/group/file management and shutdown
	(for all available commands see user's documentation).
	The second thread waits for incoming client connections and spawns
	a task for each and reassigns the client to a new port from its
	port pool.
	More information about the function of this class as well as about
	its public and private fields can be found in the auto generated
	documentation.

---------------------------
- 3.3 Client --------------
---------------------------
	The client has a single primary thread that loops indefinetly
	accepting incoming server connections, which then handles. Alongside
	this main thread a timer is created that asynchronously starts
	periodic checks, during which it scans the tracked directory for
	file updates and then informs the server if necessary.
	More information about the function of this class as well as about
	its public and private fields can be found in the auto generated
	documentation.


---------------------------
- 3.4 Protocol ------------
---------------------------
	This is an overview of the program's communication protocol, showing
	possible messages sent between the server and its clients and their
	expected responses.
	Note that whenever a file path is being sent, it is always relative
	to the location of the server executable and thus starting with
	the 'dir' directory. The clients must make sure to prepend the
	file paths with their <SubTreePrefix> when sending them to the
	server and with <SubTreePrefix + TrackedDirectory + '\'> when
	checking if the file belongs to their subtree.
	The messages that can be sent are as follows:

	| AUTHENTICATE & REGISTER
	These messages are the only possible openings of a communication
	between the server and a client , as they
	cause the server to retrieve the client's user entry in the
	database, which is then used for file access authorization.
	The only difference between AUTHENTICATE and REGISTER is that
	when REGISTER is sent, new user account gets created and the
	communication will result in a FAIL if the user's name is taken
	already.
	[CLIENT]			[SERVER]
	AUTHENTICATE/REGISTER
	<name>
	<pass-hash>
					SUCCESS/FAIL

	| NEW_CONNECTION
	This is the message a client sends to the server after startup once
	it has been initially authenticated. It will tell the server the
	client is now ready to accept its announcements and also which
	port they will listen on for them. (Note: No response.)
	[CLIENT]			[SERVER]
	NEW_CONNECTION
	<port>

	| REQUEST_INITIAL_INFO
	This is the message a client sends to the server after startup once
	they have been authenticated and sent their listening port numbers.
	It requests all files the client has READ access to and their times
	of last modification, which the clients uses to choose files they
	need updated. In case the response to this message is FAIL or
	the server finnishes sending file info to the client, the
	connection remains open and needs to be closed by the message
	TRANSMISSION_END (from client, see below) or the client can send another
	message (like REQUEST_FILE_CONTENTS for the files he needs).
	[CLIENT]			[SERVER]
	REQUEST_INITIAL_INFO
					SUCCESS/FAIL
					<file_1>
					<time_1>
					<file_2>
					<time_2>
					  ...
					<file_n>
					<time_n>
					TRANSMISSION_END

	| REQUEST_FILE_CONTENTS
	This message can be sent by a client to request the contents of
	a file it has READ access to.
	[CLIENT]			[SERVER]
	REQUEST_FILE_CONTENTS
	<file>
					SUCCESS/FAIL
					<file>
	SUCCESS/FAIL
					<file contents>
					TRANSMISSION_END

	| FILE_CHANGED, FILE_CREATED, FILE_DELETE
	These messages are used to notify about the change in the contents
	of a file (newer version, new file, deleted file) and can be sent
	both client->server and	server->client. Note that in the case of
	FILE_DELETED, the communication ends once <file> is sent and
	acknowledged by SUCCESS or FAIL.
	> Client->Server:
	[CLIENT]			[SERVER]
	FILE_CHANGED/DELETE/CREATED
	<file>
					SUCCESS/FAIL
	<file contents>
	TRANSMISSION_END

	> Server->Client: (Differs for FILE_DELETED.)
	[CLIENT]			[SERVER]
					FILE_CHANGED/CREATED
					<file>
	SUCCESS/FAIL+T_E
					<file contents>
					TRANSMISSION_END
	
	> Server->Client: (FILE_DELETED variant.)
	[CLIENT]			[SERVER]
					FILE_DELETED
					<file>
					TRANSMISSION_END

	| TRANSMISSION_END
	Serves to signal an end of a connection or a connection segment
	(e.g. sending file contents).
	This message should end all connections to the server.


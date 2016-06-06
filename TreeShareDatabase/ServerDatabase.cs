using System;
using TreeShare.Utils;

namespace TreeShare.DB
{
	/// <summary>
	/// Specialization of the database, used by the server. Keeps track
	/// of files, users and groups.
	/// </summary>
	public sealed class ServerDatabase : SerializableDatabase
	{
		/// <summary>
		/// Table keeping track of files. 
		/// </summary>
		private DatabaseTable<File> files = new DatabaseTable<File>();

		/// <summary>
		/// Table keeping track of users.
		/// </summary>
		private DatabaseTable<User> users = new DatabaseTable<User>();

		/// <summary>
		/// Table keeping track of groups.
		/// </summary>
		private DatabaseTable<Group> groups = new DatabaseTable<Group>();

		/// <summary>
		/// Serialization target for the user table.
		/// </summary>
		public readonly string UserSaveFile = "users.xml";

		/// <summary>
		/// Serialization target for the file table.
		/// </summary>
		public readonly string FileSaveFile = "files.xml";

		/// <summary>
		/// Serialization target for the group table.
		/// </summary>
		public readonly string GroupSaveFile = "groups.xml";

		#region Getters
		/// <summary>
		/// File table getter.
		/// </summary>
		/// <returns>The table containing files.</returns>
		public DatabaseTable<File> GetFiles()
		{
			return files;
		}

		/// <summary>
		/// User table getter.
		/// </summary>
		/// <returns>The table containing users.</returns>
		public DatabaseTable<User> GetUsers()
		{
			return users;
		}

		/// <summary>
		/// Group table getter.
		/// </summary>
		/// <returns>The table containing groups.</returns>
		public DatabaseTable<Group> GetGroups()
		{
			return groups;
		}
		#endregion

		/// <summary>
		/// Reassigns a given user to another group.
		/// </summary>
		/// <param name="userName">Name of the user.</param>
		/// <param name="groupName">Name of the new group.</param>
		public void MoveUserToGroup(string userName, string groupName)
		{
			if(!users.Contains(userName) || !groups.Contains(groupName))
				return;

			var user = users[userName];
			if(user == null)
				return;

			if(user.Group != null && groups.Contains(user.Group))
			{
				var originalGroup = groups[user.Group];
				originalGroup.RemoveUser(user);
			}

			var group = groups[groupName];
			if(group != null)
			{
				user.Group = group.Name;
				group.AddUser(user);
			}
		}

		/// <summary>
		/// Creates a new user (in the 'default' group) using password from the console.
		/// </summary>
		/// <param name="userName">Name of the user.</param>
		public void CreateNewUser(string userName)
		{
			if(users.Contains(userName))
				Console.WriteLine("[ERROR] That user name is already in use.");
			else
			{
				Console.WriteLine("[PASSWORD] ");
				string pwdHash = User.GetSaltedHash(userName, Hasher.CreatePasswordHash(ConsoleManager.GetPassword()));
				User user = new User { Name = userName, Group = "default", PasswordHash = pwdHash };
				users.Add(user);
				Save();
			}
		}

		/// <summary>
		/// Returns true if a file is tracked.
		/// </summary>
		/// <param name="path">Path to the file.</param>
		/// <returns>True if the file is tracked, false otherwise.</returns>
		public bool FileTracked(string path)
		{
			return files.Contains(path);
		}

		/// <summary>
		/// Creates a new entry to the file table if necessary.
		/// </summary>
		/// <param name="path">Path to the file.</param>
		/// <param name="group">Name of the owning group.</param>
		/// <returns>The new entry or the already existing entry if it already existed.</returns>
		public File CreateNewFile(string path, Group group)
		{
			File tmp = null;
			if(files.Contains(path) && files.TryGet(path, out tmp))
				return tmp;

			tmp = new File(path);
			tmp.DateModified = DateTime.Now;
			files.Add(tmp);

			foreach(var g in groups)
			{
				if(g != group)
					tmp.Access.Add(new AccessInfo { Group = g, Right = g.DefaultAccessRight, GroupDummy = g.Name });
				else if(group != null)
					tmp.Access.Add(new AccessInfo { Group = group, Right = AccessRight.READ_WRITE, GroupDummy = g.Name });
			}

			return tmp;
		}

		/// <summary>
		/// Adds an access right to a group for a given file.
		/// </summary>
		/// <param name="path">Path to the file.</param>
		/// <param name="group">Name of the group.</param>
		/// <param name="right">Right to add.</param>
		public void AddRightToGroup(string path, string group, AccessRight right)
		{
			File file = null;
			Group grp = null;
			if(!files.TryGet(path, out file))
				Console.WriteLine("[ERROR] File not tracked or non-existent: {0}", path);
			else if(!groups.TryGet(group, out grp))
				Console.WriteLine("[ERROR] Group non-existent: {0}", group);
			else if(right == AccessRight.NONE)
				file.SetRight(grp, right);
			else
				file.AddRight(grp, right);
		}

		/// <summary>
		/// Remoes and access right from a group for a given file.
		/// </summary>
		/// <param name="path">Path to the file.</param>
		/// <param name="group">Name of the group.</param>
		/// <param name="right">Right to remove.</param>
		public void RemoveRightFromGroup(string path, string group, AccessRight right)
		{
			File file = null;
			Group grp = null;
			if(!files.TryGet(path, out file))
				Console.WriteLine("[ERROR] File not tracked or non-existent: {0}", path);
			else if(!groups.TryGet(group, out grp))
				Console.WriteLine("[ERROR] Group non-existent: {0}", group);
			else
				file.RemoveRight(grp, right);
		}

		/// <summary>
		/// Creates a new group with a given default access right.
		/// </summary>
		/// <param name="name">Name of the group.</param>
		/// <param name="defaultRight">Access right to add to all files.</param>
		/// <param name="canCreateFiles">If true, the group members will be allows to create new files.</param>
		public void CreateNewGroup(string name, AccessRight defaultRight, bool canCreateFiles)
		{
			if(groups.Contains(name))
			{
				Console.WriteLine("[ERROR] Group with the name {0} already exists!", name);
				return;
			}

			Group group = new Group { Name = name, CanCreateFiles = canCreateFiles, DefaultAccessRight = defaultRight };
			groups.Add(group);
			AddAccessRightToSubDirectory(group, defaultRight, "");
		}

		/// <summary>
		/// Adds a given access right to a group for all files in a given
		/// directory (and its sub directories).
		/// </summary>
		/// <param name="group">Name of the group.</param>
		/// <param name="right">Right to add.</param>
		/// <param name="sub">Target directory.</param>
		public void AddAccessRightToSubDirectory(Group group, AccessRight right, string sub)
		{
			foreach(var file in files)
			{
				if(file != null && file.Name.StartsWith(sub))
					file.AddRight(group, right);
			}
		}

		#region Serialization
		/// <summary>
		/// Serializes all tables into XML files.
		/// </summary>
		public override void Save()
		{
			SaveDB(FileSaveFile, files.GetDictionary());
			SaveDB(UserSaveFile, users.GetDictionary());
			SaveDB(GroupSaveFile, groups.GetDictionary());
		}

		/// <summary>
		/// Deserializes all tables from their XML files.
		/// </summary>
		public override void Load()
		{
			// To alow reloading during runtime.
			if(files.Count != 0)
				files = new DatabaseTable<File>(); 
			if(groups.Count != 0)
				groups = new DatabaseTable<Group>(); 
			if(users.Count != 0)
				users = new DatabaseTable<User>(); 

			LoadDB(FileSaveFile, files.GetDictionary());
			LoadDB(UserSaveFile, users.GetDictionary());
			LoadDB(GroupSaveFile, groups.GetDictionary());

			// This avoids redundant serialization of groups.
			Group tmp;
			foreach(var file in files)
			{
				foreach(var access in file.Access)
				{
					if(access.GroupDummy != null && groups.TryGet(access.GroupDummy, out tmp))
						access.Group = tmp;
				}
			}
		}
		#endregion
	}
}

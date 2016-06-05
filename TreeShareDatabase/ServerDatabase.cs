using System;
using TreeShare.Utils;

namespace TreeShare.DB
{
	/// <summary>
	/// 
	/// </summary>
	public sealed class ServerDatabase : SerializableDatabase
	{
		/// <summary>
		/// 
		/// </summary>
		private DatabaseTable<File> files = new DatabaseTable<File>();

		/// <summary>
		/// 
		/// </summary>
		private DatabaseTable<User> users = new DatabaseTable<User>();

		/// <summary>
		/// 
		/// </summary>
		private DatabaseTable<Group> groups = new DatabaseTable<Group>();

		/// <summary>
		/// 
		/// </summary>
		public readonly string UserSaveFile = "users.xml";

		/// <summary>
		/// 
		/// </summary>
		public readonly string FileSaveFile = "files.xml";

		/// <summary>
		/// 
		/// </summary>
		public readonly string GroupSaveFile = "groups.xml";

		#region Getters
		/// <summary>
		/// 
		/// </summary>
		/// <returns></returns>
		public DatabaseTable<File> GetFiles()
		{
			return files;
		}

		/// <summary>
		/// 
		/// </summary>
		/// <returns></returns>
		public DatabaseTable<User> GetUsers()
		{
			return users;
		}

		/// <summary>
		/// 
		/// </summary>
		/// <returns></returns>
		public DatabaseTable<Group> GetGroups()
		{
			return groups;
		}
		#endregion

		/// <summary>
		/// 
		/// </summary>
		/// <param name="userName"></param>
		/// <param name="groupName"></param>
		public void MoveUserToGroup(string userName, string groupName)
		{
			if(!users.Contains(userName) || !groups.Contains(groupName))
				return;

			var user = users[userName] as User;
			if(user == null)
				return;

			if(user.Group != null && groups.Contains(user.Group))
			{
				var originalGroup = groups[user.Group] as Group;
				originalGroup.RemoveUser(user);
			}

			var group = groups[groupName] as Group;
			if(group != null)
			{
				user.Group = group.Name;
				group.AddUser(user);
			}
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="userName"></param>
		public void CreateNewUser(string userName)
		{
			if(users.Contains(userName))
				Console.WriteLine("[ERROR] That user name is already in use.");
			else
			{
				string pwdHash = Hasher.CreatePasswordHash(ConsoleManager.GetPassword());
				User user = new User { Name = userName, Group = "default", PasswordHash = pwdHash };
				users.Add(user);
				Save();
			}
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="path"></param>
		/// <returns></returns>
		public bool FileTracked(string path)
		{
			return files.Contains(path);
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="path"></param>
		/// <param name="group"></param>
		/// <returns></returns>
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
		/// 
		/// </summary>
		/// <param name="path"></param>
		/// <param name="group"></param>
		/// <param name="right"></param>
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
		/// 
		/// </summary>
		/// <param name="path"></param>
		/// <param name="group"></param>
		/// <param name="right"></param>
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
		/// 
		/// </summary>
		/// <param name="name"></param>
		/// <param name="defaultRight"></param>
		/// <param name="canCreateFiles"></param>
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
		/// 
		/// </summary>
		/// <param name="group"></param>
		/// <param name="right"></param>
		/// <param name="sub"></param>
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
		/// 
		/// </summary>
		public override void Save()
		{
			SaveDB(FileSaveFile, files.GetDictionary());
			SaveDB(UserSaveFile, users.GetDictionary());
			SaveDB(GroupSaveFile, groups.GetDictionary());
		}

		/// <summary>
		/// 
		/// </summary>
		public override void Load()
		{
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

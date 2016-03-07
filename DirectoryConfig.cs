﻿namespace Pluton.Core
{
	using System.IO;
	using UnityEngine;
	
	public class DirectoryConfig : Singleton<DirectoryConfig>, ISingleton
	{
		IniParser DirConfig;

		public void Initialize()
		{
			string ConfigPath = Path.Combine(Util.GetInstance().GetServerFolder(), "DirectoryConfig.cfg");

			if (File.Exists(ConfigPath)) {
				DirConfig = new IniParser(ConfigPath);
				Debug.Log("Config " + ConfigPath + " loaded!");
			} else {
				char sc = Path.DirectorySeparatorChar;
				Directory.CreateDirectory(Util.GetInstance().GetPublicFolder());
				File.Create(ConfigPath).Close();
				DirConfig = new IniParser(ConfigPath);
				Debug.Log("Config " + ConfigPath + " Created!");
				Debug.Log("The config will be filled with the default values.");
				DirConfig.AddSetting("Directories", "Core", "%data%" + sc + "Core.cfg");
				DirConfig.AddSetting("Directories", "Pluton", "%public%" + sc + "Pluton.cfg");
				DirConfig.AddSetting("Directories", "Hashes", "%data%" + sc + "Hashes.ini");
				DirConfig.Save();
			}
		}

		public string GetConfigPath(string config)
		{
			string path = DirConfig.GetSetting("Directories", config);

			if (path.StartsWith("%public%"))
				path = path.Replace("%public%", Util.GetInstance().GetPublicFolder());

			if (path.StartsWith("%data%"))
				path = path.Replace("%data%", Util.GetInstance().GetServerFolder());

			if (path.StartsWith("%root%"))
				path = path.Replace("%root%", Util.GetInstance().GetRootFolder());
			
			return path;
		}

		public void Reload()
		{
			string ConfigPath = Path.Combine(Util.GetInstance().GetServerFolder(), "DirectoryConfig.cfg");

			if (File.Exists(ConfigPath))
				DirConfig = new IniParser(ConfigPath);
		}
	}
}


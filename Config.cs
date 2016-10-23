namespace Pluton.Core {
	using System;
	using System.IO;
	using UnityEngine;

	public class Config : Singleton<Config>, ISingleton {
		IniParser PlutonConfig;

		public void Initialize() {
			string ConfigPath = DirectoryConfig.GetInstance().GetConfigPath("Pluton");

			if (File.Exists(ConfigPath)) {
				PlutonConfig = new IniParser(ConfigPath);
				Debug.Log("Config " + ConfigPath + " loaded!");
			} else {
				Directory.CreateDirectory(Util.GetInstance().GetPublicFolder());
				File.Create(ConfigPath).Close();
				PlutonConfig = new IniParser(ConfigPath);
				Debug.Log("Config " + ConfigPath + " Created!");
				Debug.Log("The config will be filled with the default values.");
			}
		}

		static Config() {
			dependencies = new System.Collections.Generic.List<Func<bool>> {
				() => SingletonEx.IsInitialized<DirectoryConfig>()
			};
		}

		public string GetValue(string Section, string Setting, string defaultValue = "") {
			if (!PlutonConfig.ContainsSetting(Section, Setting)) {
				PlutonConfig.AddSetting(Section, Setting, defaultValue);
				PlutonConfig.Save();
			}
			return PlutonConfig.GetSetting(Section, Setting, defaultValue);
		}

		public bool GetBoolValue(string Section, string Setting, bool defaultValue = false) {
			if (!PlutonConfig.ContainsSetting(Section, Setting)) {
				PlutonConfig.AddSetting(Section, Setting, defaultValue.ToString().ToLower());
				PlutonConfig.Save();
			}
			return PlutonConfig.GetBoolSetting(Section, Setting, defaultValue);
		}

		public void Reload() {
			string ConfigPath = DirectoryConfig.GetInstance().GetConfigPath("Pluton");

			if (File.Exists(ConfigPath))
				PlutonConfig = new IniParser(ConfigPath);
		}
	}
}


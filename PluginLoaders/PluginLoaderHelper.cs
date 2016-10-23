namespace Pluton.Core.PluginLoaders {
	using System;
	using System.Collections.Generic;

	public static class PluginLoaderHelper {
		static Dictionary<Type, string> PluginType2Extension = new Dictionary<Type, string>();
		static Dictionary<Type, string> PluginType2SettingName = new Dictionary<Type, string>();

		public static void RegisterPluginType(Type t, string extension, string settingName) {
			RegisterExtension(t, extension);
			RegisterSettingName(t, settingName);
		}

		public static void RegisterPluginType<T>(string extension, string settingName) where T : IPlugin => RegisterPluginType(typeof(T), extension, settingName);

		public static string GetExtension(Type t) {
			if (PluginType2Extension.ContainsKey(t))
				return PluginType2Extension[t];
			return null;
		}

		public static string GetExtension<T>() where T : IPlugin => GetExtension(typeof(T));

		public static string GetSettingName(Type t) {
			if (PluginType2SettingName.ContainsKey(t))
				return PluginType2SettingName[t];
			return null;
		}

		public static string GetSettingName<T>() where T : IPlugin => GetSettingName(typeof(T));

		static void RegisterExtension(Type plugintype, string extension) {
			Console.WriteLine("Registering extension: '" + extension + "' for type: " + plugintype.FullName);
			if (!PluginType2Extension.ContainsKey(plugintype))
				PluginType2Extension.Add(plugintype, extension);
			else
				PluginType2Extension[plugintype] = extension;
		}

		static void RegisterExtension<T>(string extension) where T : IPlugin => RegisterExtension(typeof(T), extension);

		static void RegisterSettingName(Type plugintype, string settingName) {
			Console.WriteLine("Registering setting name: '" + settingName + "' for type: " + plugintype.FullName);
			if (!PluginType2SettingName.ContainsKey(plugintype))
				PluginType2SettingName.Add(plugintype, settingName);
			else
				PluginType2SettingName[plugintype] = settingName;
		}

		static void RegisterSettingName<T>(string settingName) where T : IPlugin => RegisterSettingName(typeof(T), settingName);
	}
}


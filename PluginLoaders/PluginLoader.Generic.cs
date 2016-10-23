namespace Pluton.Core.PluginLoaders {
	using System;
	using System.IO;
	using System.Linq;

	public class PluginLoader<T1> : Singleton<PluginLoader<T1>>, ISingleton, IPluginLoader where T1 : BasePlugin {
		string _name = $"PluginLoader<{typeof(T1).Name}>";

		public void Initialize() {
			if (PluginLoader.GetInstance().PluginLoaders.TryAdd(typeof(T1), Instance as IPluginLoader))
				Logger.Log($"New PluginLoader added: PluginLoader<{typeof(T1).FullName}>");

			PluginWatcher.GetInstance().AddWatcher(PluginType.FromType<T1>());
			LoadPlugins();
		}

		public string GetExtension() {
			return PluginLoaderHelper.GetExtension<T1>();
		}

		public System.Collections.Generic.IEnumerable<string> GetPluginNames() {
			foreach (DirectoryInfo dirInfo in PluginLoader.GetInstance().pluginDirectory.GetDirectories()) {
				string path = Path.Combine(dirInfo.FullName, dirInfo.Name + GetExtension());
				if (File.Exists(path))
					yield return dirInfo.Name;
			}
		}

		public void LoadPlugin(string name) {
			Logger.LogDebug($"[{ToString()}] Loading plugin {name}.");

			if (PluginLoader.GetInstance().Plugins.Keys.Contains(name)) {
				Logger.LogError($"[{ToString()}] {name} plugin is already loaded. Returning.");
				return;
			}
			if (PluginLoader.GetInstance().CurrentlyLoadingPlugins.Any(plugin => plugin.Name == name)) {
				Logger.LogWarning(name + " plugin is already being loaded. Returning.");
				return;
			}

			try {
				var plugin = Activator.CreateInstance(typeof(T1), name);
				PluginLoader.GetInstance().CurrentlyLoadingPlugins.Add(plugin as BasePlugin);
			} catch (Exception ex) {
				Logger.Log($"[{ToString()}] {name} plugin could not be loaded.");
				Logger.LogException(ex);
			}
		}

		public void LoadPlugins() {
			if (CoreConfig.GetInstance().GetBoolValue(PluginLoaderHelper.GetSettingName<T1>(), "enabled")) {
				foreach (string name in GetPluginNames())
					LoadPlugin(name);
			} else {
				Logger.LogDebug($"[{ToString()}] {PluginLoaderHelper.GetSettingName<T1>()} is disabled in Core.cfg.");
			}
		}

		public void ReloadPlugin(string name) {
			UnloadPlugin(name);
			LoadPlugin(name);
		}

		public void ReloadPlugins() {
			foreach (BasePlugin plugin in PluginLoader.GetInstance().Plugins.Values.ToArray()) {
				if (!plugin.DontReload && plugin is T1) {
					UnloadPlugin(plugin.Name);
					LoadPlugin(plugin.Name);
				}
			}
		}

		public void UnloadPlugin(string name) {
			Logger.LogDebug($"[{ToString()}] Unloading {name} plugin.");

			BasePlugin plugin = PluginLoader.GetInstance().Plugins[name];
			if (plugin.DontReload || !(plugin is T1))
				return;

			if (plugin.Globals.Contains("On_PluginDeinit"))
				plugin.Invoke("On_PluginDeinit");

			plugin.KillTimers();
			PluginLoader.GetInstance().RemoveHooks(plugin);
			PluginLoader.GetInstance().Plugins.TryRemove(name, out plugin);

			Logger.LogDebug($"[{ToString()}] {name} plugin was unloaded successfuly.");
		}

		public void UnloadPlugins() {
			foreach (string name in PluginLoader.GetInstance().Plugins.Keys.ToArray())
				UnloadPlugin(name);
		}

		public override string ToString() => _name;
	}
}


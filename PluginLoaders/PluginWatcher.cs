namespace Pluton.Core.PluginLoaders
{
	using System;
	using System.IO;
	using System.Collections.Generic;
	using System.Linq;

	public class PluginWatcher : Singleton<PluginWatcher>, ISingleton
	{
		public List<PluginTypeWatcher> Watchers = new List<PluginTypeWatcher>();

		public void AddWatcher(PluginType type)
		{
			foreach (PluginTypeWatcher watch in Watchers)
				if (watch.Type.Type == type.Type)
					return;

			Watchers.Add(new PluginTypeWatcher(type));
		}

		public void Initialize()
		{
		}
	}

	public class PluginTypeWatcher : CountedInstance
	{
		public PluginType Type;

		public FileSystemWatcher Watcher;

		public PluginTypeWatcher(PluginType type)
		{
			Type = type;
			Watcher = new FileSystemWatcher(Path.Combine(Util.GetInstance().GetPublicFolder(), "Plugins"), "*" + type.Extension);
			Watcher.EnableRaisingEvents = true;
			Watcher.IncludeSubdirectories = true;
			Watcher.Changed += OnPluginChanged;
			Watcher.Created += OnPluginCreated;
		}

		public override string ToString() => $"PluginTypeWatcher<{Type}>";

		bool TryLoadPlugin(string name)
		{
			try {
				IPluginLoader pluginLoader;
				if (PluginLoader.GetInstance().PluginLoaders.TryGetValue(Type.Type, out pluginLoader)) {
					if (PluginLoader.GetInstance().Plugins.ContainsKey(name)) {
						if (pluginLoader == null)
							Logger.LogError("### pluginloader is null, wtf");
						pluginLoader.ReloadPlugin(name);
					} else
						pluginLoader.LoadPlugin(name);
					return true;
				}
			} catch (Exception ex) {
				Logger.LogException(ex);
			}
			return false;
		}

		void OnPluginCreated(object sender, FileSystemEventArgs e)
		{
			string filename = Path.GetFileNameWithoutExtension(e.Name);
			string dir = Path.GetDirectoryName(e.FullPath).Split(Path.DirectorySeparatorChar).Last();

			if (filename == dir) {
				if (!TryLoadPlugin(filename)) {
					Logger.Log($"[{ToString()}] Couldn't load: {dir}{Path.DirectorySeparatorChar}{filename}{Type.Extension}");
				}
			}
		}

		void OnPluginChanged(object sender, FileSystemEventArgs e)
		{
			string filename = Path.GetFileNameWithoutExtension(e.Name);
			string dir = Path.GetDirectoryName(e.FullPath).Split(Path.DirectorySeparatorChar).Last();

			string assumedPluginPathFromDir = Path.Combine(Path.Combine(Watcher.Path, dir),
																	dir + Path.GetExtension(e.Name));

			if (filename == dir) {
				if (File.Exists(e.FullPath)) {
					if (!TryLoadPlugin(filename)) {
						Logger.Log($"[{ToString()}] Couldn't load: {dir}{Path.DirectorySeparatorChar}{filename}{Type.Extension}");
					}
				}
			} else if (File.Exists(assumedPluginPathFromDir)) {
				if (!TryLoadPlugin(dir)) {
					Logger.Log($"[{ToString()}] Couldn't load: {dir}{Path.DirectorySeparatorChar}{filename}{Type.Extension}");
				}
			}
		}
	}
}


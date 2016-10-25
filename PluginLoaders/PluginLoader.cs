namespace Pluton.Core.PluginLoaders
{
	using System;
	using System.Collections.Generic;
	using System.Collections.Concurrent;
	using System.IO;
	using System.Linq;
	using System.Reactive.Subjects;

	public class PluginLoader : Singleton<PluginLoader>, ISingleton
	{
		public List<BasePlugin> CurrentlyLoadingPlugins = new List<BasePlugin>();

		public ConcurrentDictionary<string, BasePlugin> Plugins = new ConcurrentDictionary<string, BasePlugin>();

		public ConcurrentDictionary<Type, IPluginLoader> PluginLoaders = new ConcurrentDictionary<Type, IPluginLoader>();

		public DirectoryInfo pluginDirectory;

		public Subject<string> OnAllLoaded = new Subject<string>();

		public void Initialize()
		{
			PYPlugin.LibPath = Path.Combine(Util.GetInstance().GetPublicFolder(), Path.Combine("Python", "Lib"));
			BasePlugin.GlobalData = new Dictionary<string, object>();
			pluginDirectory = new DirectoryInfo(Util.GetInstance().GetPluginsFolder());
			if (!Directory.Exists(pluginDirectory.FullName)) {
				Directory.CreateDirectory(pluginDirectory.FullName);
			}
		}

		#region re/un/loadplugin(s)

		public void OnPluginLoaded(BasePlugin plugin)
		{
			var pluginType = plugin.GetType();

			Instance.CurrentlyLoadingPlugins.RemoveAll(p => p.Name == plugin.Name);

			if (plugin.State != PluginState.Loaded) {
				throw new FileLoadException("Couldn't initialize " + pluginType.Name + " plugin.",
											Path.Combine(plugin.RootDir.FullName,
														 plugin.Name + PluginLoaderHelper.GetExtension(pluginType))
				);
			}

			InstallHooks(plugin);
			Plugins.TryAdd(plugin.Name, plugin);

			// probably make an event here that others can hook?

			if (CurrentlyLoadingPlugins.Count == 0)
				Hooks.OnNext("On_AllPluginLoaded");

			Logger.Log($"[PluginLoader] {pluginType.Name}<{plugin.Name}> plugin was loaded successfuly.");
		}

		public void LoadPlugin(string name, PluginType t)
		{
			PluginLoaders[t.Type].CallMethod("LoadPlugin", name);
		}

		public void LoadPlugins()
		{
			foreach (IPluginLoader loader in PluginLoaders.Values.ToArray())
				loader.LoadPlugins();
		}

		public void UnloadPlugins()
		{
			foreach (IPluginLoader loader in PluginLoaders.Values.ToArray())
				loader.UnloadPlugins();
		}

		public void ReloadPlugins()
		{
			foreach (IPluginLoader loader in PluginLoaders.Values.ToArray())
				loader.ReloadPlugins();
		}

		public void ReloadPlugin(string name)
		{
			IPluginLoader pluginloader;

			if (Plugins.ContainsKey(name) && PluginLoaders.TryGetValue(Plugins[name].GetType(), out pluginloader))
				pluginloader?.ReloadPlugin(name);
		}

		public void ReloadPlugin(BasePlugin plugin)
		{
			string name = plugin.Name;
			IPluginLoader pluginloader;

			if (Plugins.ContainsKey(name) && PluginLoaders.TryGetValue(plugin.GetType(), out pluginloader))
				pluginloader?.ReloadPlugin(name);
		}

		#endregion

		#region install/remove hooks

		public void InstallHooks(BasePlugin plugin)
		{
			if (plugin.State != PluginState.Loaded)
				return;

			foreach (string method in plugin.Globals) {
				if (Hooks.GetInstance().HookNames.Contains(method)) {
					plugin.Hooks.Add(
						Hooks.Subscribe(method, plugin)
					);
					Logger.LogDebug($"[{plugin.GetType().Name}] Adding hook: {plugin.Name}.{method}");
				}
			}

			if (plugin.Globals.Contains("On_PluginInit"))
				plugin.Invoke("On_PluginInit");
		}

		public void RemoveHooks(BasePlugin plugin)
		{
			if (plugin.State != PluginState.Loaded)
				return;

			foreach (Hook hook in plugin.Hooks) {
				Logger.LogDebug($"[{plugin.GetType().Name}] Removing hook: {plugin.Name}.{hook.Name}");
				hook.hook.Dispose();
			}
			plugin.Hooks.Clear();
		}

		#endregion
	}
}


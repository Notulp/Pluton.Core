namespace Pluton.Core.PluginLoaders {
	using System.Collections.Generic;

	public interface IPluginLoader {
		string GetExtension();

		IEnumerable<string> GetPluginNames();

		void LoadPlugin(string name);

		void LoadPlugins();

		void ReloadPlugin(string name);

		void ReloadPlugins();

		void UnloadPlugin(string name);

		void UnloadPlugins();
	}
}


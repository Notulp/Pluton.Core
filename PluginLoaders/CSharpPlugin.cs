namespace Pluton.Core.PluginLoaders
{
	public class CSharpPlugin
	{

		public string Author {
			get { return Plugin.Author; }
			set { Plugin.Author = value; }
		}

		public string About {
			get { return Plugin.About; }
			set { Plugin.About = value; }
		}

		public string Version {
			get { return Plugin.Version; }
			set { Plugin.Version = value; }
		}

		public DataStore DataStore => DataStore.GetInstance();

		public BasePlugin Plugin;

		public Util Util => Util.GetInstance();

		public Web Web => Web.GetInstance();

		public System.Collections.Generic.Dictionary<string, object> GlobalData => BasePlugin.GlobalData;
	}
}


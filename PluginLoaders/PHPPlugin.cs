namespace Pluton.Core.PluginLoaders {
	using System;
	using System.IO;
	using PHP.Core;

    public class PHPPlugin : BasePlugin {
		public static new PluginType Type => PluginType.PHP;
		public static new string Extension => ".php";

        ScriptContext context;
        public PhpArray PHPGlobals;
        PhpObject Class;
        private DirectoryInfo rpath;

        public PHPPlugin(string name)
            : base(name) {
            rpath = rootdir;

            if (CoreConfig.GetInstance().GetBoolValue("php", "checkHash") && !code.VerifyMD5Hash()) {
                Logger.LogDebug(String.Format("[Plugin] MD5Hash not found for: {0} [{1}]!", name, Type));
                State = PluginState.HashNotFound;
                return;
            }

            System.Threading.ThreadPool.QueueUserWorkItem(
                new System.Threading.WaitCallback(a => Load(code)), null);
        }

        public override object Invoke(string func, params object[] args) {
            try {
                if (State == PluginState.Loaded && Globals.Contains(func)) {
                    object result = (object)null;

                    using (new Stopper(Name, func)) {
                        var caller = new PhpCallback(Class, func);
                        result = caller.Invoke(args);
                    }
                    return result;
                } else {
                    Logger.LogWarning("[Plugin] Function: " + func + " not found in plugin: " + Name + ", or plugin is not loaded.");
                    return null;
                }
            } catch (Exception ex) {
                string fileinfo = (String.Format("{0}<{1}>.{2}()", Name, Type, func) + Environment.NewLine);
                Logger.LogError(fileinfo + FormatException(ex));
                return null;
            }
        }

        public override void Load(string code = "") {
            try {
                context = ScriptContext.CurrentContext;
                context.Include(rpath  + "\\" + Name + ".php", true);
                Class = (PhpObject) context.NewObject(Name);
                PHPGlobals = context.GlobalVariables;
                context.GlobalVariables.Add("Commands", chatCommands);
                context.GlobalVariables.Add("DataStore", DataStore.GetInstance());
                context.GlobalVariables.Add("Find", Find.GetInstance());
                context.GlobalVariables.Add("GlobalData", GlobalData);
                context.GlobalVariables.Add("Plugin", this);
                context.GlobalVariables.Add("Server", Pluton.Server.GetInstance());
                context.GlobalVariables.Add("ServerConsoleCommands", consoleCommands);
                context.GlobalVariables.Add("Util", Util.GetInstance());
                context.GlobalVariables.Add("Web", Web.GetInstance());
                context.GlobalVariables.Add("World", World.GetInstance());

				AssignVariables();

                foreach (var x in PHPGlobals) {
                    Globals.Add(x.Key.ToString());
                }

                State = PluginState.Loaded;
            } catch (Exception ex) {
                Logger.LogException(ex);
                State = PluginState.FailedToLoad;
			}

            PluginLoader.GetInstance().OnPluginLoaded(this);
        }
    }
}

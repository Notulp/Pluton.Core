namespace Pluton.Core.PluginLoaders
{
	using System;
	using System.IO;
	using System.Linq;
	using Jint;
	using Jint.Expressions;

    public class JSPlugin : BasePlugin
	{
        public JintEngine Engine;
        public Program Program;

        public JSPlugin(string name) : base(name)
        {
			string code = File.ReadAllText(GetPluginPath());

            System.Threading.ThreadPool.QueueUserWorkItem(
                new System.Threading.WaitCallback(a => Load(code)), null);
        }

        public override object Invoke(string method, params object[] args)
        {
            try {
				if (State == PluginState.Loaded && Globals.Contains(method))
				{
					object result = null;

					using (new Stopper(Name, method))
					{
						result = Engine.CallFunction(method, args);
					}
					return result;
				}
				Logger.LogWarning($"[{GetType().Name}] Function: {method} not found in plugin: {Name}, or plugin is not loaded.");
				return null;
			} catch (Exception ex) {
				string fileinfo = $"{GetType().Name}<{Name}>.{method}(){Environment.NewLine}";
                Logger.LogError(fileinfo + FormatException(ex));
                return null;
            }
        }

        public override void Load(string code)
        {
			try
			{
				if (CoreConfig.GetInstance().GetBoolValue("javascript", "checkHash") && !code.VerifyMD5Hash())
				{
					Logger.LogDebug($"[{GetType().Name}] MD5Hash not found for: {Name}");
					State = PluginState.HashNotFound;
				}
				else
				{

					Engine = new JintEngine(Options.Ecmascript5)
						.AllowClr(true);

					Engine.SetParameter("DataStore", DataStore.GetInstance())
						.SetParameter("GlobalData", GlobalData)
						.SetParameter("Plugin", this)
						.SetParameter("Util", Util.GetInstance())
						.SetParameter("Web", Web)
						.SetFunction("importClass", new importit(importClass));

					AssignVariables();

					Program = JintEngine.Compile(code, false);

					Globals = (from statement in Program.Statements
							   where statement.GetType() == typeof(FunctionDeclarationStatement)
							   select ((FunctionDeclarationStatement)statement).Name).ToList();

					Engine.Run(Program);

					object author = GetGlobalObject("Author");
					object about = GetGlobalObject("About");
					object version = GetGlobalObject("Version");
					Author = author == null ? "" : author.ToString();
					About = about == null ? "" : about.ToString();
					Version = version == null ? "" : version.ToString();

					State = PluginState.Loaded;
				}
            }
			catch (Exception ex)
			{
                Logger.LogException(ex);
                State = PluginState.FailedToLoad;
			}

            PluginLoader.GetInstance().OnPluginLoaded(this);
        }

		public override object GetGlobalObject(string id) => Engine.Run($"return {id};");

        public delegate Jint.Native.JsInstance importit(string t);

        public Jint.Native.JsInstance importClass(string type)
        {
            Engine.SetParameter(type.Split('.').Last(), Util.GetInstance().TryFindReturnType(type));
            return (Engine.Global as Jint.Native.JsDictionaryObject)[type.Split('.').Last()];
        }
    }
}


namespace Pluton.Core.PluginLoaders
{
	using System;
	using System.IO;
	using Microsoft.Scripting.Hosting;

    public class PYPlugin : BasePlugin
	{
		public static string LibPath;

        ScriptEngine Engine;
        public ScriptScope Scope;
        object Class;

        public PYPlugin(string name) : base(name)
        {
			string code = File.ReadAllText(GetPluginPath());

            if (CoreConfig.GetInstance().GetBoolValue("python", "checkHash") && !code.VerifyMD5Hash()) {
				Logger.LogDebug($"[{GetType().Name}] MD5Hash not found for: {name}");
                State = PluginState.HashNotFound;
                return;
            }

            System.Threading.ThreadPool.QueueUserWorkItem(
                new System.Threading.WaitCallback(a => Load(code)), null);
        }

        public override string FormatException(Exception ex) => base.FormatException(ex) + Environment.NewLine + Engine.GetService<ExceptionOperations>().FormatException(ex);

        public override object Invoke(string method, params object[] args)
        {
            try {
                if (State == PluginState.Loaded && Globals.Contains(method)) {
                    object result = null;

                    using (new Stopper(Name, method)) {
                        result = Engine.Operations.InvokeMember(Class, method, args);
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
			try {
	            Engine = IronPython.Hosting.Python.CreateEngine();
	            Scope = Engine.CreateScope();
	            Scope.SetVariable("DataStore", DataStore.GetInstance());
	            Scope.SetVariable("GlobalData", GlobalData);
				Scope.SetVariable("Plugin", this);
	            Scope.SetVariable("Util", Util.GetInstance());
	            Scope.SetVariable("Web", Web.GetInstance());

				AssignVariables();

                Engine.Execute(code, Scope);
                Class = Engine.Operations.Invoke(Scope.GetVariable(Name));
                Globals = Engine.Operations.GetMemberNames(Class);

                object author = GetGlobalObject("__author__");
                object about = GetGlobalObject("__about__");
                object version = GetGlobalObject("__version__");
                Author = author == null ? "" : author.ToString();
                About = about == null ? "" : about.ToString();
                Version = version == null ? "" : version.ToString();

                State = PluginState.Loaded;
            } catch (Exception ex) {
                Logger.LogException(ex);
				State = PluginState.FailedToLoad;
            }

            PluginLoader.GetInstance().OnPluginLoaded(this);
        }

		public override object GetGlobalObject(string id)
        {
            try {
                return Scope.GetVariable(id);
            } catch {
                return null;
            }
        }
    }
}


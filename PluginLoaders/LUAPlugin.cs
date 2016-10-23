namespace Pluton.Core.PluginLoaders
{
	using System;
	using System.IO;
	using MoonSharp.Interpreter;
	using MoonSharp.Interpreter.Interop;

	public class LUAPlugin : BasePlugin
	{
		public Table Tables;
		public Script script;

		public LUAPlugin(string name)
			: base(name)
		{
			string code = File.ReadAllText(GetPluginPath());

			System.Threading.ThreadPool.QueueUserWorkItem(
				new System.Threading.WaitCallback(a => Load(code)), null);
		}

		public override object GetGlobalObject(string id)
		{
			return script.Globals.Get(id).ToObject();
		}

		public override object Invoke(string method, params object[] args)
		{
			try {
				if (State == PluginState.Loaded && Globals.Contains(method)) {
					object result = null;

					using (new Stopper(Name, method)) {
						result = script.Call(script.Globals[method], args);
					}
					return result;
				} else {
					Logger.LogWarning("[Plugin] Function: " + method + " not found in plugin: " + Name + ", or plugin is not loaded.");
					return null;
				}
			} catch (Exception ex) {
				string fileinfo = $"{GetType().Name}<{Name}>.{method}(){Environment.NewLine}";
				Logger.LogError(fileinfo + FormatException(ex));
				return null;
			}
		}

		public override string FormatException(Exception ex)
		{
			return base.FormatException(ex) +
			(ex is ScriptRuntimeException ? Environment.NewLine + (ex as ScriptRuntimeException).DecoratedMessage : "");
		}

		public override void Load(string code = "")
		{
			try {
				if (CoreConfig.GetInstance().GetBoolValue("lua", "checkHash") && !code.VerifyMD5Hash()) {
					Logger.LogDebug($"[{GetType().Name}] MD5Hash not found for: {Name}");
					State = PluginState.HashNotFound;
				} else {

					UserData.RegistrationPolicy = InteropRegistrationPolicy.Automatic;
					script = new Script();
					script.Globals["Plugin"] = this;
					script.Globals["Util"] = Util.GetInstance();
					script.Globals["DataStore"] = DataStore.GetInstance();
					script.Globals["GlobalData"] = GlobalData;
					script.Globals["Web"] = Web.GetInstance();

					AssignVariables();

					script.DoString(code);

					Author = script.Globals.Get("Author").String;
					About = script.Globals.Get("About").String;
					Version = script.Globals.Get("Version").String;

					State = PluginState.Loaded;
					foreach (DynValue v in script.Globals.Keys) {
						Globals.Add(v.ToString().Replace("\"", ""));
					}
					Tables = script.Globals;
				}
			} catch (Exception ex) {
				Logger.LogException(ex);
				State = PluginState.FailedToLoad;
			}

			PluginLoader.GetInstance().OnPluginLoaded(this);
		}
	}
}


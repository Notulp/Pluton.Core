namespace Pluton.Core.PluginLoaders
{
	using System;
	using System.IO;
	using System.Reflection;
	using System.Linq;
	using System.Collections.Generic;

    public class CSPlugin : BasePlugin
    {
        public CSharpPlugin Engine;

		public CSPlugin(string name) : base(name)
        {
            System.Threading.ThreadPool.QueueUserWorkItem(
				new System.Threading.WaitCallback(a => Load(GetPluginPath())), null);
        }

		public override object GetGlobalObject(string id)
		{
			return Engine.GetFieldValue(id);
		}

        public override object Invoke(string method, params object[] args)
        {
            try {
                if (State == PluginState.Loaded && Globals.Contains(method)) {
                    object result = null;

                    using (new Stopper(Name, method)) {
                        result = Engine.CallMethod(method, args);
                    }
                    return result;
                }
				Logger.LogWarning($"[{GetType().Name}] Function: {method} not found in plugin: {Name}, or plugin is not loaded.");
				return null;
            } catch (Exception ex) {
				string fileinfo = $"{GetType().Name}<{Name}>.{method}(){Environment.NewLine}";
                HasErrors = true;
                if (ex is TargetInvocationException) {
                    LastError = FormatException(ex.InnerException);
                    Logger.LogError(fileinfo + FormatException(ex.InnerException));
                } else {
                    LastError = FormatException(ex);
                    Logger.LogError(fileinfo + FormatException(ex));
                }
                return null;
            }
        }

        public override void Load(string code)
        {
            try {
                byte[] bin = File.ReadAllBytes(code);
				if (CoreConfig.GetInstance().GetBoolValue("csharp", "checkHash") && !bin.VerifyMD5Hash())
				{
					Logger.LogDebug($"[{GetType().Name}] MD5Hash not found for: {Name}");
					State = PluginState.HashNotFound;
				}
				else
				{
					LoadReferences();

					Assembly assembly = Assembly.Load(bin);
					Type classType = assembly.GetType($"{Name}.{Name}");
					if (classType == null || !classType.IsSubclassOf(typeof(CSharpPlugin)) || !classType.IsPublic || classType.IsAbstract)
					{
						Console.WriteLine("Main module class not found:" + Name);
						State = PluginState.FailedToLoad;
					}
					else
					{
						Engine = (CSharpPlugin)Activator.CreateInstance(classType);
						Engine.Plugin = this;

						Globals = (from method in classType.GetMethods()
								   select method.Name).ToList();

						State = PluginState.Loaded;
					}
				}
            }
			catch (Exception ex)
			{
                Logger.LogException(ex);
				State = PluginState.FailedToLoad;
			}

            PluginLoader.GetInstance().OnPluginLoaded(this);
        }

        public void LoadReferences()
        {
            List<string> dllpaths = GetRefDllPaths().ToList();
            foreach (Assembly ass in AppDomain.CurrentDomain.GetAssemblies()) {
                if (dllpaths.Contains(ass.FullName)) {
                    dllpaths.Remove(ass.FullName);
                }
            }
            dllpaths.ForEach(path => {
                Assembly.LoadFile(path);
            });
        }

        IEnumerable<string> GetRefDllPaths()
        {
            string refpath = Path.Combine(RootDir.FullName, "References");
            if (Directory.Exists(refpath)) {
                var refdir = new DirectoryInfo(refpath);
                FileInfo[] files = refdir.GetFiles("*.dll");
                foreach (FileInfo file in files) {
                    yield return file.FullName;
                }
            }
        }
    }
}


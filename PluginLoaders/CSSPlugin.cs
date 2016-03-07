namespace Pluton.Core.PluginLoaders
{
	using System;
	using System.IO;
	using System.Reflection;
	using System.Linq;
	using System.Diagnostics;
	using System.Collections.Generic;

    public class CSSPlugin : BasePlugin
	{
        public CSharpPlugin Engine;

		const string compileParams = "/langversion:6 /target:library /debug- /optimize+ /out:%PLUGINPATH%%PLUGINNAME%.plugin %REFERENCES% %PLUGINPATH%*.cs";

		string CompilePluginParams = String.Empty;
		string CompilationResults = String.Empty;
        System.Threading.Mutex mutex = new System.Threading.Mutex();
        public bool Compiled = false;

        public CSSPlugin(string name) : base(name)
        {
			CompilePluginParams = compileParams.Replace("%PLUGINPATH%", RootDir.FullName + Path.DirectorySeparatorChar).Replace("%PLUGINNAME%", name).Replace("%REFERENCES%", String.Join(" ", GetDllPaths().ToArray()));

            System.Threading.ThreadPool.QueueUserWorkItem(
                new System.Threading.WaitCallback(a => Load(null)), null);
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
                } else {
					Logger.LogWarning($"[{GetType().Name}] Function: {method} not found in plugin: {Name}, or plugin is not loaded.");
                    return null;
                }
            } catch (Exception ex) {
				string fileinfo = $"{GetType().Name}<{Name}>.{method}(){Environment.NewLine}";
                HasErrors = true;
                if (ex is TargetInvocationException) {
                    LastError = FormatException(ex.InnerException);
                } else {
                    LastError = FormatException(ex);
                }
				Logger.LogError(fileinfo + LastError);
                return null;
            }
        }

		public override void Load(string nothing)
        {
            try {
                LoadReferences();

                Assembly plugin = Compile();

				/*//For C# plugins code is the dll path
                byte[] bin = File.ReadAllBytes(code);
                if (CoreConfig.GetInstance().GetBoolValue("csharp", "checkHash") && !bin.VerifyMD5Hash()) {
                    Logger.LogDebug(String.Format("[Plugin] MD5Hash not found for: {0} [{1}]!", name, Type));
                    State = PluginState.HashNotFound;
                    return;
                }*/
				if (plugin == null)
				{
					State = PluginState.FailedToLoad;
				}
				else
				{
					Type classType = plugin.GetType($"{Name}.{Name}");

					if (classType == null || !classType.IsSubclassOf(typeof(CSharpPlugin)) || !classType.IsPublic || classType.IsAbstract)
					{
						Console.WriteLine("Main module class not found: " + Name);
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
            } catch (Exception ex) {
                Logger.LogException(ex);
                State = PluginState.FailedToLoad;
			}

            PluginLoader.GetInstance().OnPluginLoaded(this);
        }

        public Assembly Compile()
        {
            try {
                string mcspath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "mcs.exe");

                using (new Stopper("CSSPlugin", "Compile()")) {

                    var compiler = new Process();

                    compiler.StartInfo.FileName = mcspath;
                    compiler.StartInfo.Arguments = CompilePluginParams;

                    compiler.StartInfo.WorkingDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

                    compiler.EnableRaisingEvents = true;

                    compiler.ErrorDataReceived += MCSReturnedErrorData;
                    compiler.Exited += MCSExited;
                    compiler.OutputDataReceived += MCSReturnedOutputData;

                    compiler.StartInfo.CreateNoWindow = true;
                    compiler.StartInfo.UseShellExecute = false;
                    compiler.StartInfo.RedirectStandardOutput = true;
                    compiler.StartInfo.RedirectStandardError = true;

                    string temppath = Path.Combine(RootDir.FullName, Name + ".plugin");

                    if (File.Exists(temppath))
                        File.Delete(temppath);

                    compiler.Start();

                    DateTime start = compiler.StartTime;

                    compiler.BeginOutputReadLine();
                    compiler.BeginErrorReadLine();

                    compiler.WaitForExit();

                    Logger.Log("Compile time: " + (compiler.ExitTime - start).ToString());

                    compiler.Close();

                    while (!Compiled) {
                        System.Threading.Thread.Sleep(50);
                    }
                }
                string path = Path.Combine(RootDir.FullName, Name + ".plugin");
				if (!String.IsNullOrEmpty(CompilationResults))
				{
					File.WriteAllText(Path.Combine(RootDir.FullName, Name + "_result.txt"), CompilationResults);
				}
				if (File.Exists(path))
					return Assembly.Load(File.ReadAllBytes(path));

				Logger.LogError("Couldn't compile " + Name + ".cs plugin.");
				Logger.LogError(CompilationResults);

				return null;
			} catch (Exception ex) {
                Logger.LogException(ex);
                return null;
            }
        }

        void MCSReturnedOutputData(object sender, DataReceivedEventArgs e)
        {
            if (e.Data != null) {
                mutex.WaitOne();
                CompilationResults += e.Data + Environment.NewLine;
                mutex.ReleaseMutex();
            }
        }

        void MCSExited(object sender, EventArgs e)
        {
            Compiled = true;
        }

        void MCSReturnedErrorData(object sender, DataReceivedEventArgs e)
        {
            if (e.Data != null) {
                mutex.WaitOne();
                CompilationResults += e.Data + Environment.NewLine;
                mutex.ReleaseMutex();
            }
        }

        IEnumerable<string> GetDllPaths()
        {
            string refpath = Path.Combine(RootDir.FullName, "References");
            if (Directory.Exists(refpath)) {
                var refdir = new DirectoryInfo(refpath);
                var files = refdir.GetFiles("*.dll");
                foreach (var file in files) {
                    yield return "/r:" + file.FullName.QuoteSafe();
                }
            }
            string assLoc = Assembly.GetExecutingAssembly().Location.Replace("Pluton.Core.dll", "");
            if (Directory.Exists(assLoc)) {
                var files2 = new DirectoryInfo(assLoc).GetFiles("*.dll");
                foreach (var file2 in files2) {
                    yield return "/r:" + file2.FullName.QuoteSafe();
                }
            }
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


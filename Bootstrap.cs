namespace Pluton.Core
{
	using System;
	using System.IO;
	using System.Timers;
	using UnityEngine;
	using System.Linq;

	public class Bootstrap : MonoBehaviour
	{
		public static string Version => System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString();

		public static ServerTimers timers;

		public static bool PlutonLoaded = false;

		public static void AttachBootstrap()
		{
			try {
				foreach (var file in Directory.GetFiles(System.Reflection.Assembly.GetExecutingAssembly().CodeBase.Replace("file:///", "").Replace("/", "\\").Replace("\\Pluton.Core.dll", ""), "Pluton.*.dll")) {
					if (!file.EndsWith ("Pluton.Core.dll")) {
						System.Reflection.Assembly module = System.Reflection.Assembly.LoadFile (file);
						foreach (var type in module.GetTypes()) {
							if (type.ToString() == file.Split('\\').Last().Replace("dll", "Bootstrap")) {
								module.CreateInstance(type.ToString());
							}
						}
					}
				}

				DirectoryConfig.GetInstance();
				CoreConfig.GetInstance();
				Config.GetInstance();

				Init();

				PlutonLoaded = true;
				Console.WriteLine($"[v.{Version}] Pluton loaded!");
			} catch (Exception ex) {
				Debug.LogException(ex);
				Debug.Log("[Bootstarp] Error while loading Pluton!");
			}
		}

		public static void SaveAll(object x = null)
		{
			try {
				DataStore.GetInstance().Save();
			} catch (Exception ex) {
				Logger.LogDebug("[Bootstrap] Failed to save the server!");
				Logger.LogException(ex);
			}
		}

		public static void ReloadTimers()
		{
			if (timers != null)
				timers.Dispose();

			var saver = Config.GetInstance().GetValue("Config", "saveInterval", "180000");
			if (saver != null) {
				double save = Double.Parse(saver);

				timers = new ServerTimers(save);
				timers.Start();
			}
		}

		public static void Init()
		{
			if (!Directory.Exists(Util.GetInstance().GetPublicFolder()))
				Directory.CreateDirectory(Util.GetInstance().GetPublicFolder());
			
			Logger.Init();
			CryptoExtensions.Init();
			DataStore.GetInstance().Load();

			ReloadTimers();
		}

		public class ServerTimers
		{
			public readonly Timer _savetimer;

			public ServerTimers(double save)
			{
				_savetimer = new Timer(save);

				Debug.Log("Server timers started!");
				_savetimer.Elapsed += _savetimer_Elapsed;
			}

			public void Dispose()
			{
				Stop();
				_savetimer.Dispose();
			}

			public void Start() => _savetimer.Start();

			public void Stop() => _savetimer.Stop();

			private void _savetimer_Elapsed(object sender, ElapsedEventArgs e) => SaveAll();
		}
	}
}


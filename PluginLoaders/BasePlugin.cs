namespace Pluton.Core.PluginLoaders
{
	using System;
	using System.IO;
	using System.Collections.Generic;
	using System.Linq;
	using System.Net;
	using System.Text;

    public class BasePlugin : CountedInstance, IPlugin
	{
        public string Author;

        public string About;

        public string Version;

        public bool DontReload = false;

        public bool HasErrors = false;

        public string LastError = String.Empty;

        public List<Hook> Hooks = new List<Hook>();

        public string Name {
            get;
            private set;
        }

        public DirectoryInfo RootDir {
            get;
            private set;
        }

        public IList<string> Globals {
            get;
            protected set;
        }

        public readonly Dictionary<string, TimedEvent> Timers;

        public readonly List<TimedEvent> ParallelTimers;

        public static Dictionary<string, object> GlobalData;

        public PluginState State = PluginState.NotLoaded;

		public Web Web => Web.GetInstance();

        public virtual void Load(string code) { }

		public virtual void AssignVariables() { }

        public BasePlugin(string name)
        {
            Name = name;
			RootDir = new DirectoryInfo(Path.Combine(PluginLoader.GetInstance().pluginDirectory.FullName, name));
            Globals = new List<string>();

            Timers = new Dictionary<string, TimedEvent>();
            ParallelTimers = new List<TimedEvent>();
        }

		public string GetPluginPath() => Path.Combine(RootDir.FullName, Name + PluginLoaderHelper.GetExtension(GetType()));

        public virtual string FormatException(Exception ex)
        {
            string nuline = Environment.NewLine;
            return ex.Message + nuline + ex.TargetSite.ToString() + nuline + ex.StackTrace;
        }

		public virtual object GetGlobalObject(string id) => new NotImplementedException($"Plugin.GetGlobalData is not implemented on: {GetType().FullName}");

		public virtual object Invoke(string method, params object[] args) => null;

        #region file operations

        private static string NormalizePath(string path)
        {
            return Path.GetFullPath(new Uri(path).LocalPath)
                .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        }

        public string ValidateRelativePath(string path)
        {
            string normalizedPath = NormalizePath(Path.Combine(RootDir.FullName, path));
            string rootDirNormalizedPath = NormalizePath(RootDir.FullName);

            if (!normalizedPath.StartsWith(rootDirNormalizedPath))
                return null;

            return normalizedPath;
        }

        public bool CreateDir(string path)
        {
            try {
                path = ValidateRelativePath(path);
                if (path == null)
                    return false;

                if (Directory.Exists(path))
                    return true;

                Directory.CreateDirectory(path);
                return true;
            } catch (Exception ex) {
                Logger.LogException(ex);
            }
            return false;
        }

        public void DeleteLog(string path)
        {
            path = ValidateRelativePath(path + ".log");
            if (path == null)
                return;

            if (File.Exists(path))
                File.Delete(path);
        }

        public void Log(string path, string text)
        {
            path = ValidateRelativePath(path + ".log");
            if (path == null)
                return;

            File.AppendAllText(path, "[" + DateTime.Now.ToShortDateString() + " " + DateTime.Now.ToShortTimeString() + "] " + text + "\r\n");
        }

        public void RotateLog(string logfile, int max = 6)
        {
            logfile = ValidateRelativePath(logfile + ".log");
            if (logfile == null)
                return;

            string pathh, pathi;
            int i, h;
            for (i = max, h = i - 1; i > 1; i--, h--) {
                pathi = ValidateRelativePath(logfile + i + ".log");
                pathh = ValidateRelativePath(logfile + h + ".log");

                try {
                    if (!File.Exists(pathi))
                        File.Create(pathi);

                    if (!File.Exists(pathh)) {
                        File.Replace(logfile, pathi, null);
                    } else {
                        File.Replace(pathh, pathi, null);
                    }
                } catch (Exception ex) {
                    Logger.LogError("[Plugin] RotateLog " + logfile + ", " + pathh + ", " + pathi + ", " + ex.StackTrace);
                    continue;
                }
            }
        }

        #endregion

        #region jsonfiles

        public bool JsonFileExists(string path)
        {
            path = ValidateRelativePath(path + ".json");
            if (path == null)
                return false;

            return File.Exists(path);
        }

        public string FromJsonFile(string path)
        {
            path = ValidateRelativePath(path + ".json");
            if (JsonFileExists(path))
                return File.ReadAllText(path);

            return null;
        }

        public void ToJsonFile(string path, string json)
        {
            path = ValidateRelativePath(path + ".json");
            if (path == null)
                return;

            File.WriteAllText(path, json);
        }

        #endregion

        #region inifiles

        public IniParser GetIni(string path)
        {
            path = ValidateRelativePath(path + ".ini");
            if (path == null)
                return null;

            if (File.Exists(path))
                return new IniParser(path);

            return null;
        }

        public bool IniExists(string path)
        {
            path = ValidateRelativePath(path + ".ini");
            if (path == null)
                return false;

            return File.Exists(path);
        }

        public IniParser CreateIni(string path = null)
        {
            try {
                path = ValidateRelativePath(path + ".ini");
                if (String.IsNullOrEmpty(path)) {
                    path = Name;
                }
                if (IniExists(path))
                    return GetIni(path);

                File.WriteAllText(path, "");
                return new IniParser(path);
            } catch (Exception ex) {
                Logger.LogException(ex);
            }
            return null;
        }

        public List<IniParser> GetInis(string path)
        {
            path = ValidateRelativePath(path);
            if (path == null)
                return new List<IniParser>();

            return Directory.GetFiles(path).Select(p => new IniParser(p)).ToList();
        }

        #endregion

        public BasePlugin GetPlugin(string name)
        {
            BasePlugin plugin;
            if (!PluginLoader.GetInstance().Plugins.TryGetValue(name, out plugin)) {
                return null;
            }
            return plugin;
        }

        #region time

		public string GetDate() => DateTime.Now.ToShortDateString();

		public int GetTicks() =>  Environment.TickCount;

		public string GetTime() => DateTime.Now.ToShortTimeString();

		public long GetTimestamp() => (long)(DateTime.UtcNow - new DateTime(0x7b2, 1, 1, 0, 0, 0)).TotalSeconds;

        #endregion

        #region hooks

        public void OnTimerCB(TimedEvent evt)
        {
            if (Globals.Contains(evt.Name + "Callback"))
                Invoke(evt.Name + "Callback", evt);
        }

        #endregion

        #region timer methods

        public TimedEvent CreateTimer(string name, int timeoutDelay)
        {
            TimedEvent timedEvent = GetTimer(name);
            if (timedEvent == null) {
                timedEvent = new TimedEvent(name, timeoutDelay);
                timedEvent.OnFire += OnTimerCB;
                Timers.Add(name, timedEvent);
            }
            return timedEvent;
        }

        public TimedEvent CreateTimer(string name, int timeoutDelay, Action<TimedEvent> callback)
        {
            TimedEvent timedEvent = GetTimer(name);
            if (timedEvent == null) {
                timedEvent = new TimedEvent(name, timeoutDelay);
				timedEvent.OnFire += callback.Invoke;
                Timers.Add(name, timedEvent);
            }
            return timedEvent;
        }

        public TimedEvent CreateTimer(string name, int timeoutDelay, Dictionary<string, object> args)
        {
            TimedEvent timedEvent = GetTimer(name);
            if (timedEvent == null) {
                timedEvent = new TimedEvent(name, timeoutDelay);
                timedEvent.Args = args;
                timedEvent.OnFire += OnTimerCB;
                Timers.Add(name, timedEvent);
            }
            return timedEvent;
        }

        public TimedEvent CreateTimer(string name, int timeoutDelay, Dictionary<string, object> args, Action<TimedEvent> callback)
        {
            TimedEvent timedEvent = GetTimer(name);
            if (timedEvent == null) {
                timedEvent = new TimedEvent(name, timeoutDelay);
                timedEvent.Args = args;
				timedEvent.OnFire += callback.Invoke;
                Timers.Add(name, timedEvent);
            }
            return timedEvent;
        }

        public TimedEvent GetTimer(string name)
        {
            TimedEvent result;
            if (Timers.ContainsKey(name)) {
                result = Timers[name];
            } else {
                result = null;
            }
            return result;
        }

        public void KillTimer(string name)
        {
            TimedEvent timer = GetTimer(name);
            if (timer == null)
                return;

            timer.Kill();
            Timers.Remove(name);
        }

        public void KillTimers()
        {
            foreach (TimedEvent current in Timers.Values) {
                current.Kill();
            }
            foreach (TimedEvent timer in ParallelTimers) {
                timer.Kill();
            }
            Timers.Clear();
            ParallelTimers.Clear();
        }

        #endregion

        #region ParalellTimers

        public TimedEvent CreateParallelTimer(string name, int timeoutDelay, Dictionary<string, object> args)
        {
            var timedEvent = new TimedEvent(name, timeoutDelay);
            timedEvent.Args = args;
            timedEvent.OnFire += OnTimerCB;
            ParallelTimers.Add(timedEvent);
            return timedEvent;
        }

        public TimedEvent CreateParallelTimer(string name, int timeoutDelay, Dictionary<string, object> args, Action<TimedEvent> callback)
        {
            var timedEvent = new TimedEvent(name, timeoutDelay);
            timedEvent.Args = args;
			timedEvent.OnFire += callback.Invoke;
            ParallelTimers.Add(timedEvent);
            return timedEvent;
        }

        public List<TimedEvent> GetParallelTimer(string name)
        {
            return (from timer in ParallelTimers
                             where timer.Name == name
                             select timer).ToList();
        }

        public void KillParallelTimer(string name)
        {
            foreach (TimedEvent timer in GetParallelTimer(name)) {
                timer.Kill();
                ParallelTimers.Remove(timer);
            }
        }

        #endregion

        #region WEB

		public string GET(string url) => Web.GET(url);

		public string POST(string url, string data) => Web.POST(url, data);

		public string POSTJSON(string url, string json) => Web.POSTJSON(url, json);

        #endregion

		public Dictionary<string, object> CreateDict(int cap = 10) => new Dictionary<string, object>(cap);
    }

    public class Web : Singleton<Web>, ISingleton
    {
        public void Initialize(){ }

        public string UserAgent = "Pluton Plugin - " + Bootstrap.Version;

        public string GET(string url)
        {
            using (System.Net.WebClient client = new System.Net.WebClient()) {
                client.Headers[HttpRequestHeader.UserAgent] = UserAgent;
                return client.DownloadString(url);
            }
        }

        public string GET(string url, Func<SSLVerificationEvent, bool> verifySSLcallback)
        {
            using (System.Net.WebClient client = new System.Net.WebClient()) {
                System.Net.Security.RemoteCertificateValidationCallback verifyssl = (sender, cert, chain, errors) => verifySSLcallback.Invoke(new SSLVerificationEvent(cert, chain, errors));
                System.Net.ServicePointManager.ServerCertificateValidationCallback += verifyssl;
                client.Headers[HttpRequestHeader.UserAgent] = UserAgent;
                string result = client.DownloadString(url);
                System.Net.ServicePointManager.ServerCertificateValidationCallback -= verifyssl;
                return result;
            }
        }

        public void GETAsync(string url, Action<string> callback)
        {
            using (System.Net.WebClient client = new System.Net.WebClient()) {
                client.Headers[HttpRequestHeader.UserAgent] = UserAgent;
                client.DownloadStringCompleted += (s, e) => callback.Invoke(e.Result);
                client.DownloadStringAsync(new Uri(url));
            }
        }

        public string POST(string url, string data)
        {
            using (WebClient client = new WebClient()) {
                client.Headers[HttpRequestHeader.UserAgent] = UserAgent;
                client.Headers[HttpRequestHeader.ContentType] = "application/x-www-form-urlencoded";
                return client.UploadString(url, "POST", data);
            }
        }

        public string POST(string url, string data, Func<SSLVerificationEvent, bool> verifySSLcallback)
        {
            using (WebClient client = new WebClient()) {
                System.Net.Security.RemoteCertificateValidationCallback verifyssl = (sender, cert, chain, errors) => verifySSLcallback.Invoke(new SSLVerificationEvent(cert, chain, errors));
                System.Net.ServicePointManager.ServerCertificateValidationCallback += verifyssl;
                client.Headers[HttpRequestHeader.UserAgent] = UserAgent;
                client.Headers[HttpRequestHeader.ContentType] = "application/x-www-form-urlencoded";
                string result = client.UploadString(url, "POST", data);
                System.Net.ServicePointManager.ServerCertificateValidationCallback -= verifyssl;
                return result;
            }
        }

        public void POSTAsync(string url, string data, Action<string> callback)
        {
            using (WebClient client = new WebClient()) {
                client.Headers[HttpRequestHeader.UserAgent] = UserAgent;
                client.Headers[HttpRequestHeader.ContentType] = "application/x-www-form-urlencoded";
                client.UploadStringCompleted += (s, e) => callback.Invoke(e.Result);
                client.UploadStringAsync(new Uri(url), "POST", data);
            }
        }

        public string DELETE(string url)
        {
            using (WebClient client = new WebClient()) {
                client.Headers[HttpRequestHeader.UserAgent] = UserAgent;
                return client.UploadString(url, "DELETE", "");
            }
        }

        public string DELETE(string url, Func<SSLVerificationEvent, bool> verifySSLcallback)
        {
            using (WebClient client = new WebClient()) {
                System.Net.Security.RemoteCertificateValidationCallback verifyssl = (sender, cert, chain, errors) => verifySSLcallback.Invoke(new SSLVerificationEvent(cert, chain, errors));
                System.Net.ServicePointManager.ServerCertificateValidationCallback += verifyssl;
                client.Headers[HttpRequestHeader.UserAgent] = UserAgent;
                string result = client.UploadString(url, "DELETE", "");
                System.Net.ServicePointManager.ServerCertificateValidationCallback -= verifyssl;
                return result;
            }
        }

        public void DELETEAsync(string url, Action<string> callback)
        {
            using (WebClient client = new WebClient()) {
                client.Headers[HttpRequestHeader.UserAgent] = UserAgent;
                client.UploadStringCompleted += (s, e) => callback.Invoke(e.Result);
                client.UploadStringAsync(new Uri(url), "DELETE", "");
            }
        }

        public string PUT(string url, string data)
        {
            using (WebClient client = new WebClient()) {
                client.Headers[HttpRequestHeader.UserAgent] = UserAgent;
                client.Headers[HttpRequestHeader.ContentType] = "application/x-www-form-urlencoded";
                return client.UploadString(url, "PUT", data);
            }
        }

        public string PUT(string url, string data, Func<SSLVerificationEvent, bool> verifySSLcallback)
        {
            using (WebClient client = new WebClient()) {
                System.Net.Security.RemoteCertificateValidationCallback verifyssl = (sender, cert, chain, errors) => verifySSLcallback.Invoke(new SSLVerificationEvent(cert, chain, errors));
                System.Net.ServicePointManager.ServerCertificateValidationCallback += verifyssl;
                client.Headers[HttpRequestHeader.UserAgent] = UserAgent;
                client.Headers[HttpRequestHeader.ContentType] = "application/x-www-form-urlencoded";
                string result = client.UploadString(url, "PUT", data);
                System.Net.ServicePointManager.ServerCertificateValidationCallback -= verifyssl;
                return result;
            }
        }

        public void PUTAsync(string url, string data, Action<string> callback)
        {
            using (WebClient client = new WebClient()) {
                client.Headers[HttpRequestHeader.UserAgent] = UserAgent;
                client.Headers[HttpRequestHeader.ContentType] = "application/x-www-form-urlencoded";
                client.UploadStringCompleted += (s, e) => callback.Invoke(e.Result);
                client.UploadStringAsync(new Uri(url), "PUT", data);
            }
        }

        public string PATCH(string url, string data)
        {
            using (WebClient client = new WebClient()) {
                client.Headers[HttpRequestHeader.UserAgent] = UserAgent;
                client.Headers[HttpRequestHeader.ContentType] = "application/x-www-form-urlencoded";
                return client.UploadString(url, "PATCH", data);
            }
        }

        public string PATCH(string url, string data, Func<SSLVerificationEvent, bool> verifySSLcallback)
        {
            using (WebClient client = new WebClient()) {
                System.Net.Security.RemoteCertificateValidationCallback verifyssl = (sender, cert, chain, errors) => verifySSLcallback.Invoke(new SSLVerificationEvent(cert, chain, errors));
                System.Net.ServicePointManager.ServerCertificateValidationCallback += verifyssl;
                client.Headers[HttpRequestHeader.UserAgent] = UserAgent;
                client.Headers[HttpRequestHeader.ContentType] = "application/x-www-form-urlencoded";
                string result = client.UploadString(url, "PATCH", data);
                System.Net.ServicePointManager.ServerCertificateValidationCallback -= verifyssl;
                return result;
            }
        }

        public void PATCHAsync(string url, string data, Action<string> callback)
        {
            using (WebClient client = new WebClient()) {
                client.Headers[HttpRequestHeader.UserAgent] = UserAgent;
                client.Headers[HttpRequestHeader.ContentType] = "application/x-www-form-urlencoded";
                client.UploadStringCompleted += (s, e) => callback.Invoke(e.Result);
                client.UploadStringAsync(new Uri(url), "PATCH", data);
            }
        }

        public string OPTIONS(string url)
        {
            using (WebClient client = new WebClient()) {
                client.Headers[HttpRequestHeader.UserAgent] = UserAgent;
                client.UploadString(url, "OPTIONS", "");
                return client.ResponseHeaders["Allow"];
            }
        }

        public string OPTIONS(string url, Func<SSLVerificationEvent, bool> verifySSLcallback)
        {
            using (WebClient client = new WebClient()) {
                System.Net.Security.RemoteCertificateValidationCallback verifyssl = (sender, cert, chain, errors) => verifySSLcallback.Invoke(new SSLVerificationEvent(cert, chain, errors));
                System.Net.ServicePointManager.ServerCertificateValidationCallback += verifyssl;
                client.Headers[HttpRequestHeader.UserAgent] = UserAgent;
                client.UploadString(url, "OPTIONS", "");
                string result = client.ResponseHeaders["Allow"];
                System.Net.ServicePointManager.ServerCertificateValidationCallback -= verifyssl;
                return result;
            }
        }

        public void OPTIONSAsync(string url, Action<string> callback)
        {
            using (WebClient client = new WebClient()) {
                client.Headers[HttpRequestHeader.UserAgent] = UserAgent;
                client.UploadStringCompleted += (s, e) => callback.Invoke(client.ResponseHeaders["Allow"]);
                client.UploadStringAsync(new Uri(url), "OPTIONS", "");
            }
        }

        public string POSTJSON(string url, string json)
        {
            using (WebClient client = new WebClient()) {
                client.Headers[HttpRequestHeader.UserAgent] = UserAgent;
                client.Headers[HttpRequestHeader.ContentType] = "application/json";
                return client.UploadString(url, "POST", json);
            }
        }

        public string POSTJSON(string url, string json, Func<SSLVerificationEvent, bool> verifySSLcallback)
        {
            using (WebClient client = new WebClient()) {
                System.Net.Security.RemoteCertificateValidationCallback verifyssl = (sender, cert, chain, errors) => verifySSLcallback.Invoke(new SSLVerificationEvent(cert, chain, errors));
				ServicePointManager.ServerCertificateValidationCallback += verifyssl;
                client.Headers[HttpRequestHeader.UserAgent] = UserAgent;
                client.Headers[HttpRequestHeader.ContentType] = "application/json";
                string result = client.UploadString(url, "POST", json);
                System.Net.ServicePointManager.ServerCertificateValidationCallback -= verifyssl;
                return result;
            }
        }

        public class SSLVerificationEvent
        {
            public System.Security.Cryptography.X509Certificates.X509Certificate Cert;
            public System.Security.Cryptography.X509Certificates.X509Chain Chain;
            public System.Net.Security.SslPolicyErrors Errors;

            public bool HasErrors = false;

            public SSLVerificationEvent(
                System.Security.Cryptography.X509Certificates.X509Certificate cert,
                System.Security.Cryptography.X509Certificates.X509Chain chain,
                System.Net.Security.SslPolicyErrors errors
            )
            {
                Cert = cert;
                Chain = chain;
                Errors = errors;
                HasErrors = Errors != System.Net.Security.SslPolicyErrors.None;
            }
        }
    }
}


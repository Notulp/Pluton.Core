namespace Pluton.Core {
	using System;
	using System.IO;
	using UnityEngine;
	using System.Collections.Generic;

	public static class Logger {
		struct Writer {
			public StreamWriter LogWriter;
			public string DateTime;
		}

		static string LogsFolder;
		static Writer LogWriter;
		static Writer WarnWriter;
		static Writer ErrorWriter;
		static Writer ChatWriter;
		static bool showChat = false;
		static bool showDebug = false;
		static bool showErrors = false;
		static bool showException = false;
		static bool showWarnings = false;
		static bool logChat = false;
		static bool logDebug = false;
		static bool logErrors = false;
		static bool logException = false;
		static bool logWarnings = false;

		static bool Initialized = false;

		public static void Init() {
			try {
				logChat = Config.GetInstance().GetBoolValue("Logging", "chatInLog", true);
				logDebug = Config.GetInstance().GetBoolValue("Logging", "debugInLog", true);
				logErrors = Config.GetInstance().GetBoolValue("Logging", "errorInLog", true);
				logException = Config.GetInstance().GetBoolValue("Logging", "exceptionInLog", true);
				logWarnings = Config.GetInstance().GetBoolValue("Logging", "warningInLog", true);

				showChat = Config.GetInstance().GetBoolValue("Logging", "chatInConsole", true);
				showDebug = Config.GetInstance().GetBoolValue("Logging", "debugInConsole", true);
				showErrors = Config.GetInstance().GetBoolValue("Logging", "errorInConsole", true);
				showException = Config.GetInstance().GetBoolValue("Logging", "exceptionInConsole", true);
				showWarnings = Config.GetInstance().GetBoolValue("Logging", "warningInConsole", true);
			} catch (Exception ex) {
				Debug.LogException(ex);
			}

			try {
				LogsFolder = Path.Combine(Util.GetInstance().GetPublicFolder(), "Logs");
				Debug.Log("logsfolder: " + LogsFolder);

				if (!Directory.Exists(LogsFolder))
					Directory.CreateDirectory(LogsFolder);

				LogWriterInit();
				ChatWriterInit();
				ErrorWriterInit();
				WarnWriterInit();

				Initialized = true;
			} catch (Exception ex) {
				Debug.LogException(ex);
			}
		}

		static void LogWriterInit() {
			try {
				if (LogWriter.LogWriter != null)
					LogWriter.LogWriter.Close();

				LogWriter.DateTime = DateTime.Now.ToString("dd_MM_yyyy");
				LogWriter.LogWriter = new StreamWriter(Path.Combine(LogsFolder, $"Log {LogWriter.DateTime}.txt"), true);
				LogWriter.LogWriter.AutoFlush = true;
			} catch (Exception ex) {
				Debug.LogException(ex);
			}
		}

		static void WarnWriterInit() {
			try {
				if (WarnWriter.LogWriter != null)
					WarnWriter.LogWriter.Close();

				WarnWriter.DateTime = DateTime.Now.ToString("dd_MM_yyyy");
				WarnWriter.LogWriter = new StreamWriter(Path.Combine(LogsFolder, $"Warning {WarnWriter.DateTime}.txt"), true);
				                                        
				                                        
				WarnWriter.LogWriter.AutoFlush = true;
			} catch (Exception ex) {
				Debug.LogException(ex);
			}
		}

		static void ErrorWriterInit() {
			try {
				if (ErrorWriter.LogWriter != null)
					ErrorWriter.LogWriter.Close();

				ErrorWriter.DateTime = DateTime.Now.ToString("dd_MM_yyyy");
				ErrorWriter.LogWriter = new StreamWriter(Path.Combine(LogsFolder, $"Error {ErrorWriter.DateTime}.txt"), true);
				                                         
				                                         
				ErrorWriter.LogWriter.AutoFlush = true;
			} catch (Exception ex) {
				Debug.LogException(ex);
			}
		}

		static void ChatWriterInit() {
			try {
				if (ChatWriter.LogWriter != null)
					ChatWriter.LogWriter.Close();

				ChatWriter.DateTime = DateTime.Now.ToString("dd_MM_yyyy");
				ChatWriter.LogWriter = new StreamWriter(Path.Combine(LogsFolder, $"Chat {ChatWriter.DateTime}.txt"), true);
				ChatWriter.LogWriter.AutoFlush = true;
			} catch (Exception ex) {
				Debug.LogException(ex);
			}
		}

		static string LogFormat(string Text) {
			return $"[{DateTime.Now}] {Text}";
		}

		static void WriteLog(string Message) {
			try {
				if (LogWriter.DateTime != DateTime.Now.ToString("dd_MM_yyyy"))
					LogWriterInit();
				
				LogWriter.LogWriter.WriteLine(LogFormat(Message));
			} catch (Exception ex) {
				Debug.LogException(ex);
			}
		}

		static void WriteWarn(string Message) {
			try {
				if (WarnWriter.DateTime != DateTime.Now.ToString("dd_MM_yyyy"))
					WarnWriterInit();
				
				WarnWriter.LogWriter.WriteLine(LogFormat(Message));
			} catch (Exception ex) {
				Debug.LogException(ex);
			}
		}

		static void WriteError(string Message) {
			try {
				if (ErrorWriter.DateTime != DateTime.Now.ToString("dd_MM_yyyy"))
					ErrorWriterInit();
				
				ErrorWriter.LogWriter.WriteLine(LogFormat(Message));
			} catch (Exception ex) {
				Debug.LogException(ex);
			}
		}

		static void WriteChat(string Message) {
			try {
				if (ChatWriter.DateTime != DateTime.Now.ToString("dd_MM_yyyy"))
					ChatWriterInit();
				ChatWriter.LogWriter.WriteLine(LogFormat(Message));
			} catch (Exception ex) {
				Debug.LogException(ex);
			}
		}

		// verbose?
		public static void Log(string Message, UnityEngine.Object Context = null) {
			Message = "[Console] " + Message;
			Debug.Log(Message, Context);
			if (Initialized)
				WriteLog(Message);
		}

		public static void LogWarning(string Message, UnityEngine.Object Context = null) {
			Message = "[Warning] " + Message;
			if (showWarnings)
				Debug.LogWarning(Message, Context);

			if (!logWarnings || !Initialized)
				return;

			WriteWarn(Message);
		}

		public static void LogError(string Message, UnityEngine.Object Context = null) {
			Message = "[Error] " + Message;
			if (showErrors)
				Debug.LogError(Message, Context);

			if (!logErrors || !Initialized)
				return;

			WriteError(Message);
		}

		public static void LogException(Exception Ex, UnityEngine.Object Context = null) {
			if (showException)
				Debug.LogException(Ex, Context);
			
			if (!logException || !Initialized)
				return;
			
			string Message = "[Exception]\r\n\r\n" + (Ex == null ? "(null) exception" : Ex.ToString()) + "\r\n";
			
			WriteError(Message);
		}

		public static void LogDebug(string Message, UnityEngine.Object Context = null) {
			Message = "[Debug] " + Message;
			if (showDebug)
				Debug.Log(Message, Context);

			if (!logDebug || !Initialized)
				return;

			WriteLog(Message);
		}

		public static void ChatLog(string Sender, string Msg) {
			Msg = "[CHAT] " + Sender + ": " + Msg;
			if (showChat)
				Debug.Log(Msg);
			if (!logChat || !Initialized)
				return;

			WriteChat(Msg);
		}
	}
}


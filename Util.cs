namespace Pluton.Core {
	using Pluton.Core.PluginLoaders;
	using System;
	using System.Collections;
	using System.Collections.Generic;
	using System.IO;
	using System.Reflection;
	using System.Runtime.Serialization.Formatters.Binary;
	using System.Text.RegularExpressions;
	using UnityEngine;

	public class Util : Singleton<Util>, ISingleton {
		public readonly Dictionary<string, Type> typeCache = new Dictionary<string, Type>();

		public void DestroyObject(GameObject go) => UnityEngine.Object.DestroyImmediate(go);

		public bool DumpObjToFile(string path, object obj, string prefix = "")
					=> DumpObjToFile(path, obj, 1, 30, false, false, prefix);

		public bool DumpObjToFile(string path, object obj, int depth, string prefix = "")
					=> DumpObjToFile(path, obj, depth, 30, false, false, prefix);

		public bool DumpObjToFile(string path, object obj, int depth, int maxItems, string prefix = "")
					=> DumpObjToFile(path, obj, depth, maxItems, false, false, prefix);

		public bool DumpObjToFile(string path, object obj, int depth, int maxItems, bool disPrivate, string prefix = "")
					=> DumpObjToFile(path, obj, depth, maxItems, disPrivate, false, prefix);

		public bool DumpObjToFile(string path, object obj, int depth, int maxItems, bool disPrivate, bool fullClassName, string prefix = "") {
			path = DataStore.GetInstance().RemoveChars(path);
			path = Path.Combine(Path.Combine(GetPublicFolder(), "Dumps"), path + ".dump");
			if (path == null)
				return false;

			string result = String.Empty;

			var settings = new DumpSettings();
			settings.MaxDepth = depth;
			settings.MaxItems = maxItems;
			settings.DisplayPrivate = disPrivate;
			settings.UseFullClassNames = fullClassName;
			result = Dump.ToDump(obj, obj.GetType(), prefix, settings);

			string dumpHeader =
				"Object type: " + obj.GetType().ToString() + "\r\n" +
				"TimeNow: " + DateTime.Now.ToShortDateString() + " " + DateTime.Now.ToShortTimeString() + "\r\n" +
				"Depth: " + depth.ToString() + "\r\n" +
				"MaxItems: " + maxItems.ToString() + "\r\n" +
				"ShowPrivate: " + disPrivate.ToString() + "\r\n" +
				"UseFullClassName: " + fullClassName.ToString() + "\r\n\r\n";

			File.AppendAllText(path, dumpHeader);
			File.AppendAllText(path, result + "\r\n\r\n");
			return true;
		}

		public string NormalizePath(string path) => path.Replace(@"\\", @"\").Replace("//", "/").Trim();

		public string GetAbsoluteFilePath(string fileName) => Path.Combine(GetPublicFolder(), fileName);

		public string GetPluginsFolder() => Path.Combine(GetPublicFolder(), "Plugins");

		public virtual string GetPublicFolder() => Path.Combine(GetRootFolder(), "Pluton");

		public string GetRootFolder() => Path.GetDirectoryName(Path.GetDirectoryName(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)));

		public string GetServerFolder() => Path.GetDirectoryName(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location));

		public string GetManagedFolder() => Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

		public void Initialize() { }

		public float GetVectorsDistance(Vector3 v1, Vector3 v2) => Vector3.Distance(v1, v2);

		public string[] GetQuotedArgs(string[] sArr) {
			bool inQuote = false;
			string current = "";
			var final = new List<string>();

			foreach (string str in sArr) {
				inQuote |= str.StartsWith("\"", StringComparison.CurrentCulture);

				inQuote &= !str.EndsWith("\"", StringComparison.CurrentCulture);

				if (inQuote) {
					if (current != "")
						current += " " + str;
					if (current == "")
						current = str;
				}

				if (!inQuote) {
					if (current != "")
						final.Add((current + " " + str).Replace("\"", ""));
					if (current == "")
						final.Add(str.Replace("\"", ""));
					current = "";
				}
			}
			return final.ToArray();
		}

		public static Hashtable HashtableFromFile(string path) {
			using (FileStream stream = new FileStream(path, FileMode.Open)) {
				var formatter = new BinaryFormatter();
				return (Hashtable)formatter.Deserialize(stream);
			}
		}

		public static void HashtableToFile(Hashtable ht, string path) {
			using (FileStream stream = new FileStream(path, FileMode.Create)) {
				var formatter = new BinaryFormatter();
				formatter.Serialize(stream, ht);
			}
		}

		public Vector3 Infront(Vector3 v3, float length) => v3 + Vector3.forward * length;

		public bool IsNull(object obj) => obj == null;

		public void Log(string str) => Logger.Log(str);

		public Match Regex(string input, string match) => new Regex(input).Match(match);

		public Quaternion RotateX(Quaternion q, float angle) => q *= Quaternion.Euler(angle, 0f, 0f);

		public Quaternion RotateY(Quaternion q, float angle) => q *= Quaternion.Euler(0f, angle, 0f);

		public Quaternion RotateZ(Quaternion q, float angle) => q *= Quaternion.Euler(0f, 0f, angle);

		public bool TryFindType(string typeName, out Type t) {
			lock (typeCache) {
				if (!typeCache.TryGetValue(typeName, out t)) {
					foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies()) {
						t = assembly.GetType(typeName);
						if (t != null) {
							break;
						}
					}
					typeCache[typeName] = t;
				}
			}
			return (t != null);
		}

		public Type TryFindReturnType(string typeName) {
			Type t;
			if (TryFindType(typeName, out t))
				return t;
			throw new Exception("Type not found " + typeName);
		}
	}
}


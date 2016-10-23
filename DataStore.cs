namespace Pluton.Core
{
	using System;
	using System.Collections;
	using System.Collections.Generic;
	using System.IO;
	using System.Linq;
	using UnityEngine;
	using Serialize;

	public class DataStore : CountedInstance
	{
		public readonly Hashtable datastore;
		static DataStore instance;
		public string PATH;

		public object this[string tablename, object key] {
			get {
				return Get(tablename, key);
			}
			set {
				Add(tablename, key, value);
			}
		}

		static object SerializeIfPossible(object keyorval)
		{
			if (keyorval == null)
				return keyorval;

			if (keyorval is Vector3) {
				return ((Vector3)keyorval).Serialize();
			}
			if (keyorval is Quaternion) {
				return ((Quaternion)keyorval).Serialize();
			}

			return keyorval;
		}

		static object DeserializeIfPossible(object keyorval)
		{
			if (keyorval is ISerializable) {
				return ((ISerializable)keyorval).Deserialize();
			}
			return keyorval;
		}

		public string RemoveChars(string str)
		{
			foreach (string c in new string[] { "/", "\\", "%", "$" }) {
				str = str.Replace(c, "");
			}
			return str;
		}

		public bool ToIni(string inifilename = "DataStore")
		{
			string inipath = Path.Combine(Util.GetInstance().GetPublicFolder(), RemoveChars(inifilename).Trim() + ".ini");
			File.WriteAllText(inipath, "");
			var ini = new IniParser(inipath);
			ini.Save();

			foreach (string section in datastore.Keys) {
				var ht = (Hashtable)datastore[section];
				foreach (object setting in ht.Keys) {
					try {
						string key = "NullReference";
						string val = "NullReference";
						if (setting != null) {
							if (setting.GetType().GetMethod("ToString", Type.EmptyTypes) == null) {
								key = "type:" + setting.GetType().ToString();
							} else {
								key = setting.ToString();
							}
						}

						if (ht[setting] != null) {
							if (ht[setting].GetType().GetMethod("ToString", Type.EmptyTypes) == null) {
								val = "type:" + ht[setting].GetType().ToString();
							} else {
								val = ht[setting].ToString();
							}
						}

						ini.AddSetting(section, key, val);
					} catch (Exception ex) {
						Logger.LogException(ex);
					}
				}
			}
			ini.Save();
			return true;
		}

		public bool TableToIni(string tablename, string inipath)
		{
			var hashtable = (Hashtable)datastore[tablename];
			if (hashtable != null) {
				if (!Path.HasExtension(inipath))
					inipath += ".ini";
				File.WriteAllText(inipath, "");
				var ini = new IniParser(inipath);
				ini.Save();

				foreach (string setting in hashtable.Keys) {
					ini.AddSetting(tablename, setting, hashtable[setting].ToString());
				}
				ini.Save();
				return true;
			}
			return false;
		}

		public bool AddFromIni(string inipath)
		{
			if (File.Exists(inipath)) {
				var ini = new IniParser(inipath);
				foreach (string section in ini.Sections.Keys) {
					foreach (string setting in ini.EnumSection(section)) {
						Add(section, setting, ini.GetSetting(section, setting));
					}
				}
				return true;
			}
			return false;
		}

		public void Add(string tablename, object key, object val)
		{
			if (key == null)
				key = "NullReference";

			var hashtable = (Hashtable)datastore[tablename];
			if (hashtable == null) {
				hashtable = new Hashtable();
				datastore.Add(tablename, hashtable);
			}
			hashtable[SerializeIfPossible(key)] = SerializeIfPossible(val);
		}

		public bool ContainsKey(string tablename, object key)
		{
			if (key == null)
				return false;

			var hashtable = (Hashtable)datastore[tablename];
			if (hashtable != null) {
				return hashtable.ContainsKey(SerializeIfPossible(key));
			}
			return false;
		}

		public bool ContainsValue(string tablename, object val)
		{
			var hashtable = (Hashtable)datastore[tablename];
			if (hashtable != null) {
				return hashtable.ContainsValue(SerializeIfPossible(val));
			}
			return false;
		}

		public int Count(string tablename)
		{
			var hashtable = (Hashtable)datastore[tablename];
			if (hashtable == null) {
				return 0;
			}
			return hashtable.Count;
		}

		public void Flush(string tablename)
		{
			if (((Hashtable)datastore[tablename]) != null) {
				datastore.Remove(tablename);
			}
		}

		public object Get(string tablename, object key)
		{
			if (key == null)
				return null;

			var hashtable = (Hashtable)datastore[tablename];
			return hashtable == null ? null : DeserializeIfPossible(hashtable[SerializeIfPossible(key)]);
		}

		public static DataStore GetInstance()
		{
			if (instance == null) {
				instance = new DataStore("PlutonDatastore.ds");
			}
			return instance;
		}

		public Hashtable GetTable(string tablename)
		{
			var hashtable = (Hashtable)datastore[tablename];
			if (hashtable == null) {
				return null;
			}
			var parse = new Hashtable(hashtable.Count);
			foreach (DictionaryEntry entry in hashtable) {
				parse.Add(DeserializeIfPossible(entry.Key), DeserializeIfPossible(entry.Value));
			}
			return parse;
		}

		public object[] Keys(string tablename)
		{
			var hashtable = (Hashtable)datastore[tablename];
			if (hashtable == null) {
				return null;
			}
			var parse = new List<object>(hashtable.Keys.Count);
			foreach (object key in hashtable.Keys) {
				parse.Add(DeserializeIfPossible(key));
			}
			return parse.ToArray<object>();
		}

		public void Load()
		{
			if (File.Exists(PATH)) {
				try {
					Hashtable hashtable = Util.HashtableFromFile(PATH);

					datastore.Clear();
					int count = 0;
					foreach (DictionaryEntry entry in hashtable) {
						datastore[entry.Key] = entry.Value;
						count += (entry.Value as Hashtable).Count;
					}

					Debug.Log("DataStore Loaded from " + PATH);
					Debug.Log($"Tables: {datastore.Count}! Keys: {count}");
				} catch (Exception ex) {
					Logger.LogException(ex);
				}
			}
		}

		public void Remove(string tablename, object key)
		{
			if (key == null)
				return;

			var hashtable = (Hashtable)datastore[tablename];
			if (hashtable != null) {
				hashtable.Remove(SerializeIfPossible(key));
			}
		}

		public void Save()
		{
			if (datastore.Count != 0) {
				Util.HashtableToFile(datastore, PATH);
				Debug.Log("DataStore saved to " + PATH);
			}
		}

		public object[] Values(string tablename)
		{
			var hashtable = (Hashtable)datastore[tablename];
			if (hashtable == null) {
				return null;
			}
			var parse = new List<object>(hashtable.Values.Count);
			foreach (object val in hashtable.Values) {
				parse.Add(DeserializeIfPossible(val));
			}
			return parse.ToArray();
		}

		public DataStore(string path)
		{
			path = RemoveChars(path);
			datastore = new Hashtable();
			PATH = Path.Combine(Util.GetInstance().GetPublicFolder(), path);
			Debug.Log("New DataStore instance: " + PATH);
		}
	}
}


//#define USING_EASY_SAVE
#define USING_MESSAGE_PACK
#if USING_MESSAGE_PACK
using MessagePack;
using MessagePack.Resolvers;
using System.IO;
using System.Security.Cryptography;
using System;
#endif
using System.Collections;
using System.Collections.Generic;
using DanielDangToolkit;
using UnityEngine;

public class SaveManager : Singleton<SaveManager> {
#if USING_EASY_SAVE
	ES3Settings settings;

    public static string SAVE_FILE_UNIQUE_TAG = "savefile-auth-id";
    public static string SPECIALS_SAVE_FILE_KEY = "hunger-noun-fish-dark-replace-me";

	public static ES3Settings Settings
	{
		get { return Instance.settings; }
	}

	// Use this for initialization
	override protected void Awake () {
		base.Awake();
		settings = new ES3Settings();
		settings.encryptionType = ES3.EncryptionType.AES;

		ValidateExistingSaveFile ();
	}

	/// <summary>
	/// Validates the existing save file. This WILL nuke the file if userID cannot be read. This could be due to 
	/// tampering, corruption or switching between encryption or no encrption save file.
	/// </summary>
	void ValidateExistingSaveFile()
	{
		if (ES3.FileExists (settings)) {
			try {
				ES3.KeyExists(SAVE_FILE_UNIQUE_TAG, settings);
			}
			catch {
				ES3.DeleteFile (settings);
                NewSaveFile();
                return;
			}

            if (Load(SAVE_FILE_UNIQUE_TAG, "") != SPECIALS_SAVE_FILE_KEY)
            {
                Debug.Log("Corrupt or incorrect file!");
                ES3.DeleteFile(settings);
                NewSaveFile();
            }
        }
        else
        {
            Save(SAVE_FILE_UNIQUE_TAG, SPECIALS_SAVE_FILE_KEY);
        }
    }

    void NewSaveFile()
    {
        Save(SAVE_FILE_UNIQUE_TAG, SPECIALS_SAVE_FILE_KEY);
    }

    public static void Save<T>(string key, T value)
	{
		if (value != null) {
			ES3.Save<T> (key, value, Settings);
		}
	}

	public static T Load<T>(string key)
	{
		if (Exists (key))
			return ES3.Load<T> (key, SaveManager.Settings);
		else
			return default(T);
	}

	public static T Load<T>(string key, T defaultValue)
	{
		if (Exists (key))
			return ES3.Load<T> (key, defaultValue, SaveManager.Settings);
		else
			return defaultValue;
	}

	public static bool Exists (string key)
	{
		bool fileExists = ES3.FileExists (Settings);
		if (fileExists)
			return ES3.KeyExists (key, Settings);
		else
			return false;
	}

	public static void Delete(string key) 
	{
		if (Exists(key))
		{
			ES3.DeleteKey (key, Settings);
		}
	}

    public static void DeleteSaveFile()
    {
        ES3.DeleteFile (Instance.settings);  
    }
#elif USING_MESSAGE_PACK
	private static string SaveFilePath;
	private static byte[] EncryptionKey;
	private static byte[] EncryptionIV;

	public const string SAVE_FILE_UNIQUE_TAG = "savefile-auth-id";
    public string UNIQUE_PASSWORD = "";
    public string outputFilename = "savegame.mpak";
    public static string SPECIALS_SAVE_FILE_KEY = "";

    private Dictionary<string, object> wholeSaveFileInMemory = new Dictionary<string, object>();

	// Use this for initialization
	override protected void Awake () {
		base.Awake();

        SaveFilePath = Path.Combine(Application.persistentDataPath, Instance.outputFilename);
        if (string.IsNullOrEmpty(SPECIALS_SAVE_FILE_KEY))
            SPECIALS_SAVE_FILE_KEY = Instance.UNIQUE_PASSWORD;

        // Derive proper 32-byte key + 16-byte IV from your password (same as EasySave does internally)
        using var aes = Aes.Create();
        aes.KeySize = 256;
        aes.BlockSize = 128;
        aes.Mode = CipherMode.CBC;
        aes.Padding = PaddingMode.PKCS7;

        var pdb = new Rfc2898DeriveBytes(SPECIALS_SAVE_FILE_KEY, salt: System.Text.Encoding.UTF8.GetBytes("EasySaveReplacementSalt"), 100000, HashAlgorithmName.SHA256);
        EncryptionKey = pdb.GetBytes(32); // 256-bit key
        EncryptionIV = pdb.GetBytes(16); // 128-bit IV (fixed per password+salt)

        ValidateExistingSaveFile ();
	}

    /// <summary>
    /// Validates the existing save file. This WILL nuke the file if userID cannot be read. This could be due to 
    /// tampering, corruption or switching between encryption or no encrption save file.
    /// </summary>
    void ValidateExistingSaveFile()
    {
        if (!File.Exists(SaveFilePath))
        {
            CreateNewSaveFile();
            return;
        }

        try
        {
            // Load into Memory!
            wholeSaveFileInMemory = LoadIntoMemory();
            string storedTag = Load<string>(SAVE_FILE_UNIQUE_TAG);
			if (storedTag != SPECIALS_SAVE_FILE_KEY)
			{
				Debug.LogWarning("Save file validation failed! Possible tampering or wrong password. Deleting...");
				DeleteSaveFile();
				CreateNewSaveFile();
			}
		}
		catch (Exception e) when (e is CryptographicException || e is InvalidOperationException || e is MessagePackSerializationException)
		{
			Debug.LogWarning("Save file corrupted or encrypted with different key. Deleting... " + e.GetType().Name);
			DeleteSaveFile();
			CreateNewSaveFile();
		}
	}

    Dictionary<string, object> LoadIntoMemory()
    {
        try
        {
            byte[] encryptedBytes = File.ReadAllBytes(SaveFilePath);
            byte[] plainBytes = AesDecrypt(encryptedBytes);
            return MessagePackSerializer.Deserialize<Dictionary<string, object>>(
                plainBytes,
                MessagePackSerializerOptions.Standard.WithResolver(ContractlessStandardResolver.Instance)
                                                   .WithCompression(MessagePackCompression.Lz4Block));
        }
        catch
        {
            return new Dictionary<string, object>();
        }
    }


	private void CreateNewSaveFile()
	{
        Instance.wholeSaveFileInMemory = new Dictionary<string, object>();
		Save(SAVE_FILE_UNIQUE_TAG, SPECIALS_SAVE_FILE_KEY);
	}

    public static void Save<T>(string key, T value)
    {
        if (value == null) return;

        // Automatically convert List<T> -> T[] for MessagePack compatibility
        if (value is System.Collections.IList list && !value.GetType().IsArray)
        {
            var elementType = value.GetType().GetGenericArguments()[0];
            var array = Array.CreateInstance(elementType, list.Count);
            for (int i = 0; i < list.Count; i++)
                array.SetValue(list[i], i);
            Instance.wholeSaveFileInMemory[key] = array;
        }
        else
        {
            Instance.wholeSaveFileInMemory[key] = value;
        }

        SaveAllData(Instance.wholeSaveFileInMemory);
    }

    public static T Load<T>(string key)
    {
        if (!Instance.wholeSaveFileInMemory.TryGetValue(key, out var obj))
            return default(T);

        if (obj is T exactMatch)
            return exactMatch;

        var targetType = typeof(T);

        // === 1. Handle List<T> <-> T[] automatic conversion ===
        if (targetType.IsGenericType && targetType.GetGenericTypeDefinition() == typeof(List<>))
        {
            if (obj is System.Collections.IEnumerable enumerable && obj is not string)
            {
                var listType = targetType.GetGenericArguments()[0];
                var templist = (System.Collections.IList)Activator.CreateInstance(targetType);
                foreach (var item in enumerable)
                    templist.Add(item);
                return (T)templist;
            }
        }

        // === 2. Handle T[] -> List<T> (your exact case) ===
        if (obj is Array array && targetType.IsGenericType &&
            targetType.GetGenericTypeDefinition() == typeof(List<>))
        {
            var elementType = targetType.GetGenericArguments()[0];
            if (array.GetType().GetElementType() == elementType ||
                elementType.IsAssignableFrom(array.GetType().GetElementType()))
            {
                var templist = (System.Collections.IList)Activator.CreateInstance(targetType);
                foreach (var item in array)
                    templist.Add(item);
                return (T)templist;
            }
        }

        // === 3. Handle List<T> -> T[] ===
        if (obj is System.Collections.IList list && targetType.IsArray)
        {
            var elementType = targetType.GetElementType();
            var arr = Array.CreateInstance(elementType, list.Count);
            for (int i = 0; i < list.Count; i++)
                arr.SetValue(list[i], i);
            return (T)(object)arr;
        }

        // === 4. Fallback: try direct cast anyway (for primitives, Vector3, etc.) ===
        try
        {
            return (T)Convert.ChangeType(obj, Nullable.GetUnderlyingType(targetType) ?? targetType);
        }
        catch { }

        return default(T);
    }

    public static T Load<T>(string key, T defaultValue)
    {
        var val = Load<T>(key);
        return val == null ? defaultValue : val; // handles null properly
    }

    public static bool Exists(string key)
    {
        //if (!File.Exists(SaveFilePath)) return false;
        return Instance.wholeSaveFileInMemory != null && Instance.wholeSaveFileInMemory.ContainsKey(key);
    }

    public static void Delete(string key)
    {
        if (!Exists(key)) return;
        Instance.wholeSaveFileInMemory.Remove(key);
        SaveAllData(Instance.wholeSaveFileInMemory);
    }

    public static void DeleteSaveFile()
    {
        if (File.Exists(SaveFilePath))
            File.Delete(SaveFilePath);
    }

    // ============================
    // Internal MessagePack + AES methods
    // ============================

    private static Dictionary<string, object> LoadAllDataOrCreateNew()
    {
        var data = LoadAllData();
        return data ?? new Dictionary<string, object>();
    }

    private static Dictionary<string, object> LoadAllData()
    {
        if (!File.Exists(SaveFilePath)) return null;

        try
        {
            byte[] encryptedBytes = File.ReadAllBytes(SaveFilePath);
            byte[] plainBytes = AesDecrypt(encryptedBytes);
            return MessagePackSerializer.Deserialize<Dictionary<string, object>>(
                plainBytes,
                MessagePackSerializerOptions.Standard.WithResolver(ContractlessStandardResolver.Instance)
                                                   .WithCompression(MessagePackCompression.Lz4Block));
        }
        catch
        {
            return null; // will trigger validation delete on next read
        }
    }

    private static void SaveAllData(Dictionary<string, object> data)
    {
        var options = MessagePackSerializerOptions.Standard
            .WithResolver(ContractlessStandardResolver.Instance)
            .WithCompression(MessagePackCompression.Lz4Block);

        byte[] plainBytes = MessagePackSerializer.Serialize(data, options);
        byte[] encryptedBytes = AesEncrypt(plainBytes);
        File.WriteAllBytes(SaveFilePath, encryptedBytes);
    }

    private static byte[] AesEncrypt(byte[] data)
    {
        using var aes = Aes.Create();
        aes.Key = EncryptionKey;
        aes.IV = EncryptionIV;
        aes.Mode = CipherMode.CBC;
        aes.Padding = PaddingMode.PKCS7;

        using var ms = new MemoryStream();
        using var cs = new CryptoStream(ms, aes.CreateEncryptor(), CryptoStreamMode.Write);
        cs.Write(data, 0, data.Length);
        cs.FlushFinalBlock();
        return ms.ToArray();
    }

    private static byte[] AesDecrypt(byte[] data)
    {
        using var aes = Aes.Create();
        aes.Key = EncryptionKey;
        aes.IV = EncryptionIV;
        aes.Mode = CipherMode.CBC;
        aes.Padding = PaddingMode.PKCS7;

        using var ms = new MemoryStream();
        using var cs = new CryptoStream(ms, aes.CreateDecryptor(), CryptoStreamMode.Write);
        cs.Write(data, 0, data.Length);
        cs.FlushFinalBlock();
        return ms.ToArray();
    }
#endif

    [ContextMenu("Open Persistent Data")]
    public void OpenPersistentData()
    {
        Application.OpenURL(Application.persistentDataPath);
    }
}

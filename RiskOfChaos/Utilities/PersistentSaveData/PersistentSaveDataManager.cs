using System;
using System.IO;
using System.Text;
using UnityEngine;

namespace RiskOfChaos.Utilities.PersistentSaveData
{
    public static class PersistentSaveDataManager
    {
        const byte SAVE_TYPE_COUNT = (byte)PersistentSaveType.Count;

        static readonly Encoding _indexFileEncoding = Encoding.ASCII;

        public static readonly string DirectoryPath = Path.Combine(Application.persistentDataPath, Main.PluginName);
        static readonly string _indexFilePath = Path.Combine(DirectoryPath, "index");

        static readonly string[] _saveFileNames = new string[SAVE_TYPE_COUNT];
        static readonly string[] _saveFilePaths = new string[SAVE_TYPE_COUNT];

        static PersistentSaveDataManager()
        {
            if (!Directory.Exists(DirectoryPath))
            {
                Directory.CreateDirectory(DirectoryPath);

#if DEBUG
                Log.Debug($"Created persistent save data directory: {DirectoryPath}");
#endif
            }
            else if (File.Exists(_indexFilePath))
            {
                string indexContents = File.ReadAllText(_indexFilePath, _indexFileEncoding);

                byte[] indexBytes = Convert.FromBase64String(indexContents);

                using MemoryStream stream = new MemoryStream(indexBytes);
                using BinaryReader reader = new BinaryReader(stream, _indexFileEncoding);

                byte pathsCount = Math.Min(SAVE_TYPE_COUNT, reader.ReadByte());

                for (int i = 0; i < pathsCount; i++)
                {
                    _saveFileNames[i] = reader.ReadString();
                }

                refreshPaths();
            }
        }

        static void refreshPaths()
        {
            for (int i = 0; i < SAVE_TYPE_COUNT; i++)
            {
                string fileName = _saveFileNames[i];

                _saveFilePaths[i] = string.IsNullOrEmpty(fileName) ? string.Empty : Path.Combine(DirectoryPath, fileName);
            }
        }

        static void refreshIndexFile()
        {
            int estimatedByteCount = 0;
            for (int i = 0; i < SAVE_TYPE_COUNT; i++)
            {
                estimatedByteCount += _indexFileEncoding.GetMaxByteCount(_saveFileNames[i].Length);
            }

            using MemoryStream stream = new MemoryStream(estimatedByteCount);
            using BinaryWriter writer = new BinaryWriter(stream, _indexFileEncoding);

            writer.Write(SAVE_TYPE_COUNT);

            for (int i = 0; i < SAVE_TYPE_COUNT; i++)
            {
                writer.Write(_saveFileNames[i]);
            }

            File.WriteAllText(_indexFilePath, Convert.ToBase64String(stream.ToArray()), _indexFileEncoding);
        }

        static string generateSaveFilePath()
        {
            return Guid.NewGuid().ToString();
        }

        public static string GetOrGenerateSaveFilePath(PersistentSaveType saveType)
        {
            if (saveType is < 0 or >= (PersistentSaveType)SAVE_TYPE_COUNT)
            {
                Log.Error($"Invalid save type {saveType}, using temporary file");
                return Path.GetTempFileName();
            }

            ref string filePath = ref _saveFilePaths[(int)saveType];

            if (string.IsNullOrEmpty(filePath))
            {
                _saveFileNames[(int)saveType] = generateSaveFilePath();
                refreshPaths();
                refreshIndexFile();
            }

            return filePath;
        }
    }
}

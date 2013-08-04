using System;
using System.IO;
using UnityEngine;

namespace Kerbtown
{
    public static class Extensions
    {
        private static DateTime _lastWriteTime;
        private static ConfigNode _settings;

        public static void WriteSetting(string settingName, string settingValue)
        {
            var fileInfo = new FileInfo(GetSaveFilePath());

            // File doesn't exist, so we only need to create the file and write the value.
            if (!fileInfo.Exists)
            {
                var cNode = new ConfigNode();
                cNode.AddValue(settingName, settingValue);
                cNode.Save(fileInfo.FullName);

                _settings = cNode;
                return;
            }

            // (Re)load the settings file if it has changed or hasn't been loaded.
            if (fileInfo.LastWriteTime != _lastWriteTime || _settings == null)
                _settings = ConfigNode.Load(fileInfo.FullName);

            if (_settings.HasValue(settingName))
                _settings.SetValue(settingName, settingValue);
            else
                _settings.AddValue(settingName, settingValue);

            _settings.Save(fileInfo.FullName);

            // Update _lastWriteTime
            fileInfo.Refresh();
            _lastWriteTime = fileInfo.LastWriteTime;
        }

        public static string ReadSetting(string settingName)
        {
            var fileInfo = new FileInfo(GetSaveFilePath());

            if (!fileInfo.Exists) return null;

            // (Re)load the settings file if it has changed or hasn't been loaded.
            if (fileInfo.LastWriteTime != _lastWriteTime || _settings == null)
                _settings = ConfigNode.Load(fileInfo.FullName);

            return _settings.GetValue(settingName);
        }

        public static string GetSaveFilePath()
        {
            return string.Format("{0}saves/{1}/KerbTown.cfg", KSPUtil.ApplicationRootPath, HighLogic.SaveFolder);
        }

        public static void PostScreenMessage(string message)
        {
            ScreenMessages.PostScreenMessage(message, 3f, ScreenMessageStyle.UPPER_CENTER);
        }

        public static void LogError(string message)
        {
            Debug.LogError("KerbTown: " + message);
        }

        public static void LogWarning(string message)
        {
            Debug.LogWarning("KerbTown: " + message);
        }

        public static void LogInfo(string message)
        {
            Debug.Log("KerbTown: " + message);
        }

        public static int SecondLastIndex(this string str, char searchCharacter)
        {
            int lastIndex = str.LastIndexOf(searchCharacter);

            if (lastIndex != -1)

            {
                return str.LastIndexOf(searchCharacter, lastIndex - 1);
            }

            return -1;
        }

        public static int SecondLastIndex(this string str, string searchString)
        {
            int lastIndex = str.LastIndexOf(searchString, StringComparison.Ordinal);

            if (lastIndex != -1)
            {
                return str.LastIndexOf(searchString, lastIndex - 1, StringComparison.Ordinal);
            }

            return -1;
        }
    }
}
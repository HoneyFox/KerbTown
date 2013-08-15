/* LICENSE
 * This source code is copyrighted.
 * All rights reserved.
 * Copyright © Ryan Irecki 2013
 */

using System;
using System.IO;
using System.Linq;
using UnityEngine;

namespace Kerbtown
{
    public static class Extensions
    {
        private static ConfigNode _ktSave;

        public static void WriteSetting(string settingName, string settingValue)
        {
            var fileInfo = new FileInfo(GetSaveFilePath());

            // File doesn't exist, so we only need to create the file and write the value.
            if (!fileInfo.Exists)
            {
                var rootConfigNode = new ConfigNode();
                var persistenceNode = rootConfigNode.AddNode("KerbTownPersistence");
                persistenceNode.AddValue(settingName, settingValue);
                
                rootConfigNode.Save(fileInfo.FullName);

                _ktSave = rootConfigNode;
                return;
            }

            var rootNode = ConfigNode.Load(fileInfo.FullName);
            _ktSave = rootNode.GetNode("KerbTownPersistence");

            if (_ktSave.HasValue(settingName))
            {
                _ktSave.SetValue(settingName, settingValue);
            }
            else
            {
                _ktSave.AddValue(settingName, settingValue);
            }

            rootNode.Save(fileInfo.FullName);
        }

        public static string ReadSetting(string settingName)
        {
            var fileInfo = new FileInfo(GetSaveFilePath());
            if (!fileInfo.Exists) return null;

            _ktSave = ConfigNode.Load(fileInfo.FullName).GetNode("KerbTownPersistence");

            return _ktSave.GetValue(settingName);
        }

        public static void WriteNode(ConfigNode newNode)
        {
            var fileInfo = new FileInfo(GetSaveFilePath());
            ConfigNode persistenceNode;
            if (!fileInfo.Exists)
            {
                var cNode = new ConfigNode();
                persistenceNode = cNode.AddNode("KerbTownPersistence");
                persistenceNode.AddNode(newNode);
                cNode.Save(fileInfo.FullName);

                _ktSave = cNode;
                return;
            }

            _ktSave = ConfigNode.Load(fileInfo.FullName);
            persistenceNode = _ktSave.GetNode("KerbTownPersistence");

            var nodes = persistenceNode.GetNodes(newNode.name);
            var rootID = newNode.GetValue("ID");
            bool nodeSet = false;

            foreach (var node in from node in nodes let nodeID = node.GetValue("ID") where nodeID == rootID select node)
            {
                persistenceNode.nodes.Remove(node);
                persistenceNode.nodes.Add(newNode);
                nodeSet = true;
            }

            if (nodeSet == false)
            {
                persistenceNode.nodes.Add(newNode);
            }

            _ktSave.RemoveNode("KerbTownPersistence");
            _ktSave.AddNode(persistenceNode);

            _ktSave.Save(GetSaveFilePath());

            // Note: SetNode() isn't recursive on descendant ConfigNodes
        }

        public static ConfigNode ReadNode(string nodeName, string id = "")
        {
            var fileInfo = new FileInfo(GetSaveFilePath());
            if (!fileInfo.Exists) return null;

            _ktSave = ConfigNode.Load(fileInfo.FullName);
            var persistenceNode = _ktSave.GetNode("KerbTownPersistence");

            if (id == "")
                return persistenceNode.GetNode(nodeName);

            return persistenceNode.nodes.Cast<ConfigNode>().FirstOrDefault(node => node.GetValue("ID") == id);
        }

        public static string GetSaveFilePath()
        {
            return string.Format("{0}saves/{1}/KerbTown.cfg", KSPUtil.ApplicationRootPath, HighLogic.SaveFolder);
        }

        public static void PostScreenMessage(string message, float timeToShow = 3f)
        {
            ScreenMessages.PostScreenMessage(message, timeToShow, ScreenMessageStyle.UPPER_CENTER);
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
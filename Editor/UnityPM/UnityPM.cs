using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using System.Xml;
using System.Xml.Serialization;
using System.Linq;
using SimpleJSON;
using System.IO;
using System.Net;

namespace UnityUtils.UnityPM
{
    partial class UnityPM : EditorWindow
    {
        string searchText = "";
        UnityPMTab tab = UnityPMTab.Packages;

        List<Installable> installQueue = new List<Installable>();
        ReorderableList installQueueList;
        bool dontAsk = false;
        string downloadFileDirectory = "/";

        Vector2 scrollPosition = new Vector2();

        List<Package> packages = new List<Package>();

        Settings settings;

        private enum UnityPMTab
        {
            Packages,
            Settings
        }

        [MenuItem("Tools/Unity Utils/UnityPM")]
        public static void Open()
        {
            GetWindow<UnityPM>();
        }

        void OnEnable()
        {
            settings = new Settings();
            settings.LoadSettings();

            installQueueList = new ReorderableList(installQueue, typeof(UnityEngine.Object), true, true, false, true)
            {
                drawHeaderCallback = (rect) =>
                {
                    EditorGUI.LabelField(rect, "Install Queue");
                },
                drawElementCallback = (Rect rect, int index, bool isActive, bool isFocused) =>
                {
                    EditorGUI.LabelField(rect, installQueue[index].Name);
                }
            };
        }

        private void OnDisable()
        {
            settings.SaveSettings();
        }

        private void TestStuff()
        {
            Sources.GumroadSource source = new Sources.GumroadSource();
            List<Package> p = source.GetPackages();

        }

        void DrawPackages()
        {
            if (GUILayout.Button("Test"))
                TestStuff();

            EditorGUILayout.BeginVertical();
            if (GUILayout.Button("Refresh"))
            {
                List<Package> newPackages = new List<Package>();
                Debug.Log("Pulling packages..");
                foreach(ISource source in settings.sources)
                {
                    newPackages.AddRange(source.GetPackages());
                }
                packages = newPackages;
                Debug.Log($"Pulled {newPackages.Count} packages from {settings.sources.Count} sources!");
            }
            EditorGUILayout.Space();

            searchText = EditorGUILayout.TextField("Search", searchText);
            EditorGUILayout.EndVertical();

            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

            foreach (Package package in packages)
            {
                if (package.name.IndexOf(searchText, StringComparison.OrdinalIgnoreCase) < 0)
                    continue;

                EditorGUILayout.LabelField(package.name);

                if (package.unityPackages?.Count > 0)
                {
                    package.GUIFoldoutPackages = EditorGUILayout.BeginFoldoutHeaderGroup(package.GUIFoldoutPackages, "Unity Packages");
                    if (package.GUIFoldoutPackages)
                    {
                        foreach (UnityPackage unityPackage in package.unityPackages)
                        {
                            bool inQueue = installQueue.Contains(unityPackage);
                            if (EditorGUILayout.Toggle(unityPackage.Name, inQueue))
                            {
                                if (!inQueue)
                                    installQueue.Add(unityPackage);
                            }
                            else
                            {
                                if (inQueue)
                                    installQueue.Remove(unityPackage);
                            }
                        }
                    }
                    EditorGUILayout.EndFoldoutHeaderGroup();
                }

                if (package.files?.Count > 0)
                {
                    package.GUIFoldoutFiles = EditorGUILayout.BeginFoldoutHeaderGroup(package.GUIFoldoutFiles, "Files");
                    if (package.GUIFoldoutFiles)
                    {
                        foreach (File file in package.files)
                        {
                            bool inQueue = installQueue.Contains(file);
                            if (EditorGUILayout.Toggle(file.Name, inQueue))
                            {
                                if (!inQueue)
                                    installQueue.Add(file);
                            }
                            else
                            {
                                if (inQueue)
                                    installQueue.Remove(file);
                            }
                        }
                    }
                    EditorGUILayout.EndFoldoutHeaderGroup();
                }

                EditorGUILayout.Space();
            }

            GUILayout.FlexibleSpace();
            EditorGUILayout.EndScrollView();

            GUILayout.BeginVertical();
            installQueueList.DoLayoutList();
            dontAsk = EditorGUILayout.Toggle("Don't ask", dontAsk);
            if (installQueue.OfType<File>().Any())
                downloadFileDirectory = EditorGUILayout.TextField("Download Files To:", downloadFileDirectory);
            if (GUILayout.Button("Install"))
            {
                foreach (File file in installQueue.OfType<File>())
                {
                    if (settings.UseCache)
                    {

                    }
                    else
                    {
                        string dstDir = $"{Application.dataPath}/{downloadFileDirectory}/";
                        Directory.CreateDirectory(dstDir);
                        string dstPath = $"{dstDir}/{file.GetFileName()}";
                        file.GetWebClient().DownloadFile(file.GetURI(), dstPath);
                    }
                }
                foreach (UnityPackage file in installQueue.OfType<UnityPackage>())
                {
                    if(settings.UseCache)
                    {

                    }
                    else
                    {
                        string tmpPath = $"{settings.CacheDirectory}/downloading";
                        file.GetWebClient().DownloadFile(file.GetURI(), tmpPath);
                        AssetDatabase.ImportPackage(tmpPath, !dontAsk);
                    }
                    //file.Download(dontAsk);
                }
                AssetDatabase.Refresh();
            }
            GUILayout.EndVertical();
        }

        void OnGUI()
        {
            tab = (UnityPMTab)GUILayout.Toolbar((int)tab, Enum.GetNames(typeof(UnityPMTab)));
            
            switch (tab)
            {
                case UnityPMTab.Packages:
                    DrawPackages();
                    break;
                case UnityPMTab.Settings:
                    settings.DrawUI();
                    break;
            }
        }
    }
}

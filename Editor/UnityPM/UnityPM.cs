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
        Dictionary<Installable, InstallStatus> installQueueStatus;
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

        private enum InstallStatus
        {
            Downloading,
            Installing,
            Installed,
            Failed
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
                    Installable installable = installQueue[index];
                    Rect statusRect = rect;
                    statusRect.xMin = rect.xMax - 50;
                    statusRect.xMax = rect.xMax;

                    EditorGUI.LabelField(rect, installable.Name);

                    if(installQueueStatus != null && installQueueStatus.ContainsKey(installable))
                    {
                        string status = Enum.GetName(typeof(InstallStatus), installQueueStatus[installable]);
                        EditorGUI.LabelField(statusRect, status);
                    }
                }
            };
        }

        private void OnDisable()
        {
            settings.SaveSettings();
        }

        private void TestStuff()
        {
            //Sources.GumroadSource source = new Sources.GumroadSource();
            //List<Package> p = source.GetPackages();
            foreach(EditorWindow w in Resources.FindObjectsOfTypeAll<EditorWindow>())
            {
                if(w.titleContent.text == "Import Unity Package")
                {
                    Debug.Log(w.GetType().Name);
                }
            }
            //Debug.Log("AAAA");
            //CodeStage.PackageToFolder.Package2Folder.ImportPackageToFolder(@"C:\Users\Roliga\Desktop\hmm.unitypackage", "Import", true);
            //Debug.Log("Done");
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
                if (package.name?.IndexOf(searchText, StringComparison.OrdinalIgnoreCase) < 0)
                    continue;

                EditorGUILayout.LabelField(package.name ?? "Unnamed Package");

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
                installQueueStatus = new Dictionary<Installable, InstallStatus>();
                //foreach (Installable installable in installQueue)
                //    installQueueStatus.Add(installable, false);
                installQueue.GetEnumerator().Dispose();
                InstallNext(true, installQueue.ToList().GetEnumerator());
            }
            GUILayout.EndVertical();
        }

        private void InstallNext(bool success, List<Installable>.Enumerator enumerator)
        {
            if (enumerator.Current != null)
            {
                if (success)
                    installQueueStatus[enumerator.Current] = InstallStatus.Installed;
                else
                    installQueueStatus[enumerator.Current] = InstallStatus.Failed;
            }

            if (!enumerator.MoveNext())
                return;
            installQueueStatus[enumerator.Current] = InstallStatus.Downloading;

            if(enumerator.Current is File)
            {
                File file = (File)enumerator.Current;
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
                InstallNext(true, enumerator);
            }
            else if (enumerator.Current is UnityPackage)
            {
                UnityPackage file = (UnityPackage)enumerator.Current;
                if (settings.UseCache)
                {

                }
                else
                {
                    string tmpPath = $"{settings.CacheDirectory}/downloading";
                    file.GetWebClient().DownloadFile(file.GetURI(), tmpPath);
                    installQueueStatus[enumerator.Current] = InstallStatus.Installing;

                    ImportPackageWithCallback.ImportPackage(tmpPath, !dontAsk, () => { InstallNext(true, enumerator); });
                }
            }
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

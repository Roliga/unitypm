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
        Dictionary<Installable, InstallStatus> installQueueStatus = new Dictionary<Installable, InstallStatus>();
        ReorderableList installQueueList;
        bool dontAsk = false;
        string downloadFileDirectory = "/";

        Vector2 scrollPosition = new Vector2();

        List<Package> packages = new List<Package>();

        Settings settings;

        private static class Styles
        {
            public static readonly GUIStyle paddedArea = new GUIStyle();
            public static readonly GUIStyle searchField = new GUIStyle(EditorStyles.toolbarSearchField);
            public static readonly GUIStyle scroll = new GUIStyle("HelpBox");
            public static readonly GUIStyle packageEven = new GUIStyle("Box");
            public static readonly GUIStyle packageOdd = new GUIStyle();
            public static readonly GUIStyle fileArea = new GUIStyle();
            public static readonly GUIStyle clearQueueButton = new GUIStyle(GUI.skin.button);

            static Styles()
            {
                paddedArea.padding = new RectOffset(10, 10, 0, 10);

                searchField.fixedHeight = 0;

                scroll.margin = new RectOffset(0, 0, 10, 10);
                scroll.padding = new RectOffset(1, 0, 0, 0);

                packageEven.padding = new RectOffset(5, 5, 5, 5);
                packageEven.margin = new RectOffset();

                packageOdd.padding = new RectOffset(5, 5, 5, 5);
                packageOdd.margin = new RectOffset();

                fileArea.padding.left = 10;

                clearQueueButton.stretchWidth = false;
            }
        }

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
            titleContent = new GUIContent("UnityPM", EditorGUIUtility.FindTexture("d_Toolbar Plus"));

            settings = new Settings();
            settings.LoadSettings();

            installQueueList = new ReorderableList(installQueue, typeof(UnityEngine.Object), true, true, true, true)
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

                    if(installQueueStatus.ContainsKey(installable))
                    {
                        string status = Enum.GetName(typeof(InstallStatus), installQueueStatus[installable]);
                        EditorGUI.LabelField(statusRect, status);
                    }
                },
                onAddDropdownCallback = (Rect buttonRect, ReorderableList l) =>
                {
                    GenericMenu menu = new GenericMenu();
                    menu.AddDisabledItem(new GUIContent("Presets"));
                    menu.AddSeparator("");

                    foreach (QueuePreset preset in settings.queuePresets)
                    {
                        menu.AddItem(new GUIContent(preset.Name), false, (p) => {

                            List<Installable> newQueue = new List<Installable>();
                            List<QueuePresetEntry> notFound = new List<QueuePresetEntry>();

                            foreach(QueuePresetEntry entry in ((QueuePreset)p).entries)
                            {
                                var i = (from package in packages
                                          where package.source.Name == entry.sourceName
                                          && package.name == entry.packageName
                                          from installable in entry.isPackage ? package.unityPackages : package.files.Cast<Installable>()
                                          where installable.Name == entry.fileName
                                          select installable).FirstOrDefault();

                                if (i == null)
                                    notFound.Add(entry);
                                else
                                    newQueue.Add(i);
                            }

                            if (notFound.Count > 0)
                            {
                                string msg = $"{notFound.Count} packages from the preset could not be found. Replace the queue anyway?";
                                if (EditorUtility.DisplayDialog("Replace queue",
                                    msg,
                                    "Replace", "Cancel"))
                                {
                                    installQueue.Clear();
                                    installQueueStatus.Clear();
                                    installQueue.AddRange(newQueue);
                                }
                            }
                            else
                            {
                                installQueue.Clear();
                                installQueueStatus.Clear();
                                installQueue.AddRange(newQueue);
                            }

                        }, preset);
                    }

                    menu.AddSeparator("");
                    menu.AddItem(new GUIContent("Save Queue..."), false, () =>
                    {
                        PopupWindow.Show(buttonRect, new AddQueuePresetWindow(installQueue, settings.queuePresets, packages));
                    });

                    menu.ShowAsContext();
                }
            };
        }

        private void OnDisable()
        {
            settings.SaveSettings();
        }

        bool MatchPackage(Package package, string searchString)
        {
            if (package.name?.IndexOf(searchText, StringComparison.OrdinalIgnoreCase) >= 0)
                return true;

            foreach (Installable i in package.files)
                if (i.Name?.IndexOf(searchText, StringComparison.OrdinalIgnoreCase) >= 0)
                    return true;

            foreach (Installable i in package.unityPackages)
                if (i.Name?.IndexOf(searchText, StringComparison.OrdinalIgnoreCase) >= 0)
                    return true;

            return false;
        }

        void DrawPackages()
        {
            //
            // Search section
            //

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
            
            searchText = EditorGUILayout.TextField(searchText, Styles.searchField);

            EditorGUILayout.EndVertical();

            //
            // Package list
            //
            
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition, Styles.scroll);

            bool oddEven = false;
            foreach (Package package in packages)
            {
                if (package.files.Count == 0 && package.unityPackages.Count == 0)
                    continue;

                if (!MatchPackage(package, searchText))
                    continue;

                oddEven = !oddEven;

                EditorGUILayout.BeginVertical(oddEven ? Styles.packageEven : Styles.packageOdd);

                EditorGUILayout.LabelField(package.name ?? "Unnamed Package", style: EditorStyles.boldLabel);

                EditorGUILayout.LabelField(package.source.Name, style: EditorStyles.miniLabel);

                if (package.unityPackages?.Count > 0)
                {
                    EditorGUILayout.LabelField("Unity Packages", EditorStyles.boldLabel);

                    EditorGUILayout.BeginVertical(Styles.fileArea);
                    foreach (UnityPackage unityPackage in package.unityPackages)
                        {
                            bool inQueue = installQueue.Contains(unityPackage);
                            if (EditorGUILayout.ToggleLeft(unityPackage.Name, inQueue))
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
                    EditorGUILayout.EndVertical();
                }

                if (package.files?.Count > 0)
                {
                    EditorGUILayout.LabelField("Files", EditorStyles.boldLabel);

                    EditorGUILayout.BeginVertical(Styles.fileArea);
                    foreach (File file in package.files)
                        {
                            bool inQueue = installQueue.Contains(file);
                            if (EditorGUILayout.ToggleLeft(file.Name, inQueue))
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
                    EditorGUILayout.EndVertical();
                }
                EditorGUILayout.EndVertical();
            }

            GUILayout.FlexibleSpace();
            EditorGUILayout.EndScrollView();

            //
            // Install section
            //

            EditorGUILayout.Space();
            GUILayout.BeginVertical();
            installQueueList.DoLayoutList();
            dontAsk = EditorGUILayout.Toggle("Don't ask", dontAsk);
            if (installQueue.OfType<File>().Any())
                downloadFileDirectory = EditorGUILayout.TextField("Download Files To:", downloadFileDirectory);
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Install"))
            {
                installQueueStatus.Clear();
                //foreach (Installable installable in installQueue)
                //    installQueueStatus.Add(installable, false);
                installQueue.GetEnumerator().Dispose();
                InstallNext(true, installQueue.ToList().GetEnumerator());
            }
            if (GUILayout.Button("Clear Queue", Styles.clearQueueButton))
            {
                installQueue.Clear();
                installQueueStatus.Clear();
            }
            EditorGUILayout.EndHorizontal();
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
            EditorGUILayout.BeginVertical(Styles.paddedArea);
            EditorGUILayout.BeginVertical();

            EditorGUILayout.Space();
            
            switch (tab)
            {
                case UnityPMTab.Packages:
                    DrawPackages();
                    break;
                case UnityPMTab.Settings:
                    settings.DrawUI();
                    break;
            }

            EditorGUILayout.EndVertical();
            EditorGUILayout.EndVertical();
        }
    }
}

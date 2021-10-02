using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using System.Xml;
using System.Xml.Serialization;
using SimpleJSON;
using System.IO;

namespace UnityUtils.UnityPM
{
    partial class UnityPM
    {
        private class Settings
        {
            public List<ISource> sources = new List<ISource>();
            public string CacheDirectory
            {
                get
                {
                    string tempDir = Path.Combine(Application.dataPath, @"../Temp/");
                    if (!Directory.Exists(tempDir))
                        throw new DirectoryNotFoundException($"Could not find project Temp directory at '{tempDir}'");

                    string dir = Path.Combine(tempDir, @"UnityPM/");
                    if (Directory.Exists(dir))
                    {
                        return dir;
                    }
                    else
                    {
                        Directory.CreateDirectory(dir);
                        return dir;
                    }
                }
            }
            public bool UseCache = false;

            private readonly ReorderableList sourcesList;
            private bool useCustomCacheDirectory;

            public void LoadSettings()
            {
                JSONObject settingsJSON = JSON.Parse(EditorPrefs.GetString("UnityPMSettings")) as JSONObject;

                if (settingsJSON is null)
                    return;

                Debug.Log("Loaded JSON:\n\n" + settingsJSON.ToString(4));

                try
                {
                    sources.Clear();
                    foreach (JSONObject sourceJSON in settingsJSON["Sources"])
                    {
                        Type sourceType = SourceTypes.GetByName(sourceJSON["Type"]);
                        ISource source = (ISource)Activator.CreateInstance(sourceType);

                        if (source is ISourceWithSettings)
                            ((ISourceWithSettings)source).LoadSettings((JSONObject)sourceJSON["Settings"]);

                        sources.Add(source);
                    }
                }
                catch (Exception)
                {
                    Debug.Log("Invalid settings JSON!");
                }
            }

            public void SaveSettings()
            {
                JSONObject settingsJSON = new JSONObject();

                JSONArray sourceArrayJSON = new JSONArray();
                foreach (ISource source in sources)
                {
                    JSONObject sourceJSON = new JSONObject();
                    sourceJSON["Type"] = source.GetType().Name;

                    if (source is ISourceWithSettings)
                        sourceJSON["Settings"] = ((ISourceWithSettings)source).SaveSettings();

                    sourceArrayJSON.Add(sourceJSON);
                }
                settingsJSON.Add("Sources", sourceArrayJSON);

                EditorPrefs.SetString("UnityPMSettings", settingsJSON.ToString());
                Debug.Log("Saved JSON:\n\n" + settingsJSON.ToString(4));
            }

            public void DrawUI()
            {
                sourcesList.DoLayoutList();

                // if (GUILayout.Button("Save"))
                //     SaveSettings();
                // if (GUILayout.Button("Load"))
                //     LoadSettings();
            }

            public Settings()
            {
                sourcesList = new ReorderableList(sources, typeof(UnityEngine.Object), false, true, true, true)
                {
                    drawHeaderCallback = (rect) =>
                    {
                        EditorGUI.LabelField(rect, "Sources");
                    },
                    drawElementCallback = (Rect rect, int index, bool isActive, bool isFocused) =>
                    {
                        ISource source = sources[index];
                        EditorGUI.LabelField(rect, source.Name);

                        if (source is ISourceWithEditUI)
                        {
                            Rect buttonRect = rect;
                            buttonRect.xMin = rect.xMax - 50;
                            buttonRect.xMax = rect.xMax;
                            if (GUI.Button(buttonRect, "Edit"))
                            {
                                PopupWindow.Show(buttonRect, new EditSourceWindow((ISourceWithEditUI)source));
                            }
                        }
                    },
                    onAddDropdownCallback = (Rect buttonRect, ReorderableList l) =>
                    {
                        GenericMenu menu = new GenericMenu();

                        foreach (Type type in SourceTypes.GetAll())
                        {
                            if (typeof(ISourceUnique).IsAssignableFrom(type) && sources.FindIndex(type.IsInstanceOfType) >= 0)
                                continue;

                            menu.AddItem(new GUIContent(type.Name), false, (t) => {
                                if (typeof(ISourceWithEditUI).IsAssignableFrom(type))
                                    PopupWindow.Show(buttonRect, new AddSourceWindow((ISourceWithEditUI)Activator.CreateInstance((Type)t), sources));
                                else
                                    sources.Add((ISource)Activator.CreateInstance((Type)t));
                            }, type);
                        }

                        menu.ShowAsContext();
                    }
                };
            }
        }
    }
}
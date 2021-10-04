using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using System.Xml;
using System.Xml.Serialization;
using SimpleJSON;
using System.IO;
using System.Collections.Specialized;
using System.Linq;

namespace UnityUtils.UnityPM
{
    partial class UnityPM
    {
        private class Settings
        {
            public List<ISource> sources = new List<ISource>();
            public List<QueuePreset> queuePresets = new List<QueuePreset>()
            {
                new QueuePreset("A preset")
                {
                    entries = new List<QueuePresetEntry>()
                    {
                        new QueuePresetEntry()
                        {
                            sourceName = "VRChat SDKs",
                            packageName = "VRChat SDK3",
                            fileName = "SDK3 Worlds",
                            isPackage = true
                        },
                        new QueuePresetEntry()
                        {
                            sourceName = "VRChat SDKs",
                            packageName = "VRChat SDK3",
                            fileName = "SDK3 Avatars",
                            isPackage = true
                        },
                        new QueuePresetEntry()
                        {
                            sourceName = "VRChat SDKs",
                            packageName = "VRChat SDK33",
                            fileName = "SDK3 Avatars",
                            isPackage = true
                        }
                    }
                }
            };

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
            private readonly ReorderableList queuePresetsList;
            private bool useCustomCacheDirectory;

            public void LoadSettings()
            {
                JSONObject settingsJSON = JSON.Parse(EditorPrefs.GetString("UnityPMSettings")) as JSONObject;

                if (settingsJSON is null)
                    return;

#if UNITY_UTILS_DEBUG
                Debug.Log("Loaded JSON:\n\n" + settingsJSON.ToString(4));
#endif

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

                    queuePresets.Clear();
                    foreach (JSONObject presetJSON in settingsJSON["queue_presets"])
                    {
                        QueuePreset preset = new QueuePreset(presetJSON["name"]);
                        foreach (JSONObject entryJSON in presetJSON["entries"])
                        {
                            preset.entries.Add(new QueuePresetEntry()
                            {
                                sourceName = entryJSON["source_name"],
                                packageName = entryJSON["package_name"],
                                fileName = entryJSON["file_name"],
                                isPackage = entryJSON["is_package"],
                            });
                        }
                        queuePresets.Add(preset);
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

                JSONArray presetArrayJSON = new JSONArray();
                foreach(QueuePreset preset in queuePresets)
                {
                    JSONObject presetJSON = new JSONObject();
                    presetJSON["name"] = preset.Name;

                    JSONArray entriesJSON = new JSONArray();
                    foreach(QueuePresetEntry entry in preset.entries)
                    {
                        JSONObject entryJSON = new JSONObject();
                        entryJSON["source_name"] = entry.sourceName;
                        entryJSON["package_name"] = entry.packageName;
                        entryJSON["file_name"] = entry.fileName;
                        entryJSON["is_package"] = entry.isPackage;
                        entriesJSON.Add(entryJSON);
                    }
                    presetJSON["entries"] = entriesJSON;

                    presetArrayJSON.Add(presetJSON);
                }
                settingsJSON["queue_presets"] = presetArrayJSON;

                EditorPrefs.SetString("UnityPMSettings", settingsJSON.ToString());

#if UNITY_UTILS_DEBUG
                Debug.Log("Saved JSON:\n\n" + settingsJSON.ToString(4));
#endif
            }

            public void DrawUI()
            {
                sourcesList.DoLayoutList();
                EditorGUILayout.Space();
                queuePresetsList.DoLayoutList();
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

                queuePresetsList = new ReorderableList(queuePresets, typeof(UnityEngine.Object), false, true, false, true)
                {
                    drawHeaderCallback = (rect) =>
                    {
                        EditorGUI.LabelField(rect, "Queue Presets");
                    },
                    drawElementCallback = (Rect rect, int index, bool isActive, bool isFocused) =>
                    {
                        EditorGUI.LabelField(rect, queuePresets[index].Name);
                    }
                };
            }
        }
    }
}
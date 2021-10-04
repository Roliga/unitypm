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
    class QueuePreset
    {
        public string Name;
        public List<QueuePresetEntry> entries = new List<QueuePresetEntry>();

        public QueuePreset(string name)
        {
            Name = name;
        }
    }

    class QueuePresetEntry
    {
        public string sourceName = "";
        public string packageName = "";
        public string fileName = "";
        public bool isPackage;
    }
    class AddQueuePresetWindow : PopupWindowContent
    {
        private string presetName = "";
        private float windowHeight;
        private List<Installable> installQueue;
        private List<QueuePreset> queuePresets;
        private List<Package> packages;

        public override void OnGUI(Rect rect)
        {
            Rect contentRect = EditorGUILayout.BeginHorizontal(new GUIStyle());

            presetName = EditorGUILayout.TextField(presetName);
            if (String.IsNullOrEmpty(presetName))
            {
                var guiColor = GUI.color;
                GUI.color = Color.grey;
                EditorGUI.LabelField(GUILayoutUtility.GetLastRect(), "Preset name");
                GUI.color = guiColor;
            }

            if (GUILayout.Button("Save"))
            {
                queuePresets.RemoveAll(x => x.Name == presetName);
                QueuePreset preset = new QueuePreset(presetName);
                queuePresets.Add(preset);

                foreach (Installable installable in installQueue)
                {
                    bool isPackage = installable is UnityPackage;

                    Package package = (from p in packages
                                       where (isPackage ? p.unityPackages : p.files.Cast<Installable>()).Contains(installable)
                                       select p).First();

                    preset.entries.Add(new QueuePresetEntry()
                    {
                        sourceName = package.source.Name,
                        packageName = package.name,
                        fileName = installable.Name,
                        isPackage = isPackage
                    });
                }

                editorWindow.Close();
            }
            if (GUILayout.Button("Cancel"))
            {
                editorWindow.Close();
            }

            EditorGUILayout.EndHorizontal();

            if (contentRect.height > 10)
                windowHeight = contentRect.height;
        }

        public override Vector2 GetWindowSize()
        {
            return new Vector2(300, windowHeight);
        }

        public AddQueuePresetWindow(List<Installable> installQueue, List<QueuePreset> queuePresets, List<Package> packages)
        {
            this.installQueue = installQueue;
            this.queuePresets = queuePresets;
            this.packages = packages;
        }
    }
}
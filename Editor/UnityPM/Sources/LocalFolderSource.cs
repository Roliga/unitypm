using SimpleJSON;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using System.Collections.Specialized;
using System.IO;

namespace UnityUtils.UnityPM.Sources
{
    public class LocalFolderSource : ISource, ISourceWithEditUI, ISourceWithSettings
    {
        private class EditUI : ISourceEditUI
        {
            private LocalFolderSource source;

            private string path;
            private bool subFolders;
            private bool separatePackages;
            private string searchPattern;

            public void Apply()
            {
                source.path = path;
                source.subFolders = subFolders;
                source.separatePackages = separatePackages;

                if (string.IsNullOrWhiteSpace(searchPattern))
                    source.searchPattern = "*";
                else
                    source.searchPattern = searchPattern;
            }

            public bool Draw()
            {
                EditorGUILayout.BeginHorizontal();
                path = EditorGUILayout.TextField("Folder path", path);
                if (GUILayout.Button("Select"))
                    path = EditorUtility.OpenFolderPanel("Folder", path, "");
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.Space();

                searchPattern = EditorGUILayout.TextField("Search pattern", searchPattern);

                EditorGUILayout.Space();

                subFolders = EditorGUILayout.Toggle("Include sub folders", subFolders);

                EditorGUI.BeginDisabledGroup(!subFolders);
                separatePackages = EditorGUILayout.Toggle("Sub-folders as separate packages", separatePackages);
                EditorGUI.EndDisabledGroup();

                return true;
            }

            public EditUI(LocalFolderSource source)
            {
                this.source = source;
                path = source.path;
                subFolders = source.subFolders;
                separatePackages = source.separatePackages;
                searchPattern = source.searchPattern;
            }
        }
        public ISourceEditUI GetEditUI() { return new EditUI(this); }

        public string Name
        {
            get
            {
                if (path is null)
                    return "Local Folder";
                else
                    return $"Local Folder: {path}";
            }
        }

        private string path;
        private bool subFolders;
        private bool separatePackages;
        private string searchPattern;

        public List<Package> GetPackages()
        {
            List<Package> packages = new List<Package>();

            DirectoryInfo directory = new DirectoryInfo(path);

            Package package = new Package();
            package.name = directory.Name;
            foreach(var file in directory.GetFiles(searchPattern))
                if(file.Extension == ".unitypackage")
                    package.unityPackages.Add(new SimpleUnityPackage(file.Name, new Uri(file.FullName)));
                else
                    package.files.Add(new SimpleFile(file.Name, new Uri(file.FullName)));
            packages.Add(package);

            if (subFolders)
            {
                if (separatePackages)
                {
                    foreach (DirectoryInfo d in directory.GetDirectories("*", SearchOption.AllDirectories))
                    {
                        Package p = new Package();
                        p.name = d.Name;
                        foreach (var file in d.GetFiles(searchPattern))
                            if (file.Extension == ".unitypackage")
                                p.unityPackages.Add(new SimpleUnityPackage(file.Name, new Uri(file.FullName)));
                            else
                                p.files.Add(new SimpleFile(file.Name, new Uri(file.FullName)));
                        packages.Add(p);
                    }
                }
                else
                {
                    foreach (DirectoryInfo d in directory.GetDirectories())
                    {
                        foreach (var file in d.GetFiles(searchPattern, SearchOption.AllDirectories))
                            if (file.Extension == ".unitypackage")
                                package.unityPackages.Add(new SimpleUnityPackage(file.Name, new Uri(file.FullName)));
                            else
                                package.files.Add(new SimpleFile(file.Name, new Uri(file.FullName)));
                    }
                }
            }

            return packages;
        }

        public void LoadSettings(JSONObject settings)
        {
            path = settings["path"];
            subFolders = settings["sub_folders"];
            separatePackages = settings["separate_packages"];
            searchPattern = settings["search_pattern"];
        }

        public JSONObject SaveSettings()
        {
            JSONObject json = new JSONObject();
            json.Add("path", path);
            json.Add("sub_folders", subFolders);
            json.Add("separate_packages", separatePackages);
            json.Add("search_pattern", searchPattern);
            return json;
        }
    }
}
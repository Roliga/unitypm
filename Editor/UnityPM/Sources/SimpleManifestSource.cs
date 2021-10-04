using SimpleJSON;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using System.Collections.Specialized;

namespace UnityUtils.UnityPM.Sources
{
    public class SimpleManifestSource : ISource, ISourceWithEditUI, ISourceWithSettings
    {
        private class EditUI : ISourceEditUI
        {
            private SimpleManifestSource source;

            private string url;
            private string manifestName;

            private string urlPrev;
            private bool validated;

            public void Apply()
            {
                source.url = url;
                source.manifestName = manifestName;
            }

            public bool Draw()
            {
                urlPrev = url;
                url = EditorGUILayout.TextField("Manifest URL", url);

                if (urlPrev != url)
                    validated = false;

                if (GUILayout.Button("Check"))
                {
                    JSONObject manifestJSON = WebUtils.APIGet(new Uri(url));

                    if(manifestJSON.HasKey("source_name"))
                    {
                        manifestName = manifestJSON["source_name"];
                        validated = true;
                    }
                }

                return validated;
            }

            public EditUI(SimpleManifestSource source)
            {
                this.source = source;
                url = source.url;
                manifestName = source.manifestName;
            }
        }
        public ISourceEditUI GetEditUI() { return new EditUI(this); }

        public string Name
        {
            get
            {
                if (manifestName is null)
                    return "Simple Manifest";
                else
                    return $"Simple Manifest: {manifestName}";
            }
        }

        private string url;
        private string manifestName;

        public List<Package> GetPackages()
        {
            List<Package> packages = new List<Package>();

            JSONObject manifestJSON = WebUtils.APIGet(new Uri(url));

            if(manifestJSON["packages"] is JSONArray)
                foreach(JSONNode packageJSON in manifestJSON["packages"])
                {
                    Package package = new Package(this);

                    if (packageJSON["name"] is null)
                        package.name = "Unnamed Package";
                    else
                        package.name = packageJSON["name"];

                    if (packageJSON["files"] is JSONArray)
                        foreach(JSONNode fileJSON in packageJSON["files"])
                        {
                            if (string.IsNullOrWhiteSpace(fileJSON["name"]))
                                continue;
                            if (string.IsNullOrWhiteSpace(fileJSON["url"]))
                                continue;

                            if (fileJSON["type"] == "unitypackage")
                                package.unityPackages.Add(new SimpleUnityPackage(fileJSON["name"], new Uri(fileJSON["url"])));
                            else
                                package.files.Add(new SimpleFile(fileJSON["name"], new Uri(fileJSON["url"])));
                        }

                    packages.Add(package);
                }

            return packages;
        }

        public void LoadSettings(JSONObject settings)
        {
            url = settings["url"];
            manifestName = settings["source_name"];
        }

        public JSONObject SaveSettings()
        {
            JSONObject json = new JSONObject();
            json.Add("url", url);
            json.Add("source_name", manifestName);
            return json;
        }
    }
}
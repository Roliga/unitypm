using SimpleJSON;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using System.Collections.Specialized;
using System.Text.RegularExpressions;

namespace UnityUtils.UnityPM.Sources
{
    public class GitHubSource : ISource, ISourceWithEditUI, ISourceWithSettings
    {
        private class EditUI : ISourceEditUI
        {
            private GitHubSource source;

            private string repo;
            private string repoPrev;
            private bool latest;

            private bool valid;

            private Regex repoRegex = new Regex(@"^[\w,\-,_]+\/[\w,\-,_]+$");

            public void Apply()
            {
                source.repo = repo;
                source.latest = latest;
            }

            public bool Draw()
            {
                repo = EditorGUILayout.TextField("github.com/", repo);

                EditorGUILayout.Space();

                latest = EditorGUILayout.Toggle("Only latest release", latest);

                if(repo != repoPrev)
                {
                    valid = repoRegex.IsMatch(repo);
                    repoPrev = repo;
                }

                return valid;
            }

            public EditUI(GitHubSource source)
            {
                this.source = source;
                repo = source.repo;
                latest = source.latest;
            }
        }
        public ISourceEditUI GetEditUI() { return new EditUI(this); }

        public string Name
        {
            get
            {
                if (repo is null)
                    return "GitHub Repo";
                else
                    return $"GitHub Repo: {repo}";
            }
        }

        private string repo;
        private bool latest = true;

        private Package MakePackage(JSONObject releaseJSON)
        {
            Package package = new Package { name = releaseJSON["name"] };

            foreach (JSONObject assetJSON in releaseJSON["assets"])
            {
                if (((string)assetJSON["name"]).EndsWith(".unitypackage"))
                {
                    package.unityPackages.Add(new SimpleUnityPackage(assetJSON["name"],
                        new Uri(assetJSON["browser_download_url"])));
                }
                else
                {
                    package.files.Add(new SimpleFile(assetJSON["name"],
                        new Uri(assetJSON["browser_download_url"])));
                }
            }

            return package;
        }

        public List<Package> GetPackages()
        {
            List<Package> packages = new List<Package>();

            JSONObject releasesJSON = WebUtils.APIGet($"https://api.github.com/repos/{repo}/releases{(latest ? "/latest" : "")}");

            if (latest)
            {
                packages.Add(MakePackage(releasesJSON));
            }
            else
            {
                foreach (JSONObject releaseJSON in releasesJSON)
                    packages.Add(MakePackage(releaseJSON));
            }

            return packages;
        }

        public void LoadSettings(JSONObject settings)
        {
            repo = settings["repo"];
            latest = settings["latest"];
        }

        public JSONObject SaveSettings()
        {
            JSONObject json = new JSONObject();
            json.Add("repo", repo);
            json.Add("latest", latest);
            return json;
        }
    }
}
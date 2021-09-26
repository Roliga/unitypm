using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using SimpleJSON;

namespace UnityUtils.UnityPM.Sources
{
    public class TestSource : ISource, ISourceWithEditUI, ISourceWithSettings
    {
        private class TestSourceUnityPackage : SimpleUnityPackage
        {
            // public override void Download(bool ask)
            // {
            //     Debug.Log($"Downloaded TestSource unity package {Name}");
            // }

            public TestSourceUnityPackage(string name, Uri uri) : base(name, uri) { }
        }

        private class TestSourceFile : SimpleFile
        {
            // public override void Download(string destination)
            // {
            //     Debug.Log($"Downloaded TestSource file {Name}");
            // }
            public TestSourceFile(string name, Uri uri) : base(name, uri, "TestSource " + name) { }

            public TestSourceFile(string name, Uri uri, string fileName) : base(name, uri, "TestSource " + fileName) { }
        }

        private class EditUI : ISourceEditUI
        {
            private TestSource source;

            private bool work = false;
            private string aPackage = "Something";

            public void Apply()
            {
                source.APackage = aPackage;
                source.Work = work;
            }

            public bool Draw()
            {
                EditorGUILayout.LabelField("This is kinda a test thing or something");
                aPackage = EditorGUILayout.TextField("Package name", aPackage);
                work = EditorGUILayout.Toggle("Work!", work);
                return true;
            }

            public EditUI(TestSource source)
            {
                this.source = source;
                work = source.Work;
                aPackage = source.APackage;
            }
        }

        public ISourceEditUI GetEditUI() { return new EditUI(this); }
        public string Name { get; } = "Test Source";

        private bool Work = false;
        private string APackage = "DEFAULT NAME";

        public List<Package> GetPackages()
        {
            Uri testFileUri = new Uri(@"C:\Users\Roliga\Desktop\hmm.txt");
            Uri testPackageUri = new Uri(@"C:\Users\Roliga\Desktop\hmm.unitypackage");
            List<Package> packages = new List<Package>() {
                new Package()
                {
                    name = "Hello",
                    unityPackages = new List<UnityPackage>()
                    {
                        new TestSourceUnityPackage("Hello.unitypackage", testPackageUri)
                    },
                    files = new List<File>()
                    {
                        new TestSourceFile("Hello.txt", testFileUri),
                        new TestSourceFile("World.txt", testFileUri)
                    }
                },
                new Package()
                {
                    name = "What the",
                    unityPackages = new List<UnityPackage>()
                    {
                        new TestSourceUnityPackage("Hello.unitypackage", testPackageUri),
                        new TestSourceUnityPackage(APackage, testPackageUri),
                        new TestSourceUnityPackage(APackage, testPackageUri)
                    },
                    files = new List<File>()
                    {
                        new TestSourceFile("Hello.txt", testFileUri),
                        new TestSourceFile("World.txt", testFileUri)
                    }
                }
            };
            if (Work)
                return packages;
            else
                return new List<Package>();
        }

        public JSONObject SaveSettings()
        {
            JSONObject json = new JSONObject();
            json.Add("APackage", APackage);
            json.Add("Work", Work);
            return json;
        }

        public void LoadSettings(JSONObject settings)
        {
            if (settings.HasKey("APackage"))
                APackage = settings["APackage"];
            if (settings.HasKey("Work"))
                Work = settings["Work"];
        }
    }
}

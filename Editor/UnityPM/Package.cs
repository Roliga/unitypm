using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEditor;
using System.Net;

namespace UnityUtils.UnityPM
{
    public class Package
    {
        public List<UnityPackage> unityPackages = new List<UnityPackage>();
        public List<File> files = new List<File>();
        public string name;
        public bool GUIFoldoutPackages;
        public bool GUIFoldoutFiles;
        public ISource source;

        public Package(ISource source)
        {
            this.source = source;
        }
    }

    public abstract class Installable
    {
        public string Name { get; }

        public virtual WebClient GetWebClient() {
            WebClient wc = new WebClient();
            wc.Headers.Add("User-Agent", "Mozilla/5.0 (X11; Linux x86_64; rv:91.0) Gecko/20100101 Firefox/91.0");
            return wc;
        }
        public abstract Uri GetURI();

        public Installable(string name)
        {
            Name = name;
        }
    }

    public abstract class UnityPackage : Installable
    {
        //public abstract void Download(bool dontAsk);
        public UnityPackage(string name) : base(name) { }
    }

    public class SimpleUnityPackage : UnityPackage
    {
        private readonly Uri uri;

        // public override void Download(bool dontAsk)
        // {
        //     Debug.Log($"Downloading unity pacakge '{Name}'.. Don't Ask: {dontAsk}");
        //     System.Net.WebClient wc = new System.Net.WebClient();
        //     wc.DownloadFileAsync("",);
        // }

        public override Uri GetURI()
        {
            return uri;
        }

        public SimpleUnityPackage(string name, Uri uri) : base(name) { this.uri = uri; }
    }

    public abstract class File : Installable
    {
        //public abstract void Download(string destination);
        public abstract string GetFileName();
        public File(string name) : base(name) { }
    }

    public class SimpleFile : File
    {
        private readonly Uri uri;
        private readonly string fileName;
        //public override void Download(string destination)
        //{
        //    Debug.Log($"Downloading file '{Name}' to '{destination}'..");
        //}

        public override Uri GetURI()
        {
            return uri;
        }

        public override string GetFileName()
        {
            //WebRequest wr = WebRequest.Create()
            return fileName;
        }

        public SimpleFile(string name, Uri uri) : base(name)
        {
            this.uri = uri;
            fileName = name;
        }

        public SimpleFile(string name, Uri uri, string fileName) : base(name)
        {
            this.uri = uri;
            this.fileName = fileName;
        }
    }
}

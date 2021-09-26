using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using System.Reflection;
using SimpleJSON;

namespace UnityUtils.UnityPM
{
    public interface ISource
    {
        string Name { get; }
        List<Package> GetPackages();
    }

    public interface ISourceWithEditUI : ISource
    {
        ISourceEditUI GetEditUI();
    }

    public interface ISourceWithSettings : ISource
    {
        JSONObject SaveSettings();
        void LoadSettings(JSONObject settings);
    }

    public interface ISourceUnique : ISource { }

    public interface ISourceEditUI
    {
        bool Draw();
        void Apply();
    }

    static class SourceTypes
    {
        public static Type[] GetAll()
        {
            List<Type> output = new List<Type>();

            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
                foreach (var type in assembly.GetTypes())
                    if (typeof(ISource).IsAssignableFrom(type) && type.IsClass)
                        output.Add(type);

            return output.ToArray();
        }

        public static Type GetByName(string name)
        {
            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
                foreach (var type in assembly.GetTypes())
                    if (typeof(ISource).IsAssignableFrom(type) && type.Name == name)
                        return type;
            return null;
        }
    }
}
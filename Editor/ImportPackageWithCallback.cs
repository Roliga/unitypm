using System;
using System.Collections.Generic;
using UnityEditor;
using System.Reflection;

// 
// The usual ImportPackage function is asynchronous if run in interactive mode.
// There's no way to reliably get a callback when it's done. There are the
// AssetDatabase.ImportPackage* events, but those are incomplete and don't take
// into account if you close the import window for example.
//
// So let's just commit all the crimes and make our own ImportPackage function.
//
// This is all such a mess. Heck Unity.
// 

namespace UnityUtils.UnityPM
{
    static class ImportPackageWithCallback
    {
        //
        // WindowsReordered event
        // From https://github.com/kirurobo/UniWinApiAsset/blob/master/Scripts/WindowController.cs#L996
        //

        #region WindowsReordered

        private const BindingFlags BINDING_ATTR = BindingFlags.Static | BindingFlags.Instance | BindingFlags.NonPublic;

        private static readonly FieldInfo m_info = typeof(EditorApplication).GetField("windowsReordered", BINDING_ATTR);

        public static EditorApplication.CallbackFunction windowsReordered
        {
            get
            {
                return m_info.GetValue(null) as EditorApplication.CallbackFunction;
            }
            set
            {
                var functions = m_info.GetValue(null) as EditorApplication.CallbackFunction;
                functions += value;
                m_info.SetValue(null, functions);
            }
        }

        #endregion WindowsReordered

        //
        // PackageImport editor window type and methods
        // Mostly from https://assetstore.unity.com/packages/tools/utilities/package2folder-64829
        //

        #region PackageImport

        private static Type packageImportType;
        private static Type PackageImportType
        {
            get
            {
                if (packageImportType == null)
                {
                    packageImportType = typeof(MenuItem).Assembly.GetType("UnityEditor.PackageImport");
                }
                return packageImportType;
            }
        }

        private static MethodInfo validateInputMethodInfo;
        private static MethodInfo ValidateInputMethodInfo
        {
            get
            {
                if (validateInputMethodInfo == null)
                {
                    validateInputMethodInfo = PackageImportType.GetMethod("ValidateInput", BindingFlags.Static | BindingFlags.NonPublic);
                }

                return validateInputMethodInfo;
            }
        }

        private static bool ValidateInput(object[] items)
        {
            return (bool)ValidateInputMethodInfo.Invoke(null, new object[] { items });
        }

        private static MethodInfo initMethodInfo;
        private static MethodInfo InitMethodInfo
        {
            get
            {
                if (initMethodInfo == null)
                {
                    initMethodInfo = PackageImportType.GetMethod("Init", BindingFlags.NonPublic | BindingFlags.Instance);
                }

                return initMethodInfo;
            }
        }

        #endregion PackageImport

        //
        // PackageUtility type + methods
        // Mostly from https://assetstore.unity.com/packages/tools/utilities/package2folder-64829
        //

        #region PackageUtility

        private static Type packageUtilityType;
        private static Type PackageUtilityType
        {
            get
            {
                if (packageUtilityType == null)
                {
                    packageUtilityType = typeof(MenuItem).Assembly.GetType("UnityEditor.PackageUtility");
                }
                return packageUtilityType;
            }
        }

        private delegate object[] ExtractAndPrepareAssetListDelegate(string packagePath, out string packageIconPath, out string packageManagerDependenciesPath);

        private static ExtractAndPrepareAssetListDelegate extractAndPrepareAssetList;
        private static ExtractAndPrepareAssetListDelegate ExtractAndPrepareAssetList
        {
            get
            {
                if (extractAndPrepareAssetList == null)
                {
                    var method = PackageUtilityType.GetMethod("ExtractAndPrepareAssetList");
                    if (method == null)
                    {
                        throw new Exception("Couldn't extract method with ExtractAndPrepareAssetListDelegate delegate!");
                    }

                    extractAndPrepareAssetList = (ExtractAndPrepareAssetListDelegate)Delegate.CreateDelegate(
                       typeof(ExtractAndPrepareAssetListDelegate),
                       null,
                       method);
                }

                return extractAndPrepareAssetList;
            }
        }

        #endregion PackageUtility

        private static List<(EditorWindow, Action)> windowCallbacks = new List<(EditorWindow, Action)>();

        public static void ImportPackage(string packagePath, bool interactive, Action callback)
        {
            if (interactive)
            {
                // From https://assetstore.unity.com/packages/tools/utilities/package2folder-64829
                string packageIconPath;
                string packageManagerDependenciesPath;
                var assetsItems = ExtractAndPrepareAssetList(packagePath, out packageIconPath, out packageManagerDependenciesPath);

                if (assetsItems == null)
                {
                    callback.Invoke();
                    return;
                }

                // Emulate ShowImportPackage on PackageImport window
                // See https://github.com/Unity-Technologies/UnityCsReference/blob/61f92bd79ae862c4465d35270f9d1d57befd1761/Editor/Mono/GUI/PackageImport.cs#L53
                if (!ValidateInput(assetsItems))
                {
                    callback.Invoke();
                    return;
                }
                
                dynamic w = Convert.ChangeType(EditorWindow.GetWindow(PackageImportType, true, "Import Unity Package"), PackageImportType);
                InitMethodInfo.Invoke(w, new object[] { packagePath, assetsItems, packageIconPath });

                windowCallbacks.Add((w, callback));
            }
            else
            {
                AssetDatabase.ImportPackage(packagePath, false);
                callback.Invoke();
            }
        }

        static ImportPackageWithCallback()
        {
            windowsReordered += () =>
            {
                for (int i = windowCallbacks.Count - 1; i >= 0; i--)
                {
                    if (windowCallbacks[i].Item1 == null)
                    {
                        windowCallbacks[i].Item2.Invoke();
                        windowCallbacks.RemoveAt(i);
                    }
                }
            };
        }
    }
}

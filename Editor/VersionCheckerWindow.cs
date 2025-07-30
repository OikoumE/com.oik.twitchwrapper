using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

namespace Editor
{
#if UNITY_EDITOR

    public class VersionCheckerWindow : EditorWindow
    {
        private void OnGUI()
        {
            // if (GUILayout.Button("Check & Push Tag"))
            // {
            //     EditorCoroutineUtility.StartCoroutineOwnerless(CheckVersionCoroutine());
            // }
        }

        private IEnumerator CheckVersionCoroutine()
        {
            // CheckAndPushTag();
            // var v1 = GetLocalVersion();
            // var fetchTask = FetchVersionAsync();
            // yield return new EditorWaitForTask(fetchTask);
            // var v2 = fetchTask.Result;
            // var asd = new VersionComparer().Compare(v1, v2);
            // if (asd < 0)
            //     Debug.LogWarning($"local version: <{v1}> is out of date, available version: <{v2}>");
            yield break;
        }

        [MenuItem("Tools/Version Checker")]
        public static void ShowWindow()
        {
            GetWindow<VersionCheckerWindow>("Version Checker");
        }

        private static async Task<string> FetchVersionAsync()
        {
            using var client = new HttpClient();
            var url = "https://raw.githubusercontent.com/OikoumE/com.oik.twitchwrapper/master/package.json";
            var json = await client.GetStringAsync(url);
            var match = Regex.Match(json, @"""version""\s*:\s*""([^""]+)""");
            return match.Success ? match.Groups[1].Value : null;
        }

        private void CheckAndPushTag()
        {
            var version = GetLocalVersion();

            RunGit("tag", $"v{version}");
            RunGit("push", "origin", $"v{version}");
        }

        private string GetLocalVersion()
        {
            var path = GetPackageJsonPath();
            var json = File.ReadAllText(path);
            return JsonUtility.FromJson<PackageJson>(json).version;
        }

        private string GetPackageJsonPath()
        {
            var scriptPath = AssetDatabase.GetAssetPath(MonoScript.FromScriptableObject(this));
            if (string.IsNullOrEmpty(scriptPath))
                throw new NullReferenceException(nameof(scriptPath));
            var directory = Path.GetDirectoryName(scriptPath);
            if (string.IsNullOrEmpty(directory))
                throw new NullReferenceException(nameof(directory));
            return Path.GetFullPath(Path.Combine(directory, "..", "package.json"));
        }

        private void RunGit(params string[] args)
        {
            var psi = new ProcessStartInfo("git")
            {
                Arguments = string.Join(" ", args),
                WorkingDirectory = Path.GetFullPath(Path.Combine(Application.dataPath, "..")),
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            var p = Process.Start(psi);
            p?.WaitForExit();
        }

        [Serializable]
        private class PackageJson
        {
            public string version;
        }
        //TODO https://raw.githubusercontent.com/OikoumE/com.oik.twitchwrapper/refs/heads/master/package.json
        // grab package.json
        // parse to grab version
        // compare to local package.json
        // if localVersion < externalVersion
        // logwarn(update required)
        // editorButton to increment version
        // (and eventually commit&push)?

        private class VersionComparer : IComparer<string>
        {
            public int Compare(string x, string y)
            {
                var v1 = new Version(x ?? "0.0.0");
                var v2 = new Version(y ?? "0.0.0");
                return v1.CompareTo(v2);
            }
        }
    }
#endif
}
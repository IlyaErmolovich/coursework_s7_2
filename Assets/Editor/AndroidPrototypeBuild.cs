using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;
namespace WinterDuel.EditorTools
{
    public static class AndroidPrototypeBuild
    {
        public const string Output="Builds/Android/WinterDuel.apk";
        [MenuItem("Winter Duel/Build Android APK")]
        public static void Build()
        {
            Directory.CreateDirectory("Builds/Android");
            string statusPath="Builds/Android/build-status.json";
            File.WriteAllText(statusPath,"{\"status\":\"building\"}");
            try
            {
                var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{
                    scenes=new[]{"Assets/Scenes/MainMenu.unity","Assets/Scenes/WinterField.unity"},
                    locationPathName=Output,target=BuildTarget.Android,options=BuildOptions.Development
                });
                var s=report.summary;
                File.WriteAllText(statusPath,JsonUtility.ToJson(new BuildStatus{status=s.result.ToString(),bytes=s.totalSize,errors=s.totalErrors,warnings=s.totalWarnings,duration=s.totalTime.TotalSeconds},true));
                if(s.result!=BuildResult.Succeeded)Debug.LogError("Winter Duel Android build failed: "+s.result);
            }
            catch(Exception e){File.WriteAllText(statusPath,JsonUtility.ToJson(new BuildStatus{status="Exception",detail=e.ToString()},true));Debug.LogException(e);}
        }
        [Serializable] private sealed class BuildStatus
        {public string status,detail;public ulong bytes;public int errors,warnings;public double duration;}
    }
}

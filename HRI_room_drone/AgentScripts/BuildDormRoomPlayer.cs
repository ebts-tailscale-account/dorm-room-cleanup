using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

public static class BuildDormRoomPlayer
{
    public static string BuildWindows()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            throw new InvalidOperationException("Stop Play Mode before building the Windows player.");

        string output = Path.GetFullPath(Path.Combine(Application.dataPath, "../../Builds/Windows/DormRoom.exe"));
        if (File.Exists(output))
            throw new InvalidOperationException("Move the previous Windows build before rebuilding.");
        Directory.CreateDirectory(Path.GetDirectoryName(output));
        var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
        {
            scenes = new[] { "Assets/Scenes/DormRoom.unity" },
            locationPathName = output,
            target = BuildTarget.StandaloneWindows64,
            options = BuildOptions.None
        });
        if (report.summary.result != BuildResult.Succeeded)
            throw new InvalidOperationException($"Windows build {report.summary.result} with {report.summary.totalErrors} errors.");
        return $"{output} ({report.summary.totalSize / (1024 * 1024)} MiB, {PlayerSettings.fullScreenMode})";
    }
}

using UnityEditor;
using UnityEngine;
using System.Diagnostics;
using System.IO;

public class OpenGitBash
{
    [MenuItem("Tools/Open Git Bash")]
    public static void OpenBash()
    {
        string projectPath = Directory.GetParent(Application.dataPath).FullName;

        string gitBashPath = @"C:\Program Files\Git\git-bash.exe";
    

    ProcessStartInfo startInfo = new ProcessStartInfo
        {
            FileName = gitBashPath,
            WorkingDirectory = projectPath,
            UseShellExecute = true
        };

        Process.Start(startInfo);
    }
}
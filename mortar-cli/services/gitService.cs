using System;
using System.Diagnostics;
using System.IO;

namespace mortarCli.services
{
    public static class gitService
    {
        public static bool isGitRepo()
        {
            try
            {
                var result = runGitCommand("rev-parse --is-inside-work-tree");
                return result.success && result.output.Trim() == "true";
            }
            catch
            {
                return false;
            }
        }

        public static bool hasUncommittedChanges(string filePath)
        {
            try
            {
                var result = runGitCommand($"status --porcelain \"{filePath}\"");
                return result.success && !string.IsNullOrWhiteSpace(result.output);
            }
            catch
            {
                return false;
            }
        }

        public static bool stageFile(string filePath)
        {
            try
            {
                var result = runGitCommand($"add \"{filePath}\"");
                return result.success;
            }
            catch
            {
                return false;
            }
        }

        public static bool initAndCommit(string filePath)
        {
            try
            {
                stageFile(filePath);
                var result = runGitCommand("commit -m \"mortar: initial doclinks.json commit\"");
                return result.success;
            }
            catch
            {
                return false;
            }
        }

        private static (bool success, string output) runGitCommand(string args)
        {
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "git",
                    Arguments = args,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };

            process.Start();
            string output = process.StandardOutput.ReadToEnd();
            process.WaitForExit();

            return (process.ExitCode == 0, output);
        }
    }
}
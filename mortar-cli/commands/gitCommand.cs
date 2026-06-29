using System;
using System.IO;
using mortarCli.services;

namespace mortarCli.commands
{
    public static class gitCommand
    {
        public static void execute(string[] args)
        {
            string subCommand = args.Length > 1 ? args[1].ToLower() : null;

            if (string.IsNullOrEmpty(subCommand))
            {
                printUsage();
                return;
            }

            switch (subCommand)
            {
                case "init":
                    init();
                    break;
                case "status":
                    status();
                    break;
                default:
                    Console.WriteLine($"Error: Unknown git subcommand \"{subCommand}\"");
                    printUsage();
                    break;
            }
        }

        private static void init()
        {
            if (!gitService.isGitRepo())
            {
                Console.WriteLine("Error: Not inside a Git repository.");
                Console.WriteLine("Run 'git init' first to initialize a repository.");
                return;
            }

            string linksFile = Path.Combine(Directory.GetCurrentDirectory(), "docLinks.mor");

            if (!File.Exists(linksFile))
            {
                Console.WriteLine("Error: No docLinks.mor found in current directory.");
                Console.WriteLine("Add at least one link first using mortar-cli link.");
                return;
            }

            Console.WriteLine("Staging docLinks.mor...");
            if (!gitService.stageFile(linksFile))
            {
                Console.WriteLine("Error: Failed to stage docLinks.mor.");
                return;
            }

            Console.WriteLine("Committing...");
            if (!gitService.initAndCommit(linksFile))
            {
                Console.WriteLine("Error: Failed to commit docLinks.mor.");
                Console.WriteLine("Make sure you have set up your Git user name and email.");
                return;
            }

            Console.WriteLine("Done. docLinks.mor is now tracked by Git.");
            Console.WriteLine("Team members will see mortar links after pulling.");
        }

        private static void status()
        {
            if (!gitService.isGitRepo())
            {
                Console.WriteLine("Not inside a Git repository.");
                return;
            }

            string linksFile = Path.Combine(Directory.GetCurrentDirectory(), "docLinks.mor");

            if (!File.Exists(linksFile))
            {
                Console.WriteLine("No docLinks.mor found in current directory.");
                return;
            }

            bool hasChanges = gitService.hasUncommittedChanges(linksFile);

            if (hasChanges)
            {
                Console.WriteLine("Warning: docLinks.mor has uncommitted changes.");
                Console.WriteLine("Run 'git add docLinks.mor && git commit' to share with your team.");
                Console.WriteLine("Or run 'mortar-cli git init' if this is your first commit.");
            }
            else
            {
                Console.WriteLine("docLinks.mor is up to date with Git.");
            }
        }

        private static void printUsage()
        {
            Console.WriteLine("Usage: mortar-cli git <subcommand>");
            Console.WriteLine();
            Console.WriteLine("Subcommands:");
            Console.WriteLine("  init    Stage and commit docLinks.mor to Git");
            Console.WriteLine("  status  Check if docLinks.mor has uncommitted changes");
        }
    }
}
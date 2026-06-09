using System;
using mortarCli.commands;

namespace mortarCli
{
    class program
    {
        static void Main(string[] args)
        {
            Console.OutputEncoding = System.Text.Encoding.UTF8;

            if (args.Length == 0)
            {
                printUsage();
                return;
            }

            switch (args[0].ToLower())
            {
                case "link":
                    linkCommand.execute(args);
                    break;
                case "unlink":
                    unlinkCommand.execute(args);
                    break;
                case "rename":
                    renameCommand.execute(args);
                    break;
                case "status":
                    statusCommand.execute(args);
                    break;
                case "info":
                    infoCommand.execute(args);
                    break;
                case "git":
                    gitCommand.execute(args);
                    break;
                default:
                    Console.WriteLine($"Unknown command: \"{args[0]}\"");
                    Console.WriteLine();
                    printUsage();
                    break;
            }
        }

        static void printUsage()
        {
            Console.WriteLine("mortar-cli — documentation linker");
            Console.WriteLine();
            Console.WriteLine("Usage: mortar-cli <command> [arguments]");
            Console.WriteLine();
            Console.WriteLine("Commands:");
            Console.WriteLine("  link <sourceFile> [--url <url>] [--name <nickname>] [--type <docType>]");
            Console.WriteLine("                    [--notes <notes>] [--primary] [--no-sync]");
            Console.WriteLine("  unlink <sourceFile> [<documentPath> | --name <nickname> | --all]");
            Console.WriteLine("  rename <sourceFile> [<documentPath> | --name <oldNickname>] <newNickname>");
            Console.WriteLine("  status [--type <docType>]");
            Console.WriteLine("  info [<sourceFile>]");
            Console.WriteLine("  git init");
            Console.WriteLine("  git status");
            Console.WriteLine();
            Console.WriteLine("Doc types:");
            Console.WriteLine("  datasheet, requirements, schematic, testSpec, apiSpec,");
            Console.WriteLine("  researchPaper, designSpec, runbook, license, changelog, other");
            Console.WriteLine();
            Console.WriteLine("Notes:");
            Console.WriteLine("  Running a command without required arguments launches interactive mode.");
            Console.WriteLine("  Use silent mode (all args provided) for scripting and CI pipelines.");
            Console.WriteLine("  Commit doclinks.json to Git to share links across your team.");
            Console.WriteLine();
            Console.WriteLine("Flags:");
            Console.WriteLine("  --url <url>       Link a web URL instead of or alongside a local file");
            Console.WriteLine("  --name <nickname> Give the link a short display name");
            Console.WriteLine("  --type <docType>  Categorize the document");
            Console.WriteLine("  --notes <text>    Add a short note about this link");
            Console.WriteLine("  --primary         Mark as the main reference for this source file.");
            Console.WriteLine("                    Purely informational for now — shown with * in status.");
            Console.WriteLine("                    One primary per source file recommended.");
            Console.WriteLine("  --no-sync         Disable out of date detection for this entry.");
            Console.WriteLine("                    Useful for URLs or docs that change independently.");
        }
    }
}
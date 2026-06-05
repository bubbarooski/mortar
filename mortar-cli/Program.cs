using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;

namespace mortar_cli
{
    class documentEntry
    {
        public string path { get; set; }
        public string nickname { get; set; }
    }

    class docLink
    {
        public string sourceFile { get; set; }
        public List<documentEntry> documentPaths { get; set; } = new List<documentEntry>();
        public string linkedAt { get; set; }
    }

    class Program
    {
        static string getLinksFilePath()
        {
            return System.IO.Path.Combine(Directory.GetCurrentDirectory(), "doclinks.json");
        }

        // Normalize path to absolute, trimmed, with consistent separators
        static string normalizePath(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                return null;

            path = path.Trim();

            try
            {
                // Convert to absolute path if relative
                if (!System.IO.Path.IsPathRooted(path))
                    path = System.IO.Path.GetFullPath(
                        System.IO.Path.Combine(Directory.GetCurrentDirectory(), path));
                else
                    path = System.IO.Path.GetFullPath(path);

                return path;
            }
            catch
            {
                return null;
            }
        }

        static bool pathsEqual(string a, string b)
        {
            // Case-insensitive comparison on Windows
            return string.Equals(
                System.IO.Path.GetFullPath(a),
                System.IO.Path.GetFullPath(b),
                StringComparison.OrdinalIgnoreCase);
        }

        static bool nicknamesEqual(string a, string b)
        {
            return string.Equals(a, b, StringComparison.OrdinalIgnoreCase);
        }

        static List<docLink> loadLinks()
        {
            string path = getLinksFilePath();

            if (!File.Exists(path))
                return new List<docLink>();

            try
            {
                string json = File.ReadAllText(path);

                if (string.IsNullOrWhiteSpace(json))
                    return new List<docLink>();

                return JsonConvert.DeserializeObject<List<docLink>>(json)
                    ?? new List<docLink>();
            }
            catch (JsonException)
            {
                Console.WriteLine("Warning: doclinks.json is corrupted or malformed.");
                Console.WriteLine("Rename or delete it to start fresh.");
                return null;
            }
            catch (UnauthorizedAccessException)
            {
                Console.WriteLine("Error: No permission to read doclinks.json.");
                return null;
            }
            catch (IOException ex)
            {
                Console.WriteLine($"Error reading doclinks.json: {ex.Message}");
                return null;
            }
        }

        static bool saveLinks(List<docLink> links)
        {
            try
            {
                string json = JsonConvert.SerializeObject(links, Formatting.Indented);
                File.WriteAllText(getLinksFilePath(), json);
                return true;
            }
            catch (UnauthorizedAccessException)
            {
                Console.WriteLine("Error: No permission to write doclinks.json.");
                return false;
            }
            catch (IOException ex)
            {
                Console.WriteLine($"Error writing doclinks.json: {ex.Message}");
                return false;
            }
        }

        static void Main(string[] args)
        {
            if (args.Length == 0)
            {
                PrintUsage();
                return;
            }

            switch (args[0].ToLower())
            {
                case "link":
                    link(args);
                    break;

                case "unlink":
                    unlink(args);
                    break;

                case "rename":
                    rename(args);
                    break;

                case "status":
                    status();
                    break;

                default:
                    Console.WriteLine($"Unknown command: \"{args[0]}\"");
                    Console.WriteLine();
                    PrintUsage();
                    break;
            }
        }

        static void PrintUsage()
        {
            Console.WriteLine("Usage: mortar-cli <command> [arguments]");
            Console.WriteLine();
            Console.WriteLine("Commands:");
            Console.WriteLine("  link <sourceFile> <documentPath> [--name <nickname>]");
            Console.WriteLine("  unlink <sourceFile> <documentPath>");
            Console.WriteLine("  unlink <sourceFile> --name <nickname>");
            Console.WriteLine("  unlink <sourceFile> --all");
            Console.WriteLine("  rename <sourceFile> <documentPath> <newNickname>");
            Console.WriteLine("  rename <sourceFile> --name <oldNickname> <newNickname>");
            Console.WriteLine("  status");
            Console.WriteLine();
            Console.WriteLine("Note: Use quotes around paths or nicknames containing spaces.");
            Console.WriteLine("      e.g. mortar-cli link sensor.c \"C:\\My Docs\\sheet.pdf\" --name \"Sensor Doc\"");
        }

        static void link(string[] args)
        {
            if (args.Length < 3)
            {
                Console.WriteLine("Usage: mortar-cli link <sourceFile> <documentPath> [--name <nickname>]");
                return;
            }

            string sourceFile = normalizePath(args[1]);
            string documentPath = normalizePath(args[2]);

            // Validate normalized paths
            if (sourceFile == null)
            {
                Console.WriteLine($"Error: Invalid source file path \"{args[1]}\"");
                return;
            }
            if (documentPath == null)
            {
                Console.WriteLine($"Error: Invalid document path \"{args[2]}\"");
                return;
            }

            // Prevent linking a file to itself
            if (pathsEqual(sourceFile, documentPath))
            {
                Console.WriteLine("Error: Source file and document path cannot be the same.");
                return;
            }

            // Warn if source file doesn't exist
            if (!File.Exists(sourceFile))
                Console.WriteLine($"Warning: Source file does not exist: {sourceFile}");

            // Warn if document doesn't exist
            if (!File.Exists(documentPath))
                Console.WriteLine($"Warning: Document does not exist: {documentPath}");

            // Parse optional --name flag
            string nickname = null;
            for (int i = 3; i < args.Length; i++)
            {
                if (args[i].ToLower() == "--name")
                {
                    if (i + 1 >= args.Length)
                    {
                        Console.WriteLine("Error: --name flag requires a value.");
                        return;
                    }
                    nickname = args[i + 1].Trim();
                    if (string.IsNullOrWhiteSpace(nickname))
                    {
                        Console.WriteLine("Error: Nickname cannot be empty or whitespace.");
                        return;
                    }
                    break;
                }
                else if (args[i].StartsWith("--"))
                {
                    Console.WriteLine($"Error: Unknown flag \"{args[i]}\"");
                    return;
                }
            }

            var links = loadLinks();
            if (links == null) return;

            var existing = links.Find(l => pathsEqual(l.sourceFile, sourceFile));

            if (existing != null)
            {
                // Check for duplicate document path
                if (existing.documentPaths.Exists(d => pathsEqual(d.path, documentPath)))
                {
                    Console.WriteLine($"Error: {documentPath} is already linked to {sourceFile}");
                    return;
                }

                // Check for duplicate nickname
                if (nickname != null &&
                    existing.documentPaths.Exists(d =>
                        d.nickname != null && nicknamesEqual(d.nickname, nickname)))
                {
                    Console.WriteLine($"Error: Nickname \"{nickname}\" is already used for another document on this file.");
                    return;
                }

                existing.documentPaths.Add(new documentEntry
                {
                    path = documentPath,
                    nickname = nickname
                });
            }
            else
            {
                links.Add(new docLink
                {
                    sourceFile = sourceFile,
                    documentPaths = new List<documentEntry>
                    {
                        new documentEntry { path = documentPath, nickname = nickname }
                    },
                    linkedAt = DateTime.UtcNow.ToString("o")
                });
            }

            if (!saveLinks(links)) return;

            string label = nickname != null ? $" (\"{nickname}\")" : "";
            Console.WriteLine($"Linked {sourceFile} -> {documentPath}{label}");
        }

        static void unlink(string[] args)
        {
            if (args.Length < 2)
            {
                Console.WriteLine("Usage: mortar-cli unlink <sourceFile> <documentPath>");
                Console.WriteLine("       mortar-cli unlink <sourceFile> --name <nickname>");
                Console.WriteLine("       mortar-cli unlink <sourceFile> --all");
                return;
            }

            string sourceFile = normalizePath(args[1]);
            if (sourceFile == null)
            {
                Console.WriteLine($"Error: Invalid source file path \"{args[1]}\"");
                return;
            }

            var links = loadLinks();
            if (links == null) return;

            var existing = links.Find(l => pathsEqual(l.sourceFile, sourceFile));
            if (existing == null)
            {
                Console.WriteLine($"No links found for {sourceFile}");
                return;
            }

            if (args.Length < 3)
            {
                Console.WriteLine("Specify a document path, --name <nickname>, or --all");
                return;
            }

            // Check for contradictory flags
            bool hasAll = false;
            bool hasName = false;
            foreach (var a in args)
            {
                if (a.ToLower() == "--all") hasAll = true;
                if (a.ToLower() == "--name") hasName = true;
            }
            if (hasAll && hasName)
            {
                Console.WriteLine("Error: Cannot use --all and --name together.");
                return;
            }

            if (args[2].ToLower() == "--all")
            {
                links.Remove(existing);
                if (!saveLinks(links)) return;
                Console.WriteLine($"Removed all links from {sourceFile}");
                return;
            }

            documentEntry entry = null;

            if (args[2].ToLower() == "--name")
            {
                if (args.Length < 4)
                {
                    Console.WriteLine("Error: --name flag requires a value.");
                    return;
                }
                string nickname = args[3].Trim();
                if (string.IsNullOrWhiteSpace(nickname))
                {
                    Console.WriteLine("Error: Nickname cannot be empty or whitespace.");
                    return;
                }
                entry = existing.documentPaths.Find(d =>
                    d.nickname != null && nicknamesEqual(d.nickname, nickname));
                if (entry == null)
                {
                    Console.WriteLine($"No link with nickname \"{nickname}\" found for {sourceFile}");
                    return;
                }
            }
            else if (args[2].StartsWith("--"))
            {
                Console.WriteLine($"Error: Unknown flag \"{args[2]}\"");
                return;
            }
            else
            {
                string documentPath = normalizePath(args[2]);
                if (documentPath == null)
                {
                    Console.WriteLine($"Error: Invalid document path \"{args[2]}\"");
                    return;
                }
                entry = existing.documentPaths.Find(d => pathsEqual(d.path, documentPath));
                if (entry == null)
                {
                    Console.WriteLine($"{documentPath} is not linked to {sourceFile}");
                    return;
                }
            }

            existing.documentPaths.Remove(entry);
            if (existing.documentPaths.Count == 0)
                links.Remove(existing);

            if (!saveLinks(links)) return;
            Console.WriteLine($"Unlinked \"{entry.path}\" from {sourceFile}");
        }

        static void rename(string[] args)
        {
            if (args.Length < 4)
            {
                Console.WriteLine("Usage: mortar-cli rename <sourceFile> <documentPath> <newNickname>");
                Console.WriteLine("       mortar-cli rename <sourceFile> --name <oldNickname> <newNickname>");
                return;
            }

            string sourceFile = normalizePath(args[1]);
            if (sourceFile == null)
            {
                Console.WriteLine($"Error: Invalid source file path \"{args[1]}\"");
                return;
            }

            var links = loadLinks();
            if (links == null) return;

            var existing = links.Find(l => pathsEqual(l.sourceFile, sourceFile));
            if (existing == null)
            {
                Console.WriteLine($"No links found for {sourceFile}");
                return;
            }

            documentEntry entry = null;
            string newNickname = null;

            if (args[2].ToLower() == "--name")
            {
                if (args.Length < 5)
                {
                    Console.WriteLine("Usage: mortar-cli rename <sourceFile> --name <oldNickname> <newNickname>");
                    return;
                }
                string oldNickname = args[3].Trim();
                newNickname = args[4].Trim();

                if (string.IsNullOrWhiteSpace(oldNickname))
                {
                    Console.WriteLine("Error: Old nickname cannot be empty or whitespace.");
                    return;
                }

                entry = existing.documentPaths.Find(d =>
                    d.nickname != null && nicknamesEqual(d.nickname, oldNickname));
                if (entry == null)
                {
                    Console.WriteLine($"No link with nickname \"{oldNickname}\" found for {sourceFile}");
                    return;
                }
            }
            else if (args[2].StartsWith("--"))
            {
                Console.WriteLine($"Error: Unknown flag \"{args[2]}\"");
                return;
            }
            else
            {
                string documentPath = normalizePath(args[2]);
                if (documentPath == null)
                {
                    Console.WriteLine($"Error: Invalid document path \"{args[2]}\"");
                    return;
                }
                newNickname = args[3].Trim();
                entry = existing.documentPaths.Find(d => pathsEqual(d.path, documentPath));
                if (entry == null)
                {
                    Console.WriteLine($"{documentPath} is not linked to {sourceFile}");
                    return;
                }
            }

            if (string.IsNullOrWhiteSpace(newNickname))
            {
                Console.WriteLine("Error: New nickname cannot be empty or whitespace.");
                return;
            }

            // Check renaming to same nickname
            if (entry.nickname != null && nicknamesEqual(entry.nickname, newNickname))
            {
                Console.WriteLine($"Warning: \"{entry.path}\" already has nickname \"{newNickname}\"");
                return;
            }

            // Check duplicate nickname on same source file
            if (existing.documentPaths.Exists(d =>
                d != entry && d.nickname != null && nicknamesEqual(d.nickname, newNickname)))
            {
                Console.WriteLine($"Error: Nickname \"{newNickname}\" is already used for another document on this file.");
                return;
            }

            string oldName = entry.nickname ?? "none";
            entry.nickname = newNickname;
            if (!saveLinks(links)) return;
            Console.WriteLine($"Renamed \"{oldName}\" to \"{newNickname}\" for {entry.path}");
        }

        static void status()
        {
            var links = loadLinks();
            if (links == null) return;

            if (links.Count == 0)
            {
                Console.WriteLine("No files linked.");
                return;
            }

            // Truncate long strings for display only
            string Truncate(string s, int max) =>
                s.Length <= max ? s : "..." + s.Substring(s.Length - (max - 3));

            Console.WriteLine($"{"Source File",-30} {"Nickname",-20} {"Status",-15} {"Document"}");
            Console.WriteLine(new string('-', 100));

            foreach (var link in links)
            {
                foreach (var doc in link.documentPaths)
                {
                    string status = getStatus(link.sourceFile, doc.path);
                    string nickname = doc.nickname ?? "-";
                    string srcDisplay = Truncate(
                        System.IO.Path.GetFileName(link.sourceFile), 30);
                    string docDisplay = Truncate(doc.path, 40);

                    Console.WriteLine($"{srcDisplay,-30} {nickname,-20} {status,-15} {docDisplay}");
                }
            }
        }

        static string getStatus(string sourceFile, string documentPath)
        {
            if (!File.Exists(sourceFile))
                return "SRC NOT FOUND";
            if (!File.Exists(documentPath))
                return "DOC NOT FOUND";

            try
            {
                DateTime srcModified = File.GetLastWriteTimeUtc(sourceFile);
                DateTime docModified = File.GetLastWriteTimeUtc(documentPath);
                return docModified > srcModified ? "OUT OF DATE" : "UP TO DATE";
            }
            catch
            {
                return "READ ERROR";
            }
        }
    }
}
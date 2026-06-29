using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using mortarCli.models;

namespace mortarCli.services
{
    public static class storageService
    {
        private static string getLinksFilePath()
        {
            return Path.Combine(Directory.GetCurrentDirectory(), "docLinks.mor");
        }

        // Original — used by all CLI commands
        public static List<docLink> loadLinks()
        {
            return loadLinks(getLinksFilePath());
        }

        public static bool saveLinks(List<docLink> links)
        {
            return saveLinks(getLinksFilePath(), links);
        }

        // Overloads — used by tests
        public static List<docLink> loadLinks(string path)
        {
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
                Console.WriteLine("Warning: doclinks.mor is corrupted or malformed.");
                return null;
            }
            catch (UnauthorizedAccessException)
            {
                Console.WriteLine("Error: No permission to read doclinks.mor.");
                return null;
            }
            catch (IOException ex)
            {
                Console.WriteLine($"Error reading doclinks.mor: {ex.Message}");
                return null;
            }
        }

        public static bool saveLinks(string path, List<docLink> links)
        {
            try
            {
                string json = JsonConvert.SerializeObject(links, Formatting.Indented);
                File.WriteAllText(path, json);
                return true;
            }
            catch (UnauthorizedAccessException)
            {
                Console.WriteLine("Error: No permission to write doclinks.mor.");
                return false;
            }
            catch (IOException ex)
            {
                Console.WriteLine($"Error writing doclinks.mor: {ex.Message}");
                return false;
            }
        }
    }
}
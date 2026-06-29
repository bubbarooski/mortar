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

        public static List<docLink> loadLinks()
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
                Console.WriteLine("Warning: docLinks.mor is corrupted or malformed.");
                Console.WriteLine("Rename or delete it to start fresh.");
                return null;
            }
            catch (UnauthorizedAccessException)
            {
                Console.WriteLine("Error: No permission to read docLinks.mor.");
                return null;
            }
            catch (IOException ex)
            {
                Console.WriteLine($"Error reading docLinks.mor: {ex.Message}");
                return null;
            }
        }

        public static bool saveLinks(List<docLink> links)
        {
            try
            {
                string json = JsonConvert.SerializeObject(links, Formatting.Indented);
                File.WriteAllText(getLinksFilePath(), json);
                return true;
            }
            catch (UnauthorizedAccessException)
            {
                Console.WriteLine("Error: No permission to write docLinks.mor.");
                return false;
            }
            catch (IOException ex)
            {
                Console.WriteLine($"Error writing docLinks.mor: {ex.Message}");
                return false;
            }
        }
    }
}
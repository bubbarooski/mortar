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
            return Path.Combine(Directory.GetCurrentDirectory(), "doclinks.json");
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
                Console.WriteLine("Error: No permission to write doclinks.json.");
                return false;
            }
            catch (IOException ex)
            {
                Console.WriteLine($"Error writing doclinks.json: {ex.Message}");
                return false;
            }
        }
    }
}
using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using mortar.models;

namespace mortar.services
{
    public static class storageService
    {
        public static List<docLink>? loadLinks(string path)
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
            catch
            {
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
            catch
            {
                return false;
            }
        }
    }
}
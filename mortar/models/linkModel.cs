using System.Collections.Generic;

namespace mortar.models
{
    public class documentEntry
    {
        public string path { get; set; }
        public string url { get; set; }
        public string nickname { get; set; }
        public string docType { get; set; }
        public string notes { get; set; }
        public bool isPrimary { get; set; } = false;
        public bool outOfDateDetection { get; set; } = true;
    }

    public class docLink
    {
        public string sourceFile { get; set; }
        public List<documentEntry> documentPaths { get; set; } = new List<documentEntry>();
        public string linkedAt { get; set; }
    }

    public class documentNode
    {
        public string displayName { get; set; }
        public string fullPath { get; set; }
        public string url { get; set; }
        public string docType { get; set; }
        public string notes { get; set; }
        public bool isPrimary { get; set; }
        public bool isOutOfDate { get; set; }
    }

    public class sourceFileNode
    {
        public string displayName { get; set; }
        public string fullPath { get; set; }
        public List<documentNode> documents { get; set; } = new List<documentNode>();
    }
}
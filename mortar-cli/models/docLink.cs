using System.Collections.Generic;

namespace mortarCli.models
{
    public class docLink
    {
        public string sourceFile { get; set; }
        public List<documentEntry> documentPaths { get; set; } = new List<documentEntry>();
        public string linkedAt { get; set; }
    }
}
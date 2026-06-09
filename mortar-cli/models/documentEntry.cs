namespace mortarCli.models
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

    public static class docTypes
    {
        public const string datasheet = "datasheet";
        public const string requirements = "requirements";
        public const string schematic = "schematic";
        public const string testSpec = "testSpec";
        public const string apiSpec = "apiSpec";
        public const string researchPaper = "researchPaper";
        public const string designSpec = "designSpec";
        public const string runbook = "runbook";
        public const string license = "license";
        public const string changelog = "changelog";
        public const string other = "other";

        public static readonly string[] all = {
            datasheet, requirements, schematic, testSpec,
            apiSpec, researchPaper, designSpec, runbook,
            license, changelog, other
        };
    }
}
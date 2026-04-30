namespace LearnToCode.API.Services;

public class TheoryStorageOptions
{
    public string BucketName { get; set; } = "learn-to-code";

    public string Prefix { get; set; } = "theory";

    public string ManifestObject { get; set; } = "theory/manifest.json";

    public bool AutoCreateBucket { get; set; } = true;

    public bool SeedDefaults { get; set; } = true;
}

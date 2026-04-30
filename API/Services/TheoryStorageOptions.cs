namespace LearnToCode.API.Services;

public class TheoryStorageOptions
{
    public string BucketName { get; set; } = "learn-to-code";

    public string Prefix { get; set; } = "theory";

    public bool AutoCreateBucket { get; set; } = true;
}

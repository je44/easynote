namespace EasyNote;

internal sealed class SelfTestReport
{
    public bool Success => Checks.All(check => check.Passed);
    public List<SelfTestCheck> Checks { get; } = [];
}

internal sealed class SelfTestCheck
{
    public string Name { get; init; } = string.Empty;
    public bool Passed { get; init; }
    public string Details { get; init; } = string.Empty;
}

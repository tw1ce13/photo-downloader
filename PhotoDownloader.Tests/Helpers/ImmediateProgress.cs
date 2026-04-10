namespace PhotoDownloader.Tests.Helpers;

public sealed class ImmediateProgress : IProgress<double>
{
    public double Last { get; private set; }
    public void Report(double value) => Last = value;
}
namespace PhotoDownloader.Tests.Helpers;

public sealed class ListProgress : IProgress<double>
{
    private readonly List<double> _values;
    public ListProgress(List<double> values) => _values = values;
    public void Report(double value) => _values.Add(value);
}
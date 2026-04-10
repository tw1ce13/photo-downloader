using PhotoDownloader.Services;
using PhotoDownloader.Tests.Helpers;
using Xunit;

namespace PhotoDownloader.Tests;

public sealed class DownloadProgressReporterTests
{
    [Fact]
    public void Report_NullProgress_DoesNotThrow()
    {
        var ex = Record.Exception(() => DownloadProgressReporter.Report(null, 1024, null));
        Assert.Null(ex);
    }

    [Fact]
    public void Report_WithContentLength_ReportsProportionalUpToDownloadWeight()
    {
        var progress = new ImmediateProgress();

        DownloadProgressReporter.Report(progress, 50, 100);

        Assert.Equal(0.5 * DownloadProgressReporter.DownloadWeight, progress.Last, precision: 10);
    }

    [Fact]
    public void Report_WithContentLength_CappedAtDownloadWeight()
    {
        var progress = new ImmediateProgress();

        DownloadProgressReporter.Report(progress, 200, 100);

        Assert.Equal(DownloadProgressReporter.DownloadWeight, progress.Last, precision: 10);
    }

    [Fact]
    public void Report_WithoutContentLength_IncreasesAndStaysBelowPhaseCap()
    {
        var values = new List<double>();
        var progress = new ListProgress(values);

        DownloadProgressReporter.Report(progress, 0, null);
        DownloadProgressReporter.Report(progress, 1024 * 1024, null);
        var small = values[^1];
        DownloadProgressReporter.Report(progress, 50L * 1024 * 1024, null);
        var large = values[^1];

        Assert.True(values[0] < small);
        Assert.True(small < large);
        Assert.True(large < DownloadProgressReporter.DownloadWeight);
    }
}

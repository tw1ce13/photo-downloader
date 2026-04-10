using System.Diagnostics.CodeAnalysis;

namespace PhotoDownloader.Infrastructure;

/// <summary>
/// Проверка URL перед загрузкой изображения.
/// </summary>
public static class ImageUrlValidator
{
    public static bool TryValidate(string? url, [NotNullWhen(true)] out Uri? uri, out string? errorMessage)
    {
        uri = null;
        errorMessage = null;

        if (string.IsNullOrWhiteSpace(url))
        {
            errorMessage = "Введен пустой URL";
            return false;
        }

        if (!Uri.TryCreate(url.Trim(), UriKind.Absolute, out var absolute))
        {
            errorMessage = "Введен некорректный URL";
            return false;
        }

        if (absolute.Scheme != Uri.UriSchemeHttp && absolute.Scheme != Uri.UriSchemeHttps)
        {
            errorMessage = "Строка url должна содержать http или https";
            return false;
        }

        uri = absolute;
        return true;
    }
}

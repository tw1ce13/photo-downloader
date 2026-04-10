using System.IO;
using System.Net.Http;

namespace PhotoDownloader.Infrastructure;

/// <summary>
/// Сообщения об ошибках загрузки для отображения пользователю.
/// </summary>
public static class DownloadUserMessage
{
    public static string From(Exception ex)
    {
        return ex switch
        {
            HttpRequestException => "Сеть недоступна или сервер не отвечает. Проверьте подключение.",
            IOException => "Сбой при получении данных по сети.",
            InvalidOperationException => ex.Message,
            _ => ex.Message,
        };
    }
}

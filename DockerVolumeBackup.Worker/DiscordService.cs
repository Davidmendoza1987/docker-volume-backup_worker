using Newtonsoft.Json;
using System.Text;

namespace DockerVolumeBackup.Worker;

/// <summary>
/// Interface IDiscordService
/// </summary>
public interface IDiscordService
{
    /// <summary>
    /// Posts the discord message asynchronous.
    /// </summary>
    /// <param name="webhookUrl">The webhook URL.</param>
    /// <param name="message">The message.</param>
    /// <returns>Task.</returns>
    Task PostDiscordMessageAsync(string webhookUrl, string message);
}

/// <summary>
/// Service for interacting with Discord webhooks.
/// </summary>
public class DiscordService : IDiscordService
{
    /// <summary>
    /// The HTTP client
    /// </summary>
    private readonly HttpClient _httpClient;

    /// <summary>
    /// The logger
    /// </summary>
    private readonly ILogger<DiscordService> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="DiscordService"/> class.
    /// </summary>
    /// <param name="httpClient">The HTTP client used for making requests.</param>
    /// <param name="logger">The logger for logging information and errors.</param>
    public DiscordService(ILogger<DiscordService> logger, HttpClient httpClient)
    {
        _logger = logger;
        _httpClient = httpClient;
    }

    /// <summary>
    /// Posts a message to the specified Discord webhook.
    /// </summary>
    /// <param name="webhookUrl">The Discord webhook URL.</param>
    /// <param name="message">The message to post.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task PostDiscordMessageAsync(string webhookUrl, string message)
    {
        // Serialize the message content into JSON format
        var request = JsonConvert.SerializeObject(new { content = message });

        // Create HTTP content with the serialized JSON
        var httpContent = new StringContent(request, Encoding.UTF8, "application/json");

        try
        {
            // Send a POST request to the Discord webhook URL
            var response = await _httpClient.PostAsync(webhookUrl, httpContent);

            // Ensure the response indicates success
            response.EnsureSuccessStatusCode();
        }
        catch (HttpRequestException ex)
        {
            // Log the exception if the request fails
            _logger.LogError(ex, $"{DateTime.Now} | Error posting message to Discord webhook.");
        }
    }
}

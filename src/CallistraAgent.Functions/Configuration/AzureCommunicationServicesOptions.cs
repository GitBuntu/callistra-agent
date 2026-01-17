namespace CallistraAgent.Functions.Configuration;

/// <summary>
/// Configuration options for Azure Communication Services
/// </summary>
public class AzureCommunicationServicesOptions
{
    /// <summary>
    /// Configuration section name
    /// </summary>
    public const string SectionName = "AzureCommunicationServices";

    /// <summary>
    /// Azure Communication Services connection string
    /// </summary>
    public string ConnectionString { get; set; } = string.Empty;

    /// <summary>
    /// Phone number to use for outbound calls (E.164 format)
    /// </summary>
    public string PhoneNumber { get; set; } = string.Empty;

    /// <summary>
    /// Azure Communication Services endpoint URL (for managed identity authentication)
    /// </summary>
    public string? Endpoint { get; set; }

    /// <summary>
    /// Base URL for call event callbacks/webhooks
    /// </summary>
    public string CallbackBaseUrl { get; set; } = string.Empty;

    /// <summary>
    /// Azure Cognitive Services endpoint URL for text-to-speech and speech recognition
    /// </summary>
    public string? CognitiveServicesEndpoint { get; set; }
}

namespace CallistraAgent.Functions.Configuration;

/// <summary>
/// Configuration options for database connection
/// </summary>
public class DatabaseOptions
{
    /// <summary>
    /// Configuration section name
    /// </summary>
    public const string SectionName = "ConnectionStrings";

    /// <summary>
    /// SQL Server connection string
    /// </summary>
    public string CallistraAgentDb { get; set; } = string.Empty;
}

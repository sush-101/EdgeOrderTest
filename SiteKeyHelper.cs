using System.Text;
using System.Text.Json;

namespace EdgeOrderTest;

public class SiteKeyHelper
{
    public class SiteKeyData
    {
        public string? resourceId { get; set; }
        public string? aadEndpoint { get; set; }
        public string? armEndPoint { get; set; }
        public string? tenantId { get; set; }
        public string? clientId { get; set; }
        public string? clientSecret { get; set; }
    }

    /// <summary>
    /// Decodes and inspects a Base64-encoded site key
    /// </summary>
    public static SiteKeyData DecodeSiteKey(string encodedSiteKey)
    {
        try
        {
            // Decode from Base64
            byte[] decodedBytes = Convert.FromBase64String(encodedSiteKey);
            string jsonString = Encoding.UTF8.GetString(decodedBytes);
            
            // Parse JSON
            var siteKeyData = JsonSerializer.Deserialize<SiteKeyData>(jsonString);
            return siteKeyData ?? throw new InvalidOperationException("Failed to deserialize site key");
        }
        catch (FormatException ex)
        {
            throw new ArgumentException("Invalid Base64 string", nameof(encodedSiteKey), ex);
        }
        catch (JsonException ex)
        {
            throw new ArgumentException("Invalid JSON in site key", nameof(encodedSiteKey), ex);
        }
    }

    /// <summary>
    /// Creates a Base64-encoded site key from individual components
    /// </summary>
    public static string EncodeSiteKey(
        string resourceId,
        string tenantId,
        string clientId,
        string clientSecret,
        string aadEndpoint = "https://login.microsoftonline.com/",
        string armEndPoint = "https://management.azure.com/")
    {
        var siteKeyData = new SiteKeyData
        {
            resourceId = resourceId,
            aadEndpoint = aadEndpoint,
            armEndPoint = armEndPoint,
            tenantId = tenantId,
            clientId = clientId,
            clientSecret = clientSecret
        };

        string jsonString = JsonSerializer.Serialize(siteKeyData);
        byte[] jsonBytes = Encoding.UTF8.GetBytes(jsonString);
        return Convert.ToBase64String(jsonBytes);
    }

    /// <summary>
    /// Displays site key information (masks sensitive data)
    /// </summary>
    public static void DisplaySiteKeyInfo(SiteKeyData siteKey, bool showSecrets = false)
    {
        Console.WriteLine("Site Key Information:");
        Console.WriteLine("=====================");
        Console.WriteLine($"Resource ID:   {siteKey.resourceId}");
        Console.WriteLine($"AAD Endpoint:  {siteKey.aadEndpoint}");
        Console.WriteLine($"ARM Endpoint:  {siteKey.armEndPoint}");
        Console.WriteLine($"Tenant ID:     {siteKey.tenantId}");
        Console.WriteLine($"Client ID:     {siteKey.clientId}");
        
        if (showSecrets && !string.IsNullOrEmpty(siteKey.clientSecret))
        {
            Console.WriteLine($"Client Secret: {siteKey.clientSecret}");
        }
        else if (!string.IsNullOrEmpty(siteKey.clientSecret))
        {
            Console.WriteLine($"Client Secret: {MaskSecret(siteKey.clientSecret)}");
        }
        Console.WriteLine();
    }

    private static string MaskSecret(string secret)
    {
        if (string.IsNullOrEmpty(secret) || secret.Length <= 4)
            return "****";
        
        return secret.Substring(0, 4) + new string('*', Math.Min(secret.Length - 4, 20)) + 
               (secret.Length > 24 ? secret.Substring(secret.Length - 4) : "");
    }

    /// <summary>
    /// Validates a site key has all required fields
    /// </summary>
    public static bool ValidateSiteKey(SiteKeyData siteKey, out List<string> errors)
    {
        errors = new List<string>();

        if (string.IsNullOrWhiteSpace(siteKey.resourceId))
            errors.Add("resourceId is required");
        
        if (string.IsNullOrWhiteSpace(siteKey.aadEndpoint))
            errors.Add("aadEndpoint is required");
        
        if (string.IsNullOrWhiteSpace(siteKey.tenantId))
            errors.Add("tenantId is required");
        else if (!Guid.TryParse(siteKey.tenantId, out _))
            errors.Add("tenantId must be a valid GUID");
        
        if (string.IsNullOrWhiteSpace(siteKey.clientId))
            errors.Add("clientId is required");
        else if (!Guid.TryParse(siteKey.clientId, out _))
            errors.Add("clientId must be a valid GUID");
        
        if (string.IsNullOrWhiteSpace(siteKey.clientSecret))
            errors.Add("clientSecret is required");

        return errors.Count == 0;
    }
}

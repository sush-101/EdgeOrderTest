using Azure.ResourceManager.EdgeOrder;
using Azure.ResourceManager.EdgeOrder.Customizations.Models;
using EdgeOrderTest;
using System.Text.Json;

Console.WriteLine("Azure EdgeOrder - GetEdgeOrderDevices Test");
Console.WriteLine("==========================================\n");

Console.WriteLine("Choose an option:");
Console.WriteLine("1. Test GetEdgeOrderDevices with encoded site key");
Console.WriteLine("2. Decode and inspect a site key");
Console.WriteLine("3. Create a new encoded site key");
Console.Write("\nEnter option (1-3): ");

string? option = Console.ReadLine();
Console.WriteLine();

if (option == "2")
{
    // Decode and inspect site key
    Console.WriteLine("Paste your Base64-encoded site key:");
    string? encodedKey = Console.ReadLine();
    
    if (string.IsNullOrWhiteSpace(encodedKey))
    {
        Console.WriteLine("Error: Site key cannot be empty.");
        return;
    }

    try
    {
        var decoded = SiteKeyHelper.DecodeSiteKey(encodedKey);
        
        Console.Write("\nShow client secret? (y/n): ");
        bool showSecrets = Console.ReadLine()?.ToLower() == "y";
        Console.WriteLine();
        
        SiteKeyHelper.DisplaySiteKeyInfo(decoded, showSecrets);
        
        // Validate
        if (SiteKeyHelper.ValidateSiteKey(decoded, out var errors))
        {
            Console.WriteLine("✓ Site key is valid and ready to use!");
        }
        else
        {
            Console.WriteLine("✗ Site key validation errors:");
            foreach (var error in errors)
            {
                Console.WriteLine($"  - {error}");
            }
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error decoding site key: {ex.Message}");
    }
    return;
}
else if (option == "3")
{
    // Create new site key
    Console.WriteLine("Create a new site key");
    Console.WriteLine("=====================\n");
    
    Console.Write("Resource ID (e.g., /subscriptions/{sub}/resourceGroups/{rg}/providers/...): ");
    string? resourceId = Console.ReadLine();
    
    Console.Write("Tenant ID (GUID): ");
    string? tenantId = Console.ReadLine();
    
    Console.Write("Client ID (GUID): ");
    string? clientId = Console.ReadLine();
    
    Console.Write("Client Secret: ");
    string? clientSecret = Console.ReadLine();
    
    Console.Write("AAD Endpoint (press Enter for default): ");
    string? aadEndpoint = Console.ReadLine();
    if (string.IsNullOrWhiteSpace(aadEndpoint))
        aadEndpoint = "https://login.microsoftonline.com/";
    
    Console.Write("ARM Endpoint (press Enter for default): ");
    string? armEndpoint = Console.ReadLine();
    if (string.IsNullOrWhiteSpace(armEndpoint))
        armEndpoint = "https://management.azure.com/";

    try
    {
        string encodedKey = SiteKeyHelper.EncodeSiteKey(
            resourceId: resourceId!,
            tenantId: tenantId!,
            clientId: clientId!,
            clientSecret: clientSecret!,
            aadEndpoint: aadEndpoint,
            armEndPoint: armEndpoint
        );
        
        Console.WriteLine("\n✓ Site key created successfully!");
        Console.WriteLine("\nEncoded Site Key (use this with GetEdgeOrderDevices):");
        Console.WriteLine("========================================================");
        Console.WriteLine(encodedKey);
        
        // Save to file
        File.WriteAllText("site_key.txt", encodedKey);
        Console.WriteLine("\n✓ Saved to site_key.txt");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"\nError creating site key: {ex.Message}");
    }
    return;
}
else if (option != "1")
{
    Console.WriteLine("Invalid option.");
    return;
}

// Option 1: Test GetEdgeOrderDevices
Console.WriteLine("Paste your Base64-encoded site key:");
string? siteKey = Console.ReadLine();

if (string.IsNullOrWhiteSpace(siteKey))
{
    Console.WriteLine("Error: Site key cannot be empty.");
    return;
}

// First, decode and show site key info
try
{
    Console.WriteLine("\nDecoding site key...");
    var decoded = SiteKeyHelper.DecodeSiteKey(siteKey);
    SiteKeyHelper.DisplaySiteKeyInfo(decoded, showSecrets: false);
    
    if (!SiteKeyHelper.ValidateSiteKey(decoded, out var errors))
    {
        Console.WriteLine("✗ Site key validation failed:");
        foreach (var error in errors)
        {
            Console.WriteLine($"  - {error}");
        }
        return;
    }
}
catch (Exception ex)
{
    Console.WriteLine($"Warning: Could not decode site key: {ex.Message}");
    Console.WriteLine("Continuing anyway...\n");
}

Console.WriteLine("\nTesting GetEdgeOrderDevices method...\n");

try
{
    // Optional parameters
    int? top = null; // You can set this to limit results, e.g., top = 10
    string? skipToken = null; // Used for pagination

    // Call the GetEdgeOrderDevices method
    Console.WriteLine("Calling EdgeOrderExtensions.GetEdgeOrderDevices()...");
    EdgeOrderDeviceResponse response = EdgeOrderExtensions.GetEdgeOrderDevices(
        siteKey: siteKey,
        top: top,
        skipToken: skipToken,
        cancellationToken: default
    );

    Console.WriteLine("✓ Method call successful!\n");

    // Display results
    Console.WriteLine($"Results:");
    Console.WriteLine($"--------");
    Console.WriteLine($"Number of devices found: {response.EdgeOrderDevices?.Count ?? 0}");
    Console.WriteLine($"Skip Token: {response.SkipToken ?? "(none)"}\n");

    if (response.EdgeOrderDevices != null && response.EdgeOrderDevices.Count > 0)
    {
        Console.WriteLine("Device Details:");
        Console.WriteLine(new string('=', 80));
        
        int deviceIndex = 1;
        foreach (var device in response.EdgeOrderDevices)
        {
            Console.WriteLine($"\nDevice #{deviceIndex}:");
            Console.WriteLine($"  Order Item ID: {device.OrderItemId ?? "N/A"}");
            Console.WriteLine($"  Manufacturer:  {device.Manufacturer ?? "N/A"}");
            Console.WriteLine($"  Model Name:    {device.ModelName ?? "N/A"}");
            Console.WriteLine($"  Serial Number: {device.SerialNumber ?? "N/A"}");

            if (device.DeviceConfiguration != null)
            {
                Console.WriteLine($"\n  Configuration:");
                Console.WriteLine($"    Hostname: {device.DeviceConfiguration.HostName ?? "N/A"}");
                
                if (device.DeviceConfiguration.Network?.NetworkAdapters != null && 
                    device.DeviceConfiguration.Network.NetworkAdapters.Count > 0)
                {
                    Console.WriteLine($"\n    Network Adapters ({device.DeviceConfiguration.Network.NetworkAdapters.Count}):");
                    for (int i = 0; i < device.DeviceConfiguration.Network.NetworkAdapters.Count; i++)
                    {
                        var adapter = device.DeviceConfiguration.Network.NetworkAdapters[i];
                        Console.WriteLine($"      Adapter {i + 1}:");
                        Console.WriteLine($"        IP Assignment: {adapter.IpAssignmentType}");
                        Console.WriteLine($"        IP Address:    {adapter.IpAddress ?? "N/A"}");
                        Console.WriteLine($"        Gateway:       {adapter.Gateway ?? "N/A"}");
                        Console.WriteLine($"        Subnet Mask:   {adapter.SubnetMask ?? "N/A"}");
                        Console.WriteLine($"        VLAN ID:       {adapter.VlanId ?? "N/A"}");
                    }
                }

                if (device.DeviceConfiguration.Time != null)
                {
                    Console.WriteLine($"\n    Time Configuration:");
                    Console.WriteLine($"      Primary Time Server:   {device.DeviceConfiguration.Time.PrimaryTimeServer ?? "N/A"}");
                    Console.WriteLine($"      Secondary Time Server: {device.DeviceConfiguration.Time.SecondaryTimeServer ?? "N/A"}");
                    Console.WriteLine($"      Time Zone:             {device.DeviceConfiguration.Time.TimeZone ?? "N/A"}");
                }

                if (device.DeviceConfiguration.WebProxy != null)
                {
                    Console.WriteLine($"\n    Web Proxy Configuration:");
                    Console.WriteLine($"      URI:  {device.DeviceConfiguration.WebProxy.ConnectionUri ?? "N/A"}");
                    Console.WriteLine($"      Port: {device.DeviceConfiguration.WebProxy.Port ?? "N/A"}");
                }
            }
            
            Console.WriteLine(new string('-', 80));
            deviceIndex++;
        }

        // Export to JSON file
        Console.WriteLine("\nExporting results to JSON file...");
        string jsonOutput = JsonSerializer.Serialize(response, new JsonSerializerOptions 
        { 
            WriteIndented = true 
        });
        
        string outputFile = Path.Combine(Environment.CurrentDirectory, "edge_order_devices.json");
        File.WriteAllText(outputFile, jsonOutput);
        Console.WriteLine($"✓ Results exported to: {outputFile}");
    }
    else
    {
        Console.WriteLine("\nNo devices found.");
    }

    Console.WriteLine("\n==========================================");
    Console.WriteLine("Test completed successfully!");
}
catch (ArgumentException ex)
{
    Console.WriteLine($"\n✗ Argument Error:");
    Console.WriteLine($"  {ex.Message}");
    Console.WriteLine("\nPlease ensure your site key is valid and properly formatted.");
}
catch (Exception ex)
{
    Console.WriteLine($"\n✗ Error occurred:");
    Console.WriteLine($"  Type: {ex.GetType().Name}");
    Console.WriteLine($"  Message: {ex.Message}");
    
    if (ex.InnerException != null)
    {
        Console.WriteLine($"\n  Inner Exception: {ex.InnerException.GetType().Name}");
        Console.WriteLine($"  Inner Message: {ex.InnerException.Message}");
    }
    
    Console.WriteLine($"\n  Stack Trace:");
    Console.WriteLine($"  {ex.StackTrace}");
}

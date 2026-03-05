using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using Azure.Identity;
using Azure.ResourceManager.AzureStackHCI;
using Azure.ResourceManager.AzureStackHCI.Custom.Models;

var jsonOptions = new JsonSerializerOptions { WriteIndented = true };
jsonOptions.Converters.Add(new AzureSdkExtensibleEnumConverterFactory());

Console.WriteLine("Azure Stack HCI - Edge Machines SDK Test");
Console.WriteLine("  1 - Site Key");
Console.WriteLine("  2 - Azure Token (DefaultAzureCredential)");
Console.Write("Choice [1/2]: ");

switch (Console.ReadLine()?.Trim())
{
    case "1": RunSiteKeyTest(jsonOptions); break;
    case "2": RunTokenBasedTest(jsonOptions); break;
    default: Console.WriteLine("Invalid choice."); break;
}

static void RunSiteKeyTest(JsonSerializerOptions jsonOptions)
{
    Console.Write("\nPaste Base64-encoded site key: ");
    string? siteKey = Console.ReadLine();
    if (string.IsNullOrWhiteSpace(siteKey)) { Console.WriteLine("Error: Site key cannot be empty."); return; }

    try
    {
        var response = AzureStackHciCustomExtensions.GetEdgeMachines(encodedSiteKey: siteKey);
        PrintResponse(response, jsonOptions);
    }
    catch (Exception ex) { PrintError(ex); }
}

static void RunTokenBasedTest(JsonSerializerOptions jsonOptions)
{
    Console.Write("\nSubscription ID: ");
    string? subscriptionId = Console.ReadLine()?.Trim();
    if (string.IsNullOrWhiteSpace(subscriptionId)) { Console.WriteLine("Error: required."); return; }

    Console.Write("Resource Group: ");
    string? resourceGroupName = Console.ReadLine()?.Trim();
    if (string.IsNullOrWhiteSpace(resourceGroupName)) { Console.WriteLine("Error: required."); return; }

    Console.Write("Tenant ID (Enter to skip): ");
    string? tenantId = Console.ReadLine()?.Trim();

    try
    {
        var opts = new DefaultAzureCredentialOptions();
        if (!string.IsNullOrWhiteSpace(tenantId)) opts.TenantId = tenantId;
        var credential = new DefaultAzureCredential(opts);

        var response = AzureStackHciCustomExtensions.GetEdgeMachinesWithToken(
            credential: credential,
            subscriptionId: subscriptionId,
            resourceGroupName: resourceGroupName);

        PrintResponse(response, jsonOptions);
    }
    catch (Exception ex) { PrintError(ex); }
}

static void PrintResponse(EdgeMachineResponse response, JsonSerializerOptions jsonOptions)
{
    Console.WriteLine($"\nDevices found: {response.EdgeMachines?.Count ?? 0}");
    if (response.EdgeMachines is not { Count: > 0 }) { Console.WriteLine("(no devices)"); return; }

    for (int i = 0; i < response.EdgeMachines.Count; i++)
    {
        Console.WriteLine($"\n--- EdgeMachine [{i}] ---");
        Console.WriteLine(JsonSerializer.Serialize(response.EdgeMachines[i], jsonOptions));
    }
}

static void PrintError(Exception ex)
{
    Console.WriteLine($"\nError [{ex.GetType().Name}]: {ex.Message}");
    if (ex.InnerException != null)
        Console.WriteLine($"  Inner: {ex.InnerException.Message}");
    Console.WriteLine($"\n{ex.StackTrace}");
}

// ===========================================================================
//  Custom JSON converter for Azure SDK extensible-enum structs
//  (readonly structs like IpAssignmentType that serialize as {} by default)
// ===========================================================================
public class AzureSdkExtensibleEnumConverterFactory : JsonConverterFactory
{
    public override bool CanConvert(Type typeToConvert) =>
        typeToConvert.IsValueType
        && !typeToConvert.IsPrimitive
        && !typeToConvert.IsEnum
        && typeToConvert.Namespace?.Contains("AzureStackHCI") == true
        && typeToConvert.GetField("_value", BindingFlags.NonPublic | BindingFlags.Instance) != null;

    public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options)
    {
        var converterType = typeof(Converter<>).MakeGenericType(typeToConvert);
        return (JsonConverter)Activator.CreateInstance(converterType)!;
    }

    private class Converter<T> : JsonConverter<T> where T : struct
    {
        public override T Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
            (T)Activator.CreateInstance(typeToConvert, reader.GetString())!;

        public override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options) =>
            writer.WriteStringValue(value.ToString());
    }
}


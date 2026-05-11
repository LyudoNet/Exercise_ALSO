// Console entry point for the GUHC Hierarchy Accounts viewer.
//
// Usage:
//   dotnet run                              → print the full account tree
//   dotnet run -- <guid>                    → print the subtree of the given account
//   dotnet run -- --api-url <url>           → override the default API base URL
//   dotnet run -- <guid> --api-url <url>   → combine both options

using HierarchyAccounts.Console.Display;
using HierarchyAccounts.Console.Services;

const string DefaultApiUrl = "https://localhost:7001/";

var apiUrl = DefaultApiUrl;
Guid? accountId = null;

// Parse CLI arguments
for (int i = 0; i < args.Length; i++)
{
    if (args[i] == "--api-url" && i + 1 < args.Length)
    {
        apiUrl = args[++i];
    }
    else if (Guid.TryParse(args[i], out var parsedId))
    {
        accountId = parsedId;
    }
}

var client = new ApiClient(apiUrl);

try
{
    var tree = accountId.HasValue
        ? await client.GetSubtreeAsync(accountId.Value)
        : await client.GetFullTreeAsync();

    if (tree is null)
    {
        Console.Error.WriteLine("No data returned from the API.");
        return;
    }

    Console.WriteLine();
    Console.WriteLine(accountId.HasValue
        ? $"Subtree for account {accountId}:"
        : "Full Account Hierarchy:");
    Console.WriteLine(new string('─', 60));

    TreePrinter.Print(tree);
    Console.WriteLine();
}
catch (HttpRequestException ex)
{
    Console.Error.WriteLine($"[ERROR] Could not reach the API at {apiUrl}");
    Console.Error.WriteLine($"        {ex.Message}");
}
finally
{
    Console.WriteLine("\nPress any key to exit...");
    Console.ReadKey();
}

namespace HierarchyAccounts.Console.Display;

using HierarchyAccounts.Application.DTOs;

/// <summary>
/// Renders an AccountTreeDto as a readable ASCII tree in the terminal.
/// </summary>
/// <example>
/// Global Corp (depth: 1) [id: xxxxxxxx-...]
/// ├── Europe Region (depth: 2)
/// │   ├── Germany Office (depth: 3)
/// │   └── France Office (depth: 3)
/// └── Asia Region (depth: 2)
///     └── Japan Office (depth: 3)
/// </example>
public static class TreePrinter
{
    public static void Print(AccountTreeDto node, string prefix = "", bool isLast = true)
    {
        // Determine the connector character based on whether this is the last sibling
        var connector = prefix == "" ? "" : (isLast ? "└── " : "├── ");

        System.Console.WriteLine($"{prefix}{connector}{node.Name} (depth: {node.Depth}) [id: {node.Id}]");

        // Calculate prefix for child nodes
        var childPrefix = prefix + (prefix == "" ? "" : (isLast ? "    " : "│   "));

        for (int i = 0; i < node.Children.Count; i++)
        {
            var isLastChild = i == node.Children.Count - 1;
            Print(node.Children[i], childPrefix, isLastChild);
        }
    }
}

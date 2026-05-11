namespace HierarchyAccounts.Console.Display;

using HierarchyAccounts.Application.DTOs;
using System;

/// <summary>
/// Renders an AccountTreeDto as a readable ASCII tree in the terminal.
/// </summary>
public static class TreePrinter
{
    /// <summary> Prints account tree with depth-based tabulation. </summary>
    public static void Print(AccountTreeDto node, string prefix = "", bool isLast = true, bool isRoot = true)
    {
        var connector = isRoot ? "" : (isLast ? "└── " : "├── ");
        var originalColor = Console.ForegroundColor;

        // Print prefix and connector
        Console.Write(prefix);
        Console.Write(connector);

        // Print name with highlight
        Console.ForegroundColor = isRoot ? ConsoleColor.Yellow : ConsoleColor.Cyan;
        Console.Write(node.Name);

        // Print metadata
        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.WriteLine($" (depth: {node.Depth}) [id: {node.Id}]");

        Console.ForegroundColor = originalColor;

        // Calculate next level prefix (using tabs)
        var childPrefix = prefix + (isRoot ? "\t" : (isLast ? "\t" : "│\t"));

        for (int i = 0; i < node.Children.Count; i++)
        {
            var isLastChild = i == node.Children.Count - 1;
            Print(node.Children[i], childPrefix, isLastChild, false);
        }
    }

}


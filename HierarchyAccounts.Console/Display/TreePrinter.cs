namespace HierarchyAccounts.Console.Display;

using HierarchyAccounts.Application.DTOs;
using System;

/// <summary>
/// Renders an AccountTreeDto as a readable ASCII tree in the terminal.
/// </summary>
public static class TreePrinter
{
    /// <summary>
    /// Recursively prints the account tree with tabulation according to depth.
    /// </summary>
    /// <param name="node">The current account node.</param>
    /// <param name="prefix">The prefix string for the current line (indentation + bars).</param>
    /// <param name="isLast">True if this is the last child of its parent.</param>
    /// <param name="isRoot">True if this is the starting point of the print.</param>
    public static void Print(AccountTreeDto node, string prefix = "", bool isLast = true, bool isRoot = true)
    {
        // Determine the connector character based on whether this is the last sibling
        var connector = isRoot ? "" : (isLast ? "└── " : "├── ");

        // Save original color for restoration
        var originalColor = Console.ForegroundColor;

        // 1. Print indentation and connector
        Console.Write(prefix);
        Console.Write(connector);

        // 2. Print Account Name with highlight
        Console.ForegroundColor = isRoot ? ConsoleColor.Yellow : ConsoleColor.Cyan;
        Console.Write(node.Name);

        // 3. Print Metadata in a subtle color
        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.WriteLine($" (depth: {node.Depth}) [id: {node.Id}]");

        // Restore color
        Console.ForegroundColor = originalColor;

        // 4. Calculate prefix for child nodes using tabs for "tabulation according to depth"
        // If a tab is 4 or 8 spaces, this ensures clear hierarchy.
        var childPrefix = prefix + (isRoot ? "\t" : (isLast ? "\t" : "│\t"));

        for (int i = 0; i < node.Children.Count; i++)
        {
            var isLastChild = i == node.Children.Count - 1;
            Print(node.Children[i], childPrefix, isLastChild, false);
        }
    }
}


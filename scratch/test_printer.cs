
using System;
using System.Collections.Generic;

public record AccountTreeDto(
    Guid Id,
    string Name,
    Guid? ParentId,
    int Depth,
    DateTime CreatedAt,
    List<AccountTreeDto> Children
);

public static class TreePrinter
{
    public static void Print(AccountTreeDto node, string prefix = "", bool isLast = true)
    {
        // Determine the connector character based on whether this is the last sibling
        var connector = prefix == "" ? "" : (isLast ? "└── " : "├── ");

        Console.WriteLine($"{prefix}{connector}{node.Name} (depth: {node.Depth}) [id: {node.Id}]");

        // Calculate prefix for child nodes
        var childPrefix = prefix + (prefix == "" ? "" : (isLast ? "    " : "│   "));

        for (int i = 0; i < node.Children.Count; i++)
        {
            var isLastChild = i == node.Children.Count - 1;
            Print(node.Children[i], childPrefix, isLastChild);
        }
    }
}

class Program {
    static void Main() {
        var root = new AccountTreeDto(Guid.NewGuid(), "Root", null, 1, DateTime.Now, new List<AccountTreeDto>());
        var child1 = new AccountTreeDto(Guid.NewGuid(), "Child 1", root.Id, 2, DateTime.Now, new List<AccountTreeDto>());
        var child2 = new AccountTreeDto(Guid.NewGuid(), "Child 2", root.Id, 2, DateTime.Now, new List<AccountTreeDto>());
        var gchild1 = new AccountTreeDto(Guid.NewGuid(), "GChild 1", child1.Id, 3, DateTime.Now, new List<AccountTreeDto>());
        
        root.Children.Add(child1);
        root.Children.Add(child2);
        child1.Children.Add(gchild1);
        
        Console.WriteLine("Current implementation output:");
        TreePrinter.Print(root);
    }
}

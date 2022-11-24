# SideData
Attach data to any object, for the lifetime of that object.

## Example Basic Usage

```csharp
using SideData;

var obj = new { Name = "Some object" };
obj.SideData().Shape = "Rectangle";
obj.SideData().NumberOfEdges = 4;
obj.SideData().EdgeLengths = new int[] { 6, 4, 6, 4 };

Console.WriteLine($"A {obj.SideData().Shape} has {obj.SideData().NumberOfEdges} sides");
```

See the unit tests for a few more examples

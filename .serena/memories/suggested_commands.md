# Suggested Commands

## Build & Test
```bash
dotnet build                                                    # Build entire solution
dotnet test                                                     # Run all tests (xUnit v3 with MTP-v2)
dotnet test --framework net8.0                                 # Run tests against specific TFM
dotnet test --filter-class "SideData.SideDataTests"            # Filter by class (requires fully qualified name)
dotnet test --filter-method "SideData.SideDataTests.MethodName"  # Filter by method (requires fully qualified name)
dotnet test --filter-namespace "SideData"                      # Filter by namespace
dotnet test --list-tests                                       # List all available tests
dotnet pack -c Release                                         # Create NuGet package
```

**Note:** xUnit v3 with MTP uses different filter syntax than VSTest:
- Use `--filter-class`, `--filter-method`, `--filter-namespace` (not `--filter "expression"`)
- Requires fully qualified names for class and method filters
- Space-separated multiple values: `--filter-class "ClassA" "ClassB"`

## Git (Windows with bash shell)
```bash
git status
git log --oneline -10
git diff
git add <file>
git commit -m "message"
```

## System Utilities (bash on Windows)
```bash
ls                   # List directory
find . -name "*.cs"  # Find files
grep -r "pattern" .  # Search content
```

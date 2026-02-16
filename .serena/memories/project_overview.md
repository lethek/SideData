# SideData - Project Overview

## Purpose
SideData is a small .NET library that attaches arbitrary data to any .NET reference-type object using `ConditionalWeakTable`, so attached data is automatically garbage-collected with its host object.

## Tech Stack
- **Language**: C# (latest LangVersion)
- **Library targets**: `net8.0`, `netstandard2.1`, `netstandard2.0`
- **Test targets**: `net10.0`, `net8.0`
- **Testing**: xUnit v3.2.2 with Microsoft Testing Platform v2 (MTP-v2)
- **Test runner**: Microsoft Testing Platform (configured via global.json)
- **Test data**: Bogus v35.6.5 for fake data generation
- **Test projects**: Executable (`OutputType=Exe`) as required by xUnit v3
- **Versioning**: GitVersion (ContinuousDeployment mode)
- **CI**: GitHub Actions
- **Package**: NuGet (MIT license)

## Repository Structure
```
SideData.sln
src/SideData/
  ObjectExtensions.cs        - Extension method entry point (.SideData())
  SideDataAttachment.cs      - Core attachment type
  SideDataBag.cs             - Bag for storing attached data
  Internals/
    ReferenceEqualityComparer.cs - Internal helper
tests/SideData.Tests/
  SideDataTests.cs           - Tests for main SideData API
  SideDataAttachmentTests.cs - Tests for SideDataAttachment
  Fixtures/TestDataFixture.cs
  TestData/                  - Test data classes (User, ContactData, PersonalData, etc.)
```

## Key Classes
- `ObjectExtensions` (namespace `SideData`) - Extension methods for attaching side data
- `SideDataAttachment` - Represents an attachment of data to an object
- `SideDataBag` - Container for side data values
- `ReferenceEqualityComparer<T>` (namespace `SideData.Internals`) - Internal comparer

## Author
Michael Monsour

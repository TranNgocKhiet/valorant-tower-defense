# Streaming Tests

This directory contains unit tests and property-based tests for the game live streaming feature.

## Test Framework Setup

### NUnit
- **Version**: 3.13.3
- **Purpose**: Unit testing framework for C#
- **Usage**: Write unit tests for specific examples and edge cases

### FsCheck
- **Version**: 2.16.5
- **Purpose**: Property-based testing framework for .NET
- **Usage**: Write property tests that verify universal properties across randomized inputs
- **Configuration**: Each property test runs a minimum of 100 iterations

### FSharp.Core
- **Version**: 6.0.0
- **Purpose**: Required dependency for FsCheck

## Installation

1. Install NuGet for Unity if not already installed
2. Use NuGet for Unity to restore packages from `packages.config`
3. Alternatively, manually download and place DLLs in `Assets/Plugins/` directory:
   - `nunit.framework.dll`
   - `FsCheck.dll`
   - `FSharp.Core.dll`

## Test Structure

- **Unit Tests**: Located in `Unit/` subdirectory
- **Property Tests**: Located in `Properties/` subdirectory
- **Test Utilities**: Located in `Utilities/` subdirectory (generators, helpers)

## Running Tests

Tests can be run from the Unity Test Runner window:
1. Open Window > General > Test Runner
2. Select EditMode tab
3. Run All or select specific tests

## Property Test Tagging

Each property test must include a comment tag referencing the design document:
```csharp
// Feature: game-live-streaming, Property 1: Connection Establishment
[Property(Iterations = 100)]
public Property TestName()
{
    // Test implementation
}
```

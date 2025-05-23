# Arcor2.ClientSdk

This repository contains client libraries and related projects for the ARCOR2 system.

For detailed information, see the `README` included in each project directory.

## Contents

- `Arcor2.ClientSdk.Communication.OpenApi`: Data models generated from the server's OpenAPI specification. Use the included `generate_models.sh` script to regenerate.
- `Arcor2.ClientSdk.Communication`: Lightweight maintainable client library designed to act as a universal typed wrapper.
- `Arcor2.ClientSdk.Communication.UnitTests`: Unit tests for the Communication library.
- `Arcor2.ClientSdk.ClientServices`: All-in-one client library designed to be a complete backend solution. Manages the local state and provides simpler object-oriented interface.
- `Arcor2.ClientSdk.ClientServices.ConsoleTestApp`: A trivial console client using the ClientServices library. Showcases the basic usage in a simple way.
- `Arcor2.ClientSdk.ClientServices.UnitTests`: Unit tests for extension and helper methods for the ClientServices library.
- `Arcor2.ClientSdk.ClientServices.IntegrationTests`: Integration tests for RPCs and events for the ClientServices library. It uses the FIT Demo server using `Testcontainers` with mock Dobot robots.

## Included CI/CD Pipeline

The CI/CD pipeline automatically performs DevOps operations on pull requests or commits to the `main` branch. It runs unit tests, builds the libraries into .NET Standard 2.1 assemblies, and releases the created NuGet packages. 

To automatically release NuGet packages, make sure the `Publish NuGet packages` task is uncommented and the `NUGET_API_KEY` secret is set.
Package configuration is configured within the project files. Make sure to bump the version up according to [Semantic versioning](https://learn.microsoft.com/en-us/dotnet/csharp/versioning) in .NET.

Integration tests are executed only for pull requests labelled `run-integration-tests`. Currently, each test instance regenerates the ARCOR2 server, leading to excessive resource consumption (approximately 4 hours of pipeline time). Although tests are written to clean up after themselves, bugs with the ARCOR2 server cause cascading failures. It is recommended that the integration tests be run locally. See the `README` of the integration test project for more information.

## Manual Usage and Building
The libraries can be used by either:
1. Directly including their source code within a solution.
2. Downloading their compiled assemblies from the latest GitHub Action run and linking them within a project.
3. Downloading them as packages from NuGet package repository (or from the latest GitHub Action run) and referencing them within a project.

The libraries can be built manually using Visual Studio or the expected `dotnet` commands. The test projects require .NET 8 while the library projects only require .NET Standard 2.1 compatible runtime.

```
// Restore dependencies
dotnet restore Arcor2.ClientSdk.sln

// Run all tests within a solution (to run only a specific project, reference its .csproj file)
dotnet test  Arcor2.ClientSdk.sln

// Build the whole solution
dotnet build Arcor2.ClientSdk.sln --configuration Release --no-restore

// Package the libraries
dotnet pack src/Arcor2.ClientSdk.Communication/Arcor2.ClientSdk.Communication.csproj -c Release -o packages --no-build
dotnet pack src/Arcor2.ClientSdk.ClientServices/Arcor2.ClientSdk.ClientServices.csproj -c Release -o packages --no-build
```

## Release Checklist

- [ ] **Implementation Quality**
  - [ ] A reasonable effort was given to make public APIs of `ClientServices` library backward-compatible
  - [ ] All members are documented using up-to-date XML comments
  - [ ] Linter and formatter (Code Cleanup in Visual Studio) was executed upon the solution according to the included `.editorconfig`
  - [ ] All warnings are fixed (or suppressed, if appropriate)

- [ ] **Test Quality**
  - [ ] All new functionality is tested 
  - [ ] All existing change-related tests pass

- [ ] **Project Configuration**
  - [ ] Version number of library was bumped if its or any referenced projects were changed
  - [ ] Embedded changelog was updated

- [ ] **Documentation**
  - [ ] README is still up-to-date
  - [ ] GitHub changelog was updated

- [ ] **Release**
  - [ ] Created and accepted new PR into the `main` branch
  - [ ] CI/CD pipeline passes (build, test, publish)
  - [ ] GitHub version tag created (e.g., `git tag vX.Y.Z`)
  - [ ] Tested installation from package registry (e.g., NuGet)
  - [ ] Optionally create a new GitHub releaase
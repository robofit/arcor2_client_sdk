# Arcor2.ClientSdk

This repository contains client libraries and related projects for the ARCOR2 system.

For detailed information see the `README` included in each project directory.

## Contents

- `Arcor2.ClientSdk.Communication.OpenApi`: Data models generated from server's OpenAPI specification. Use the included `generate_models.sh` script to regenerate.
- `Arcor2.ClientSdk.Communication`: Lightweight maintainable client library designed to act as a universel typed wrapper.
- `Arcor2.ClientSdk.Communication.UnitTests`: Unit tests for the Communication library.
- `Arcor2.ClientSdk.ClientServices`: All in one client library designed to be a complete backend solution. Manages the local state and provides simpler object-oriented interface.
- `Arcor2.ClientSdk.ClientServices.ConsoleTestApp`: A trivial console client using the ClientServices library. Showcases the basic usage in a simple way.
- `Arcor2.ClientSdk.ClientServices.UnitTests`: Unit tests for extension and helper methods for the ClientServices library.
- `Arcor2.ClientSdk.ClientServices.IntegrationTests`: Integration tests for RPCs and events for the ClientServices library. Uses FIT Demo server using `Testcontainers` with mock Dobot robots.

## Builds and CI/CD  

The CI/CD pipeline automatically runs unit tests and builds the libraries into .NET Standard 2.1 assemblies and NuGet packages whenever the `main` branch is updated or a pull request is created.  

Integration tests are executed only for pull requests labeled `run-integration-tests`. Currently, each test instance regenerates the ARCOR2 server, leading to excessive resource consumption (approximately 4 hours of pipeline time). Although tests are written to clean up after themselves, bugs with the ARCOR2 server cause cascading failures. It is recommended to run the integration tests locally. See the `README` of the integration test project for more information.

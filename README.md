# Arcor2.ClientSdk

This repository contains client libraries and related projects for the ARCOR2 system.

For detailed information see the `README` included in each project directory.

## Contents

- `Arcor2.ClientSdk.Communication.OpenApi`: Data models generated from server's OpenAPI specification. Contains regeneration script for the OpenAPI Generator.
- `Arcor2.ClientSdk.Communication`: Lightweight maintainable client library designed to act as a universel typed wrapper.
- `Arcor2.ClientSdk.Communication.UnitTests`: Unit tests for the Communication library.
- `Arcor2.ClientSdk.ClientServices`: All in one client library designed to be a complete backend solution. Manages the local state and provides simpler object-oriented interface.
- `Arcor2.ClientSdk.ClientServices.UnitTests`: Unit tests for extension and helper methods for the ClientServices library.
- `Arcor2.ClientSdk.ClientServices.IntegrationTests`: Integration tests for RPCs and events for the ClientServices library. Uses FIT Demo server using `Testcontainers` with mock Dobot robots.
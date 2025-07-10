# Sufficit.Asterisk.Shared

[![NuGet](https://img.shields.io/nuget/v/Sufficit.Asterisk.Shared.svg)](https://www.nuget.org/packages/Sufficit.Asterisk.Shared/)

## Description

`Sufficit.Asterisk.Shared` provides **shared components, extensions, and utilities** used across the Sufficit Asterisk ecosystem. This library serves as the **common foundation** that prevents code duplication and ensures consistency throughout all Asterisk-related projects in the Sufficit platform.

## Features

### Core Shared Components
- **Common data models** for telephony entities (calls, channels, peers)
- **Shared interfaces** for service contracts and abstractions
- **Extension methods** for enhanced type functionality
- **Utility classes** for common operations
- **Constants and enums** for Asterisk-specific values
- **Validation helpers** for data integrity

### Telephony Shared Models
- **Call information** models and DTOs
- **Channel state** representations
- **SIP/IAX registration** models
- **Queue and agent** data structures
- **CDR (Call Detail Record)** shared models
- **Configuration** data transfer objects

### Cross-Project Integration
- **Service abstractions** used by multiple projects
- **Event models** shared between AMI and AGI components
- **Configuration interfaces** for consistent settings
- **Logging abstractions** and adapters
- **Health check** shared components
- **Metrics and monitoring** shared contracts

### Framework Support
- **Multi-target framework support** (.NET Standard 2.0, .NET 7, 8, 9)
- **Dependency injection** ready interfaces and implementations
- **ASP.NET Core integration** helpers
- **Modern async/await** patterns in shared contracts

### Performance Considerations
- **Efficient shared types** with value types for high-performance scenarios
- **Memory-efficient extensions** with Span-based parsing
- **Object pooling** for frequently used objects
- **Thread-safe operations** where applicable

## Installation
dotnet add package Sufficit.Asterisk.Shared
## Usage

For detailed usage examples and documentation, see [USAGE.md](USAGE.md).

## License

This project is licensed under the [MIT License](LICENSE).

## References and Thanks

This shared library builds upon the collective wisdom of the Asterisk .NET community. We acknowledge and thank the pioneering projects that shaped our understanding:

### Reference Projects

- **[Asterisk.NET by roblthegreat](https://github.com/roblthegreat/Asterisk.NET)** - Provided fundamental insights into common Asterisk data structures, validation patterns, and shared utilities that are essential across different integration approaches.

- **[AsterNET by AsterNET Team](https://github.com/AsterNET/AsterNET)** - Offered excellent examples of shared components, extension methods, and common patterns that promote code reusability across AMI and AGI implementations.

These projects illuminated the importance of shared components in maintaining consistency and reducing duplication across complex telephony systems. Our implementation extends these concepts with modern .NET practices and comprehensive utility functions.

**Made with ❤️ by the Sufficit Team**
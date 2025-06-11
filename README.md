<h1>
  Sufficit.Asterisk.Shared
  <a href="https://github.com/sufficit"><img src="https://avatars.githubusercontent.com/u/66928451?s=200&v=4" alt="Sufficit Logo" width="80" align="right"></a>
</h1>

[![NuGet](https://img.shields.io/nuget/v/Sufficit.Asterisk.Shared.svg)](https://www.nuget.org/packages/Sufficit.Asterisk.Shared/)

## 📖 About the Project

`Sufficit.Asterisk.Shared` is a repository containing shared code, models, and utilities used across the different modules of the Sufficit Asterisk integration. The purpose of this project is to avoid code duplication and ensure consistency throughout the ecosystem.

### ✨ What's Included?

* Common data models and DTOs (Data Transfer Objects).
* Shared interfaces and contracts.
* Utility and helper classes.
* Constants and enumerations used by multiple projects.

## 🚀 Getting Started

This project is intended to be used as a dependency for other repositories and has no standalone executable functionality.

### 📦 NuGet Package

Install the package into your project via the .NET CLI or the NuGet Package Manager Console.

**.NET CLI:**

    dotnet add package Sufficit.Asterisk.Shared

**Package Manager Console:**

    Install-Package Sufficit.Asterisk.Shared

## 🛠️ Usage

After referencing the package, you can use the shared classes and utilities in your code.

**Example:**

    using Sufficit.Asterisk.Shared.Models;

    var callDetails = new CallInfo
    {
        CallerID = "1001",
        UniqueID = "1678886400.123"
    };

    Console.WriteLine($"Processing call from {callDetails.CallerID}");

## 🤝 Contributing

Contributions are welcome! If you have improvements or fixes, please follow the standard Pull Request process.

1.  Fork the Project.
2.  Create your Feature Branch.
3.  Commit your Changes.
4.  Open a Pull Request.

## 📄 License

Distributed under the MIT License. See `LICENSE` for more information.

## 📧 Contact

Sufficit - [contato@sufficit.com.br](mailto:contato@sufficit.com.br)

Project Link: [https://github.com/sufficit/sufficit-asterisk-shared](https://github.com/sufficit/sufficit-asterisk-shared)
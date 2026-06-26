#  Archneter Architecture CLI

![Archneter Logo](https://raw.githubusercontent.com/Eng-ahmed-dev1/Archneter-Library/main/Archneter.Cli/icon.png)

> **Empower your engineering teams with instant, production-ready .NET architectures.**

![LinkedIn Intro](https://raw.githubusercontent.com/Eng-ahmed-dev1/Archneter-Library/main/linkedin_photo.png)

**Archneter** is an enterprise-grade, extensible command-line interface (CLI) engineered to accelerate .NET application development. Built upon Microsoft's best practices and `Microsoft.Extensions.DependencyInjection`, Archneter eliminates manual boilerplate setup by automating the scaffolding of highly cohesive, scalable software architectures. 

Whether you are building a lightweight API or a complex distributed system, Archneter configures your project folders, layers, solutions, and cross-project references in seconds—guaranteeing a standardized foundation every time.

---

## Project Structure

The codebase is organized into three primary projects grouped under a single .NET solution (`archneter.slnx`):

```text
Archneter/
├── Archneter.Core/          # Domain abstractions, models, and shared enums
├── Archneter.Generators/    # Architecture-specific code generation logic
└── Archneter.Cli/           # Command-Line Interface and argument dispatching
```

---

## Detailed Directory & File Breakdown

### 1. [Archneter.Core](./Archneter.Core)
This project defines the core abstractions, shared models, and configurations used across the solution. It does not depend on any other projects.

*   **`Abstractions/`**
    *   **`IArchitectureGenerator.cs`**: Defines the primary interface for all architecture generators. It exposes `Task GenerateAsync(ProjectOptions options)`.
*   **`Enums/`**
    *   **`ArchitectureType.cs`**: An enum representing the supported architecture styles:
        *   `CleanArchitecture` (1)
        *   `VerticalSlice` (2)
        *   `ModularMonolith` (3)
        *   `Microservices` (4)
        *   `NTier` (5)
*   **`Models/`**
    *   **`ProjectOptions.cs`**: Data class holding configuration parameters specified by the user (e.g., `ProjectName`, `ArchitectureType`, `GenerateTests`, and `ServiceNames`).

---

### 2. [Archneter.Generators](./Archneter.Generators)
This project implements the architecture generators. It depends on `Archneter.Core` and uses the underlying system's `dotnet` CLI to perform project bootstrapping.

*   **`CleanArchitecture/`**
    *   **`CleanArchitectureGenerator.cs`**: Scaffolds a 4-layer architecture (`.Domain`, `.Application`, `.Infrastructure`, `.Api`).
*   **`Microservices/`**
    *   **`MicroservicesArchitectureGenerator.cs`**: Scaffolds an API Gateway, Shared Contracts, and multiple microservices. Each microservice gets its own internal Clean Architecture layers.
*   **`ModularMonolith/`**
    *   **`ModularMonolithArchitectureGenerator.cs`**: Scaffolds a single Host API, a Shared project, and separates features into independent module class libraries.
*   **`VerticalSlice/`**
    *   **`VerticalSliceArchitectureGenerator.cs`**: Scaffolds a highly cohesive single Web API project with feature-sliced folders (`Commands`, `Queries`, `DTOs`, `Endpoints`).
*   **`N-Tier/`**
    *   **`NTierArchitectureGenerator.cs`**: Scaffolds a traditional 3-tier architecture (`.DAL`, `.BLL`, `.PL`).
*   **`Infrastructure/`**
    *   **`DotnetCliService.cs`**: Runs actual `dotnet` CLI commands (`new`, `sln`, `add reference`).
    *   **`DryRunCliService.cs`**: Simulates the CLI execution for the `--dry-run` flag, printing the execution plan without modifying the disk.

---

### 3. [Archneter.Cli](./Archneter.Cli)
The entry point of the CLI application. It handles parsing command-line parameters, matching them to commands, and executing actions.

*   **`Commands/`**
    *   **`NewCommand.cs`**: Implements the `new` command. Parses the project name, selected architecture flags, and feature/module flags. Contains an interactive wizard fallback.
    *   **`HelpCommand.cs`**: Displays dynamic and formatted usage instructions.
*   **`Parsing/`**
    *   **`ArgumentParser.cs`**: Maps console arguments into a structured `CommandContext`.
*   **`Services/`**
    *   **`CommandRegistry.cs`**: Discovers and manages CLI commands dynamically.
    *   **`GeneratorFactory.cs`**: Resolves the appropriate implementation of `IArchitectureGenerator` based on the specified architecture type and dry-run state.

---

## How to Get Started

### Prerequisites
*   [.NET SDK](https://dotnet.microsoft.com/download)

### Build the Tool
To build the solution, run:
```bash
dotnet build
```

### Install the Tool (Global)
Archneter is packaged as a .NET Global Tool. You can install it globally on your machine by running:
```bash
dotnet tool install -g Archneter
```

### Troubleshooting: `archneter: command not found`
The .NET SDK usually adds the global tools directory to your system's `PATH` automatically. However, if your terminal doesn't recognize the `archneter` command after installation, you must manually add the `.dotnet/tools` directory to your `PATH`:

#### Windows
1. Open Command Prompt and run:
   ```cmd
   setx PATH "%PATH%;%USERPROFILE%\.dotnet\tools"
   ```
2. Restart your Command Prompt or PowerShell.

#### Linux & macOS
1. Open your terminal and append the export command to your shell profile (e.g., `~/.bashrc`, `~/.zshrc`, or `~/.profile`):
   ```bash
   echo 'export PATH="$PATH:$HOME/.dotnet/tools"' >> ~/.bashrc
   ```
2. Apply the changes immediately:
   ```bash
   source ~/.bashrc  # (or source ~/.zshrc)
   ```

### Run from Source (Development)
If you are developing or testing locally without installing:
```bash
dotnet run --project Archneter.Cli/Archneter.Cli.csproj -- [command] [options]
```

---

## Usage Guide

### Display Help
To see all commands and configurations, run the `help` command:
```bash
archneter help
```

### Create a New Project
Use the `new` command to generate a template solution.

```bash
archneter new <ProjectName> --arch <type> [options]
```

#### Options:
*   `--arch <type>`: Specifies the architecture template. Supported values:
    *   `clean` (Clean Architecture - Default)
    *   `microservices` (Microservices)
    *   `modularmonolith` (Modular Monolith)
    *   `verticalslice` (Vertical Slice)
    *   `n-tier` (N-Tier)
*   `--services <names>`: Comma-separated service names *(microservices only)*
*   `--modules <names>`: Comma-separated module names *(modular monolith only)*
*   `--features <names>`: Comma-separated feature names *(vertical slice only)*
*   `--tests <true|false>`: Scaffolds accompanying unit and integration test projects. (Default: `false`)
*   `--dry-run`: Previews the generated structure in the terminal without creating any files on disk.

> **Note:** If you forget to provide the `--services`, `--modules`, or `--features` flags for their respective architectures, Archneter will trigger a smart **interactive wizard** asking you for the count and names dynamically!

#### Examples:

1.  **Generate a standard Clean Architecture solution:**
    ```bash
    archneter new CleanApp --arch clean --tests true
    ```

2.  **Generate a traditional N-Tier architecture:**
    ```bash
    archneter new LegacyApp --arch n-tier --tests true
    ```

3.  **Generate a Microservices architecture with specific services:**
    ```bash
    archneter new DistributedApp --arch microservices --services Order,Product,Identity --tests true
    ```

4.  **Generate a Modular Monolith with specific modules:**
    ```bash
    archneter new MonolithApp --arch modularmonolith --modules Sales,Catalog --tests true
    ```

5.  **Generate a Vertical Slice architecture with specific features:**
    ```bash
    archneter new SliceApp --arch verticalslice --features Orders,Cart --tests true
    ```

6.  **Preview execution using Dry-Run:**
    ```bash
    archneter new FullDemoApp --arch n-tier --tests true --dry-run
    ```

### Refactor an Existing Project (v1.2.0+)
Use the `refactor` command to intelligently analyze and transform an existing legacy codebase (e.g. monolithic or unstructured code) into a well-defined target architecture. Archneter handles creating the boundary projects, moving files, recalculating namespaces, rewiring cross-project dependencies, and even migrating NuGet package references (`<PackageReference>`).

```bash
archneter refactor --to <architecture> [--dir <path>] [options]
```

#### Options:
*   `--to <architecture>`: Specifies the target architecture template. Supported values: `clean`, `microservices`, `modularmonolith`, `verticalslice`, `n-tier`.
*   `--dir <path>`: Root directory of the project to refactor (default: current directory).
*   `--deep-refactor`: **Optional.** Enables a Roslyn-based deep refactoring mode. Archneter will safely extract concrete direct instantiations (e.g., `new Repository()`), generate Interfaces (`IRepository`), and rewire the dependencies into Constructor Injections.
*   `--skip-backup`: Skips the automatic backup step before refactoring.
*   `--force`: Skips the confirmation prompt.
*   `--dry-run`: Previews what would happen without touching the disk.

#### Examples:

1.  **Refactor an unstructured API into Clean Architecture:**
    ```bash
    archneter refactor --to clean
    ```

2.  **Refactor with Deep Dependency Extraction (Roslyn DI & Interface Generation):**
    ```bash
    archneter refactor --to clean --deep-refactor
    ```

3.  **Refactor an N-Tier monolithic application into Microservices without prompting:**
    ```bash
    archneter refactor --to microservices --dir ./LegacySystem --force
    ```

> **Note on NuGet Package Propagation:** During refactoring, Archneter analyzes your source `.csproj` files. If you move a file that depends on external packages (like `Dapper` or `Newtonsoft.Json`), Archneter ensures that those `<PackageReference>` tags are seamlessly copied to the new `.csproj` destination, guaranteeing that your projects compile with all required dependencies!

---

## What's New in v1.2.1
This patch release brings critical stability improvements for the `refactor` command when processing complex, real-world repositories (like CQRS architectures):
- **Intelligent Module/Service Inference:** Enhanced the heuristics in `Microservices` and `Modular Monolith` refactoring to correctly parse feature-based directories (e.g. `UseCases/`, `Features/`) and recursively strip CQRS verbs/suffixes. This prevents the tool from generating excessive projects for each individual command/query.
- **Resilient File Operations:** Implemented a robust retry mechanism to handle transient `.NET` background locks (e.g., MSBuild/OmniSharp locking files during scaffold) that previously caused `IOException` crashes.
- **Solution File Discovery:** Improved project name inference to prioritize existing `.sln` and `.slnx` files, preventing `dotnet sln add` failures when the root directory name differs from the actual solution name.

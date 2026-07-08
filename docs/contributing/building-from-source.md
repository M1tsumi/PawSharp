# Building from Source

How to build, test, and develop PawSharp locally.

---

## Prerequisites

- [.NET 10.0 SDK](https://dotnet.microsoft.com/download/dotnet/10.0) or later
- [Git](https://git-scm.com/)
- An IDE: [Visual Studio 2022](https://visualstudio.microsoft.com/), [Rider](https://www.jetbrains.com/rider/), or [VS Code](https://code.visualstudio.com/)

Verify your SDK:

```bash
dotnet --version
# Must be 10.0.x or higher
```

---

## Cloning

```bash
git clone https://github.com/M1tsumi/PawSharp.git
cd PawSharp
```

---

## Restoring packages

```bash
dotnet restore PawSharp.sln
```

This restores all NuGet packages using the central package management defined in `Directory.Packages.props`.

---

## Building

**Debug (default):**

```bash
dotnet build PawSharp.sln
```

**Release:**

```bash
dotnet build PawSharp.sln -c Release --nologo
```

**Specific project:**

```bash
dotnet build src/PawSharp.Core/PawSharp.Core.csproj -c Release
```

### Common build flags

| Flag | Purpose |
|---|---|
| `--no-restore` | Skip package restore (faster after first build) |
| `--nologo` | Suppress Microsoft branding banner |
| `-o ./output` | Output to custom directory |

---

## Running Tests

**All tests:**

```bash
dotnet test PawSharp.sln
```

**Specific project:**

```bash
dotnet test tests/PawSharp.Core.Tests/PawSharp.Core.Tests.csproj
```

**With verbose output:**

```bash
dotnet test PawSharp.sln -v d
```

**Filter by category:**

```bash
dotnet test --filter "Category=Unit"
dotnet test --filter "Category=Integration"
```

See [running-tests.md](running-tests.md) for details.

---

## Opening in an IDE

### Visual Studio 2022

Open `PawSharp.sln`. Select a startup project (e.g. one of the examples) or run tests via **Test Explorer**.

### Rider

Open `PawSharp.sln`. Rider detects test projects automatically and shows them in the **Unit Tests** window.

### VS Code

```bash
code .
```

Install the [C# Dev Kit](https://marketplace.visualstudio.com/items?itemName=ms-dotnettools.csdevkit) extension. It will detect the solution and configure intellisense, build, and test integration.

---

## Common Build Issues

### "SDK not found" or "NETSDK1045"

Your .NET SDK is too old. Install the [.NET 10.0 SDK](https://dotnet.microsoft.com/download/dotnet/10.0).

```
error : The current .NET SDK does not support targeting .NET 10.0.
```

### "The framework 'Microsoft.NETCore.App' version '10.0.0' was not found"

Install the .NET 10.0 runtime/SDK.

### Package restore fails on Windows

PowerShell execution policy may block scripts. Run:

```powershell
Set-ExecutionPolicy -Scope CurrentUser RemoteSigned
```

### Out of memory during build

The solution is large. Build with:

```bash
dotnet build PawSharp.sln -m:1
```

### DocFX errors during build

DocFX is not required for library development. The CI workflow handles documentation. You can skip DocFX by building only the library projects:

```bash
for /R src %f in (*.csproj) do dotnet build "%f"
```

### `check-release-hygiene.ps1` fails in CI

This is expected for PR branches. The script validates version alignment and runs on `main` pushes. You can skip it locally with:

```bash
dotnet build PawSharp.sln --no-restore --nologo
```

---

## Packaging

To create NuGet packages locally:

```bash
for /R src %f in (*.csproj) do dotnet pack "%f" -c Release --output ./nupkgs
```

Or pack all at once via the CI workflow's pack step (see `.github/workflows/ci.yml`).

Packages are output to `./nupkgs/` and include debug symbols (`.snupkg`).

---

## Building Documentation

> DocFX is required for documentation builds.

Install the DocFX tool:

```bash
dotnet tool restore
```

Build metadata and site:

```bash
dotnet docfx metadata
dotnet docfx build
```

The output goes to `./_site/`.

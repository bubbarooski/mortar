# mortar - a file linking system

**mortar** links your source files to their documentation — datasheets, schematics, API specs, requirements docs — and surfaces them directly inside Visual Studio.

No more hunting through file explorers or bookmarks. Open a file, see its docs.

---

## What it does

mortar adds a tool window to Visual Studio that shows every document linked to your source files. Links are stored in a `doclinks.mor` file at your solution root, which means your whole team shares them automatically through Git.

<img width="377" height="495" alt="Screenshot 2026-07-09 214446" src="https://github.com/user-attachments/assets/a73c4dc1-011f-433b-b2de-fd7d11c73ce9" />

---

## Getting started

### Visual Studio Extension

1. Install the mortar extension from the Visual Studio Marketplace *(coming soon)*
2. Open any solution in Visual Studio
3. Go to **View > Other Windows > mortar** to open the tool window
4. Right-click any source file in the tree to add a link, or use **+ Track File** to start tracking a new file

### CLI (optional)

The CLI lets you manage links from the terminal, useful for scripting or if you prefer the command line.

**Installation:**
1. Download `mortar-cli.exe` from the [latest release](https://github.com/bubbarooski/mortar/releases/latest)
2. Copy it to a folder in your PATH
3. Run from your solution root

```
mortar-cli link <sourceFile> <documentPath>
mortar-cli link <sourceFile> --url <url>
mortar-cli status
mortar-cli info <sourceFile>
```

See [CLI documentation](#cli-reference) below for full usage.

---

## Features

### Visual Studio Extension
- **Folder tree view** — source files are grouped by directory, mirroring your project structure
- **Inline editing** — edit links directly in the tool window without leaving the IDE
- **Add links from the GUI** — right-click any tracked file to add a new document link, or use **+ Track File** to track a new file
- **Doc type labels** — tag links as Datasheet, Schematic, API Spec, Requirements, and more
- **Sync status** — color-coded indicators show when a linked document is newer than the source file
- **Primary reference** — mark one link as the primary reference per file
- **Notes** — attach short notes to any link, visible inline in the tree
- **Git warning** — banner alert when `doclinks.mor` has uncommitted changes
- **Theme-aware** — respects Visual Studio's light and dark themes

### CLI
- Full CRUD for links — add, remove, rename, view
- Interactive mode — run any command without arguments to be prompted
- Silent mode — pass all arguments for scripting and CI pipelines
- Git integration — stage and commit `doclinks.mor` in one command

### CLI Installation

1. Download `mortar-cli.exe` from the [latest release](https://github.com/bubbarooski/mortar/releases)
2. Copy it to a folder in your PATH, or add its location to your PATH
3. Run `mortar-cli` from your solution root

---

## How links are stored

Links are stored in a `doclinks.mor` file at your solution root:

```json
[
  {
    "sourceFile": "src/sensors/imu.c",
    "linkedAt": "2025-01-15T10:30:00Z",
    "documentPaths": [
      {
        "path": "C:/docs/ICM-42688-P_Datasheet.pdf",
        "nickname": "IMU Datasheet",
        "docType": "datasheet",
        "isPrimary": true,
        "outOfDateDetection": true
      }
    ]
  }
]
```

Source file paths are stored relative to the solution root so links work across machines. Commit `doclinks.mor` to share links with your team.

---

## CLI reference

```
mortar-cli link <sourceFile> [documentPath] [options]
mortar-cli unlink <sourceFile> [documentPath | --name <nickname> | --all]
mortar-cli rename <sourceFile> [documentPath | --name <oldNickname>] <newNickname>
mortar-cli status [--type <docType>]
mortar-cli info [<sourceFile>]
mortar-cli git init
mortar-cli git status
```

### Options for `link`

| Flag | Description |
|------|-------------|
| `--url <url>` | Link a web URL instead of a local file |
| `--name <nickname>` | Give the link a short display name |
| `--type <docType>` | Categorize the document |
| `--notes <text>` | Add a short note about this link |
| `--primary` | Mark as the primary reference for this file |
| `--no-sync` | Disable out-of-date detection for this link |

### Doc types

`datasheet` `requirements` `schematic` `testSpec` `apiSpec` `researchPaper` `designSpec` `runbook` `license` `changelog` `other`

---

## Project structure

```
mortar/
  mortar/                    Visual Studio VSIX extension
  mortar-cli/                .NET CLI tool
    mortar-cli-tests/        xUnit test suite
```

---

## Building from source

### Extension (mortar)
1. Open `mortar.sln` in Visual Studio
2. Build > Rebuild Solution
3. Hit F5 to launch the experimental instance

### CLI (mortar-cli)
```
cd mortar-cli
dotnet build
dotnet run -- link myfile.c docs/datasheet.pdf
```

### Tests
```
cd mortar-cli/mortar-cli-tests
dotnet test
```

---

## License

MIT — see [LICENSE](LICENSE) for details.

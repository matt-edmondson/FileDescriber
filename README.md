# FileDescriber

A .NET 10 CLI that uses a local Ollama model to generate descriptions and suggested filenames for many types of files — including images, text, and more — in bulk.

## What it does

Recursively scans a directory for supported files, computes a content hash for each file, and asks a local Ollama instance to summarize or caption each unique file. Descriptions, suggested filenames, and metadata are persisted in a JSON database keyed by the content hash, so identical files at different paths share one record and re-scanning skips already-described content.

No cloud APIs are called — all inference runs locally against Ollama.

### Supported file types

| Type | Extensions |
|---|---|
| **Image** | `.jpg`, `.jpeg`, `.png`, `.gif`, `.bmp`, `.webp`, `.tiff`, `.tif` |
| **Text** | `.txt`, `.md`, `.json`, `.ini`, `.html`, `.htm`, `.yaml`, `.yml`, `.xml`, `.csv`, `.toml`, `.log` |

Support for audio, video, and document files is planned — see the open issues.

## Prerequisites

- [Ollama](https://ollama.com) running locally or on the network (defaults to `http://localhost:11434`).
- A model installed in Ollama (default `llama3.2-vision` for images; any capable model works for text):
  ```bash
  ollama pull llama3.2-vision
  ollama serve
  ```
- .NET 10 SDK.

## Installation

```bash
git clone <repo>
cd FileDescriber
dotnet build
```

## Usage

Without arguments the tool opens an interactive menu. All verbs can also be invoked directly.

```bash
# Interactive menu
FileDescriber

# Scan a directory (images and text files)
FileDescriber Scan -p "C:\documents"

# Scan with a custom model and remote endpoint
FileDescriber Scan -p "C:\documents" -m llava -e http://192.168.1.100:11434

# Search stored descriptions
FileDescriber Search -q "meeting notes"

# Export / import the database
FileDescriber Export -o descriptions.csv      # or .json
FileDescriber Import -i backup.json            # or .csv

# Print database statistics
FileDescriber Stats
```

### Verbs

| Verb | Purpose |
|---|---|
| `Menu` *(default)* | Interactive console menu. |
| `Scan` | Hash files in a directory and describe each unique one. |
| `Search` | Keyword search across stored descriptions and paths. |
| `Configure` | Edit endpoint, model, concurrency, and prompt templates. |
| `Export` | Dump the database to JSON or CSV. |
| `Import` | Merge a JSON or CSV export back into the database. |
| `Stats` | Print database statistics — total descriptions, file type breakdown, total file size, models used, date range, duplicate count, and average description length. |

### Common options

| Option | Long form | Effect |
|---|---|---|
| `-p` | `--path` | Directory to scan (`Scan`) or default path. |
| `-e` | `--endpoint` | Ollama URL. Defaults to `http://localhost:11434`. |
| `-m` | `--model` | Model name. Defaults to `llama3.2-vision`. |
| `-q` | `--query` | Search query (`Search`). |
| `-o` | `--output` | Export file path. The extension picks the format. |
| `-i` | `--input` | Import file path. |

## Storage

Settings and the description database are stored via `ktsu.AppDataStorage` (typically `%APPDATA%\ktsu\FileDescriber` on Windows).

## License

MIT — see `LICENSE.md`.

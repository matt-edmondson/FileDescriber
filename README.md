# FileDescriber

A .NET 10 CLI that uses a local or remote AI model to generate descriptions and suggested filenames for many types of files — including images, text, and more — in bulk.

## What it does

Recursively scans a directory for supported files, computes a content hash for each file, and asks an AI model to summarize or caption each unique file. Descriptions, suggested filenames, and metadata are persisted in a JSON database keyed by the content hash, so identical files at different paths share one record and re-scanning skips already-described content.

Two AI backends are supported:

| Backend | Description | Default endpoint |
|---|---|---|
| **Ollama** *(default)* | Local Ollama server using `/api/generate`. | `https://ollama.local.ktsu.dev` |
| **OpenAI-compatible** | Any server that speaks the OpenAI `/v1/chat/completions` API, including **standard OpenAI** and **LocalAI** (localai.io). | `http://localhost:8080` (LocalAI) / `https://api.openai.com` (OpenAI) |

### Supported file types

| Type | Extensions |
|---|---|
| **Image** | `.jpg`, `.jpeg`, `.png`, `.gif`, `.bmp`, `.webp`, `.tiff`, `.tif` |
| **Text** | `.txt`, `.md`, `.json`, `.ini`, `.html`, `.htm`, `.yaml`, `.yml`, `.xml`, `.csv`, `.toml`, `.log` |

Support for audio, video, and document files is planned — see the open issues.

## Prerequisites

- **For Ollama**: [Ollama](https://ollama.com) running locally or on the network (defaults to `https://ollama.local.ktsu.dev`).
  ```bash
  ollama pull gemma3:27b
  ollama serve
  ```
- **For LocalAI**: [LocalAI](https://localai.io) running locally or on the network (defaults to `http://localhost:8080`).
- **For OpenAI**: A valid OpenAI API key.
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

# Scan a directory (images and text files) — Ollama backend
FileDescriber Scan -p "C:\documents"

# Scan with a custom model and remote Ollama endpoint
FileDescriber Scan -p "C:\documents" -m llava -e http://192.168.1.100:11434

# Switch to LocalAI backend (OpenAI-compatible)
FileDescriber Configure
# → select backend [2] OpenAI-compatible
# → enter endpoint: http://localhost:8080
# → API key: (leave blank for LocalAI without auth)

# Switch to standard OpenAI backend
FileDescriber Configure
# → select backend [2] OpenAI-compatible
# → enter endpoint: https://api.openai.com
# → API key: sk-...

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
| `Configure` | Edit backend type, API key, endpoint, model, concurrency, and prompt templates. |
| `Export` | Dump the database to JSON or CSV. |
| `Import` | Merge a JSON or CSV export back into the database. |
| `Stats` | Print database statistics — total descriptions, file type breakdown, total file size, models used, date range, duplicate count, and average description length. |

### Common options

| Option | Long form | Effect |
|---|---|---|
| `-p` | `--path` | Directory to scan (`Scan`) or default path. |
| `-e` | `--endpoint` | AI server URL. |
| `-m` | `--model` | Model name. Defaults to `gemma3:27b`. |
| `-q` | `--query` | Search query (`Search`). |
| `-o` | `--output` | Export file path. The extension picks the format. |
| `-i` | `--input` | Import file path. |

## Backend configuration

The backend is configured via `Configure` (interactive) or by editing the settings file directly.

### Ollama (default)

Uses Ollama's native `/api/generate` endpoint. No API key is required.

### OpenAI-compatible (LocalAI / standard OpenAI)

Uses the standard `/v1/chat/completions` endpoint. Set the backend to `OpenAi` in the `Configure` verb:

- **LocalAI**: Set endpoint to `http://localhost:8080` (or wherever LocalAI listens). Leave the API key blank unless your deployment requires one.
- **Standard OpenAI**: Set endpoint to `https://api.openai.com` and provide your `sk-...` API key.

The API key is stored in the settings file and sent as `Authorization: Bearer <key>` on every request.

## Storage

Settings and the description database are stored via `ktsu.AppDataStorage` (typically `%APPDATA%\ktsu\FileDescriber` on Windows).

## License

MIT — see `LICENSE.md`.

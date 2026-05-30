# 🎵 MusicDuplicateFilter

A WPF (.NET 10) desktop tool that scans your music library, detects duplicate songs, and helps you reclaim storage space safely.

[![.NET](https://img.shields.io/badge/.NET-10.0-blue)](https://dotnet.microsoft.com/)
[![WPF](https://img.shields.io/badge/WPF-MVVM-purple)](https://learn.microsoft.com/dotnet/desktop/wpf/)
[![License](https://img.shields.io/badge/license-MIT-green)](LICENSE)

🌐 [中文](README.md)

## ✨ Features

- **📂 Directory Scanning** — Recursively scans selected folders for music files; supports MP3, FLAC, WAV, OGG, WMA, AAC, M4A and more
- **🔍 Smart Duplicate Detection** — Fuzzy matching based on filename, title, artist, and album metadata using Levenshtein distance
- **📊 Similarity Scoring** — 0–100% weighted score per duplicate group; fully configurable thresholds
- **🗑️ Safe Deletion** — Files are moved to the Recycle Bin (recoverable); a detailed confirmation dialog lists every file before deletion
- **👁️ Preview** — Side-by-side preview of the kept file vs. each duplicate, with play and open-in-explorer buttons
- **📤 JSON Export** — Export the full duplicate report to a timestamped JSON file (`duplicate<yyyyMMddHHmmss>.json`)
- **🌐 Multilingual UI** — Chinese and English; follows system language by default, switchable in Settings
- **📝 Operation Log** — Scan and deletion history written to `logs/crash.log` next to the executable
- **⚙️ Flexible Settings** — Customisable scan directories, file extensions, similarity weights, duration tolerance, keep-file strategy, and more

## 🚀 Quick Start

### Requirements

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- Windows 10 or Windows 11

### Build & Run

```bash
# Clone the repository
git clone <repository-url>
cd MusicDuplicateFilter

# Build
dotnet build

# Run the app
dotnet run --project src/MusicDuplicateFilter

# Run unit tests
dotnet test
```

### Download Release

Download the latest self-contained executable from the [Releases](../../releases) page — no .NET SDK required on the target machine.

## 🖥️ How to Use

1. **Add directories** — Click "Add Directory" (or type a path and press Enter). Multiple folders are supported.
2. **Start scan** — Click "Start Scan". Progress is shown in the status bar at the bottom.
3. **Review results** — Duplicate groups appear on the right, each showing similarity score and the suggested file to keep.
4. **Preview details** — Click "Preview" on any group for a side-by-side comparison with play / open-in-explorer actions.
5. **Select files** — Tick the files to delete (non-keep files are pre-selected), or use "Select All".
6. **Export report** — Click "Export JSON" to save the duplicate list for later reference.
7. **Delete** — Click "Delete Selected Files". A scrollable confirmation window lists every file. Confirmed files go to the Recycle Bin.

## 🏗️ Project Structure

```
MusicDuplicateFilter/
├── MusicDuplicateFilter.slnx
├── src/
│   └── MusicDuplicateFilter/          # WPF main project
│       ├── Models/                    # Data models
│       ├── Services/                  # Business logic & interfaces
│       ├── ViewModels/                # MVVM ViewModels
│       ├── Views/                     # XAML windows
│       ├── Helpers/                   # StringSimilarity, MetadataReader
│       ├── Converters/                # WPF value converters
│       └── Resources/                 # Localisation JSON (zh-CN / en-US)
└── tests/
    └── MusicDuplicateFilter.Tests/    # xUnit unit tests
```

## 🔧 Tech Stack

| Technology | Purpose |
|------------|---------|
| .NET 10 | Runtime |
| WPF | Desktop UI |
| CommunityToolkit.Mvvm | MVVM helpers & source generation |
| Panuon.WPF.UI | UI component library |
| TagLibSharp | Music metadata reading |
| Microsoft.Extensions.DependencyInjection | Dependency injection |
| xUnit | Unit testing |

## 📝 How Duplicate Detection Works

- **Filename matching** — Levenshtein distance on the cleaned filename (noise tags like `[320 kbps]` and `(Official Video)` are stripped first)
- **Metadata matching** — ID3/Vorbis tags: title, artist, album
- **Duration matching** — Configurable tolerance in seconds
- **File size** — Optional byte-level comparison with configurable tolerance
- **Weighted score** — Each dimension carries an adjustable weight; the final 0–100% score determines whether files are grouped as duplicates

## 📄 License

This project is licensed under the [MIT License](LICENSE).

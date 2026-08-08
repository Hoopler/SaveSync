# SaveSync

A cross-platform CLI tool for backing up and syncing game save folders through Google Drive.

Point it at a game's save folder, upload it, and pull it back down on another machine whenever you need it. Subfolders are included, and every download automatically backs up your existing save (as `<folder>.bak`) before replacing it.

## Why

Manually zipping save folders back and forth gets old fast. This handles that.

## Features

- Tracks multiple games and their save folder locations
- Uploads/downloads entire save folders, including subfolders
- Automatically backs up the local save before a download overwrites it
- Works on Windows and Linux
- Syncs through your own Google Drive — no third-party server involved
- The app can only see files and folders it created itself, not the rest of your Drive

## Getting started

You'll need the [.NET SDK](https://dotnet.microsoft.com/download) if building from source!!!

```bash
git clone https://github.com/Hoopler/SaveSync.git
cd SaveSync
dotnet build
```

Or grab a prebuilt binary from the [Releases](../../releases) page.

On first use of `upload` or `download`, a browser window opens asking you to sign into Google and approve access. This only happens once per machine — after that, your session is cached locally.

## Usage

```bash
# add a game
SaveSync add

# another way of adding a game
SaveSync add "Elden Ring" "C:\Users\me\AppData\Roaming\EldenRing\saves"

# list tracked games
SaveSync list

# upload a save to Drive
SaveSync upload "Elden Ring"

# download a save from Drive (backs up the local folder first)
SaveSync download "Elden Ring"

# remove a tracked game
SaveSync remove "Elden Ring"
```

**Typical workflow:** add the game first and upload the files after a play session on your main machine. On another machine, add the SAME game name pointing at its local save folder, then download to pull the latest save. If a download isn't what you expected, your previous save is preserved in `<folder>.bak`.

## How it works

Save folder contents are uploaded into `SaveSync/<game-name>/` in your Google Drive, preserving the local subfolder structure. Since the app only has access to files and folders it creates, it can't see or touch anything else in your Drive. Tracked games and their paths are stored locally in a small JSON file under your OS's application data folder.

## Building for distribution

```bash
# Windows
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true

# Linux
dotnet publish -c Release -r linux-x64 --self-contained true -p:PublishSingleFile=true
```

Self-contained builds run without requiring .NET installed on the target machine.

## License

[MIT](LICENSE)
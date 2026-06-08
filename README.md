# [<img src="https://raw.githubusercontent.com/EntexInteractive/Parallel/master/Parallel.Cli/parallel-red.ico" alt="Parallel Icon" width="38" height="38">](https://github.com/EntexInteractive/Parallel) Parallel

[![.NET](https://img.shields.io/github/actions/workflow/status/EntexInteractive/Parallel/dotnet-build.yml?label=Main%20build&style=for-the-badge)](https://github.com/EntexInteractive/Parallel/actions/workflows/dotnet-build.yml) [![latest version](https://img.shields.io/github/v/release/EntexInteractive/Parallel?label=Latest%20release&style=for-the-badge)](https://github.com/EntexInteractive/Parallel/releases/latest) [![GitHub Downloads](https://img.shields.io/github/downloads/EntexInteractive/Parallel/total?style=for-the-badge)](https://github.com/EntexInteractive/Parallel/releases/latest)

Your files under your control.

## What is Parallel?

Parallel is a modular, cross-platform file backup and synchronization tool built for people who want full control of their files. It ditches the cloud-first assumptions and gives you full transparency and control over how, when, and where your files move. No contracts, no vendor lock-ins, no silent overwrites. Just clean, dependable syncing on your terms.

Parallel was originally built to handle **terabytes of data** because Dropbox simply couldn’t. When commercial cloud services reach their limits, Parallel stepped in to offer **unbounded scale**, **local-first logic**, and **configurable workflows** that respect your storage, bandwidth, and rules.

## Why Parallel?

Parallel is completely free and open source. You provide the storage, and Parallel handles the sync. Whether it’s an external drive, a NAS, a remote SSH server, or an S3-compatible cloud like [Storj](https://www.storj.io/) or [Wasabi](https://wasabi.com/), Parallel adapts to what you own.

Your computer already gives you enough to fight with, your files don't have to be one of them. Parallel keeps backups simple, transparent, and under your control with no cloud drama.

| Feature                       | **Parallel** | **Dropbox** | **OneDrive** | **iCloud** | **File History (Windows)** |
|-------------------------------|--------------|-------------|--------------|------------|-----------------------------|
| **Open Source**               | ✅ Yes       | ❌ No        | ❌ No         | ❌ No       | ❌ No
| **Local-first**               | ✅ Always    | ❌ Cloud-first | ❌ Cloud-first | ⚠️ Hybrid (Apple ecosystem) | ✅ Yes
| **Modular storage options**   | ✅ Any (NAS, SSH, S3) | ❌ Vendor-locked | ❌ Vendor-locked | ❌ Vendor-locked | ❌ Local only
| **Compression**               | ✅ Always    | ❌ No        | ❌ No         | ❌ No       | ❌ No
| **Cross-platform**            | ✅ Yes       | ✅ Yes       | ✅ Yes        | ⚠️ Apple-centric | ❌ Windows only
| **Version History**           | ✅ Yes       | ✅ Yes       | ✅ Yes        | ✅ Yes | ⚠️ Limited
| **System Snapshots**          | ✅ Yes       | ❌ No       | ❌ No        | ❌ No | ⚠️ Limited
| **Offline access**            | ✅ Full      | ⚠️ Partial   | ⚠️ Partial    | ⚠️ Partial | ✅ Full
| **Free to use**               | ✅ Always    | ⚠️ 2GB free | ⚠️ 5GB free   | ⚠️ 5GB free | ✅ Yes
| **Max storage**               | ✅ Unlimited (your hardware)  | ⚠️ 2GB (free), 3TB (personal), 15TB (enterprise) | ⚠️ 5TB (personal), 25TB (enterprise) | ⚠️ 5GB–12TB (paid tiers) | ⚠️ Limited by drive size


## 📦 Quick Start Guide
#### 1. Install Parallel
Download the latest [release](https://github.com/EntexInteractive/Parallel/releases/latest) or build from source:
```
git clone https://github.com/EntexInteractive/Parallel
cd Parallel
dotnet build
```

On Linux systems, you can install via:
```
curl -sSL https://raw.githubusercontent.com/EntexInteractive/Parallel/main/install.sh | sudo bash
```

#### 2. Set up your vaults
Vaults are storage targets where Parallel sends and receives files. This can be an external drive, NAS share, SSH server, or S3-compatible cloud.
```
parallel vaults add
```
All vaults are saved as JSON in `%AppData%\Parallel\Configuration.json` for easier importing and exporting. Learn more [here](https://github.com/EntexInteractive/Parallel/wiki/Configuration#configuration-file).
#### 3. Back up files to vaults
Parallel can back up all changed files on the system with:
```
parallel sync
```
You can also specify a path, which can be a file, folder, or a specific vault configuration.
```
parallel sync --path "C:\Windows\System32"
parallel sync -p "C:\Windows\System32\cmd.exe"
parallel sync --config 1a2b3c4d
```
#### 4. Restore files from a vault
Parallel can restore files from a vault with:
```
parallel restore --path "C:\Windows\System32"
parallel restore -p "C:\Windows\System32\cmd.exe"
```
Parallel keeps revisions of files. To restore files to a previous version and not the latest, you can use the `--before` option and provide a valid timestamp string. See more about DateTime [parsing](https://learn.microsoft.com/en-us/dotnet/api/system.datetime.parse?view=net-10.0#StringToParse).
```
parallel restore --path "C:\Windows\System32" --before "2025-12-16 5:11 PM"
parallel restore -p "C:\Windows\System32\cmd.exe" --before "12/16/25"
```

## 🧪 Status

Parallel is currently in early development. Expect rapid iteration, breaking changes, and lots of modular experimentation. Contributions, feedback, and testing are welcome!

## 💬 Contact

For questions and ideas, reach out via our [GitHub Issues](https://github.com/EntexInteractive/Parallel/issues).

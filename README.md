# Unity My Assets Exporter

[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](LICENSE)
[![Unity Version](https://img.shields.io/badge/Unity-2021%20--%206000%2B-black?logo=unity)](https://unity.com/)

A lightweight, non-intrusive Unity Editor tool that scans and exports all your entitled Asset Store packages from **Package Manager > My Assets** to structured **CSV** and **JSON** files with a single click.

---

## ✨ Features

- 📦 **Complete Metadata Extraction**:
  - **Title & Package ID**
  - **Version & Download Status**
  - **Asset Store Category**
  - **Unity Compatibility Version** (e.g. `2022.3`, `6000.3`)
  - **Download Size**
  - **Publisher / Author**
  - **Direct Store Links** (`https://assetstore.unity.com/packages/p/...`)
  - **Publisher Websites & Documentation Links**
  - **Full Asset Overview & Description**
- 🔍 **In-Editor Filter & Search**: Quickly search your assets by title or category before exporting.
- 🚀 **One-Click Export**: Save to standard CSV (ready for Excel/Google Sheets) or formatted JSON.
- 🔒 **Safe & Private**: 100% offline, local-only in-memory inspection. No credentials, tokens, or personal identifiers are stored or transmitted.

---

## 📥 Installation

### Option 1: Drop-in Script
Simply copy [`MyAssetsExporterWindow.cs`](Assets/Editor/MyAssetsExporterWindow.cs) into any `Editor/` folder inside your Unity project:
```
Assets/
  └── Editor/
        └── MyAssetsExporterWindow.cs
```

### Option 2: Git URL (via UPM)
If distributed as a package, add to `Packages/manifest.json`:
```json
"com.tools.my-assets-exporter": "https://github.com/milindkatrodia/unity-my-assets-exporter.git"
```

---

## 🚀 How to Use

1. Open your Unity Project.
2. Open **Window > Package Manager**.
3. In the top-left dropdown, select **"My Assets"** (wait a moment for your purchased items to display).
4. Go to the top Unity menu and click:
   > **Tools > My Assets Exporter**
5. Click **"🔄 Scan & Load My Assets"**.
6. Click **"💾 Export to CSV"** or **"📄 Export to JSON"**.

---

## 📊 Sample Export Format (CSV)

```csv
Title,PackageId,Version,Category,Compatibility,Size,Author,PublisherWebsite,StoreLink,Overview,Status
"Bakery - GPU Lightmapper","122218","1.985.0","Tools/Level Design","2021.3","","MrF","http://mrfdev.com/","https://assetstore.unity.com/packages/p/bakery-gpu-lightmapper-122218","Bakery is a high-end GPU lightmapper...","Installed"
"NodeCanvas","14914","3.4.2","Tools/Visual Scripting","2019.4","","Paradox Notion","https://nodecanvas.paradoxnotion.com","https://assetstore.unity.com/packages/p/nodecanvas-14914","NodeCanvas is the complete node-based framework...","DownloadAvailable"
```

---

## 🔒 Security & Privacy

See [SECURITY.md](SECURITY.md) for full details.
- Runs exclusively on the client machine inside the active Unity Editor.
- Zero analytics, zero telemetries, and zero third-party network requests.
- Does not modify any project files, assets, or dependencies.

---

## 📄 License

This project is licensed under the [MIT License](LICENSE) - feel free to use, modify, and distribute it in personal or commercial projects.

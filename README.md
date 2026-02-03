# EOPubGFX Editor

A cross-platform editor for Endless Online pub files (EIF, ENF, ESF, ECF) with integrated GFX preview and export capabilities. Built with [Avalonia UI](https://avaloniaui.net/) for .NET 9.

## Features

### Pub File Editing
- **Items (EIF)** - Edit item properties including name, type, stats, requirements, and graphics
- **NPCs (ENF)** - Edit NPC properties including name, type, stats, drops, and graphics
- **Spells (ESF)** - Edit spell properties including name, type, damage, mana cost, and graphics
- **Classes (ECF)** - Edit class properties including name, base stats, and stat growth

### GFX Integration
- **Visual Graphic Picker** - Browse and select graphics from EGF files with live previews
- **Equipment Graphics** - Full support for armor, weapons, boots, hats, and shields
- **NPC Animation Frames** - View all 40 animation frames per NPC
- **Spell Effects** - View all 3 effect layers per spell

### Import/Export
- **Export to BMP** - Export any graphic as a true 24-bit BMP file
- **Import from BMP** - Import custom BMP graphics into EGF files
- **Batch Export** - Export all frames for NPCs or all layers for spells at once

## Requirements

### For Running
- Windows x64 (self-contained builds include .NET runtime)
- Endless Online GFX files (gfx001.egf - gfx025.egf)
- Pub files to edit (dat001.eif, dtn001.enf, dsl001.esf, dat001.ecf)

### For Building
- .NET 9.0 SDK
- Any OS supported by .NET (Windows, macOS, Linux)

## Installation

### Pre-built Windows Release
1. Download the latest release from the [Releases](https://github.com/Connor93/EOPubGFX/releases) page
2. Extract to any folder
3. Run `SOE_PubEditor.exe`

### Building from Source
```bash
# Clone the repository
git clone https://github.com/Connor93/EOPubGFX.git
cd EOPubGFX

# Build for your current platform
dotnet build

# Run
dotnet run

# Publish self-contained Windows build
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -o publish/win-x64
```

## First-Time Setup

On first launch, you'll be prompted to configure your directories:

1. **Pub Directory** - Folder containing your pub files (`.eif`, `.enf`, `.esf`, `.ecf`)
2. **GFX Directory** - Folder containing your EGF graphics files (`.egf`)

These settings are saved and can be changed later via **File → Settings**.

## Usage

### Editing Records

1. Select a tab (Items, NPCs, Spells, or Classes)
2. Click on a record in the list to view/edit its properties
3. Modify values in the property panel on the right
4. Use **File → Save** (or Ctrl+S) to save changes

### Selecting Graphics

1. Select an item/NPC/spell record
2. Click the graphic preview button (shows current graphic)
3. Browse available graphics in the picker dialog
4. Click a graphic to select it

### Exporting Graphics

1. Select the record whose graphics you want to export
2. Right-click the graphic preview or use the menu
3. Select **Export All Graphics...**
4. Choose an output folder
5. Graphics are saved as BMP files

### Importing Graphics

1. Prepare your BMP file (24-bit, any size)
2. Right-click the graphic preview
3. Select **Import Graphic...**
4. Select your BMP file
5. The graphic is embedded into the EGF file

## GFX File Reference

| File | Contents |
|------|----------|
| gfx001.egf | Map tiles |
| gfx002.egf | Map objects |
| gfx003.egf | Map walls |
| gfx004.egf | Map shadows/overlays |
| gfx005.egf | Map animations |
| gfx006.egf | Male hats |
| gfx007.egf | Female hats |
| gfx008.egf | Male armor |
| gfx009.egf | Female armor |
| gfx010.egf | Male boots |
| gfx011.egf | Female boots |
| gfx012.egf | Male weapons |
| gfx013.egf | Shields/back items |
| gfx014.egf | Shields/back items (female) |
| gfx015.egf | Item icons (inventory/ground) |
| gfx021.egf | NPC sprites |
| gfx022.egf | Character shadows |
| gfx023.egf | Spell icons |
| gfx024.egf | Spell effects |
| gfx025.egf | UI elements |

## Dependencies

- [Avalonia UI](https://avaloniaui.net/) - Cross-platform UI framework
- [CommunityToolkit.Mvvm](https://github.com/CommunityToolkit/dotnet) - MVVM toolkit
- [Moffat.EndlessOnline.SDK](https://github.com/MoffatOfficial/eolib-dotnet) - EO data format library
- [SkiaSharp](https://github.com/mono/SkiaSharp) - 2D graphics library

## License

This project is provided as-is for the Endless Online community.

## Acknowledgments

- The Endless Online community for documentation on file formats
- [EOLib](https://github.com/ethanmoffat/EndlessClient) for reference implementations
- [eolib-dotnet](https://github.com/MoffatOfficial/eolib-dotnet) for the pub file SDK

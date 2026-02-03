using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Moffat.EndlessOnline.SDK.Protocol.Pub;
using SkiaSharp;
using SOE_PubEditor.Models;

namespace SOE_PubEditor.Services;

/// <summary>
/// Service for exporting GFX graphics to BMP files.
/// </summary>
public class GfxExportService : IGfxExportService
{
    private readonly IGfxService _gfxService;

    public GfxExportService(IGfxService gfxService)
    {
        _gfxService = gfxService;
    }

    public async Task<GfxExportResult> ExportItemGraphicsAsync(ItemRecordWrapper item, string outputFolder)
    {
        return await Task.Run(() =>
        {
            try
            {
                var exportedPaths = new List<string>();
                var itemFolderName = SanitizeFileName($"item_{item.Id:D4}_{item.Name}");
                var baseFolder = Path.Combine(outputFolder, itemFolderName);
                Directory.CreateDirectory(baseFolder);

                // 1. Inventory icon (gfx023, even formula: 2 * GraphicId + 100)
                var inventoryDir = Path.Combine(baseFolder, "inventory");
                Directory.CreateDirectory(inventoryDir);
                var inventoryResourceId = (2 * item.GraphicId) + 100;
                var inventoryPath = Path.Combine(inventoryDir, "inventory_icon.bmp");
                if (ExportResourceToBmp(GfxType.Items, inventoryResourceId, inventoryPath))
                {
                    exportedPaths.Add(inventoryPath);
                    FileLogger.LogInfo($"GFX EXPORT: Saved inventory icon to {inventoryPath}");
                }

                // 2. Ground sprite (gfx023, odd formula: 2 * GraphicId - 1 + 100)
                var groundDir = Path.Combine(baseFolder, "ground");
                Directory.CreateDirectory(groundDir);
                var groundResourceId = (2 * item.GraphicId - 1) + 100;
                var groundPath = Path.Combine(groundDir, "ground_sprite.bmp");
                if (ExportResourceToBmp(GfxType.Items, groundResourceId, groundPath))
                {
                    exportedPaths.Add(groundPath);
                    FileLogger.LogInfo($"GFX EXPORT: Saved ground sprite to {groundPath}");
                }

                // 3. Equipment graphics (gfx011-020 based on ItemType)
                var dollGraphic = item.Spec1;
                var (gfxType, typeName) = GetEquipmentGfxType(item.Type, item.Name);
                
                FileLogger.LogInfo($"GFX EXPORT: Equipment check - Type={item.Type}, TypeName={typeName}, Spec1/DollGraphic={dollGraphic}");
                
                // Only export equipment if this is an equipment type with a valid doll graphic
                // Note: Equipment graphics use Spec1 (dollGraphic), not GraphicId
                if (!string.IsNullOrEmpty(typeName) && dollGraphic > 0)
                {
                    FileLogger.LogInfo($"GFX EXPORT: Creating equipment folder, exporting {typeName} frames...");
                    var equipmentDir = Path.Combine(baseFolder, "equipment");
                    Directory.CreateDirectory(equipmentDir);
                    
                    // Equipment formula varies by type:
                    // Weapons: DollGraphic * 100 + frame
                    // Armor:   (DollGraphic - 1) * 50 + 101 + frame (22 frames per item)
                    // Boots:   (DollGraphic - 1) * 40 + 101 + frame (16 frames per item)
                    // Hats:    (DollGraphic - 1) * 10 + 101 + frame (3 frames per item)
                    int baseResourceId;
                    int maxFrames;
                    
                    switch (item.Type)
                    {
                        case ItemType.Weapon:
                            baseResourceId = (dollGraphic * 100) + 1;
                            maxFrames = 17;
                            break;
                        case ItemType.Armor:
                            baseResourceId = ((dollGraphic - 1) * 50) + 101;
                            maxFrames = 22;
                            break;
                        case ItemType.Boots:
                            baseResourceId = ((dollGraphic - 1) * 40) + 101;
                            maxFrames = 16;
                            break;
                        case ItemType.Hat:
                            baseResourceId = ((dollGraphic - 1) * 10) + 101;
                            maxFrames = 3;
                            break;
                        default:
                            baseResourceId = ((dollGraphic - 1) * 50) + 101;
                            maxFrames = 22;
                            break;
                    }
                    
                    // Export all frames to equipment folder
                    int frameCount = 0;
                    for (int frame = 0; frame < maxFrames; frame++)
                    {
                        var resourceId = baseResourceId + frame;
                        var framePath = Path.Combine(equipmentDir, $"{typeName}_frame_{frame:D2}.bmp");
                        
                        if (ExportResourceToBmp(gfxType, resourceId, framePath))
                        {
                            exportedPaths.Add(framePath);
                            frameCount++;
                        }
                    }
                    
                    if (frameCount > 0)
                        FileLogger.LogInfo($"GFX EXPORT: Saved {frameCount} {typeName} frames");
                }

                return new GfxExportResult(true, exportedPaths.Count, exportedPaths.ToArray(), null);
            }
            catch (Exception ex)
            {
                FileLogger.LogError($"GFX EXPORT ERROR: {ex.Message}");
                return new GfxExportResult(false, 0, Array.Empty<string>(), ex.Message);
            }
        });
    }
    
    /// <summary>
    /// Maps ItemType to the corresponding GFX type for equipment graphics.
    /// Weapons/Shields only have one version (in male files).
    /// Armor/Boots/Hat determine gender by item name or can be detected.
    /// </summary>
    private static (GfxType gfxType, string name) GetEquipmentGfxType(ItemType type, string itemName)
    {
        // Check if item name suggests female variant (common pattern in EO data)
        var isFemale = itemName.Contains("(F)") || 
                       itemName.Contains("Female") || 
                       itemName.Contains(" F)") ||
                       itemName.EndsWith(" F");
        
        return type switch
        {
            ItemType.Weapon => (GfxType.MaleWeapon, "weapon"),    // Weapons only in gfx017
            ItemType.Shield => (GfxType.MaleBack, "shield"),       // Shields only in gfx019
            ItemType.Armor => (isFemale ? GfxType.FemaleArmor : GfxType.MaleArmor, "armor"),
            ItemType.Hat => (isFemale ? GfxType.FemaleHat : GfxType.MaleHat, "hat"),
            ItemType.Boots => (isFemale ? GfxType.FemaleBoots : GfxType.MaleBoots, "boots"),
            _ => (GfxType.Items, "")
        };
    }
    
    /// <summary>
    /// Logs nearby available resource IDs to help debug missing graphics.
    /// </summary>
    private void LogNearbyResources(GfxType type, int targetResourceId)
    {
        try
        {
            var availableIds = _gfxService.GetAvailableResourceIds(type);
            if (availableIds.Count == 0)
            {
                FileLogger.LogInfo($"GFX DEBUG: No resources found in {type}");
                return;
            }
            
            // Find IDs near the target
            var nearby = availableIds
                .Where(id => Math.Abs(id - targetResourceId) <= 20)
                .OrderBy(id => id)
                .ToList();
            
            if (nearby.Count > 0)
            {
                FileLogger.LogInfo($"GFX DEBUG: Nearby IDs in gfx{(int)type:D3}.egf (within ±20 of {targetResourceId}): {string.Join(", ", nearby)}");
            }
            else
            {
                // If no nearby IDs, show first few and last few
                var first = availableIds.Take(5).ToList();
                var last = availableIds.Skip(Math.Max(0, availableIds.Count - 5)).ToList();
                FileLogger.LogInfo($"GFX DEBUG: First 5 IDs in gfx{(int)type:D3}.egf: {string.Join(", ", first)}");
                FileLogger.LogInfo($"GFX DEBUG: Last 5 IDs in gfx{(int)type:D3}.egf: {string.Join(", ", last)}");
                FileLogger.LogInfo($"GFX DEBUG: Total resources in file: {availableIds.Count}");
            }
        }
        catch (Exception ex)
        {
            FileLogger.LogInfo($"GFX DEBUG: Error scanning resources: {ex.Message}");
        }
    }

    public async Task<GfxExportResult> ExportNpcGraphicsAsync(NpcRecordWrapper npc, string outputFolder)
    {
        return await Task.Run(() =>
        {
            try
            {
                var exportedPaths = new List<string>();
                var npcFolderName = SanitizeFileName($"npc_{npc.Id:D4}_{npc.Name}");
                var baseFolder = Path.Combine(outputFolder, npcFolderName);
                Directory.CreateDirectory(baseFolder);

                // NPC frames are stored with formula: (Graphic - 1) * 40 + frame + 100
                // Frame layout (16 frames per NPC, stride of 40 reserved):
                // Down/Right facing (mirrored for Down and Right directions):
                //   1-2: Standing/idle (frame 1 = standing, frame 2 = idle animation)
                //   5-8: Walking (4 frames)
                //   13-14: Attacking (2 frames)
                // Up/Left facing (mirrored for Up and Left directions):
                //   3-4: Standing/idle (frame 3 = standing, frame 4 = idle animation)
                //   9-12: Walking (4 frames)
                //   15-16: Attacking (2 frames)

                var frameCategories = new Dictionary<string, int[]>
                {
                    ["standing_down_right"] = new[] { 1, 2 },
                    ["standing_up_left"] = new[] { 3, 4 },
                    ["walking_down_right"] = new[] { 5, 6, 7, 8 },
                    ["walking_up_left"] = new[] { 9, 10, 11, 12 },
                    ["attacking_down_right"] = new[] { 13, 14 },
                    ["attacking_up_left"] = new[] { 15, 16 }
                };

                foreach (var (category, frames) in frameCategories)
                {
                    var categoryDir = Path.Combine(baseFolder, category);
                    Directory.CreateDirectory(categoryDir);

                    foreach (var frame in frames)
                    {
                        var resourceId = ((npc.GraphicId - 1) * 40) + frame + 100;
                        var framePath = Path.Combine(categoryDir, $"frame_{frame:D2}.bmp");
                        
                        if (ExportResourceToBmp(GfxType.NPC, resourceId, framePath))
                        {
                            exportedPaths.Add(framePath);
                            FileLogger.LogInfo($"GFX EXPORT: Saved NPC frame {frame} to {framePath}");
                        }
                    }
                }

                return new GfxExportResult(true, exportedPaths.Count, exportedPaths.ToArray(), null);
            }
            catch (Exception ex)
            {
                FileLogger.LogError($"GFX EXPORT ERROR: {ex.Message}");
                return new GfxExportResult(false, 0, Array.Empty<string>(), ex.Message);
            }
        });
    }

    public async Task<GfxExportResult> ExportSpellGraphicsAsync(SpellRecordWrapper spell, string outputFolder)
    {
        return await Task.Run(() =>
        {
            try
            {
                var exportedPaths = new List<string>();
                var spellFolderName = SanitizeFileName($"spell_{spell.Id:D4}_{spell.Name}");
                var baseFolder = Path.Combine(outputFolder, spellFolderName);
                Directory.CreateDirectory(baseFolder);

                // 1. Effect layers (gfx024) - 3 layers per spell
                // Formula: (GraphicId - 1) * 3 + layer + 100
                var effectsDir = Path.Combine(baseFolder, "effects");
                Directory.CreateDirectory(effectsDir);

                for (int layer = 1; layer <= 3; layer++)
                {
                    var resourceId = ((spell.GraphicId - 1) * 3) + layer + 100;
                    var layerPath = Path.Combine(effectsDir, $"layer_{layer}.bmp");
                    
                    if (ExportResourceToBmp(GfxType.Spells, resourceId, layerPath))
                    {
                        exportedPaths.Add(layerPath);
                        FileLogger.LogInfo($"GFX EXPORT: Saved spell layer {layer} to {layerPath}");
                    }
                }

                // 2. Spell icon (gfx025) - simple formula: IconId + 100
                var iconsDir = Path.Combine(baseFolder, "icons");
                Directory.CreateDirectory(iconsDir);
                var iconResourceId = spell.IconId + 100;
                var iconPath = Path.Combine(iconsDir, "spell_icon.bmp");
                
                if (ExportResourceToBmp(GfxType.SpellIcons, iconResourceId, iconPath))
                {
                    exportedPaths.Add(iconPath);
                    FileLogger.LogInfo($"GFX EXPORT: Saved spell icon to {iconPath}");
                }

                return new GfxExportResult(true, exportedPaths.Count, exportedPaths.ToArray(), null);
            }
            catch (Exception ex)
            {
                FileLogger.LogError($"GFX EXPORT ERROR: {ex.Message}");
                return new GfxExportResult(false, 0, Array.Empty<string>(), ex.Message);
            }
        });
    }

    /// <summary>
    /// Exports a single resource from an EGF file to a BMP file.
    /// Manually writes BMP format because SkiaSharp doesn't support BMP encoding.
    /// </summary>
    private bool ExportResourceToBmp(GfxType type, int resourceId, string outputPath)
    {
        try
        {
            // Load the bitmap using the existing GfxService
            // Note: We use direct resource ID here, not the entity ID
            var bitmap = LoadBitmapDirectResourceId(type, resourceId);
            
            if (bitmap == null)
            {
                FileLogger.LogInfo($"GFX EXPORT: Resource {resourceId} not found in {type}");
                return false;
            }

            // Convert Avalonia Bitmap to SkiaSharp SKBitmap for pixel access
            using var memStream = new MemoryStream();
            bitmap.Save(memStream); // Saves as PNG
            memStream.Position = 0;
            
            using var skBitmap = SKBitmap.Decode(memStream);
            if (skBitmap == null)
            {
                FileLogger.LogInfo($"GFX EXPORT: Failed to decode bitmap for resource {resourceId}");
                return false;
            }
            
            // Manually write BMP file format
            // BMP stores pixels bottom-to-top, left-to-right
            int width = skBitmap.Width;
            int height = skBitmap.Height;
            int bitsPerPixel = 24; // 24-bit RGB
            int rowSize = ((width * bitsPerPixel + 31) / 32) * 4; // Row size padded to 4-byte boundary
            int pixelDataSize = rowSize * height;
            int fileSize = 14 + 40 + pixelDataSize; // BITMAPFILEHEADER + BITMAPINFOHEADER + pixels
            
            using var fs = new FileStream(outputPath, FileMode.Create, FileAccess.Write);
            using var writer = new BinaryWriter(fs);
            
            // BITMAPFILEHEADER (14 bytes)
            writer.Write((byte)'B');
            writer.Write((byte)'M');
            writer.Write(fileSize);        // File size
            writer.Write((short)0);        // Reserved1
            writer.Write((short)0);        // Reserved2  
            writer.Write(14 + 40);         // Offset to pixel data
            
            // BITMAPINFOHEADER (40 bytes)
            writer.Write(40);              // Header size
            writer.Write(width);           // Width
            writer.Write(height);          // Height (positive = bottom-up)
            writer.Write((short)1);        // Planes
            writer.Write((short)bitsPerPixel); // Bits per pixel
            writer.Write(0);               // Compression (BI_RGB = 0)
            writer.Write(pixelDataSize);   // Image size
            writer.Write(0);               // X pixels per meter
            writer.Write(0);               // Y pixels per meter
            writer.Write(0);               // Colors used
            writer.Write(0);               // Important colors
            
            // Write pixel data (bottom-to-top, BGR format)
            byte[] row = new byte[rowSize];
            for (int y = height - 1; y >= 0; y--)
            {
                int rowOffset = 0;
                for (int x = 0; x < width; x++)
                {
                    var pixel = skBitmap.GetPixel(x, y);
                    row[rowOffset++] = pixel.Blue;
                    row[rowOffset++] = pixel.Green;
                    row[rowOffset++] = pixel.Red;
                }
                // Remaining bytes in row are already 0 (padding)
                writer.Write(row);
                Array.Clear(row, 0, row.Length); // Clear for next row
            }
            
            FileLogger.LogInfo($"GFX EXPORT: Saved {outputPath} ({width}x{height})");
            return true;
        }
        catch (Exception ex)
        {
            FileLogger.LogInfo($"GFX EXPORT: Error exporting resource {resourceId}: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Loads a bitmap using the direct PE resource ID (not the entity graphic ID).
    /// This bypasses the formula calculations in LoadBitmap.
    /// </summary>
    private Bitmap? LoadBitmapDirectResourceId(GfxType type, int resourceId)
    {
        if (string.IsNullOrEmpty(_gfxService.GfxDirectory))
            return null;

        var filePath = GetGfxFilePath(type);
        if (!File.Exists(filePath))
        {
            FileLogger.LogInfo($"GFX EXPORT: File not found: {filePath}");
            return null;
        }

        return LoadBitmapFromPE(filePath, resourceId);
    }

    private string GetGfxFilePath(GfxType type)
    {
        var fileNumber = (int)type;
        return Path.Combine(_gfxService.GfxDirectory!, $"gfx{fileNumber:D3}.egf");
    }

    /// <summary>
    /// Loads a bitmap resource from a PE (EGF) file.
    /// This is a copy of the logic from GfxService to allow direct resource ID access.
    /// </summary>
    private Bitmap? LoadBitmapFromPE(string filePath, int resourceId)
    {
        try
        {
            using var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read);
            using var reader = new BinaryReader(fs);

            // Read DOS header
            if (reader.ReadUInt16() != 0x5A4D) // "MZ"
                return null;

            // Seek to PE header offset (at 0x3C)
            fs.Seek(0x3C, SeekOrigin.Begin);
            var peOffset = reader.ReadInt32();

            // Read PE signature
            fs.Seek(peOffset, SeekOrigin.Begin);
            if (reader.ReadUInt32() != 0x4550) // "PE\0\0"
                return null;

            // Read COFF header
            reader.ReadUInt16(); // machine
            var numberOfSections = reader.ReadUInt16();
            reader.ReadUInt32(); // TimeDateStamp
            reader.ReadUInt32(); // PointerToSymbolTable
            reader.ReadUInt32(); // NumberOfSymbols
            var sizeOfOptionalHeader = reader.ReadUInt16();
            reader.ReadUInt16(); // Characteristics

            // Skip optional header to get to section headers
            var optionalHeaderStart = fs.Position;

            // Read magic to determine PE32 or PE32+
            var magic = reader.ReadUInt16();
            var isPE32Plus = magic == 0x20B;

            // Skip to data directories
            var dataDirectoryOffset = isPE32Plus ? 112 : 96;
            fs.Seek(optionalHeaderStart + dataDirectoryOffset, SeekOrigin.Begin);

            // Read resource directory (index 2)
            fs.Seek(16, SeekOrigin.Current); // Skip export and import directories
            var resourceRVA = reader.ReadUInt32();
            var resourceSize = reader.ReadUInt32();

            if (resourceRVA == 0)
                return null;

            // Skip to section headers
            fs.Seek(optionalHeaderStart + sizeOfOptionalHeader, SeekOrigin.Begin);

            // Find the section containing the resource directory
            uint resourceFileOffset = 0;
            uint sectionVirtualAddress = 0;
            for (int i = 0; i < numberOfSections; i++)
            {
                reader.ReadBytes(8); // section name
                var virtualSize = reader.ReadUInt32();
                var virtualAddress = reader.ReadUInt32();
                var sizeOfRawData = reader.ReadUInt32();
                var pointerToRawData = reader.ReadUInt32();
                reader.ReadBytes(16); // Skip rest of section header

                if (resourceRVA >= virtualAddress && resourceRVA < virtualAddress + virtualSize)
                {
                    resourceFileOffset = pointerToRawData + (resourceRVA - virtualAddress);
                    sectionVirtualAddress = virtualAddress;
                    break;
                }
            }

            if (resourceFileOffset == 0)
                return null;

            // Parse resource directory to find bitmap
            return ParseResourceDirectory(reader, fs, resourceFileOffset, resourceRVA, 
                                          sectionVirtualAddress, resourceId);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"GFX EXPORT PE: Exception: {ex.Message}");
            return null;
        }
    }

    private Bitmap? ParseResourceDirectory(BinaryReader reader, FileStream fs, 
        uint resourceFileOffset, uint resourceRVA, uint sectionVA, int targetResourceId)
    {
        fs.Seek(resourceFileOffset, SeekOrigin.Begin);

        // Read IMAGE_RESOURCE_DIRECTORY
        reader.ReadUInt32(); // Characteristics
        reader.ReadUInt32(); // TimeDateStamp
        reader.ReadUInt16(); // MajorVersion
        reader.ReadUInt16(); // MinorVersion
        var numberOfNamedEntries = reader.ReadUInt16();
        var numberOfIdEntries = reader.ReadUInt16();

        // Find RT_BITMAP type (type ID = 2)
        for (int i = 0; i < numberOfNamedEntries + numberOfIdEntries; i++)
        {
            var nameOrId = reader.ReadUInt32();
            var offsetToData = reader.ReadUInt32();

            if (nameOrId == 2) // RT_BITMAP
            {
                // This points to a subdirectory of bitmap resources
                bool isDirectory = (offsetToData & 0x80000000) != 0;
                if (!isDirectory)
                    continue;

                var subdirOffset = offsetToData & 0x7FFFFFFF;
                return FindBitmapResource(reader, fs, resourceFileOffset + subdirOffset, 
                                          resourceFileOffset, resourceRVA, sectionVA, targetResourceId);
            }
        }

        return null;
    }

    private Bitmap? FindBitmapResource(BinaryReader reader, FileStream fs, 
        uint directoryOffset, uint baseOffset, uint resourceRVA, uint sectionVA, int targetResourceId)
    {
        fs.Seek(directoryOffset, SeekOrigin.Begin);

        reader.ReadUInt32(); // Characteristics
        reader.ReadUInt32(); // TimeDateStamp
        reader.ReadUInt16(); // MajorVersion
        reader.ReadUInt16(); // MinorVersion
        var numberOfNamedEntries = reader.ReadUInt16();
        var numberOfIdEntries = reader.ReadUInt16();

        for (int i = 0; i < numberOfNamedEntries + numberOfIdEntries; i++)
        {
            var resId = reader.ReadUInt32();
            var offsetToData = reader.ReadUInt32();
            var isDirectory = (offsetToData & 0x80000000) != 0;

            if (resId == (uint)targetResourceId)
            {
                if (isDirectory)
                {
                    // Language subdirectory - get first language
                    var langDirOffset = baseOffset + (offsetToData & 0x7FFFFFFF);
                    fs.Seek(langDirOffset, SeekOrigin.Begin);

                    reader.ReadUInt32(); // Characteristics
                    reader.ReadUInt32(); // TimeDateStamp
                    reader.ReadUInt16(); // MajorVersion
                    reader.ReadUInt16(); // MinorVersion
                    var numNamed = reader.ReadUInt16();
                    var numId = reader.ReadUInt16();

                    if (numNamed + numId > 0)
                    {
                        reader.ReadUInt32(); // language ID
                        var dataEntryOffset = reader.ReadUInt32();
                        
                        if ((dataEntryOffset & 0x80000000) == 0)
                        {
                            fs.Seek(baseOffset + dataEntryOffset, SeekOrigin.Begin);
                            var dataRVA = reader.ReadUInt32();
                            var dataSize = reader.ReadUInt32();

                            var dataFileOffset = dataRVA - resourceRVA + (baseOffset);
                            return LoadBitmapData(reader, fs, dataFileOffset, dataSize);
                        }
                    }
                }
                break;
            }
        }

        return null;
    }

    private Bitmap? LoadBitmapData(BinaryReader reader, FileStream fs, uint dataOffset, uint dataSize)
    {
        try
        {
            fs.Seek(dataOffset, SeekOrigin.Begin);

            // Resource bitmaps don't have the BITMAPFILEHEADER, just BITMAPINFO
            var bitmapInfo = reader.ReadBytes((int)dataSize);

            // Read BITMAPINFOHEADER to get dimensions and compression
            using var infoReader = new BinaryReader(new MemoryStream(bitmapInfo));
            var headerSize = infoReader.ReadUInt32();
            var width = infoReader.ReadInt32();
            var height = infoReader.ReadInt32();
            infoReader.ReadUInt16(); // planes
            var bitsPerPixel = infoReader.ReadUInt16();
            var compression = infoReader.ReadUInt32();

            // Calculate pixel data offset
            uint pixelDataOffset = 14 + headerSize;

            if (compression == 3 && headerSize == 40)
            {
                pixelDataOffset += 12;
            }

            if (bitsPerPixel <= 8)
            {
                uint colorTableSize = (uint)(1 << bitsPerPixel) * 4;
                pixelDataOffset += colorTableSize;
            }

            // Create BMP file with header
            using var bmpStream = new MemoryStream();
            using var bmpWriter = new BinaryWriter(bmpStream);

            // BITMAPFILEHEADER (14 bytes)
            bmpWriter.Write((ushort)0x4D42); // "BM"
            bmpWriter.Write((uint)(14 + dataSize)); // File size
            bmpWriter.Write((ushort)0); // Reserved
            bmpWriter.Write((ushort)0); // Reserved
            bmpWriter.Write(pixelDataOffset); // Offset to pixel data

            // Write the BITMAPINFO data
            bmpWriter.Write(bitmapInfo);

            bmpStream.Seek(0, SeekOrigin.Begin);
            return new Bitmap(bmpStream);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"GFX EXPORT BMP: Exception creating bitmap: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Sanitizes a string to be safe for use as a filename.
    /// </summary>
    private static string SanitizeFileName(string name)
    {
        var invalidChars = Path.GetInvalidFileNameChars();
        var sanitized = new string(name.Select(c => invalidChars.Contains(c) ? '_' : c).ToArray());
        // Also replace any multiple underscores with single
        return Regex.Replace(sanitized, @"_+", "_").Trim('_');
    }
    
    /// <summary>
    /// Exports all graphics from all loaded pub records to organized BMP folders.
    /// Each item/NPC/spell gets its own subfolder.
    /// </summary>
    public async Task<GfxExportResult> ExportAllGraphicsAsync(
        string outputFolder, 
        IEnumerable<ItemRecordWrapper> items,
        IEnumerable<NpcRecordWrapper> npcs,
        IEnumerable<SpellRecordWrapper> spells,
        IProgress<(string status, int current, int total)>? progress = null)
    {
        return await Task.Run(async () =>
        {
            try
            {
                FileLogger.LogInfo($"BULK EXPORT: Starting export to {outputFolder}");
                
                int totalExported = 0;
                var itemsList = items.Where(i => i.GraphicId > 0 || i.Spec1 > 0).ToList();
                var npcsList = npcs.Where(n => n.GraphicId > 0).ToList();
                var spellsList = spells.Where(s => s.GraphicId > 0 || s.IconId > 0).ToList();
                
                int totalRecords = itemsList.Count + npcsList.Count + spellsList.Count;
                int currentRecord = 0;
                
                FileLogger.LogInfo($"BULK EXPORT: Found {itemsList.Count} items, {npcsList.Count} NPCs, {spellsList.Count} spells to export");
                
                // Create main category folders
                var itemsFolder = Path.Combine(outputFolder, "Items");
                var npcsFolder = Path.Combine(outputFolder, "NPCs");
                var spellsFolder = Path.Combine(outputFolder, "Spells");
                
                Directory.CreateDirectory(itemsFolder);
                Directory.CreateDirectory(npcsFolder);
                Directory.CreateDirectory(spellsFolder);
                
                // Export all items
                FileLogger.LogInfo($"BULK EXPORT: Exporting {itemsList.Count} items...");
                foreach (var item in itemsList)
                {
                    currentRecord++;
                    progress?.Report(($"Item: {item.Name}", currentRecord, totalRecords));
                    
                    var result = await ExportItemGraphicsAsync(item, itemsFolder);
                    if (result.Success)
                    {
                        totalExported += result.FilesExported;
                    }
                }
                
                // Export all NPCs
                FileLogger.LogInfo($"BULK EXPORT: Exporting {npcsList.Count} NPCs...");
                foreach (var npc in npcsList)
                {
                    currentRecord++;
                    progress?.Report(($"NPC: {npc.Name}", currentRecord, totalRecords));
                    
                    var result = await ExportNpcGraphicsAsync(npc, npcsFolder);
                    if (result.Success)
                    {
                        totalExported += result.FilesExported;
                    }
                }
                
                // Export all spells
                FileLogger.LogInfo($"BULK EXPORT: Exporting {spellsList.Count} spells...");
                foreach (var spell in spellsList)
                {
                    currentRecord++;
                    progress?.Report(($"Spell: {spell.Name}", currentRecord, totalRecords));
                    
                    var result = await ExportSpellGraphicsAsync(spell, spellsFolder);
                    if (result.Success)
                    {
                        totalExported += result.FilesExported;
                    }
                }
                
                FileLogger.LogInfo($"BULK EXPORT: Completed - exported {totalExported} files");
                
                return new GfxExportResult(true, totalExported, Array.Empty<string>(), null);
            }
            catch (Exception ex)
            {
                FileLogger.LogError($"BULK EXPORT: Exception - {ex}");
                return new GfxExportResult(false, 0, Array.Empty<string>(), ex.Message);
            }
        });
    }
}

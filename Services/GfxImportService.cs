using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Moffat.EndlessOnline.SDK.Protocol.Pub;
using SOE_PubEditor.Models;

namespace SOE_PubEditor.Services;

#if WINDOWS
/// <summary>
/// Windows implementation of GFX import service.
/// Modifies PE resources in EGF files using kernel32.dll APIs.
/// </summary>
public class GfxImportService : IGfxImportService
{
    private readonly IGfxService _gfxService;
    
    public GfxImportService(IGfxService gfxService)
    {
        _gfxService = gfxService;
    }
    
    public bool IsAvailable => true;
    
    public string CreateBackup(string egfPath)
    {
        var dir = Path.GetDirectoryName(egfPath) ?? "";
        var name = Path.GetFileNameWithoutExtension(egfPath);
        var ext = Path.GetExtension(egfPath);
        var backupPath = Path.Combine(dir, $"{name}_backup{ext}");
        
        // Don't overwrite existing backup - append timestamp if needed
        if (File.Exists(backupPath))
        {
            var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            backupPath = Path.Combine(dir, $"{name}_backup_{timestamp}{ext}");
        }
        
        File.Copy(egfPath, backupPath, false);
        FileLogger.LogInfo($"GFX IMPORT: Created backup at {backupPath}");
        return backupPath;
    }
    
    public string PrepareEgfForModification(GfxType gfxType, string outputGfxDirectory)
    {
        var sourceEgfPath = _gfxService.GetGfxFilePath(gfxType);
        var fileNumber = (int)gfxType;
        var targetEgfPath = Path.Combine(outputGfxDirectory, $"gfx{fileNumber:D3}.egf");
        
        // If destination is different and doesn't exist, copy from source
        if (!string.Equals(sourceEgfPath, targetEgfPath, StringComparison.OrdinalIgnoreCase))
        {
            if (!File.Exists(targetEgfPath))
            {
                // Ensure output directory exists
                Directory.CreateDirectory(outputGfxDirectory);
                
                if (File.Exists(sourceEgfPath))
                {
                    File.Copy(sourceEgfPath, targetEgfPath);
                    FileLogger.LogInfo($"GFX IMPORT: Copied {Path.GetFileName(sourceEgfPath)} to output directory");
                }
                else
                {
                    throw new FileNotFoundException($"Source EGF file not found: {sourceEgfPath}");
                }
            }
        }
        
        return targetEgfPath;
    }
    
    private string GetOutputEgfPath(GfxType gfxType, string? outputGfxDirectory)
    {
        if (string.IsNullOrEmpty(outputGfxDirectory))
        {
            return _gfxService.GetGfxFilePath(gfxType);
        }
        return PrepareEgfForModification(gfxType, outputGfxDirectory);
    }
    
    public async Task<GfxImportResult> ImportItemGraphicsAsync(ItemRecordWrapper item, string inputFolder, string? outputGfxDirectory = null)
    {
        return await Task.Run(() =>
        {
            try
            {
                var filesImported = 0;
                var errors = new List<string>();
                
                // Import inventory icon (gfx023)
                var inventoryPath = Path.Combine(inputFolder, "inventory", "inventory_icon.bmp");
                if (File.Exists(inventoryPath))
                {
                    var egfPath = GetOutputEgfPath(GfxType.Items, outputGfxDirectory);
                    var resourceId = (2 * item.GraphicId) + 100;
                    if (ImportBmpToEgf(egfPath, resourceId, inventoryPath))
                        filesImported++;
                    else
                        errors.Add($"Failed to import inventory icon");
                }
                
                // Import ground sprite (gfx023)
                var groundPath = Path.Combine(inputFolder, "ground", "ground_sprite.bmp");
                if (File.Exists(groundPath))
                {
                    var egfPath = GetOutputEgfPath(GfxType.Items, outputGfxDirectory);
                    var resourceId = (2 * item.GraphicId - 1) + 100;
                    if (ImportBmpToEgf(egfPath, resourceId, groundPath))
                        filesImported++;
                    else
                        errors.Add($"Failed to import ground sprite");
                }
                
                // Import equipment graphics if applicable
                var equipmentDir = Path.Combine(inputFolder, "equipment");
                if (Directory.Exists(equipmentDir))
                {
                    var (gfxType, _) = GetEquipmentGfxType(item.Type, item.Name);
                    if (gfxType != GfxType.Items)
                    {
                        var egfPath = GetOutputEgfPath(gfxType, outputGfxDirectory);
                        var bmpFiles = Directory.GetFiles(equipmentDir, "*.bmp");
                        
                        foreach (var bmpFile in bmpFiles)
                        {
                            // Parse frame number from filename 
                            // Format: "{TypeName}_frame_{XX}.bmp" (e.g., "Weapon_frame_01.bmp")
                            // Or simple "frame_{XX}.bmp" format
                            var fileName = Path.GetFileNameWithoutExtension(bmpFile);
                            int frame = -1;
                            
                            // Try to extract frame number from various formats
                            var parts = fileName.Split('_');
                            if (parts.Length >= 2 && parts[^2] == "frame" && int.TryParse(parts[^1], out int parsedFrame))
                            {
                                frame = parsedFrame;
                            }
                            else if (fileName.StartsWith("frame_") && int.TryParse(fileName.Substring(6), out int simpleFrame))
                            {
                                frame = simpleFrame;
                            }
                            
                            if (frame >= 0)
                            {
                                var resourceId = GetEquipmentResourceId(item.Type, item.Spec1, frame);
                                FileLogger.LogInfo($"GFX IMPORT: Importing equipment frame {frame} from {fileName} to resource {resourceId}");
                                if (ImportBmpToEgf(egfPath, resourceId, bmpFile))
                                    filesImported++;
                                else
                                    errors.Add($"Failed to import equipment frame {frame}");
                            }
                            else
                            {
                                FileLogger.LogWarning($"GFX IMPORT: Could not parse frame number from {fileName}");
                            }
                        }
                    }
                }
                
                var error = errors.Count > 0 ? string.Join("; ", errors) : null;
                return new GfxImportResult(errors.Count == 0, filesImported, null, error);
            }
            catch (Exception ex)
            {
                FileLogger.LogError($"GFX IMPORT ERROR: {ex.Message}");
                return new GfxImportResult(false, 0, null, ex.Message);
            }
        });
    }
    
    public async Task<GfxImportResult> ImportNpcGraphicsAsync(NpcRecordWrapper npc, string inputFolder, string? outputGfxDirectory = null)
    {
        return await Task.Run(() =>
        {
            try
            {
                var filesImported = 0;
                var errors = new List<string>();
                var egfPath = GetOutputEgfPath(GfxType.NPC, outputGfxDirectory);
                
                // Look for frame files in all subdirectories
                var allBmpFiles = Directory.GetFiles(inputFolder, "*.bmp", SearchOption.AllDirectories);
                
                foreach (var bmpFile in allBmpFiles)
                {
                    var fileName = Path.GetFileNameWithoutExtension(bmpFile);
                    // Parse frame number from filename (e.g., "frame_01.bmp" -> 1)
                    if (fileName.StartsWith("frame_") && int.TryParse(fileName.Substring(6), out int frame))
                    {
                        var resourceId = ((npc.GraphicId - 1) * 40) + frame + 100;
                        if (ImportBmpToEgf(egfPath, resourceId, bmpFile))
                        {
                            filesImported++;
                            FileLogger.LogInfo($"GFX IMPORT: Imported NPC frame {frame} to resource {resourceId}");
                        }
                        else
                        {
                            errors.Add($"Failed to import NPC frame {frame}");
                        }
                    }
                }
                
                var error = errors.Count > 0 ? string.Join("; ", errors) : null;
                return new GfxImportResult(errors.Count == 0, filesImported, null, error);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"GFX IMPORT ERROR: {ex.Message}");
                return new GfxImportResult(false, 0, null, ex.Message);
            }
        });
    }
    
    public async Task<GfxImportResult> ImportSpellGraphicsAsync(SpellRecordWrapper spell, string inputFolder, string? outputGfxDirectory = null)
    {
        return await Task.Run(() =>
        {
            try
            {
                var filesImported = 0;
                var errors = new List<string>();
                
                // Import effect layers (gfx024)
                var effectsDir = Path.Combine(inputFolder, "effects");
                if (Directory.Exists(effectsDir))
                {
                    var egfPath = GetOutputEgfPath(GfxType.Spells, outputGfxDirectory);
                    var layerFiles = Directory.GetFiles(effectsDir, "*.bmp");
                    
                    foreach (var layerFile in layerFiles)
                    {
                        var fileName = Path.GetFileNameWithoutExtension(layerFile);
                        if (fileName.StartsWith("layer_") && int.TryParse(fileName.Substring(6), out int layer))
                        {
                            var resourceId = ((spell.GraphicId - 1) * 3) + layer + 100;
                            if (ImportBmpToEgf(egfPath, resourceId, layerFile))
                            {
                                filesImported++;
                                FileLogger.LogInfo($"GFX IMPORT: Imported spell layer {layer} to resource {resourceId}");
                            }
                            else
                            {
                                errors.Add($"Failed to import spell layer {layer}");
                            }
                        }
                    }
                }
                
                // Import spell icon (gfx025)
                var iconPath = Path.Combine(inputFolder, "icons", "spell_icon.bmp");
                if (File.Exists(iconPath))
                {
                    var egfPath = GetOutputEgfPath(GfxType.SpellIcons, outputGfxDirectory);
                    var resourceId = spell.IconId + 100;
                    if (ImportBmpToEgf(egfPath, resourceId, iconPath))
                    {
                        filesImported++;
                        FileLogger.LogInfo($"GFX IMPORT: Imported spell icon to resource {resourceId}");
                    }
                    else
                    {
                        errors.Add($"Failed to import spell icon");
                    }
                }
                
                var error = errors.Count > 0 ? string.Join("; ", errors) : null;
                return new GfxImportResult(errors.Count == 0, filesImported, null, error);
            }
            catch (Exception ex)
            {
                FileLogger.LogError("GFX IMPORT ERROR (Spell)", ex);
                return new GfxImportResult(false, 0, null, ex.Message);
            }
        });
    }
    
    /// <summary>
    /// Imports a BMP file into an EGF file at the specified resource ID.
    /// </summary>
    private bool ImportBmpToEgf(string egfPath, int resourceId, string bmpPath)
    {
        try
        {
            // Read the BMP file
            var bmpData = File.ReadAllBytes(bmpPath);
            
            // Validate BMP header (should start with 'BM')
            if (bmpData.Length < 14 || bmpData[0] != 0x42 || bmpData[1] != 0x4D)
            {
                FileLogger.LogWarning($"GFX IMPORT: Invalid BMP file: {bmpPath}");
                return false;
            }
            
            // Skip the 14-byte BITMAPFILEHEADER - PE resources store raw DIB data
            var dibData = new byte[bmpData.Length - 14];
            Array.Copy(bmpData, 14, dibData, 0, dibData.Length);
            
            // Open the EGF file for update
            var updateHandle = NativeMethods.BeginUpdateResource(egfPath, false);
            if (updateHandle == IntPtr.Zero)
            {
                var error = Marshal.GetLastWin32Error();
                FileLogger.LogError($"GFX IMPORT: BeginUpdateResource failed with error {error}");
                return false;
            }
            
            try
            {
                // Pin the data and update the resource
                var gcHandle = GCHandle.Alloc(dibData, GCHandleType.Pinned);
                try
                {
                    var dataPtr = gcHandle.AddrOfPinnedObject();
                    
                    var success = NativeMethods.UpdateResource(
                        updateHandle,
                        NativeMethods.MakeIntResource(NativeMethods.RT_BITMAP),
                        NativeMethods.MakeIntResource(resourceId),
                        0, // Language neutral
                        dataPtr,
                        (uint)dibData.Length);
                    
                    if (!success)
                    {
                        var error = Marshal.GetLastWin32Error();
                        FileLogger.LogError($"GFX IMPORT: UpdateResource failed with error {error}");
                        NativeMethods.EndUpdateResource(updateHandle, true); // Discard
                        return false;
                    }
                }
                finally
                {
                    gcHandle.Free();
                }
                
                // Commit the changes
                if (!NativeMethods.EndUpdateResource(updateHandle, false))
                {
                    var error = Marshal.GetLastWin32Error();
                    FileLogger.LogError($"GFX IMPORT: EndUpdateResource failed with error {error}");
                    return false;
                }
                
                return true;
            }
            catch
            {
                NativeMethods.EndUpdateResource(updateHandle, true); // Discard on error
                throw;
            }
        }
        catch (Exception ex)
        {
            FileLogger.LogError($"GFX IMPORT: Error importing {bmpPath}", ex);
            return false;
        }
    }
    
    private static (GfxType gfxType, string name) GetEquipmentGfxType(ItemType type, string itemName)
    {
        var isFemale = itemName.Contains("(F)") || 
                       itemName.Contains("Female") || 
                       itemName.Contains(" F)") ||
                       itemName.EndsWith(" F");
        
        return type switch
        {
            ItemType.Weapon => (GfxType.MaleWeapon, "weapon"),
            ItemType.Shield => (GfxType.MaleBack, "shield"),
            ItemType.Armor => (isFemale ? GfxType.FemaleArmor : GfxType.MaleArmor, "armor"),
            ItemType.Hat => (isFemale ? GfxType.FemaleHat : GfxType.MaleHat, "hat"),
            ItemType.Boots => (isFemale ? GfxType.FemaleBoots : GfxType.MaleBoots, "boots"),
            _ => (GfxType.Items, "")
        };
    }
    
    private static int GetEquipmentResourceId(ItemType type, int dollGraphic, int frame)
    {
        return type switch
        {
            ItemType.Weapon => (dollGraphic * 100) + frame,
            ItemType.Armor => ((dollGraphic - 1) * 50) + 100 + frame,
            ItemType.Boots => ((dollGraphic - 1) * 40) + 100 + frame,
            ItemType.Hat => ((dollGraphic - 1) * 10) + 100 + frame,
            _ => ((dollGraphic - 1) * 50) + 100 + frame // Default to armor formula
        };
    }
}

#else

/// <summary>
/// Non-Windows stub implementation.
/// </summary>
public class GfxImportService : IGfxImportService
{
    public GfxImportService(IGfxService gfxService) { }
    
    public bool IsAvailable => false;
    
    public string CreateBackup(string egfPath) 
        => throw new PlatformNotSupportedException("GFX import requires Windows");
    
    public string PrepareEgfForModification(GfxType gfxType, string outputGfxDirectory)
        => throw new PlatformNotSupportedException("GFX import requires Windows");
    
    public Task<GfxImportResult> ImportItemGraphicsAsync(ItemRecordWrapper item, string inputFolder, string? outputGfxDirectory = null)
        => Task.FromResult(new GfxImportResult(false, 0, null, "GFX import requires Windows"));
    
    public Task<GfxImportResult> ImportNpcGraphicsAsync(NpcRecordWrapper npc, string inputFolder, string? outputGfxDirectory = null)
        => Task.FromResult(new GfxImportResult(false, 0, null, "GFX import requires Windows"));
    
    public Task<GfxImportResult> ImportSpellGraphicsAsync(SpellRecordWrapper spell, string inputFolder, string? outputGfxDirectory = null)
        => Task.FromResult(new GfxImportResult(false, 0, null, "GFX import requires Windows"));
}

#endif

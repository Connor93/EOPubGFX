using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Moffat.EndlessOnline.SDK.Protocol.Pub;
using SOE_PubEditor.Models;

namespace SOE_PubEditor.Services;

/// <summary>
/// Result of a GFX export operation.
/// </summary>
public record GfxExportResult(
    bool Success, 
    int FilesExported, 
    string[] ExportedPaths, 
    string? Error);

/// <summary>
/// Service for exporting GFX graphics to BMP files.
/// </summary>
public interface IGfxExportService
{
    /// <summary>
    /// Export all graphics for an item (inventory icon, ground sprite).
    /// </summary>
    Task<GfxExportResult> ExportItemGraphicsAsync(ItemRecordWrapper item, string outputFolder);
    
    /// <summary>
    /// Export all graphics for an NPC (all animation frames).
    /// </summary>
    Task<GfxExportResult> ExportNpcGraphicsAsync(NpcRecordWrapper npc, string outputFolder);
    
    /// <summary>
    /// Export all graphics for a spell (effect layers and icon).
    /// </summary>
    Task<GfxExportResult> ExportSpellGraphicsAsync(SpellRecordWrapper spell, string outputFolder);
    
    /// <summary>
    /// Export all graphics from all loaded pub records to organized BMP folders.
    /// Each item/NPC/spell gets its own subfolder.
    /// </summary>
    /// <param name="outputFolder">The root folder for exported files.</param>
    /// <param name="items">Collection of items to export graphics for.</param>
    /// <param name="npcs">Collection of NPCs to export graphics for.</param>
    /// <param name="spells">Collection of spells to export graphics for.</param>
    /// <param name="progress">Optional progress reporter (status, current, total).</param>
    /// <returns>Result containing total files exported.</returns>
    Task<GfxExportResult> ExportAllGraphicsAsync(
        string outputFolder, 
        IEnumerable<ItemRecordWrapper> items,
        IEnumerable<NpcRecordWrapper> npcs,
        IEnumerable<SpellRecordWrapper> spells,
        IProgress<(string status, int current, int total)>? progress = null);
}

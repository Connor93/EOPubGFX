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
}

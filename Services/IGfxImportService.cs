using System.Threading.Tasks;
using SOE_PubEditor.Models;

namespace SOE_PubEditor.Services;

/// <summary>
/// Result of a GFX import operation.
/// </summary>
public record GfxImportResult(bool Success, int FilesImported, int? AssignedGraphicId, string? Error);

/// <summary>
/// Service for importing BMP files into EGF (PE) resources.
/// Windows-only due to kernel32.dll dependency.
/// </summary>
public interface IGfxImportService
{
    /// <summary>
    /// Returns true if the import functionality is available on this platform.
    /// </summary>
    bool IsAvailable { get; }
    
    /// <summary>
    /// Imports BMPs from a folder back into EGF files for an existing item.
    /// Expects folder structure: inventory/inventory_icon.bmp, ground/ground_sprite.bmp, equipment/*.bmp
    /// </summary>
    /// <param name="item">The item record to import graphics for.</param>
    /// <param name="inputFolder">Folder containing the BMP files.</param>
    /// <param name="outputGfxDirectory">Optional output GFX directory. If null, uses the current GFX directory.</param>
    Task<GfxImportResult> ImportItemGraphicsAsync(ItemRecordWrapper item, string inputFolder, string? outputGfxDirectory = null);
    
    /// <summary>
    /// Imports BMPs from a folder into EGF files for an existing NPC.
    /// Expects folder structure: standing/*.bmp, walking/*.bmp, attacking/*.bmp
    /// </summary>
    /// <param name="npc">The NPC record to import graphics for.</param>
    /// <param name="inputFolder">Folder containing the BMP files.</param>
    /// <param name="outputGfxDirectory">Optional output GFX directory. If null, uses the current GFX directory.</param>
    Task<GfxImportResult> ImportNpcGraphicsAsync(NpcRecordWrapper npc, string inputFolder, string? outputGfxDirectory = null);
    
    /// <summary>
    /// Imports BMPs from a folder into EGF files for an existing spell.
    /// Expects folder structure: effects/*.bmp, icons/spell_icon.bmp
    /// </summary>
    /// <param name="spell">The spell record to import graphics for.</param>
    /// <param name="inputFolder">Folder containing the BMP files.</param>
    /// <param name="outputGfxDirectory">Optional output GFX directory. If null, uses the current GFX directory.</param>
    Task<GfxImportResult> ImportSpellGraphicsAsync(SpellRecordWrapper spell, string inputFolder, string? outputGfxDirectory = null);
    
    /// <summary>
    /// Prepares an EGF file for modification. If outputDir differs from source, copies the file.
    /// </summary>
    /// <param name="gfxType">The GFX type to prepare.</param>
    /// <param name="outputGfxDirectory">The output directory.</param>
    /// <returns>The path to the EGF file that should be modified.</returns>
    string PrepareEgfForModification(GfxType gfxType, string outputGfxDirectory);
}

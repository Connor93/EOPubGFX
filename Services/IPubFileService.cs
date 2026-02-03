using System.IO;
using System.Threading.Tasks;
using Moffat.EndlessOnline.SDK.Protocol.Pub;

namespace SOE_PubEditor.Services;

/// <summary>
/// Service for loading and saving pub files (EIF, ENF, ESF, ECF).
/// </summary>
public interface IPubFileService
{
    /// <summary>
    /// Loads item pub file from the specified path.
    /// </summary>
    Task<Eif> LoadItemsAsync(string filePath);
    
    /// <summary>
    /// Loads NPC pub file from the specified path.
    /// </summary>
    Task<Enf> LoadNpcsAsync(string filePath);
    
    /// <summary>
    /// Loads spell pub file from the specified path.
    /// </summary>
    Task<Esf> LoadSpellsAsync(string filePath);
    
    /// <summary>
    /// Loads class pub file from the specified path.
    /// </summary>
    Task<Ecf> LoadClassesAsync(string filePath);
    
    /// <summary>
    /// Saves item pub file to the specified path.
    /// </summary>
    Task SaveItemsAsync(string filePath, Eif data);
    
    /// <summary>
    /// Saves NPC pub file to the specified path.
    /// </summary>
    Task SaveNpcsAsync(string filePath, Enf data);
    
    /// <summary>
    /// Saves spell pub file to the specified path.
    /// </summary>
    Task SaveSpellsAsync(string filePath, Esf data);
    
    /// <summary>
    /// Saves class pub file to the specified path.
    /// </summary>
    Task SaveClassesAsync(string filePath, Ecf data);
}

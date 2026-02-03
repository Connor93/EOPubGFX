using System;
using System.Runtime.InteropServices;

namespace SOE_PubEditor.Services;

#if WINDOWS
/// <summary>
/// Windows P/Invoke declarations for PE resource modification.
/// Used to update BMP resources in .egf files (which are PE executables).
/// </summary>
internal static class NativeMethods
{
    private const string Kernel32 = "kernel32.dll";
    
    // Resource type for bitmaps
    public const int RT_BITMAP = 2;
    
    /// <summary>
    /// Opens a PE file for resource updates.
    /// </summary>
    [DllImport(Kernel32, SetLastError = true, CharSet = CharSet.Unicode)]
    public static extern IntPtr BeginUpdateResource(string pFileName, bool bDeleteExistingResources);
    
    /// <summary>
    /// Updates a resource within the PE file.
    /// lpType: Use MAKEINTRESOURCE(RT_BITMAP) for bitmaps
    /// lpName: Resource ID (use MAKEINTRESOURCE for numeric IDs)
    /// wLanguage: Language ID (0 for neutral)
    /// lpData: Pointer to resource data (for BMP: skip BITMAPFILEHEADER, pass DIB data)
    /// cbData: Size of resource data
    /// </summary>
    [DllImport(Kernel32, SetLastError = true)]
    public static extern bool UpdateResource(
        IntPtr hUpdate, 
        IntPtr lpType, 
        IntPtr lpName, 
        ushort wLanguage, 
        IntPtr lpData, 
        uint cbData);
    
    /// <summary>
    /// Commits or discards the resource updates.
    /// fDiscard: false to commit, true to discard changes
    /// </summary>
    [DllImport(Kernel32, SetLastError = true)]
    public static extern bool EndUpdateResource(IntPtr hUpdate, bool fDiscard);
    
    /// <summary>
    /// Loads a library for resource enumeration (use LOAD_LIBRARY_AS_DATAFILE).
    /// </summary>
    [DllImport(Kernel32, SetLastError = true, CharSet = CharSet.Unicode)]
    public static extern IntPtr LoadLibraryEx(string lpFileName, IntPtr hFile, uint dwFlags);
    
    /// <summary>
    /// Frees a loaded library.
    /// </summary>
    [DllImport(Kernel32, SetLastError = true)]
    public static extern bool FreeLibrary(IntPtr hModule);
    
    // LoadLibraryEx flags
    public const uint LOAD_LIBRARY_AS_DATAFILE = 0x00000002;
    public const uint LOAD_LIBRARY_AS_IMAGE_RESOURCE = 0x00000020;
    
    /// <summary>
    /// Creates an INTRESOURCE value from an integer ID.
    /// In Windows, resource IDs < 0x10000 are passed as IntPtr directly.
    /// </summary>
    public static IntPtr MakeIntResource(int id) => (IntPtr)id;
}
#endif

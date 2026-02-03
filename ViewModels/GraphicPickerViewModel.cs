using System.Linq;
using System.Collections.ObjectModel;
using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using SOE_PubEditor.Services;

namespace SOE_PubEditor.ViewModels;

public partial class GraphicPickerViewModel : ViewModelBase
{
    private readonly IGfxService _gfxService;
    private readonly GfxType _gfxType;
    
    [ObservableProperty]
    private ObservableCollection<GraphicItem> _graphics = new();
    
    [ObservableProperty]
    private GraphicItem? _selectedGraphic;
    
    [ObservableProperty]
    private bool _isLoading;
    
    public int? SelectedGraphicId => SelectedGraphic?.Id;
    
    public string Title { get; }
    
    public GraphicPickerViewModel(IGfxService gfxService, GfxType gfxType)
    {
        _gfxService = gfxService;
        _gfxType = gfxType;
        Title = gfxType switch
        {
            GfxType.Items => "Select Item Graphic",
            GfxType.NPC => "Select NPC Graphic",
            GfxType.Spells => "Select Spell Graphic",
            GfxType.SpellIcons => "Select Spell Icon",
            GfxType.MaleWeapon => "Select Weapon Graphic",
            GfxType.MaleArmor or GfxType.FemaleArmor => "Select Armor Graphic",
            GfxType.MaleHat or GfxType.FemaleHat => "Select Hat Graphic",
            GfxType.MaleBoots or GfxType.FemaleBoots => "Select Boots Graphic",
            GfxType.MaleBack or GfxType.FemaleBack => "Select Shield Graphic",
            _ => "Select Graphic"
        };
    }
    
    public void LoadGraphics()
    {
        IsLoading = true;
        Graphics.Clear();
        
        var ids = _gfxService.GetAvailableResourceIds(_gfxType);
        
        foreach (var id in ids)
        {
            var bitmap = _gfxService.LoadBitmap(_gfxType, id);
            if (bitmap != null)
            {
                // For spells, extract just the first frame
                if (_gfxType == GfxType.Spells)
                {
                    var frame = _gfxService.ExtractFirstFrame(bitmap);
                    if (frame != null)
                    {
                        Graphics.Add(new GraphicItem(id, frame));
                    }
                }
                else
                {
                    Graphics.Add(new GraphicItem(id, bitmap));
                }
            }
        }
        
        IsLoading = false;
    }
    
    public void SetSelectedById(int? id)
    {
        if (id.HasValue)
        {
            SelectedGraphic = Graphics.FirstOrDefault(g => g.Id == id.Value);
        }
    }
}

public record GraphicItem(int Id, Bitmap? Image);

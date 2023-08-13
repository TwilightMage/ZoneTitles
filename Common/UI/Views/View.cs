using System;
using Terraria.Audio;
using Terraria.ID;
using ZoneTitles.Common.UI.Elements;

namespace ZoneTitles.Common.UI.Views;

public abstract class View : DragablePanel
{
    public event Action OnCloseRequested;

    public bool IsOpened() => _active;

    public void Close() => OnCloseRequested?.Invoke();

    private bool _active;

    public View()
    {
        SetPadding(10);
    }
    
    public override void OnActivate()
    {
        base.OnActivate();
        
        SoundEngine.PlaySound(SoundID.MenuOpen);
        
        _active = true;
    }

    public override void OnDeactivate()
    {
        base.OnDeactivate();
        
        SoundEngine.PlaySound(SoundID.MenuClose);

        _active = false;
    }
}
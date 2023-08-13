using System;
using Terraria.UI;
using ZoneTitles.Common.UI.Views;

namespace ZoneTitles.Common.UI;

public class ZoneEditor : UIState
{
    public event Action OnCloseRequested;
    
    public ZonePanel ZonePanel { get; private set; }
    public IconPicker IconPicker { get; private set; }
    public bool ZonePanelVisible => ZonePanel.Parent == this;
    public bool IconPickerVisible => IconPicker.Parent == this;

    public ZoneEditor()
    {
        ZonePanel = new ZonePanel();
        
        IconPicker = new IconPicker();
    }
    
    public override void OnDeactivate()
    {
        base.OnDeactivate();
        
        CloseZonePanel();
    }

    public void OpenZonePanel(Zone zone)
    {
        if (!ZonePanelVisible)
        {
            Append(ZonePanel);
            ZonePanel.OnCloseRequested += () => OnCloseRequested?.Invoke();
            ZonePanel.Activate();
        }

        ZonePanel.TargetZone = zone;
    }

    private void CloseZonePanel()
    {
        if (ZonePanelVisible)
        {
            ZonePanel.Remove();
            ZonePanel.OnCloseRequested -= CloseZonePanel;
            ZonePanel.Deactivate();
        }
    }

    public void OpenIconPicker()
    {
        if (!IconPickerVisible)
        {
            Append(IconPicker);
            IconPicker.OnCloseRequested += CloseIconPicker;
            IconPicker.Activate();
        }
    }

    public void CloseIconPicker()
    {
        if (IconPickerVisible)
        {
            IconPicker.Remove();
            IconPicker.OnCloseRequested -= CloseIconPicker;
            IconPicker.Deactivate();
        }
    }
}
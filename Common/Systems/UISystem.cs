using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.ModLoader;
using Terraria.UI;
using ZoneTitles.Common.UI;

namespace ZoneTitles.Common.Systems;

public class UISystem : ModSystem
{
    public static MultiZoneSelector ZoneSelector;
    public static UserInterface ZoneSelectorUI;
    
    public static ZoneEditor ZoneEditor;
    public static UserInterface ZoneEditorUI;

    public static bool Visible => ZoneEditor?.ZonePanelVisible ?? false;
    
    private LegacyGameInterfaceLayer _layer;
    
    private bool _wasMouseLeftDown;
    public static bool JustMouseLeftDown { get; private set; }

    public override void OnModLoad()
    {
        ZoneSelector = new MultiZoneSelector();
        ZoneSelectorUI = new UserInterface();
        
        ZoneEditor = new ZoneEditor();
        ZoneEditor.OnCloseRequested += CloseZoneEditor;
        ZoneEditorUI = new UserInterface();

        _layer = new LegacyGameInterfaceLayer("Zones UI", Draw, InterfaceScaleType.UI);
    }

    public override void OnModUnload()
    {
        _layer = null;

        ZoneEditorUI = null;
        ZoneEditor.OnCloseRequested -= CloseZoneEditor;
        ZoneEditor = null;
        
        ZoneSelectorUI = null;
        ZoneSelector = null;
    }

    public override void UpdateUI(GameTime gameTime)
    {
        JustMouseLeftDown = Main.mouseLeft && !_wasMouseLeftDown;
        _wasMouseLeftDown = Main.mouseLeft;
        
        if (!Main.gameMenu)
        {
            ZoneSelectorUI?.Update(gameTime);
            ZoneEditorUI?.Update(gameTime);
        }
    }

    public override void ModifyInterfaceLayers(List<GameInterfaceLayer> layers)
    {
        layers.Insert(layers.FindIndex(layer => layer.Name == "Vanilla: Cursor"), _layer);
    }

    private bool Draw()
    {
        if (!Main.gameMenu)
        {
            ZoneSelectorUI.Draw(Main.spriteBatch, new GameTime());
            ZoneEditorUI.Draw(Main.spriteBatch, new GameTime());
        }
        return true;
    }

    public static void OpenZoneSelector(List<Zone> zones, Vector2 worldPosition, Action<Zone> onSelected)
    {
        ZoneSelectorUI.SetState(ZoneSelector);
        ZoneSelector.Setup(zones, worldPosition, onSelected);
    }

    public static void CloseZoneSelector()
    {
        ZoneSelectorUI.SetState(null);
    }

    public static void OpenZoneEditor(Zone zone)
    {
        ZoneEditorUI.SetState(ZoneEditor);
        ZoneEditor.OpenZonePanel(zone);
    }

    public static void CloseZoneEditor()
    {
        ZoneEditorUI.SetState(null);
    }

    public static void CloseOne()
    {
        if (Visible)
        {
            if (ZoneEditor.IconPickerVisible)
            {
                ZoneEditor.CloseIconPicker();
                return;
            }
            
            CloseZoneEditor();
            return;
        }
    }
}
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using Terraria;
using Terraria.ModLoader;
using ZoneTitles.Common;
using ZoneTitles.Common.Systems;
using ZoneTitles.Common.UI;

namespace ZoneTitles;

// This is a partial class, meaning some of its parts were split into other files. See ExampleMod.*.cs for other portions.
public partial class ZoneTitlesMod : Mod
{
    public static ZoneTitlesMod Instance = null;

    public override void Load()
    {
        Instance = this;

        On_Player.ToggleInv += ToggleInv;
    }

    public override void Unload()
    {
        Instance = null;
            
        On_Player.ToggleInv -= ToggleInv;
    }

    public string BTitlesHook_DynamicBiomeChecker(Player player)
    {
        return ZonesSystem.GetZoneAtTile(player.Center.ToTileCoordinates())?.ZoneId ?? "";
    }

    public dynamic BTitlesHook_DynamicBiomeProvider(string key)
    {
        return ZonesSystem.Zones.FirstOrDefault(zone => zone.ZoneId == key);
    }

    private void ToggleInv(On_Player.orig_ToggleInv orig, Terraria.Player self)
    {
        if (UISystem.Visible)
        {
            UISystem.CloseOne();
        }
        else
        {
            orig(self);
        }
    }

    // External interface
    public override object Call(params object[] args)
    {
        if (args.Length >= 1)
        {
            if (args[0] is string method)
            {
                dynamic data = args.Length >= 2 ? args[1] : null;

                switch (method)
                {
                    case nameof(AddZone):
                        if (data != null)
                        {
                            AddZone(data);
                        }
                        else
                        {
                            Logger.Warn($"Call failed. Data must be provided to call {nameof(AddZone)}!");
                        }
                        break;
                }
            }
            else
            {
                Logger.Warn("Call failed. First argument must be called method name!");
            }
        }
        else
        {
            Logger.Warn("Call failed. First argument must be called method name!");
        }

        return null;
    }

    public void AddZone(dynamic info)
    {
        Zone zone;

        if (info is Zone zoneInfo)
        {
            zone = zoneInfo;
        }
        else
        {
            zone = new Zone();
            zone.Title = Extensions.TryGetDynamicProperty<string>(info, "Title", "");
            zone.SubTitle = Extensions.TryGetDynamicProperty<string>(info, "SubTitle", "");
            zone.TitleColor = Extensions.TryGetDynamicProperty<Color>(info, "TitleColor", Color.White);
            zone.TitleStroke = Extensions.TryGetDynamicProperty<Color>(info, "TitleStroke", Color.Black);
            zone.IconProvider = Extensions.TryGetDynamicProperty<IconSystem.IconProvider>(info, "Icon", null);
            zone.Priority = Extensions.TryGetDynamicProperty<int>(info, "Priority", 0);
            zone.OwnerName = Extensions.TryGetDynamicProperty<string>(info, "OwnerName", "");
            zone.Rect = Extensions.TryGetDynamicProperty<Rectangle>(info, "Rect", new Rectangle());

            if (zone.IconProvider == null)
            {
                var rawIconString = Extensions.TryGetDynamicProperty<string>(info, "Icon", null);

                zone.IconProvider = IconSystem.IconProvider.CreateFromRawString(rawIconString);
            }

            var errors = zone.CheckErrors().ToList();
            if (errors.Count > 0)
            {
                Logger.Error($"Failed to add zone. Errors: {string.Join(", ", errors)}");
            }
        }
        
        ZonesSystem.AddZone(zone);
    }
}
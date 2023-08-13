using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using ZoneTitles.Common;
using ZoneTitles.Common.Systems;

namespace ZoneTitles.Content.Items;

public class ZoneDesignator : ModItem
{
    public override void SetDefaults()
    {
        Item.width = 32;
        Item.height = 32;
        Item.rare = ItemRarityID.Purple;
        Item.useAnimation = 25;
        Item.useTurn = true;
        Item.useStyle = ItemUseStyleID.Swing;
        Item.UseSound = SoundID.Item1;
        Item.useTime = 0;
    }

    public override void HoldItem(Player player)
    {
        if (player.whoAmI == Main.myPlayer)
        {
            ZonesSystem.EnableEdit = true;
        }
    }

    public override bool CanUseItem(Player player) 
    {
        return (player.whoAmI == Main.myPlayer) ? !ZonesSystem.MouseOverControls : true;
    }

    public override bool? UseItem(Player player)
    {
        if (player.whoAmI == Main.myPlayer && Main.mouseLeft)
        {
            UISystem.CloseZoneSelector();

            var action = (Zone zone) =>
            {
                if (Terraria.GameInput.PlayerInput.GetPressedKeys().Contains(Keys.LeftAlt))
                {
                    ZonesSystem.RemoveZone(zone);
                }
                else
                {
                    UISystem.OpenZoneEditor(zone);
                }
            };

            List<Zone> zones = ZonesSystem.GetZonesAtTile(Main.MouseWorld.ToTileCoordinates());
            
            if (zones.Count > 0)
            {
                if (zones.Count == 1)
                {
                    action(zones[0]);
                }
                else
                {
                    zones.Sort((a, b) => b.Priority.CompareTo(a.Priority));
                    UISystem.OpenZoneSelector(zones, Main.MouseWorld, (zone) =>
                    {
                        action(zone);
                    });
                }
            }
        }

        return true;
    }

    public override bool AltFunctionUse(Player player)
    {
        if (player.whoAmI == Main.myPlayer)
        {
            Point MouseTilePosition = Main.MouseWorld.ToTileCoordinates();

            var zone = new Zone
            {
                OwnerName = Main.LocalPlayer.name,
                Rect = new Rectangle(MouseTilePosition.X, MouseTilePosition.Y, 1, 1)
            };
            
            ZonesSystem.AddZone(zone);
            ZonesSystem.StartDragBorders(zone, ZonesSystem.BorderFlag.Right | ZonesSystem.BorderFlag.Bottom, true);
        }
        
        return true;
    }

    public override void AddRecipes()
    {
        CreateRecipe()
            //.AddIngredient(ItemID.Wood)
            //.AddIngredient(ItemID.IronBar)
            .AddTile(TileID.WorkBenches)
            .Register();
    }
}
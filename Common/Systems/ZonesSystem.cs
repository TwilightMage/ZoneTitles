using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using ReLogic.Graphics;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace ZoneTitles.Common.Systems;

public class ZonesSystem : ModSystem
{
    [Flags]
    public enum BorderFlag
    {
        Left = 1,
        Right = 2,
        Top = 4,
        Bottom = 8
    }

    public static List<Zone> Zones = new List<Zone>();
    private static AABB _rootAABB;

    public static List<Zone> ZonesVisible = new List<Zone>();
    private static List<Zone> ZonesTrashed = new List<Zone>();

    private static LocalizedText _dragHintText = null;

    public static bool EnableEdit = true;
    public static bool MouseOverControls => _mouseOverBorder; // ... and more
    public static bool MouseDragControls => _mouseDragBorder; // ... and more

    private static bool _mouseOverBorder = false;
    private static bool _mouseDragBorder = false;

    public static Texture2D DashHor = null;
    public static Texture2D DashVer = null;
    public static Texture2D Dot = null;
    public static DynamicSpriteFont Font = null;

    private bool _wasMouseLeft = false;

    private class AABB
    {
        public Rectangle Rect;
        public Zone Zone;
        public AABB Left;
        public AABB Right;

        public void DrawDebug(SpriteBatch spriteBatch)
        {
            Vector2 positionOnScreen = new Vector2(Rect.X * 16f, Rect.Y * 16f) - Main.screenPosition;
            Vector2 sizeOnScreen = (new Vector2(Rect.Width * 16f, Rect.Height * 16f)) * Main.GameZoomTarget;
            Main.spriteBatch.Draw(TextureAssets.MagicPixel.Value, positionOnScreen, new Rectangle(0, 0, 1, 1), Zone == null ? Color.Blue * 0.05f : Color.Red * 0.1f, 0, Vector2.Zero, sizeOnScreen, SpriteEffects.None, 0);

            Left?.DrawDebug(spriteBatch);
            Right?.DrawDebug(spriteBatch);
        }
    }

    private static AABB BuildAABB(List<Zone> zones)
    {
        if (zones == null) return null;

        zones.Sort((a, b) => a.Rect.Center.X.CompareTo(b.Rect.Center.X));
        
        return BuildAABBSegment(zones, 0, zones.Count);
    }
    
    private static AABB BuildAABBSegment(List<Zone> zones, int segmentStart, int segmentSize)
    {
        if (segmentSize == 0) return null;

        if (segmentSize == 1) return new AABB { Rect = zones[segmentStart].Rect, Zone = zones[segmentStart] };

        int leftSize = segmentSize / 2;

        AABB left = BuildAABBSegment(Zones, segmentStart, leftSize);
        AABB right = BuildAABBSegment(Zones, segmentStart + leftSize, segmentSize - leftSize);

        Rectangle bounds = Rectangle.Union(left.Rect, right.Rect);

        return new AABB { Rect = bounds, Left = left, Right = right };
    }

    public static void AddZone(Zone zone)
    {
        AddZoneNoSync(zone);

        zone.SendAddOrUpdate();
    }

    public static void AddZoneNoSync(Zone zone)
    {
        Zones.Add(zone);

        RebuildAABB();

        if (zone.Rect.Intersects(GetCullRect()))
        {
            zone.DisplayState ??= new ZoneDisplayState();
            zone.DisplayState.InfoChanged(zone);
            ZonesVisible.Add(zone);
        }
    }

    public static void RemoveZone(Zone zone)
    {
        zone.SendRemove();

        RemoveZoneNoSync(zone);
    }

    public static void RemoveZoneNoSync(Zone zone)
    {
        zone.Trashed = true;
        ZonesTrashed.Add(zone);
    }

    public static void StartDragBorders(Zone zone, BorderFlag borders, bool RMB = false)
    {
        if (zone.DisplayState != null)
        {
            zone.DisplayState.DragLeft = borders.HasFlag(BorderFlag.Left);
            zone.DisplayState.DragRight = borders.HasFlag(BorderFlag.Right);
            zone.DisplayState.DragTop = borders.HasFlag(BorderFlag.Top);
            zone.DisplayState.DragBottom = borders.HasFlag(BorderFlag.Bottom);

            zone.DisplayState.DragWithRMB = RMB;

            _mouseDragBorder = zone.DisplayState.DragAnyBorder;
        }
    }

    public static Zone GetZoneById(long id)
    {
        return Zones.FirstOrDefault(zone => zone.Id == id);
    }

    public override void OnModLoad()
    {
        DashHor = ModContent.Request<Texture2D>("ZoneTitles/Assets/Textures/UI/ZoneBorderHor", AssetRequestMode.ImmediateLoad).Value;
        DashVer = ModContent.Request<Texture2D>("ZoneTitles/Assets/Textures/UI/ZoneBorderVer", AssetRequestMode.ImmediateLoad).Value;
        Dot = ModContent.Request<Texture2D>("ZoneTitles/Assets/Textures/UI/ZoneBorderAngle", AssetRequestMode.ImmediateLoad).Value;
        Font = FontAssets.MouseText.Value;
    }

    public override void OnModUnload()
    {
        DashHor = null;
        DashVer = null;
        Dot = null;
        Font = null;
    }

    public override void OnWorldLoad()
    {
        Zones = new List<Zone>();

        _dragHintText = Localize("DragHint");
    }

    public override void SaveWorldData(TagCompound tag)
    {
        if (Zones.Count != 0)
        {
            List<TagCompound> zonesTag = new List<TagCompound>();
            foreach (var zone in Zones)
            {
                if (zone.IsFresh) continue;

                TagCompound zoneTag = new TagCompound();
                zone.Save(zoneTag);
                zonesTag.Add(zoneTag);
            }

            tag["zones"] = zonesTag;
        }
    }

    public override void LoadWorldData(TagCompound tag)
    {
        List<Zone> newZones = new List<Zone>();
        foreach (var zoneTag in tag.GetList<TagCompound>("zones"))
        {
            Zone zone = new Zone();
            zone.Load(zoneTag);
            if (zone.CheckErrors().IsEmpty())
            {
                newZones.Add(zone);
            }
        }

        Zones = newZones;

        RebuildAABB();
    }

    public override void OnWorldUnload()
    {
        Zones = null;
    }

    public override void NetReceive(BinaryReader reader)
    {
        int zoneCount = reader.Read();

        for (int i = 0; i < zoneCount; i++)
        {
            long Id = reader.ReadInt64();
            Zone zone = GetZoneById(Id);
            bool isNew = false;
            if (zone == null)
            {
                zone = Zone.CreateWithId(Id);
                isNew = true;
            }

            zone.LoadBinaryRect(reader);
            zone.LoadBinaryVisual(reader);

            if (isNew)
            {
                AddZoneNoSync(zone);
            }
        }
    }

    public override void NetSend(BinaryWriter writer)
    {
        writer.Write(Zones.Count(zone => !zone.Trashed));
        
        foreach (var zone in Zones)
        {
            if (zone.Trashed) continue;
            
            writer.Write(zone.Id);
            zone.SaveBinaryRect(writer);
            zone.SaveBinaryVisual(writer);
        }
    }

    public static void RebuildAABB()
    {
        _rootAABB = BuildAABB(Zones);
    }

    public static void DrawAABBDebug(SpriteBatch spriteBatch)
    {
        _rootAABB?.DrawDebug(spriteBatch);
    }

    public static Zone GetZoneAtTile(Point tile)
    {
        Zone foundZone = null;

        if (_rootAABB != null)
        {
            Stack<AABB> stack = new Stack<AABB>(Zones.Count / 2);
            stack.Push(_rootAABB);
            while (stack.Count > 0)
            {
                var aabb = stack.Pop();
                if (aabb.Rect.Contains(tile))
                {
                    if (aabb.Zone != null)
                    {
                        if (foundZone == null || aabb.Zone.Priority > foundZone.Priority) foundZone = aabb.Zone;
                    }
                    else
                    {
                        if (aabb.Left != null) stack.Push(aabb.Left);
                        if (aabb.Right != null) stack.Push(aabb.Right);
                    }
                }
            }
        }

        return foundZone;
    }

    public static List<Zone> GetZonesAtTile(Point tile)
    {
        List<Zone> foundZones = new List<Zone>();

        if (_rootAABB != null)
        {
            Stack<AABB> stack = new Stack<AABB>(Zones.Count / 2);
            stack.Push(_rootAABB);
            while (stack.Count > 0)
            {
                var aabb = stack.Pop();
                if (aabb.Rect.Contains(tile))
                {
                    if (aabb.Zone != null)
                    {
                        foundZones.Add(aabb.Zone);
                    }
                    else
                    {
                        if (aabb.Left != null) stack.Push(aabb.Left);
                        if (aabb.Right != null) stack.Push(aabb.Right);
                    }
                }
            }
        }

        return foundZones;
    }

    public override void PreUpdateEntities()
    {
        if (ZonesTrashed.Count > 0)
        {
            foreach (var zone in ZonesTrashed)
            {
                Zones.Remove(zone);
                ZonesVisible.Remove(zone);
            }

            ZonesTrashed.Clear();

            RebuildAABB();
        }

        if (EnableEdit)
        {
            Point mousePosTile = Main.MouseWorld.ToTileCoordinates();
            float borderWidth = 4 * Main.GameZoomTarget;

            UpdateCulling();

            _mouseOverBorder = false;

            bool mouseInUI = Main.LocalPlayer.mouseInterface;

            foreach (var zone in ZonesVisible)
            {
                if (zone.DisplayState.DragAny && (zone.DisplayState.DragWithRMB ? Main.mouseRightRelease : Main.mouseLeftRelease))
                {
                    zone.DisplayState.ResetDrag();

                    _mouseDragBorder = false;

                    if (zone.IsFresh)
                    {
                        UISystem.OpenZoneEditor(zone);
                    }
                }

                zone.DisplayState.ResetHover();
                if (!MouseDragControls && !mouseInUI && zone.Rect.Contains(mousePosTile))
                {
                    int L = zone.Rect.Left << 4;
                    int R = zone.Rect.Right << 4;
                    int T = zone.Rect.Top << 4;
                    int B = zone.Rect.Bottom << 4;

                    zone.DisplayState.HoverLeft = Main.MouseWorld.X > L &&
                                                  Main.MouseWorld.X < L + borderWidth &&
                                                  Main.MouseWorld.Y > T &&
                                                  Main.MouseWorld.Y < B;

                    zone.DisplayState.HoverRight = Main.MouseWorld.X > R - borderWidth &&
                                                   Main.MouseWorld.X < R &&
                                                   Main.MouseWorld.Y > T &&
                                                   Main.MouseWorld.Y < B;

                    zone.DisplayState.HoverTop = Main.MouseWorld.X > L &&
                                                 Main.MouseWorld.X < R &&
                                                 Main.MouseWorld.Y > T &&
                                                 Main.MouseWorld.Y < T + borderWidth;

                    zone.DisplayState.HoverBottom = Main.MouseWorld.X > L &&
                                                    Main.MouseWorld.X < R &&
                                                    Main.MouseWorld.Y > B - borderWidth &&
                                                    Main.MouseWorld.Y < B;

                    _mouseOverBorder = zone.DisplayState.HoverAny;

                    if (Main.mouseLeft && !_wasMouseLeft)
                    {
                        zone.DisplayState.HoverToDrag();

                        _mouseDragBorder = zone.DisplayState.DragAnyBorder;
                    }
                }

                if (zone.DisplayState.DragLeft)
                {
                    zone.Left = (int)MathF.Round(Main.MouseWorld.X / 16);
                }

                if (zone.DisplayState.DragRight)
                {
                    zone.Right = (int)MathF.Round(Main.MouseWorld.X / 16);
                }

                if (zone.DisplayState.DragTop)
                {
                    zone.Top = (int)MathF.Round(Main.MouseWorld.Y / 16);
                }

                if (zone.DisplayState.DragBottom)
                {
                    zone.Bottom = (int)MathF.Round(Main.MouseWorld.Y / 16);
                }
            }

            EnableEdit = false;

            _wasMouseLeft = Main.mouseLeft;
        }
    }

    private void UpdateCulling()
    {
        Rectangle cullRect = GetCullRect();

        if (_rootAABB != null)
        {
            Stack<AABB> stack = new Stack<AABB>(Zones.Count / 2);
            stack.Push(_rootAABB);
            while (stack.Count > 0)
            {
                var aabb = stack.Pop();
                if (aabb.Zone != null)
                {
                    if (aabb.Rect.Intersects(cullRect))
                    {
                        if (aabb.Zone.DisplayState == null)
                        {
                            // culled to visible
                            ZonesVisible.Add(aabb.Zone);
                            aabb.Zone.DisplayState = new ZoneDisplayState();
                            aabb.Zone.DisplayState.InfoChanged(aabb.Zone);
                        }
                        else
                        {
                            // visible to visible
                        }
                    }
                    else
                    {
                        if (aabb.Zone.DisplayState == null)
                        {
                            // culled to culled
                        }
                        else
                        {
                            // visible to culled
                            ZonesVisible.Remove(aabb.Zone);
                            aabb.Zone.DisplayState = null;
                        }
                    }
                }
                else
                {
                    if (aabb.Left != null) stack.Push(aabb.Left);
                    if (aabb.Right != null) stack.Push(aabb.Right);
                }
            }
        }
        else
        {
            ZonesVisible.Clear();
        }
    }

    private static Rectangle GetCullRect()
    {
        Point screenPos = Main.screenPosition.ToTileCoordinates();
        return new Rectangle(screenPos.X, screenPos.Y, Main.ScreenSize.X / 16 + 1, Main.ScreenSize.Y / 16 + 1);
    }

    public override void UpdateUI(GameTime gameTime)
    {
        if (_mouseOverBorder || MouseDragControls) Main.instance.MouseText(_dragHintText.Value, "", 8, hackedMouseX: Main.mouseX + 6, hackedMouseY: Main.mouseY + 6, noOverride: true);
    }

    public override void PostDrawInterface(SpriteBatch spriteBatch)
    {
        //spriteBatch.DrawString(FontAssets.MouseText.Value, $"Zone count: {Zones.Count}", new Vector2(700, 100), Color.White);
    }

    public override void PostDrawTiles()
    {
        //Main.spriteBatch.Begin();
        //DrawAABBDebug(Main.spriteBatch);
        //Main.spriteBatch.End();

        if (EnableEdit)
        {
            Main.spriteBatch.Begin();

            foreach (var zone in ZonesVisible)
            {
                zone.DisplayState.Draw(Main.spriteBatch, zone);
            }

            Main.spriteBatch.End();
        }
    }

    public static LocalizedText Localize(string key) => Language.GetText($"Mods.ZoneTitles.UI.Editor.{key}");
}
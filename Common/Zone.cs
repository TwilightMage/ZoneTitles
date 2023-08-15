using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Graphics;
using System;
using System.Collections.Generic;
using System.IO;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;
using ZoneTitles.Common.Systems;
using ZoneTitles.Common.UI;

namespace ZoneTitles.Common;

public class Zone
{
    public static Zone CreateWithId(long id)
    {
        return new Zone { Id = id, ZoneId = $"zone_{id}" };
    }

    private static ulong _versionGenerator = 0;

    public Rectangle Rect
    {
        get => _rect;
        set
        {
            if (_rect == value) return;
            _rect = value;
            if (_rect.Width == 0) _rect.Width = 1;
            if (_rect.Height == 0) _rect.Height = 1;
            
            SendRect();
            ZonesSystem.RebuildAABB();
        }
    }

    public int Left
    {
        get => _rect.Left;
        set
        {
            if (_rect.Left == value) return;
            if (value > _rect.Right)
            {
                _rect.Width = value - _rect.Right;
                _rect.X = value - _rect.Width;

                if (DisplayState != null && DisplayState.DragLeft)
                {
                    DisplayState.DragLeft = false;
                    DisplayState.DragRight = true;
                }

                SendRect();
                ZonesSystem.RebuildAABB();
            }
            else if (value < _rect.Right)
            {
                int delta = value - _rect.Left;
                _rect.X += delta;
                _rect.Width -= delta;
                
                SendRect();
                ZonesSystem.RebuildAABB();
            }
        }
    }

    public int Right
    {
        get => _rect.Right;
        set
        {
            if (_rect.Right == value) return;
            if (value > _rect.Left)
            {
                _rect.Width = value - _rect.Left;
                
                SendRect();
                ZonesSystem.RebuildAABB();
            }
            else if (value < _rect.Left)
            {
                _rect.Width = _rect.Left - value;
                _rect.X = value;

                if (DisplayState != null && DisplayState.DragRight)
                {
                    DisplayState.DragRight = false;
                    DisplayState.DragLeft = true;
                }

                SendRect();
                ZonesSystem.RebuildAABB();
            }
        }
    }

    public int Top
    {
        get => _rect.Top;
        set
        {
            if (_rect.Top == value) return;
            if (value > _rect.Bottom)
            {
                _rect.Height = value - _rect.Bottom;
                _rect.Y = value - _rect.Height;

                if (DisplayState != null && DisplayState.DragTop)
                {
                    DisplayState.DragTop = false;
                    DisplayState.DragBottom = true;
                }

                SendRect();
                ZonesSystem.RebuildAABB();
            }
            else if (value < _rect.Bottom)
            {
                int delta = value - _rect.Top;
                _rect.Y += delta;
                _rect.Height -= delta;
                
                SendRect();
                ZonesSystem.RebuildAABB();
            }
        }
    }

    public int Bottom
    {
        get => _rect.Bottom;
        set
        {
            if (_rect.Bottom == value) return;
            if (value > _rect.Top)
            {
                _rect.Height = value - _rect.Top;
                
                SendRect();
                ZonesSystem.RebuildAABB();
            }
            else if (value < _rect.Top)
            {
                _rect.Height = _rect.Top - value;
                _rect.Y = value;

                if (DisplayState != null && DisplayState.DragBottom)
                {
                    DisplayState.DragBottom = false;
                    DisplayState.DragTop = true;
                }

                SendRect();
                ZonesSystem.RebuildAABB();
            }
        }
    }

    private Rectangle _rect;

    public IconSystem.IconProvider IconProvider;

    public long Id { get; private set; } = 0;
    public bool IsFresh => Id == 0;
    public string ZoneId { get; private set; } = null;
    public string OwnerName = "";
    
    public int Priority
    {
        get => _priority;
        set
        {
            _priority = value;
            DisplayState?.InfoChanged(this);
        }
    }

    public string Title
    {
        get => _title;
        set
        {
            _title = value;
            DisplayState?.InfoChanged(this);
        }
    }
    
    public string SubTitle = "";
    public Color TitleColor = Color.White;
    public Color TitleStroke = Color.Black;
    public Texture2D Icon => IconProvider?.GetTexture();

    public ZoneDisplayState DisplayState;
    public bool Trashed;

    private string _title = "New Zone";
    private int _priority = 0;

    public void Initialize()
    {
        if (IsFresh)
        {
            Id = DateTime.Now.Ticks;
            ZoneId = $"zone_{Id}";
            SendAddOrUpdate();
        }
    }

    public void SetRectSilent(Rectangle rect) => _rect = rect;

    public static string LocalizeError(string error) => Language.GetTextValue($"Mods.ZoneTitles.ZoneError.{error}");

    public void Save(TagCompound tag)
    {
        tag["id"] = Id;
        tag["rect"] = Rect;
        tag["owner"] = OwnerName;
        tag["priority"] = Priority;
        tag["title"] = Title;
        tag["subtitle"] = SubTitle;
        tag["color"] = TitleColor;
        tag["stroke"] = TitleStroke;

        if (IconProvider != null)
        {
            var iconTag = new TagCompound();
            IconProvider.Serialize(iconTag);
            tag["icon"] = iconTag;
        }
    }

    public void Load(TagCompound tag)
    {
        Id = tag.Get<long>("id");
        ZoneId = $"zone_{Id}";
        _rect = tag.Get<Rectangle>("rect");
        OwnerName = tag.Get<string>("owner");
        Priority = tag.Get<int>("priority");
        Title = tag.Get<string>("title");
        SubTitle = tag.Get<string>("subtitle");
        TitleColor = tag.Get<Color>("color");
        TitleStroke = tag.Get<Color>("stroke");

        TagCompound iconTag = tag.Get<TagCompound>("icon");
        if (iconTag != null)
        {
            IconProvider = IconSystem.IconProvider.CreateFromTag(iconTag);
        }
    }

    public void SendAddOrUpdate(int to = -1, int skip = -1)
    {
        if (IsFresh || Main.netMode == NetmodeID.SinglePlayer) return;

        ModPacket packet = ZoneTitlesMod.Instance.GetPacket();
        packet.Write((byte)ZoneTitlesMod.MessageType.AddOrUpdateZone);
        packet.Write(Id);
        SaveBinaryRect(packet);
        SaveBinaryVisual(packet);
        packet.Send(to, skip);
    }

    public void SendRect(int to = -1, int skip = -1)
    {
        if (IsFresh || Main.netMode == NetmodeID.SinglePlayer) return;

        ModPacket packet = ZoneTitlesMod.Instance.GetPacket();
        packet.Write((byte)ZoneTitlesMod.MessageType.ChangeZoneRect);
        packet.Write(Id);
        SaveBinaryRect(packet);
        packet.Send(to, skip);
    }

    public void SendVisualData(int to = -1, int skip = -1)
    {
        if (IsFresh || Main.netMode == NetmodeID.SinglePlayer) return;

        ModPacket packet = ZoneTitlesMod.Instance.GetPacket();
        packet.Write((byte)ZoneTitlesMod.MessageType.ChangeZoneVisual);
        packet.Write(Id);
        SaveBinaryVisual(packet);
        packet.Send(to, skip);
    }

    public void SendRemove(int to = -1, int skip = -1)
    {
        if (IsFresh || Main.netMode == NetmodeID.SinglePlayer) return;

        ModPacket packet = ZoneTitlesMod.Instance.GetPacket();
        packet.Write((byte)ZoneTitlesMod.MessageType.RemoveZone);
        packet.Write(Id);
        packet.Send(to, skip);
    }

    public void SaveBinaryRect(BinaryWriter writer)
    {
        writer.WriteRect(Rect);
    }

    public void SaveBinaryVisual(BinaryWriter writer)
    {
        writer.Write(OwnerName);
        writer.Write(Priority);
        writer.Write(Title);
        writer.Write(SubTitle);
        writer.WriteRGB(TitleColor);
        writer.WriteRGB(TitleStroke);

        if (IconProvider != null)
        {
            writer.Write(true);
            IconProvider?.SerializeBinary(writer);
        }
        else
        {
            writer.Write(false);
        }
    }

    public void LoadBinaryRect(BinaryReader reader)
    {
        _rect = reader.ReadRect();
    }

    public void LoadBinaryVisual(BinaryReader reader)
    {
        OwnerName = reader.ReadString();
        Priority = reader.ReadInt32();
        Title = reader.ReadString();
        SubTitle = reader.ReadString();
        TitleColor = reader.ReadRGB();
        TitleStroke = reader.ReadRGB();

        if (reader.ReadBoolean())
        {
            IconProvider = IconSystem.IconProvider.CreateFromBinary(reader);
        }
    }

    public IEnumerable<string> CheckErrors()
    {
        if (Rect.Width == 0 || Rect.Height == 0) yield return LocalizeError("InvalidRect");

        if (string.IsNullOrWhiteSpace(OwnerName)) yield return LocalizeError("InvalidOwner");

        if (string.IsNullOrWhiteSpace(Title)) yield return LocalizeError("InvalidTitle");
    }
}

public class ZoneDisplayState
{
    public bool HoverLeft;
    public bool HoverRight;
    public bool HoverTop;
    public bool HoverBottom;
    public bool DragLeft;
    public bool DragRight;
    public bool DragTop;
    public bool DragBottom;
    public bool DragWithRMB;
    
    private Vector2 _displayTitlePosition;
    private Vector2 _displayPriorityPosition;
    private Vector2 _displayInfoSize;
    
    public bool HoverAnyBorder => HoverLeft || HoverRight || HoverTop || HoverBottom;
    public bool DragAnyBorder => DragLeft || DragRight || DragTop || DragBottom;

    public bool HoverAny => HoverAnyBorder;
    public bool DragAny => DragAnyBorder;

    public bool HoverDragLeft => HoverLeft || DragLeft;
    public bool HoverDragRight => HoverRight || DragRight;
    public bool HoverDragTop => HoverTop || DragTop;
    public bool HoverDragBottom => HoverBottom || DragBottom;

    public void ResetHover()
    {
        HoverLeft = HoverRight = HoverTop = HoverBottom = false;
    }

    public void HoverToDrag()
    {
        DragLeft = HoverLeft;
        DragRight = HoverRight;
        DragTop = HoverTop;
        DragBottom = HoverBottom;
    }

    public void ResetDrag()
    {
        DragLeft = DragRight = DragTop = DragBottom = false;
        DragWithRMB = false;
    }

    public void InfoChanged(Zone zone)
    {
        // Calculate item sizes
        var displayTitleSize = ZonesSystem.Font.MeasureString(zone.Title);
        var displayPrioritySize = zone.Priority != 0 ? ZonesSystem.Font.MeasureString($"{zone.Priority:+#;-#;+0}") : Vector2.Zero;

        // Calculate container size
        _displayInfoSize = displayTitleSize;
        
        if (zone.Priority != 0)
        {
            _displayInfoSize.X = MathF.Max(_displayInfoSize.X, displayPrioritySize.X);
            _displayInfoSize.Y += displayPrioritySize.Y - 10;
        }
        
        // Calculate item positions
        _displayTitlePosition = new Vector2(0, (_displayInfoSize.X - displayTitleSize.X) / 2);

        if (zone.Priority != 0)
        {
            _displayPriorityPosition = new Vector2((_displayInfoSize.X - displayPrioritySize.X) / 2, displayTitleSize.Y - 10);
        }
    }

    public void Draw(SpriteBatch spriteBatch, Zone zone)
    {
        Texture2D pixel = TextureAssets.MagicPixel.Value;
                
        Rectangle pixelRect      = new Rectangle(0, 0, 1, 1);
        Rectangle dashHorRect    = new Rectangle(0, 0, 18, 6);
        Rectangle dashHorRectAct = new Rectangle(0, 6, 18, 6);
        Rectangle dashVerRect    = new Rectangle(0, 0, 6, 18);
        Rectangle dashVerRectAct = new Rectangle(6, 0, 6, 18);
        Rectangle dotRect        = new Rectangle(0, 0, 10, 10);
        Rectangle dotRectAct     = new Rectangle(10, 0, 10, 10);
        Vector2 scaleVector = new Vector2(Main.GameZoomTarget, Main.GameZoomTarget);
        float scaledBorderWidth = 4 * Main.GameZoomTarget;
        float scaledBorderHalfWidth = 2 * Main.GameZoomTarget;
        
        float accent = UISystem.ZoneEditor.ZonePanel?.TargetZone == zone ? MathF.Sin((float)Main.time * MathF.PI / 15) * 0.25f + 0.75f : 1f;
        
        Vector2 positionOnScreen = new Vector2(zone.Rect.X * 16f, zone.Rect.Y * 16f) - Main.screenPosition;
        Vector2 sizeOnScreen = (new Vector2(zone.Rect.Width * 16f, zone.Rect.Height * 16f)) * Main.GameZoomTarget;
        
        ZoneUtils.ModifyPositionXByZoom(ref positionOnScreen.X);
        ZoneUtils.ModifyPositionYByZoom(ref positionOnScreen.Y);

        // Draw fill
        Color fillColor = zone.TitleColor * 0.1f * accent;

        Main.spriteBatch.Draw(pixel, positionOnScreen, pixelRect, fillColor, 0, Vector2.Zero, sizeOnScreen, SpriteEffects.None, 0);
        
        // Draw border
        Color borderColor = Color.White * accent;

        Vector2 dashOrigin = new Vector2(9, 3);
        Vector2 dashOffset = new Vector2(8, 2) * Main.GameZoomTarget;
        Rectangle dashRect;

        dashRect = zone.DisplayState.HoverDragTop ? dashHorRectAct : dashHorRect;
        for (int i = 0; i < zone.Rect.Width; i += 2)
        {
            Main.spriteBatch.Draw(ZonesSystem.DashHor, positionOnScreen + new Vector2(16 * i * Main.GameZoomTarget, 0) + dashOffset, dashRect, borderColor, MathF.Sin((zone.Rect.X + i + zone.Rect.Y) * 16f * 45) * 0.1f, dashOrigin, scaleVector, SpriteEffects.None, 0);
        }

        dashRect = zone.DisplayState.HoverDragBottom ? dashHorRectAct : dashHorRect;
        for (int i = 0; i < zone.Rect.Width; i += 2)
        {
            Main.spriteBatch.Draw(ZonesSystem.DashHor, positionOnScreen + new Vector2(16 * i * Main.GameZoomTarget, sizeOnScreen.Y - scaledBorderWidth) + dashOffset, dashRect, borderColor, MathF.Sin((zone.Rect.X + i + zone.Rect.Y + zone.Rect.Height) * 16f * 45) * 0.1f, dashOrigin, scaleVector, SpriteEffects.None, 0);
        }

        dashOrigin = new Vector2(3, 9);
        dashOffset = new Vector2(2, 8) * Main.GameZoomTarget;

        dashRect = zone.DisplayState.HoverDragLeft ? dashVerRectAct : dashVerRect;
        for (int i = 0; i < zone.Rect.Height; i += 2)
        {
            Main.spriteBatch.Draw(ZonesSystem.DashVer, positionOnScreen + new Vector2(0, 16 * i * Main.GameZoomTarget) + dashOffset, dashRect, borderColor, MathF.Sin((zone.Rect.X + i + zone.Rect.Y + 100) * 16f * 45) * 0.1f, dashOrigin, scaleVector, SpriteEffects.None, 0);
        }

        dashRect = zone.DisplayState.HoverDragRight ? dashVerRectAct : dashVerRect;
        for (int i = 0; i < zone.Rect.Height; i += 2)
        {
            Main.spriteBatch.Draw(ZonesSystem.DashVer, positionOnScreen + new Vector2(sizeOnScreen.X - scaledBorderWidth, 16 * i * Main.GameZoomTarget) + dashOffset, dashRect, borderColor, MathF.Sin((zone.Rect.X + i + zone.Rect.Y + 100 + zone.Rect.Height) * 16f * 45) * 0.1f, dashOrigin, scaleVector, SpriteEffects.None, 0);
        }

        Vector2 dotOrigin = new Vector2(5, 5);
        Main.spriteBatch.Draw(ZonesSystem.Dot, positionOnScreen + new Vector2(scaledBorderHalfWidth, scaledBorderHalfWidth), zone.DisplayState.HoverDragLeft || zone.DisplayState.HoverDragTop ? dotRectAct : dotRect, borderColor, 0, dotOrigin, scaleVector, SpriteEffects.None, 0);
        Main.spriteBatch.Draw(ZonesSystem.Dot, positionOnScreen + new Vector2(sizeOnScreen.X - scaledBorderHalfWidth, scaledBorderHalfWidth), zone.DisplayState.HoverDragRight || zone.DisplayState.HoverDragTop ? dotRectAct : dotRect, borderColor, 0, dotOrigin, scaleVector, SpriteEffects.None, 0);
        Main.spriteBatch.Draw(ZonesSystem.Dot, positionOnScreen + new Vector2(scaledBorderHalfWidth, sizeOnScreen.Y - scaledBorderHalfWidth), zone.DisplayState.HoverDragLeft || zone.DisplayState.HoverDragBottom ? dotRectAct : dotRect, borderColor, 0, dotOrigin, scaleVector, SpriteEffects.None, 0);
        Main.spriteBatch.Draw(ZonesSystem.Dot, positionOnScreen + new Vector2(sizeOnScreen.X - scaledBorderHalfWidth, sizeOnScreen.Y - scaledBorderHalfWidth), zone.DisplayState.HoverDragRight || zone.DisplayState.HoverDragBottom ? dotRectAct : dotRect, borderColor, 0, dotOrigin, scaleVector, SpriteEffects.None, 0);

        // Draw info
        Vector2 infoPosition = positionOnScreen + new Vector2((sizeOnScreen.X - _displayInfoSize.X * Main.GameZoomTarget) / 2, (sizeOnScreen.Y - _displayInfoSize.Y * Main.GameZoomTarget) / 2);
        
        Vector2 titlePosition = infoPosition + _displayTitlePosition * Main.GameZoomTarget;
        Main.spriteBatch.DrawString(ZonesSystem.Font, zone.Title, titlePosition + new Vector2(+1, 0), zone.TitleStroke, 0f, Vector2.Zero, Main.GameZoomTarget, SpriteEffects.None, 0);
        Main.spriteBatch.DrawString(ZonesSystem.Font, zone.Title, titlePosition + new Vector2(-1, 0), zone.TitleStroke, 0f, Vector2.Zero, Main.GameZoomTarget, SpriteEffects.None, 0);
        Main.spriteBatch.DrawString(ZonesSystem.Font, zone.Title, titlePosition + new Vector2(0, +1), zone.TitleStroke, 0f, Vector2.Zero, Main.GameZoomTarget, SpriteEffects.None, 0);
        Main.spriteBatch.DrawString(ZonesSystem.Font, zone.Title, titlePosition + new Vector2(0, -1), zone.TitleStroke, 0f, Vector2.Zero, Main.GameZoomTarget, SpriteEffects.None, 0);
        Main.spriteBatch.DrawString(ZonesSystem.Font, zone.Title, titlePosition, zone.TitleColor, 0f, Vector2.Zero, Main.GameZoomTarget, SpriteEffects.None, 0);

        if (zone.Priority != 0)
        {
            string priorityText = $"{zone.Priority:+#;-#;+0}";
            Vector2 priorityPosition = infoPosition + _displayPriorityPosition * Main.GameZoomTarget;
            Main.spriteBatch.DrawString(ZonesSystem.Font, priorityText, priorityPosition + new Vector2(+1, 0), zone.TitleStroke, 0f, Vector2.Zero, Main.GameZoomTarget, SpriteEffects.None, 0);
            Main.spriteBatch.DrawString(ZonesSystem.Font, priorityText, priorityPosition + new Vector2(-1, 0), zone.TitleStroke, 0f, Vector2.Zero, Main.GameZoomTarget, SpriteEffects.None, 0);
            Main.spriteBatch.DrawString(ZonesSystem.Font, priorityText, priorityPosition + new Vector2(0, +1), zone.TitleStroke, 0f, Vector2.Zero, Main.GameZoomTarget, SpriteEffects.None, 0);
            Main.spriteBatch.DrawString(ZonesSystem.Font, priorityText, priorityPosition + new Vector2(0, -1), zone.TitleStroke, 0f, Vector2.Zero, Main.GameZoomTarget, SpriteEffects.None, 0);
            Main.spriteBatch.DrawString(ZonesSystem.Font, priorityText, priorityPosition, zone.TitleColor, 0f, Vector2.Zero, Main.GameZoomTarget, SpriteEffects.None, 0);
        }
    }
}
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.IO;
using System.Reflection;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.UI;

namespace ZoneTitles;

public static class Extensions
{
    private static SpriteBatch _toolBatch;
    
    public static bool IsEmpty<T>(this IEnumerable<T> collection)
    {
        foreach (T item in collection)
        {
            return false;
        }

        return true;
    }

    public static Rectangle ReadRect(this BinaryReader reader)
    {
        return new Rectangle(reader.ReadInt32(), reader.ReadInt32(), reader.ReadInt32(), reader.ReadInt32());
    }

    public static void WriteRect(this BinaryWriter writer, Rectangle rect)
    {
        writer.Write(rect.X);
        writer.Write(rect.Y);
        writer.Write(rect.Width);
        writer.Write(rect.Height);
    }

    public static void SetRect(this UIElement element, Rectangle rect)
    {
        element.Left.Set(rect.X, 0);
        element.Top.Set(rect.Y, 0);
        element.Width.Set(rect.Width, 0);
        element.Height.Set(rect.Height, 0);
    }
    
    public static T TryGetDynamicProperty<T>(dynamic obj, string propertyName, T defaultValue = default)
    {
        if (obj is ExpandoObject)
        {
            if (((IDictionary<string, object>)obj).TryGetValue(propertyName, out object value))
                if (value is T castedDictValue)
                    return castedDictValue;

            return defaultValue;
        }

        if (obj.GetType().GetField(propertyName)?.GetValue(obj) is T castedFieldValue) return castedFieldValue;
        if (obj.GetType().GetProperty(propertyName)?.GetValue(obj) is T castedPropertyValue) return castedPropertyValue;

        return defaultValue;
    }
    
    public static T HackGetFieldValue<T>(this object target, string fieldName, T defaultValue = default)
    {
        var fieldInfo = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        return fieldInfo?.FieldType == typeof(T) ? (T)fieldInfo.GetValue(target) : defaultValue;
    }

    public static KeyValuePair<string, string> FirstWord(this string target)
    {
        int spaceIndex = target.IndexOf(' ');
        if (spaceIndex == -1) return new KeyValuePair<string, string>(target, null);
        return new KeyValuePair<string, string>(target.Substring(0, spaceIndex), target.Substring(spaceIndex + 1));
    }
    
    public static Texture2D ClipRect(this Texture2D src, Rectangle rect)
    {
        Texture2D tex = new Texture2D(src.GraphicsDevice, rect.Width, rect.Height);
        int count = rect.Width * rect.Height;
        Color[] data = new Color[count];
        src.GetData(0, rect, data, 0, count);
        tex.SetData(data);
        return tex;
    }

    public static Texture2D Resize(this Texture2D src, Point newSize)
    {
        GraphicsDevice graphicsDevice = Main.graphics.GraphicsDevice;

        if (_toolBatch == null)
        {
            _toolBatch = new SpriteBatch(graphicsDevice);
        }

        RenderTarget2D renderTarget = new RenderTarget2D(graphicsDevice, newSize.X, newSize.Y, true, SurfaceFormat.Color, DepthFormat.Depth24);

        Rectangle destinationRectangle = new Rectangle(0, 0, newSize.X, newSize.Y);

        var savedRenderTargets = graphicsDevice.GetRenderTargets();
        graphicsDevice.SetRenderTarget(renderTarget);
        graphicsDevice.Clear(Color.Transparent);

        _toolBatch.Begin();
        _toolBatch.Draw(src, destinationRectangle, Color.White);
        _toolBatch.End();

        graphicsDevice.SetRenderTargets(savedRenderTargets);

        return renderTarget;
    }

    public static void DrawSimple(this Item item, SpriteBatch spriteBatch, Vector2? center = null, float userScale = 1, int? frame = null)
    {
        Main.instance.LoadItem(item.type);
        
        Texture2D itemTexture = TextureAssets.Item[item.type].Value;
        Rectangle animationFrame = itemTexture.Frame();
        Color drawColor = Color.White;
        float essScale = 1;
        
        var animation = Main.itemAnimations[item.type];
        if (animation != null)
        {
            var savedFrame = animation.Frame;

            if (frame.HasValue)
            {
                animation.Frame = frame.Value;
            }

            animationFrame = animation.GetFrame(itemTexture);

            animation.Frame = savedFrame;
        }
        
        ItemSlot.GetItemLight(ref drawColor, ref essScale, item);
        
        float scaleToFit = animationFrame.Width > 32 || animationFrame.Height > 32 
            ? animationFrame.Width <= animationFrame.Height ? 32f / animationFrame.Height : 32f / animationFrame.Width 
            : 1;
        float totalScale = scaleToFit * userScale;
        Vector2 position = center.HasValue ? center.Value - (animationFrame.Size() * totalScale / 2) : Vector2.Zero;
        if (ItemLoader.PreDrawInInventory(item, spriteBatch, position, animationFrame, item.GetAlpha(drawColor), item.GetColor(drawColor), Vector2.Zero, totalScale))
        {
            spriteBatch.Draw(itemTexture, position, animationFrame, item.GetAlpha(drawColor), 0.0f, Vector2.Zero, totalScale, SpriteEffects.None, 0.0f);
        }
        ItemLoader.PostDrawInInventory(item, spriteBatch, position, animationFrame, item.GetAlpha(drawColor), item.GetColor(drawColor), Vector2.Zero, totalScale);
        if (ItemID.Sets.TrapSigned[item.type])
            spriteBatch.Draw(TextureAssets.Wire.Value, position + new Vector2(30f, 26f) * userScale, new Rectangle(4, 58, 8, 8), drawColor, 0.0f, new Vector2(4f), 1f, SpriteEffects.None, 0.0f);
    }

    public static Texture2D BakeToTexture(this Item item, int frame)
    {
        Main.instance.LoadItem(item.type);
        
        Texture2D itemTexture = TextureAssets.Item[item.type].Value;
        Rectangle animationFrame = itemTexture.Frame();
        
        var animation = Main.itemAnimations[item.type];
        if (animation != null)
        {
            var savedFrame = animation.Frame;

            animation.Frame = frame;

            animationFrame = animation.GetFrame(itemTexture);

            animation.Frame = savedFrame;
        }

        Point textureSize = animationFrame.Size().ToPoint();
        float scale = animationFrame.Width > 32 || animationFrame.Height > 32
            ? animationFrame.Width <= animationFrame.Height ? 32f / animationFrame.Height : 32f / animationFrame.Width
            : 1;
        
        textureSize.X = (int)(textureSize.X * scale);
        textureSize.Y = (int)(textureSize.Y * scale);
        
        GraphicsDevice graphicsDevice = Main.graphics.GraphicsDevice;
        
        if (_toolBatch == null)
        {
            _toolBatch = new SpriteBatch(graphicsDevice);
        }
        
        RenderTarget2D renderTarget = new RenderTarget2D(graphicsDevice, textureSize.X, textureSize.Y, true, SurfaceFormat.Color, DepthFormat.Depth24);
        
        var savedRenderTargets = graphicsDevice.GetRenderTargets();
        graphicsDevice.SetRenderTarget(renderTarget);
        
        graphicsDevice.Clear(Color.Transparent);

        _toolBatch.Begin();
        item.DrawSimple(_toolBatch, frame: frame);
        _toolBatch.End();
        
        graphicsDevice.SetRenderTargets(savedRenderTargets);

        return renderTarget;
    }

    public static void SaveToFile(this Texture2D texture, string path)
    {
        using (var stream = File.OpenWrite(path))
        {
            texture.SaveAsPng(stream, texture.Width, texture.Height);
        }
    }
}
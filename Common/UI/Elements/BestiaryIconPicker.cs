using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using System.IO;
using System.Linq;
using Terraria;
using Terraria.GameContent.Bestiary;
using Terraria.GameContent.UI.Elements;
using Terraria.Localization;
using Terraria.ModLoader.IO;
using Terraria.UI;
using ZoneTitles.Common.Systems;
using ZoneTitles.Common.UI.Inputs;
using ZoneTitles.Common.UI.Views;

namespace ZoneTitles.Common.UI.Elements;

[IconProvider(SourceMarker = "bestiary")]
public class BestiaryIconProvider : IconSystem.IconProvider
{
    private Texture2D _sourceTexture;
    private Rectangle _sourceFrame;
    private Texture2D _cachedClippedTexture;
    private string _cachedName;
    private string _key;

    public override Texture2D GetTexture()
    {
        if (_cachedClippedTexture == null && _sourceTexture != null)
        {
            _cachedClippedTexture = _sourceTexture.ClipRect(_sourceFrame);
        }

        return _cachedClippedTexture;
    }

    public override string GetName()
    {
        return _cachedName;
    }

    public override void Serialize(TagCompound tag)
    {
        base.Serialize(tag);

        tag.Set("key", _key);
    }

    public override void Deserialize(TagCompound tag)
    {
        _cachedClippedTexture = null;
        _cachedName = null;
        _sourceTexture = null;

        _key = tag.Get<string>("key");

        ApplyFromFilter(Main.BestiaryDB.Filters.FirstOrDefault(filter => filter.GetDisplayNameKey() == _key));
    }

    public override void SerializeBinary(BinaryWriter writer)
    {
        base.SerializeBinary(writer);

        writer.Write(_key);
    }

    public override void DeserializeBinary(BinaryReader reader)
    {
        _cachedClippedTexture = null;
        _cachedName = null;
        _sourceTexture = null;

        _key = reader.ReadString();

        ApplyFromFilter(Main.BestiaryDB.Filters.FirstOrDefault(filter => filter.GetDisplayNameKey() == _key));
    }

    public override void DeserializeBinaryFake(BinaryReader reader)
    {
        reader.ReadString();
    }

    public override void Draw(SpriteBatch spriteBatch, Vector2 position)
    {
        if (_sourceTexture != null)
        {
            spriteBatch.Draw(_sourceTexture, position, _sourceFrame, Color.White, 0, new Vector2(_sourceFrame.Width / 2f, _sourceFrame.Height / 2f), Vector2.One, SpriteEffects.None, 0);
        }
    }

    private static string IdFromFilter(IBestiaryEntryFilter filter)
    {
        return $"bestiary_{filter.GetDisplayNameKey().GetHashCode()}";
    }

    private void ApplyFromFilter(IBestiaryEntryFilter filter)
    {
        if (filter != null)
        {
            _id = IdFromFilter(filter);
            _key = filter.GetDisplayNameKey();
            _cachedName = Language.GetTextValue(filter.GetDisplayNameKey());

            var image = filter.GetImage();
            if (image is UIImageFramed)
            {
                _sourceTexture = image.HackGetFieldValue<Asset<Texture2D>>("_texture")?.Value;
                _sourceFrame = image.HackGetFieldValue<Rectangle>("_frame");
            }
            else if (image is UIImage)
            {
                _sourceTexture = image.HackGetFieldValue<Asset<Texture2D>>("_texture")?.Value;
                _sourceFrame = new Rectangle(0, 0, _sourceTexture?.Width ?? 0, _sourceTexture?.Height ?? 0);
            }
        }
    }

    public static BestiaryIconProvider CreateFromFilter(IBestiaryEntryFilter filter)
    {
        if (filter != null)
        {
            string id = IdFromFilter(filter);

            BestiaryIconProvider instance = (BestiaryIconProvider)GetProviderInstance(id);

            if (instance == null)
            {
                instance = new BestiaryIconProvider();
                instance.ApplyFromFilter(filter);
                RegisterInstance(instance);
            }

            return instance;
        }

        return null;
    }
}

public class BestiaryIconPicker : IconPickerMenu
{
    public BestiaryIconPicker()
    {
        ClickTransparent = true;
    }

    public override void OnInitialize()
    {
        base.OnInitialize();

        for (int i = 0; i < Main.BestiaryDB.Filters.Count; i++)
        {
            var iconButton = new IconButton();
            iconButton.Icon = BestiaryIconProvider.CreateFromFilter(Main.BestiaryDB.Filters[i]);
            iconButton.Left.Set(CalculateXInGrid(i, NumColumns, 40, ColumnSpace), 0);
            iconButton.Top.Set(CalculateYInGrid(i, NumColumns, 40, ColumnSpace), 0);
            iconButton.OnClick += IconClicked;
            Append(iconButton);
        }
    }

    private void IconClicked(UIMouseEvent evt, UIElement listeningElement)
    {
        if (evt.Target is IconButton button)
        {
            SelectIcon(button.Icon);
        }
    }

    public override Point GetDesiredSize() => new Point(CalculateSizeForGrid(NumColumns, 40, ColumnSpace), CalculateHeightForGrid(Main.BestiaryDB.Filters.Count, NumColumns, 40, ColumnSpace));
    public override bool HasDesiredSize() => true;
}
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using ReLogic.Graphics;
using System;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.UI.Chat;

namespace ZoneTitles.Common.UI.Inputs;

public class NumberSpinner : Widget, IInputControl<int>
{
    private Texture2D _background;
    private string _displayText;
    private DynamicSpriteFont _font;
    private Vector2 _displayTextSize;
    
    public Func<int, string> DisplayGenerator = (val) => val.ToString();
    
    public int Value
    {
        get => _value;
        set {
            _value = value;
            ValueChanged();
        }
    }
    private int _value = 0;

    public event Action<int> OnValueChanged; 

    public NumberSpinner()
    {
        MinHeight.Set(30, 0);
        
        _background = Main.Assets.Request<Texture2D>("Images/UI/CharCreation/PanelGrayscale", AssetRequestMode.ImmediateLoad).Value;
        _font = FontAssets.MouseText.Value;
    }

    public override void OnInitialize()
    {
        var lessButton = new IconTextButton(ModContent.Request<Texture2D>("ZoneTitles/Assets/Textures/UI/Less", AssetRequestMode.ImmediateLoad).Value);
        lessButton.Width.Set(30, 0);
        lessButton.Height.Set(30, 0);
        lessButton.HAlign = 0;
        lessButton.SetPadding(0);
        lessButton.OnClick += (evt, elem) =>
        {
            Value--;
            ValueChanged();
        };
        Append(lessButton);
        
        var moreButton = new IconTextButton(ModContent.Request<Texture2D>("ZoneTitles/Assets/Textures/UI/More", AssetRequestMode.ImmediateLoad).Value);
        moreButton.Width.Set(30, 0);
        moreButton.Height.Set(30, 0);
        moreButton.HAlign = 1;
        moreButton.SetPadding(0);
        moreButton.OnClick += (evt, elem) =>
        {
            Value++;
            ValueChanged();
        };
        Append(moreButton);
        
        ValueChanged();
    }

    private void ValueChanged()
    {
        _displayText = DisplayGenerator(Value);
        _displayTextSize = _font.MeasureString(_displayText) - new Vector2(0, 5);
        
        OnValueChanged?.Invoke(Value);
    }

    protected override void DrawSelf(SpriteBatch spriteBatch)
    {
        var dimensions = GetDimensions();
        
        Utils.DrawSplicedPanel(spriteBatch, _background, (int) dimensions.X + 30 + 5, (int) dimensions.Y, (int) dimensions.Width - 30 - 5 - 5 - 30, (int) dimensions.Height, 10, 10, 10, 10, Colors.InventoryDefaultColor);

        ChatManager.DrawColorCodedString(spriteBatch, _font, _displayText, dimensions.Center(), Color.White, 0, _displayTextSize / 2, Vector2.One);
;        //spriteBatch.DrawString(_font, _displayText, dimensions.Center(), Color.White, 0, _displayTextSize / 2, Vector2.One, SpriteEffects.None, 0);
    }
}
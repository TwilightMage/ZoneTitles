using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent.UI.Elements;
using Terraria.ID;
using Terraria.Localization;
using Terraria.UI;

namespace ZoneTitles.Common.UI.Inputs;

public class IconTextButton : ControlWidget
{
    private readonly Asset<Texture2D> _BasePanelTexture;
    private readonly Asset<Texture2D> _hoveredTexture;
    private Texture2D _iconTexture;
    private Color _color;
    private Color _hoverColor;
    public float FadeFromBlack = 1f;
    private float _whiteLerp = 0.7f;
    private float _opacity = 0.7f;
    private bool _hovered;
    private UIText _title;
    private float _contentAlignmentX;
    private float _contentWidth;

    public IconTextButton(
        string title,
        Color textColor,
        Texture2D icon,
        float textSize = 1f,
        float contentAlignmentX = 0f)
    {
        _contentAlignmentX = contentAlignmentX;
        
        Width = StyleDimension.FromPixels(44f);
        Height = StyleDimension.FromPixels(34f);
        SetPadding(10);
        
        _hoverColor = Color.White;
        _BasePanelTexture = Main.Assets.Request<Texture2D>("Images/UI/CharCreation/PanelGrayscale");
        _hoveredTexture = Main.Assets.Request<Texture2D>("Images/UI/CharCreation/CategoryPanelHighlight");
        
        _title = new UIText(LocalizedText.Empty, 1);
        _title.HAlign = _contentAlignmentX;
        _title.VAlign = 0.5f;
        _title.IgnoresMouseInteraction = true;
        Append(_title);
        
        SetIcon(icon);
        SetText(title, textSize, textColor);
        SetSizeAuto();
        SetColor(Color.Lerp(Color.Black, Colors.InventoryDefaultColor, FadeFromBlack), 1f);
    }
    
    public IconTextButton(
        LocalizedText title,
        Color? textColor = null,
        float textSize = 1f,
        float contentAlignmentX = 0.5f)
        : this(title.Value, textColor ?? Color.White, null, textSize, contentAlignmentX)
    {
        
    }

    public IconTextButton(
        Texture2D icon,
        float contentAlignmentX = 0.5f)
        : this(null, Color.White, icon, 1, contentAlignmentX)
    {
        
    }

    public void SetIcon(Texture2D icon)
    {
        _iconTexture = icon;
        
        _title.PaddingLeft = _iconTexture != null ? _iconTexture.Width + 10 : 0;
    }

    public void SetText(string text, float textSize, Color color)
    {
        _title.SetText(text ?? "");
        _title.TextColor = color;
    }

    public void SetText(LocalizedText text, float textSize, Color color)
    {
        SetText(text.Value, textSize, color);
    }

    public void SetSizeAuto()
    {
        _title.Recalculate();
        
        float width = PaddingLeft + _title.GetDimensions().Width + (_iconTexture?.Width ?? 0) + PaddingRight;
        if (_iconTexture != null || !string.IsNullOrEmpty(_title.Text)) width += 10;
        
        Width.Set(width, 0.0f);
        Height.Set(PaddingTop + Math.Max(_title.GetDimensions().Height, (_iconTexture?.Height ?? 0)) + PaddingBottom, 0.0f);
    }

    protected override void DrawSelf(SpriteBatch spriteBatch)
    {
        CalculatedStyle dimensions = GetDimensions();
        var innerDimensions = GetInnerDimensions();
        
        Utils.DrawSplicedPanel(spriteBatch, _BasePanelTexture.Value, (int)dimensions.X, (int)dimensions.Y, (int)dimensions.Width, (int)dimensions.Height, 10, 10, 10, 10, Color.Lerp(Color.Black, _color, FadeFromBlack) * _opacity);
        if (_iconTexture != null)
        {
            Color color2 = Color.Lerp(_color, Color.White, _whiteLerp) * _opacity;
            spriteBatch.Draw(_iconTexture, new Vector2(innerDimensions.X + (innerDimensions.Width - _iconTexture.Width) * _contentAlignmentX, dimensions.Center().Y - _iconTexture.Height / 2f), color2);
        }
    }

    public override void LeftMouseDown(UIMouseEvent evt)
    {
        SoundEngine.PlaySound(SoundID.MenuTick);
        
        base.LeftMouseDown(evt);
    }

    public override void MouseOver(UIMouseEvent evt)
    {
        base.MouseOver(evt);
        
        SetColor(Color.Lerp(Colors.InventoryDefaultColor, Color.White, _whiteLerp), 0.7f);
        SoundEngine.PlaySound(SoundID.MenuTick);
    }

    public override void MouseOut(UIMouseEvent evt)
    {
        base.MouseOut(evt);
        
        SetColor(Color.Lerp(Color.Black, Colors.InventoryDefaultColor, FadeFromBlack), 1f);
    }

    public void SetColor(Color color, float opacity)
    {
        _color = color;
        _opacity = opacity;
    }

    public void SetHoverColor(Color color) => _hoverColor = color;
}
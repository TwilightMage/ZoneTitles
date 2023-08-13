using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.UI;

namespace ZoneTitles.Common.UI.Inputs;

// Code from UIImageButton
public class ColoredImageButton : ControlWidget
{
    private Asset<Texture2D> _texture;
    private float _visibilityActive = 1f;
    private float _visibilityInactive = 0.4f;
    private Asset<Texture2D> _borderTexture;

    public Color DrawColor = Color.White;

    public ColoredImageButton(Asset<Texture2D> texture)
    {
        this._texture = texture;
        this.Width.Set((float)this._texture.Width(), 0.0f);
        this.Height.Set((float)this._texture.Height(), 0.0f);
    }

    public void SetHoverImage(Asset<Texture2D> texture) => this._borderTexture = texture;

    public void SetImage(Asset<Texture2D> texture)
    {
        this._texture = texture;
        this.Width.Set((float)this._texture.Width(), 0.0f);
        this.Height.Set((float)this._texture.Height(), 0.0f);
    }

    public void ResizeToFitWidth(float newWidth)
    {
        if (_texture == null || _texture.Value == null) return;
        
        float W = _texture.Value.Width;
        float H = _texture.Value.Height;
        float aspect = W / H;
        Width.Set(newWidth, 0);
        Height.Set(newWidth / aspect, 0);
    }
    
    public void ResizeToFitHeight(float newHeight)
    {
        if (_texture == null || _texture.Value == null) return;
        
        float W = _texture.Value.Width;
        float H = _texture.Value.Height;
        float aspect = W / H;
        Width.Set(newHeight * aspect, 0);
        Height.Set(newHeight, 0);
    }

    protected override void DrawSelf(SpriteBatch spriteBatch)
    {
        CalculatedStyle dimensions = this.GetDimensions();
        spriteBatch.Draw(this._texture.Value, dimensions.ToRectangle(), DrawColor * (this.IsMouseHovering ? this._visibilityActive : this._visibilityInactive));
        if (this._borderTexture == null || !this.IsMouseHovering)
            return;
        spriteBatch.Draw(this._borderTexture.Value, dimensions.Position(), DrawColor);
    }

    public override void MouseOver(UIMouseEvent evt)
    {
        base.MouseOver(evt);
        SoundEngine.PlaySound(SoundID.MenuTick);
    }

    public override void MouseOut(UIMouseEvent evt) => base.MouseOut(evt);

    public void SetVisibility(float whenActive, float whenInactive)
    {
        this._visibilityActive = MathHelper.Clamp(whenActive, 0.0f, 1f);
        this._visibilityInactive = MathHelper.Clamp(whenInactive, 0.0f, 1f);
    }
}
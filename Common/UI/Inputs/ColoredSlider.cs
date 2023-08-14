using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.UI;

namespace ZoneTitles.Common.UI.Inputs;

// UIColoredSlider, but fixed
public class ColoredSlider : ControlWidget, IInputControl<float>
{
    public Color Color { get; private set; }

    public float Value
    {
        get => _value;
        set
        {
            _value = MathHelper.Clamp(value, 0, 1);
            Color = _colorFunc(Value);
            
            OnColorChanged?.Invoke(Color);
            OnValueChanged?.Invoke(Value);
        }
    }

    public Func<float, Color> ColorFunc
    {
        get => _colorFunc;
        set
        {
            _colorFunc = value;
            UpdateColors();
        }
    }

    public event Action<Color> OnColorChanged;
    public event Action<float> OnValueChanged;
    
    private Func<float, Color> _colorFunc = (a) => Color.Lerp(Color.Black, Color.White, a);

    private float _value = 0;
    private Texture2D _colors = null;
    private bool _dragging = false;

    public ColoredSlider()
    {
        Width.Set(178, 0);
        Height.Set(16, 0);
    }

    public override void OnInitialize()
    {
        UpdateColors();
    }

    public void SetColorFunc(Func<float, Color> func)
    {
        _colorFunc = func;
        
        UpdateColors();
    }

    private void UpdateSize()
    {
        int size = (int)MathF.Max(0, GetDimensions().Width - 10);

        if (size == 0)
        {
            _colors = null;
            return;
        }
        
        _colors = new Texture2D(Main.graphics.GraphicsDevice, size, 8);
        
        UpdateColors();
    }

    public void UpdateColors()
    {
        if (_colors == null) return;
        
        UInt32[] newData = new UInt32[_colors.Width * _colors.Height];
        for (int x = 0; x < _colors.Width; x++)
        {
            Color c = _colorFunc((float)x / _colors.Width);
            for (int y = 0; y < _colors.Height; y++)
            {
                newData[y * _colors.Width + x] = (uint)c.GetHashCode();
            }
        }
        
        _colors.SetData(newData);

        Color = _colorFunc(Value);
            
        OnColorChanged?.Invoke(Color);
    }

    public override void MouseOver(UIMouseEvent evt)
    {
        base.MouseOver(evt);

        if (!_dragging)
        {
            SoundEngine.PlaySound(SoundID.MenuTick);
        }
    }

    public override void LeftMouseDown(UIMouseEvent evt)
    {
        base.LeftMouseDown(evt);

        _dragging = true;
    }

    public override void Update(GameTime gameTime)
    {
        base.Update(gameTime);

        if (Main.mouseLeftRelease)
        {
            _dragging = false;
        }
    }

    protected override void DrawSelf(SpriteBatch spriteBatch)
    {
        var dimensions = GetDimensions();
        
        if (_dragging)
        {
            Value = MathHelper.Clamp(Main.mouseX - dimensions.X, 0, dimensions.Width) / dimensions.Width;
            Color = ColorFunc(Value);
            
            OnColorChanged?.Invoke(Color);
            OnValueChanged?.Invoke(Value);
        }

        int DesiredSize = (int)MathF.Max(dimensions.Width - 10, 0);
        if (_colors == null && DesiredSize > 0 || _colors != null && (float)_colors.Width / DesiredSize > 2f)
        {
            UpdateSize();
        }

        float centerY = dimensions.Y + dimensions.Height / 2;
        
        Utils.DrawSplicedPanel(spriteBatch, TextureAssets.ColorBar.Value, (int)dimensions.X, (int)(centerY - TextureAssets.ColorBar.Value.Height / 2), (int)dimensions.Width, TextureAssets.ColorBar.Value.Height, 8, 8, 8, 8, IsMouseHovering || _dragging ? Main.OurFavoriteColor : Color.White);

        if (_colors != null)
        {
            spriteBatch.Draw(_colors, new Rectangle((int)(dimensions.X + 5), (int)(centerY - _colors.Height / 2), (int)(dimensions.Width - 10), _colors.Height), null, Color.White);
        }
        
        spriteBatch.Draw(TextureAssets.ColorSlider.Value, new Vector2(dimensions.X + 5 + DesiredSize * Value, centerY), null, Color.White, 0, TextureAssets.ColorSlider.Value.Size() / 2, Vector2.One, SpriteEffects.None, 0);
    }
}
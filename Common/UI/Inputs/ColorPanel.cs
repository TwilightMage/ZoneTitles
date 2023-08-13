using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using ReLogic.OS;
using System;
using System.Text.RegularExpressions;
using Terraria.Localization;
using Terraria.ModLoader;
using ZoneTitles.Common.UI.Elements;

namespace ZoneTitles.Common.UI.Inputs;

public class ColorPanel : Widget, IInputControl<Color>
{
    private ColoredSlider _inputHue;
    private ColoredSlider _inputSaturation;
    private ColoredSlider _inputValue;
    private TextPanel _hexCodeDisplay;
    private IconTextButton _copyButton;
    private IconTextButton _pasteButton;
 
    public event Action<Color> OnValueChanged;
    
    public float H { get; private set; }
    public float S { get; private set; }
    public float V { get; private set; }

    public Color Value
    {
        get => _value;
        set
        {
            _value = value;
            _hexCodeDisplay.SetText(ColorToHash(_value));
            (H, S, V) = RGBToHSV(_value);
            _inputHue.Value = H / 360;
            _inputSaturation.Value = S;
            _inputValue.Value = V;
        }
    }

    private Color _value;

    public override void OnInitialize()
    {
        ClickTransparent = true;
        
        _inputHue = new ColoredSlider();
        _inputHue.Width.Set(-110, 1);
        _inputHue.ColorFunc = (alpha) => HSVToRGB(alpha * 360, S, V);
        _inputHue.OnValueChanged += (h) =>
        {
            H = h * 360;
            _inputSaturation.UpdateColors();
            _inputValue.UpdateColors();
            ColorChanged();
        };
        _inputHue.VAlign = 0.1f;
        Append(_inputHue);
        
        _inputSaturation = new ColoredSlider();
        _inputSaturation.Width.Set(-110, 1);
        _inputSaturation.ColorFunc = (alpha) => Color.Lerp(Color.White, HSVToRGB(H, 1, V), alpha);
        _inputSaturation.OnValueChanged += (s) =>
        {
            S = s;
            _inputHue.UpdateColors();
            _inputValue.UpdateColors();
            ColorChanged();
        };
        _inputSaturation.VAlign = 0.5f;
        Append(_inputSaturation);
        
        _inputValue = new ColoredSlider();
        _inputValue.Width.Set(-110, 1);
        _inputValue.ColorFunc = (alpha) => Color.Lerp(Color.Black, HSVToRGB(H, S, 1), alpha);
        _inputValue.OnValueChanged += (v) =>
        {
            V = v;
            _inputHue.UpdateColors();
            _inputSaturation.UpdateColors();
            ColorChanged();
        };
        _inputValue.VAlign = 0.9f;
        Append(_inputValue);

        _hexCodeDisplay = new TextPanel();
        _hexCodeDisplay.Width.Set(100, 0);
        _hexCodeDisplay.Height.Set(30, 0);
        _hexCodeDisplay.TextOriginX = 0.5f;
        _hexCodeDisplay.TextOriginY = 0.5f;
        _hexCodeDisplay.HAlign = 1;
        _hexCodeDisplay.SetText(ColorToHash(Value));
        Append(_hexCodeDisplay);

        _copyButton = new IconTextButton(ModContent.Request<Texture2D>("Terraria/Images/UI/CharCreation/Copy", AssetRequestMode.ImmediateLoad).Value);
        _copyButton.Left.Set(-100, 1);
        _copyButton.Top.Set(30 + 5, 0);
        _copyButton.Width.Set(47.5f, 0);
        _copyButton.Height.Set(30, 0);
        _copyButton.SetPadding(0);
        //_copyButton.ToolTip = Localize("Copy").Value;
        _copyButton.OnClick += (evt, elem) =>
        {
            Platform.Get<IClipboard>().Value = _hexCodeDisplay.Text;
        };
        Append(_copyButton);
        
        _pasteButton = new IconTextButton(ModContent.Request<Texture2D>("Terraria/Images/UI/CharCreation/Paste", AssetRequestMode.ImmediateLoad).Value);
        _pasteButton.Left.Set(-100 + 47.5f + 5, 1);
        _pasteButton.Top.Set(30 + 5, 0);
        _pasteButton.Width.Set(47.5f, 0);
        _pasteButton.Height.Set(30, 0);
        _pasteButton.SetPadding(0);
        //_pasteButton.ToolTip = Localize("Paste").Value;
        _pasteButton.OnClick += (evt, elem) =>
        {
            var hash = Platform.Get<IClipboard>().Value;
            if (Regex.IsMatch(hash, "#?[0-9a-fA-F]{6}"))
            {
                Value = HashToColor(hash);
            }
        };
        Append(_pasteButton);
    }

    private void ColorChanged()
    {
        _hexCodeDisplay.SetText(ColorToHash(_value));
        OnValueChanged?.Invoke(_value = HSVToRGB(H, S, V));
    }
    
    public static Color HSVToRGB(float hue, float saturation, float value)
    {
        double r = 0, g = 0, b = 0;

        if (saturation == 0)
        {
            r = value;
            g = value;
            b = value;
        }
        else
        {
            int i;
            double f, p, q, t;

            while (hue >= 360) hue -= 360;
            
            hue /= 60;

            i = (int)Math.Truncate(hue);
            f = hue - i;

            p = value * (1.0 - saturation);
            q = value * (1.0 - (saturation * f));
            t = value * (1.0 - (saturation * (1.0 - f)));

            switch (i)
            {
                case 0:
                    r = value;
                    g = t;
                    b = p;
                    break;

                case 1:
                    r = q;
                    g = value;
                    b = p;
                    break;

                case 2:
                    r = p;
                    g = value;
                    b = t;
                    break;

                case 3:
                    r = p;
                    g = q;
                    b = value;
                    break;

                case 4:
                    r = t;
                    g = p;
                    b = value;
                    break;

                default:
                    r = value;
                    g = p;
                    b = q;
                    break;
            }

        }

        return new Color((byte)(r * 255), (byte)(g * 255), (byte)(b * 255));
    }

    public static (float hue, float saturation, float value) RGBToHSV(Color color)
    {
        int max = Math.Max(color.R, Math.Max(color.G, color.B));
        int min = Math.Min(color.R, Math.Min(color.G, color.B));
        
        float hue = System.Drawing.Color.FromArgb(color.R, color.G, color.B).GetHue();
        float saturation = (max == 0) ? 0 : 1f - (1f * min / max);
        float value = max / 255f;

        return (hue, saturation, value);
    }

    public static string ColorToHash(Color color)
    {
        System.Drawing.Color c = System.Drawing.Color.FromArgb(color.R, color.G, color.B);

        return System.Drawing.ColorTranslator.ToHtml(c);
    }

    public static Color HashToColor(string hash)
    {
        var c = (System.Drawing.Color)new System.Drawing.ColorConverter().ConvertFromString(hash);
        return new Color(c.R, c.G, c.B);
    }

    private LocalizedText Localize(string key) => Language.GetText($"Mods.ZoneTitles.UI.ColorPanel.{key}");
}
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent.UI.Elements;
using Terraria.GameInput;
using Terraria.ID;
using Terraria.UI;
using ZoneTitles.Common.Systems;

namespace ZoneTitles.Common.UI.Inputs;

public class TextInput : UITextBox, IInputControl<string>
{
    public bool IsWritingText { get; private set; } = false;
    
    private string _textToRevertTo;
    private bool _justMouseLeftDown;

    public string Value
    {
        get => Text;
        set
        {
            SetText(value);
        }
    }
    
    public event Action<string> OnValueChanged;
    
    public TextInput(string text = "", float textScale = 1, bool large = false) : base(text, textScale, large)
    {
        ShowInputTicker = false;
    }

    public override void MouseDown(UIMouseEvent evt)
    {
        base.MouseDown(evt);
        
        _justMouseLeftDown = true;
    }

    public override void MouseOver(UIMouseEvent evt)
    {
        base.MouseOver(evt);
        SoundEngine.PlaySound(SoundID.MenuTick);
    }

    public override void Update(GameTime gameTime)
    {
        if (UISystem.JustMouseLeftDown && !_justMouseLeftDown && IsWritingText)
        {
            ToggleWritingText();
        }

        _justMouseLeftDown = false;
        
        if (IsWritingText)
        {
            PlayerInput.WritingText = true;
            Main.CurrentInputTextTakerOverride = this;
        }

        base.Update(gameTime);
    }

    protected override void DrawSelf(SpriteBatch spriteBatch)
    {
        base.DrawSelf(spriteBatch);

        if (IsWritingText)
        {
            PlayerInput.WritingText = true;
            Main.instance.HandleIME();
            string input = Main.GetInputText(Text);
            if (Main.inputTextEnter)
            {
                ToggleWritingText();
            }
            else if (Main.inputTextEscape)
            {
                ToggleWritingText();
                SetText(_textToRevertTo);
            }
            
            SetText(input);
        }
    }

    public override void SetText(string text, float textScale, bool large)
    {
        base.SetText(text, textScale, large);
        OnValueChanged?.Invoke(Text);
    }
    
    public void TrimDisplayIfOverElementDimensions(int padding)
    {
        CalculatedStyle dimensions1 = this.GetDimensions();
        if ((double) dimensions1.Width == 0.0 && (double) dimensions1.Height == 0.0)
            return;
        Point point1 = new Point((int) dimensions1.X, (int) dimensions1.Y);
        Point point2 = new Point(point1.X + (int) dimensions1.Width, point1.Y + (int) dimensions1.Height);
        Rectangle rectangle1 = new Rectangle(point1.X, point1.Y, point2.X - point1.X, point2.Y - point1.Y);
        CalculatedStyle dimensions2 = GetDimensions();
        Point point3 = new Point((int) dimensions2.X, (int) dimensions2.Y);
        Point point4 = new Point(point3.X + (int) MinWidth.Pixels, point3.Y + (int) MinHeight.Pixels);
        Rectangle rectangle2 = new Rectangle(point3.X, point3.Y, point4.X - point3.X, point4.Y - point3.Y);
        int num = 0;
        while (rectangle2.Right > rectangle1.Right - padding && Text.Length > 0)
        {
            SetText(Text.Substring(0, Text.Length - 1));
            ++num;
            this.RecalculateChildren();
            CalculatedStyle dimensions3 = GetDimensions();
            point3 = new Point((int) dimensions3.X, (int) dimensions3.Y);
            point4 = new Point(point3.X + (int) MinWidth.Pixels, point3.Y + (int) MinHeight.Pixels);
            rectangle2 = new Rectangle(point3.X, point3.Y, point4.X - point3.X, point4.Y - point3.Y);
            //this.actualContents = this._text.Text;
        }
    }

    public void ToggleWritingText()
    {
        IsWritingText = !IsWritingText;
        ShowInputTicker = IsWritingText;

        if (IsWritingText)
        {
            _textToRevertTo = Text;
        }
    }

    public void SetWritingText(bool writingText)
    {
        if (IsWritingText == writingText) return;
        
        IsWritingText = writingText;
        ShowInputTicker = IsWritingText;

        if (IsWritingText)
        {
            _textToRevertTo = Text;
        }
    }
}
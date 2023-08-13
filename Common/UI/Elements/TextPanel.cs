using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.GameContent.UI.Elements;
using Terraria.ID;
using Terraria.Localization;

namespace ZoneTitles.Common.UI.Elements;

public class TextPanel : UIText
{
    public Texture2D Background;
    public Color BackgroundColor = Colors.InventoryDefaultColor;
    public int PanelLeftEnd = 10;
    public int PanelRightEnd = 10;
    public int PanelTopEnd = 10;
    public int PanelBottomEnd = 10;
    
    public TextPanel() : base("", 1, false)
    {
    }
    
    public TextPanel(string text, float textScale = 1, bool large = false) : base(text, textScale, large)
    {
    }

    public TextPanel(LocalizedText text, float textScale = 1, bool large = false) : base(text, textScale, large)
    {
    }
    
    public override void OnInitialize()
    {
        base.OnInitialize();

        Background ??= Main.Assets.Request<Texture2D>("Images/UI/CharCreation/PanelGrayscale", AssetRequestMode.ImmediateLoad).Value;
    }

    protected override void DrawSelf(SpriteBatch spriteBatch)
    {
        if (Background != null)
        {
            var dimensions = this.GetDimensions();
            Utils.DrawSplicedPanel(spriteBatch, Background, (int) dimensions.X, (int) dimensions.Y, (int) dimensions.Width, (int) dimensions.Height, PanelLeftEnd, PanelRightEnd, PanelTopEnd, PanelBottomEnd, BackgroundColor);
        }
        
        base.DrawSelf(spriteBatch);
    }
}
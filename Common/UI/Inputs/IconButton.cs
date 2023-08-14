using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.UI;
using ZoneTitles.Common.Systems;

namespace ZoneTitles.Common.UI.Inputs;

public class IconButton : UIElement
{
    public IconSystem.IconProvider Icon;

    private Texture2D _background;
    private Color _backgroundColor;

    public IconButton()
    {
        _background = Main.Assets.Request<Texture2D>("Images/UI/CharCreation/PanelGrayscale").Value;
        _backgroundColor = Colors.InventoryDefaultColor;
        
        Width.Set(40, 0);
        Height.Set(40, 0);
    }

    public override void MouseOver(UIMouseEvent evt)
    {
        base.MouseOver(evt);
        
        _backgroundColor = Color.Lerp(Colors.InventoryDefaultColor, Color.White, 0.7f);
        SoundEngine.PlaySound(SoundID.MenuTick);
    }

    public override void MouseOut(UIMouseEvent evt)
    {
        base.MouseOut(evt);
        
        _backgroundColor = Colors.InventoryDefaultColor;
    }

    public override void LeftMouseDown(UIMouseEvent evt)
    {
        base.LeftMouseDown(evt);
        
        SoundEngine.PlaySound(SoundID.MenuTick);
    }

    protected override void DrawSelf(SpriteBatch spriteBatch)
    {
        var dimensions = this.GetDimensions();
        Utils.DrawSplicedPanel(spriteBatch, _background, (int) dimensions.X, (int) dimensions.Y, (int) dimensions.Width, (int) dimensions.Height, 10, 10, 10, 10, _backgroundColor);

        Icon?.Draw(spriteBatch, dimensions.Center());

        var name = Icon?.GetName();
        if (IsMouseHovering && name != null)
        {
            Main.instance.MouseText(name, "", 0, 0, hackedMouseX: Main.mouseX + 6, hackedMouseY: Main.mouseY + 6, noOverride: true);
        }
    }
}
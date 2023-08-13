using Microsoft.Xna.Framework;
using Terraria;

namespace ZoneTitles.Common.UI.Inputs;

public class ControlWidget : Widget
{
    public string ToolTip = null;
    
    public override void Update(GameTime gameTime)
    {
        base.Update(gameTime);
        
        if (IsMouseHovering && ToolTip != null)
        {
            Main.instance.MouseText(ToolTip, hackedMouseX: Main.mouseX, hackedMouseY: Main.mouseY);
        }
    }
}
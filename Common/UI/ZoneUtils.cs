using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;

namespace ZoneTitles.Common.UI;

public class ZoneUtils
{
    private static Asset<Texture2D> SunflowerTexture;

    public static void DrawSunflowerLoading(SpriteBatch spriteBatch, Vector2 center, int? frame = null, float scale = 1f)
    {
        frame ??= (int)(Main.time / 3);
        frame %= 19;
        
        SunflowerTexture ??= Main.Assets.Request<Texture2D>("Images/UI/Sunflower_Loading");
        
        if (SunflowerTexture.Value != null)
            spriteBatch.Draw(SunflowerTexture.Value, center, new Rectangle(0, 53 * frame.Value, 52, 52), Color.White, 0, new Vector2(26, 26), scale, SpriteEffects.None, 0);
    }
    
    public static void ModifyPositionXByZoom(ref float value)
    {
        value += (value - Main.screenWidth / 2) * (Main.GameZoomTarget - 1);
    }
    
    public static void ModifyPositionYByZoom(ref float value)
    {
        value += (value - Main.screenHeight / 2) * (Main.GameZoomTarget - 1);
    }
}
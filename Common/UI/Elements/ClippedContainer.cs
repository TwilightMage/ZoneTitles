using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameInput;

namespace ZoneTitles.Common.UI.Elements;

public class ClippedContainer : Widget
{
    private RenderTarget2D _renderTarget;

    public ClippedContainer()
    {
        ClickTransparent = true;
    }

    ~ClippedContainer()
    {
        Main.OnPreDraw += PreDrawContent;
    }

    public override void OnActivate()
    {
        Main.OnPreDraw += PreDrawContent;
    }

    public override void OnDeactivate()
    {
        Main.OnPreDraw -= PreDrawContent;
    }

    private void PreDrawContent(GameTime gameTime)
    {
        var graphicsDevice = Main.graphics.GraphicsDevice;

        if (_renderTarget == null || _renderTarget.Width != Main.screenWidth || _renderTarget.Height != Main.screenHeight)
        {
            _renderTarget = new RenderTarget2D(graphicsDevice, Main.screenWidth, Main.screenHeight, true, SurfaceFormat.Color, DepthFormat.Depth24);
        }

        var savedRenderTargets = graphicsDevice.GetRenderTargets();
        var savedScissors = graphicsDevice.ScissorRectangle;
        var savedScissorsEnabled = graphicsDevice.RasterizerState.ScissorTestEnable;
        PlayerInput.SetZoom_UI();

        graphicsDevice.SetRenderTarget(_renderTarget);
        graphicsDevice.ScissorRectangle = GetDimensions().ToRectangle();
        graphicsDevice.RasterizerState.ScissorTestEnable = true;

        graphicsDevice.Clear(Color.Transparent);

        Main.spriteBatch.Begin();
        base.DrawChildren(Main.spriteBatch);
        Main.spriteBatch.End();

        PlayerInput.SetZoom_Unscaled();
        graphicsDevice.RasterizerState.ScissorTestEnable = savedScissorsEnabled;
        graphicsDevice.ScissorRectangle = savedScissors;
        graphicsDevice.SetRenderTargets(savedRenderTargets);
    }

    protected override void DrawChildren(SpriteBatch spriteBatch)
    {
        spriteBatch.Draw(_renderTarget, Vector2.Zero, Color.White);
    }
}
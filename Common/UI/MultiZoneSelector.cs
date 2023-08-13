using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent.UI.Elements;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.UI;
using Terraria.UI.Chat;
using ZoneTitles.Common.Systems;
using ZoneTitles.Common.UI.Elements;

namespace ZoneTitles.Common.UI;

public class MultiZoneSelector : UIState
{
    private Vector2 _worldPosition;
    private PanelWidget _panel;
    private List<UIText> _optionButtons = new List<UIText>();
    private List<MouseEvent> _options = new List<MouseEvent>();
    private bool _justAppeared = false;
    private Asset<Texture2D> _tileSelectTexture;
    private Pool<UIText> _pool = new Pool<UIText>(() =>
    {
        var item = new UIText("");
        item.TextOriginX = 0.5f;
        item.TextOriginY = 0.5f;
        item.OnMouseOver += (evt, elem) =>
        {
            SoundEngine.PlaySound(SoundID.MenuTick);
            ((UIText)elem).SetText(((UIText)elem).Text, 1.1f, false);
        };
        item.OnMouseOut += (evt, elem) => ((UIText)elem).SetText(((UIText)elem).Text, 1f, false);

        return item;
    });

    public override void OnInitialize()
    {
        base.OnInitialize();

        _tileSelectTexture = ModContent.Request<Texture2D>("ZoneTitles/Assets/Textures/UI/TileBorder", AssetRequestMode.ImmediateLoad);

        _panel = new PanelWidget();
        _panel.SetPadding(0);
        Append(_panel);
    }

    public override void OnActivate()
    {
        base.OnActivate();

        _justAppeared = true;
    }

    public override void OnDeactivate()
    {
        Clear();
    }

    public void Setup(List<Zone> zones, Vector2 worldPosition)
    {
        Clear();

        _panel.Width.Set(10 + 10, 0);
        _panel.Height.Set(10, 0);
        
        for (int i = 0; i < zones.Count; i++)
        {
            var button = _pool.Allocate();

            int j = i;
            _options.Add((evt, elem) =>
            {
                SoundEngine.PlaySound(SoundID.MenuTick);
                UISystem.OpenZoneEditor(zones[j]);
                UISystem.CloseZoneSelector();
            });
            _optionButtons.Add(button);
            
            button.Left.Set(0, 0);
            button.Top.Set(_panel.Height.Pixels, 0);
            button.SetText(zones[i].Title);
            button.Width.Set(0, 1);
            button.Height.Set(button.MinHeight.Pixels, 0);
            button.TextColor = zones[i].TitleColor;
            button.OnClick += _options[i];
            _panel.Append(button);

            _panel.Width.Pixels = MathF.Max(_panel.Width.Pixels, 10 + button.MinWidth.Pixels + 10);
            _panel.Height.Pixels += button.MinHeight.Pixels + 5;
        }

        _panel.Height.Pixels += 5;

        _worldPosition = worldPosition;
    }

    private void Clear()
    {
        for (int i = 0; i < _optionButtons.Count; i++)
        {
            _optionButtons[i].Remove();
            _optionButtons[i].OnClick -= _options[i];
            
            _pool.Free(_optionButtons[i]);
        }
        
        _optionButtons.Clear();
        _options.Clear();
    }

    public override void Update(GameTime gameTime)
    {
        base.Update(gameTime);

        _justAppeared = false;
    }

    protected override void DrawSelf(SpriteBatch spriteBatch)
    {
        var panelDimensions = _panel.GetDimensions();
        
        Vector2 pos = (_worldPosition - Main.screenPosition) / Main.UIScale;
        
        ZoneUtils.ModifyPositionXByZoom(ref pos.X);
        ZoneUtils.ModifyPositionYByZoom(ref pos.Y);

        _panel.Left.Set(pos.X - panelDimensions.Width / 2, 0);
        _panel.Top.Set(pos.Y - panelDimensions.Height / 2, 0);
        _panel.Recalculate();

        if (!_panel.IsMouseHovering && !_justAppeared)
        {
            UISystem.CloseZoneSelector();
        }

        spriteBatch.Draw(_tileSelectTexture.Value, pos, _tileSelectTexture.Value.Bounds, Main.OurFavoriteColor, 0f, Vector2.Zero, 0.5f, SpriteEffects.None, 0);
    }
}
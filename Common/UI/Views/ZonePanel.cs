using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using System;
using System.Linq;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.GameContent.UI.Elements;
using Terraria.ID;
using Terraria.Localization;
using ZoneTitles.Common.Systems;
using ZoneTitles.Common.UI.Elements;
using ZoneTitles.Common.UI.Inputs;

namespace ZoneTitles.Common.UI.Views;

public class ZonePanel : View
{
    public Zone TargetZone
    {
        get => _targetZone;
        set
        {
            Discard();
            _targetZone = value;
            Revert();
        }
    }

    private Zone _targetZone = null;
    
    private ColoredImageButton _closeButton;
    private UIText _panelTitle;
    private TextInput _titleInput;
    private TextInput _subtitleInput;
    private ColorPanel _titleColorInput;
    private ColorPanel _titleStrokeInput;
    private IconButton _iconInput;
    private NumberSpinner _priorityInput;
    private IconTextButton _cancelButton;
    private IconTextButton _confirmButton;
    
    private string _newTitle;
    private string _newSubtitle;
    private Color _newTitleColor;
    private Color _newStrokeColor;
    private IconSystem.IconProvider _newIcon;
    private int _newPriority;

    public override void OnDeactivate()
    {
        base.OnDeactivate();
        
        Discard();
    }

    public override void OnInitialize()
    {
        base.OnInitialize();
        
        float maxLabelWidth = new string[]
            {
                Localize("TitleLabel").Value,
                Localize("SubtitleLabel").Value,
                Localize("ColorLabel").Value,
                Localize("StrokeLabel").Value,
                Localize("IconLabel").Value,
                Localize("PriorityLabel").Value
            }.Select(str => FontAssets.MouseText.Value.MeasureString(str).X)
            .Max();

        Left.Set(700, 0);
        Top.Set(300, 0);
        
        int offset = 10;

        int y = offset;

        _panelTitle = new UIText(LocalizedText.Empty);
        _panelTitle.Width.Set(0, 1);
        _panelTitle.Height.Set(25, 0);
        Append(_panelTitle);
        
        _closeButton = new ColoredImageButton(Main.Assets.Request<Texture2D>("Images/UI/SearchCancel", AssetRequestMode.ImmediateLoad));
        _closeButton.DrawColor = Color.IndianRed;
        _closeButton.HAlign = 1;
        _closeButton.OnMouseOver += (evt, elem) =>
        {
            SoundEngine.PlaySound(SoundID.MenuTick);
        };
        _closeButton.OnLeftClick += (evt, elem) =>
        {
            Close();
        };
        Append(_closeButton);

        y += 25 + offset;
        
        var titleLabel = new UIText(Localize("TitleLabel"));
        titleLabel.Width.Set(maxLabelWidth, 0);
        titleLabel.Height.Set(44, 0);
        titleLabel.Top.Set(y, 0);
        titleLabel.TextOriginX = 1;
        titleLabel.TextOriginY = 0.5f;
        Append(titleLabel);

        _titleInput = new TextInput();
        _titleInput.Width.Set(-maxLabelWidth - offset, 1);
        _titleInput.Left.Set(maxLabelWidth + offset, 0);
        _titleInput.Top.Set(y, 0);
        _titleInput.TextHAlign = 0;
        _titleInput.SetTextMaxLength(40);
        _titleInput.OnLeftClick += (evt, elem) => _titleInput.SetWritingText(true);
        _titleInput.OnRightClick += (evt, elem) =>
        {
            _titleInput.SetText("");
            _titleInput.SetWritingText(true);
        };
        _titleInput.OnValueChanged += (newTitle) =>
        {
            _newTitle = newTitle;
            _panelTitle.SetText(Localize("PanelTitle").Format(_newTitle));
        };
        Append(_titleInput);

        y += 44 + offset;
        
        var subtitleLabel = new UIText(Localize("SubtitleLabel"));
        subtitleLabel.Width.Set(maxLabelWidth, 0);
        subtitleLabel.Height.Set(44, 0);
        subtitleLabel.Top.Set(y, 0);
        subtitleLabel.TextOriginX = 1;
        subtitleLabel.TextOriginY = 0.5f;
        Append(subtitleLabel);

        _subtitleInput = new TextInput();
        _subtitleInput.Width.Set(-maxLabelWidth - offset, 1);
        _subtitleInput.Left.Set(maxLabelWidth + offset, 0);
        _subtitleInput.Top.Set(y, 0);
        _subtitleInput.TextHAlign = 0;
        _subtitleInput.SetTextMaxLength(40);
        _subtitleInput.OnLeftClick += (evt, elem) => _subtitleInput.SetWritingText(true);
        _subtitleInput.OnRightClick += (evt, elem) =>
        {
            _subtitleInput.SetText("");
            _subtitleInput.SetWritingText(true);
        };
        _subtitleInput.OnValueChanged += (newSubtitle) => _newSubtitle = newSubtitle;;
        Append(_subtitleInput);

        y += 44 + offset;

        var colorLabel = new UIText(Localize("ColorLabel"));
        colorLabel.Width.Set(maxLabelWidth, 0);
        colorLabel.Height.Set(44, 0);
        colorLabel.Top.Set(y, 0);
        colorLabel.TextOriginX = 1;
        colorLabel.TextOriginY = 0.5f;
        Append(colorLabel);

        _titleColorInput = new ColorPanel();
        _titleColorInput.Width.Set(-maxLabelWidth - offset, 1);
        _titleColorInput.Height.Set(90, 0);
        _titleColorInput.Left.Set(maxLabelWidth + offset, 0);
        _titleColorInput.Top.Set(y, 0);
        _titleColorInput.OnValueChanged += (newTitleColor) => _newTitleColor = newTitleColor;
        Append(_titleColorInput);

        y += 90 + offset;

        var titleStrokeLabel = new UIText(Localize("StrokeLabel"));
        titleStrokeLabel.Width.Set(maxLabelWidth, 0);
        titleStrokeLabel.Height.Set(44, 0);
        titleStrokeLabel.Top.Set(y, 0);
        titleStrokeLabel.TextOriginX = 1;
        titleStrokeLabel.TextOriginY = 0.5f;
        Append(titleStrokeLabel);

        _titleStrokeInput = new ColorPanel();
        _titleStrokeInput.Width.Set(-maxLabelWidth - offset, 1);
        _titleStrokeInput.Height.Set(90, 0);
        _titleStrokeInput.Left.Set(maxLabelWidth + offset, 0);
        _titleStrokeInput.Top.Set(y, 0);
        _titleStrokeInput.OnValueChanged += (newStrokeColor) => _newStrokeColor = newStrokeColor;
        Append(_titleStrokeInput);

        y += 90 + offset;
        
        var iconLabel = new UIText(Localize("IconLabel"));
        iconLabel.Width.Set(maxLabelWidth, 0);
        iconLabel.Height.Set(40, 0);
        iconLabel.Top.Set(y, 0);
        iconLabel.TextOriginX = 1;
        iconLabel.TextOriginY = 0.5f;
        Append(iconLabel);

        _iconInput = new IconButton();
        _iconInput.Left.Set(maxLabelWidth + offset, 0);
        _iconInput.Top.Set(y, 0);
        _iconInput.OnLeftClick += (evt, elem) =>
        {
            if (!(Main.mouseItem?.IsAir ?? true))
            {
                _iconInput.Icon = _newIcon = ItemIconProvider.CreateFromItem(ContentSamples.ItemsByType[Main.mouseItem.type], 0);
            }
            else if (!UISystem.ZoneEditor.IconPickerVisible)
            {
                UISystem.ZoneEditor.OpenIconPicker();
                UISystem.ZoneEditor.IconPicker.Left.Set(GetDimensions().X - UISystem.ZoneEditor.IconPicker.GetDimensions().Width - 50, 0);
                UISystem.ZoneEditor.IconPicker.Top.Set(GetDimensions().Y, 0);
            }
        };
        Append(_iconInput);

        y += 40 + offset;
        
        var priorityLabel = new UIText(Localize("PriorityLabel"));
        priorityLabel.Width.Set(maxLabelWidth, 0);
        priorityLabel.Height.Set(30, 0);
        priorityLabel.Top.Set(y, 0);
        priorityLabel.TextOriginX = 1;
        priorityLabel.TextOriginY = 0.5f;
        Append(priorityLabel);

        _priorityInput = new NumberSpinner();
        _priorityInput.Left.Set(maxLabelWidth + offset, 0);
        _priorityInput.Top.Set(y, 0);
        _priorityInput.Width.Set(30 + 5 + 75 + 5 + 30, 0);
        _priorityInput.DisplayGenerator = (val) => $"{val:+#;-#;+0}";
        _priorityInput.OnValueChanged += (newPriority) => _newPriority = newPriority;
        Append(_priorityInput);
        
        y += 30 + offset;
        
        y += 10;

        _cancelButton = new IconTextButton(Localize("Revert"));
        _cancelButton.Width.Set(-5, 0.5f);
        _cancelButton.Top.Set(y, 0);
        _cancelButton.OnLeftClick += (evt, elem) => Revert();
        Append(_cancelButton);
        
        _confirmButton = new IconTextButton(Localize("Apply"));
        _confirmButton.Width.Set(-5, 0.5f);
        _confirmButton.Top.Set(y, 0);
        _confirmButton.HAlign = 1;
        _confirmButton.OnLeftClick += (evt, elem) => Apply();
        Append(_confirmButton);

        y += 38 + offset;
        
        Width.Set(10 + maxLabelWidth + 10 + 320 + 10, 0);
        Height.Set(y + 10, 0);
        Recalculate();

        UISystem.ZoneEditor.IconPicker.OnIconSelected += (icon) =>
        {
            _iconInput.Icon = icon;
            _newIcon = icon;
        };
    }

    public void Discard()
    {
        if (_targetZone == null) return;
        
        if (_targetZone.IsFresh)
        {
            ZonesSystem.RemoveZone(_targetZone);
        }
        else
        {
            Revert();
        }

        _targetZone = null;
    }
    
    public void Revert()
    {
        if (_targetZone == null) return;
        
        _panelTitle.SetText(Localize("PanelTitle").Format(TargetZone.Title));
        
        _titleInput.SetWritingText(false);
        _titleInput.SetText(_newTitle = TargetZone.Title);
        _subtitleInput.SetWritingText(false);
        _subtitleInput.SetText(_newSubtitle = TargetZone.SubTitle);
        _titleColorInput.Value = _newTitleColor = TargetZone.TitleColor;
        _titleStrokeInput.Value = _newStrokeColor = TargetZone.TitleStroke;
        _iconInput.Icon = _newIcon = TargetZone.IconProvider;
        _priorityInput.Value = _newPriority = TargetZone.Priority;
    }

    public void Apply()
    {
        if (_targetZone == null) return;
        
        _targetZone.Title = _newTitle;
        _targetZone.SubTitle = _newSubtitle;
        _targetZone.TitleColor = _newTitleColor;
        _targetZone.TitleStroke = _newStrokeColor;
        _targetZone.IconProvider = _newIcon;
        _targetZone.Priority = _newPriority;

        if (_targetZone.IsFresh)
        {
            _targetZone.Initialize();
        }
        else
        {
            _targetZone.SendVisualData();
        }
    }
    
    private LocalizedText Localize(string key) => Language.GetText($"Mods.ZoneTitles.UI.ZonePanel.{key}");
}
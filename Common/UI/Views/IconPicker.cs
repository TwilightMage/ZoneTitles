using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.GameContent.UI.Elements;
using Terraria.ID;
using Terraria.Localization;
using Terraria.UI.Chat;
using ZoneTitles.Common.Systems;
using ZoneTitles.Common.UI.Elements;
using ZoneTitles.Common.UI.Inputs;

namespace ZoneTitles.Common.UI.Views;

public class IconPickerMenu : Widget
{
    public const int NumColumns = 10;
    public const int ColumnSpace = 10;
    
    public delegate void IconSelectedDelegate(IconSystem.IconProvider icon);

    public event IconSelectedDelegate OnIconSelected;
    public event Action OnDesiredSizeChanged;

    protected void SelectIcon(IconSystem.IconProvider icon)
    {
        OnIconSelected?.Invoke(icon);
    }

    protected void DesiredSizeChanged()
    {
        OnDesiredSizeChanged?.Invoke();
    }

    public static int CalculateSizeForGrid(int numCells, int cellSize, int space)
    {
        return numCells * (cellSize + space) - space;
    }

    public static int CalculateHeightForGrid(int numItems, int numColumns, int cellSize, int space)
    {
        return CalculateSizeForGrid(((int)MathF.Ceiling(numItems / (float)NumColumns)), cellSize, ColumnSpace);
    }

    public static int CalculateXInGrid(int index, int numColumns, int cellSize, int space)
    {
        return (index % numColumns) * (cellSize + space);
    }
    
    public static int CalculateYInGrid(int index, int numColumns, int cellSize, int space)
    {
        return (index / numColumns) * (cellSize + space);
    }
    
    public virtual Point GetDesiredSize() => new Point();
    public virtual bool HasDesiredSize() => false;
}

public class IconPicker : View
{
    public event IconPickerMenu.IconSelectedDelegate OnIconSelected;

    private ColoredImageButton _closeButton;
    private PanelWidget _helpPanel;
    private UIText _helpText;
    private ColoredImageButton _helpButton;
    private OptionButton<int>[] _menuButtons;
    private int _activeCategory = 0;
    
    private IconPickerMenu _activePicker;
    private BestiaryIconPicker _bestiaryPicker;
    private ItemIconPicker _itemPicker;
    private MiscIconPicker _miscPicker;
    private UrlIconPicker _urlPicker;

    public IconPicker()
    {
        Left.Set(0, 0);
        Top.Set(0, 0);
        Width.Set(310, 0);
        Height.Set(600, 0);
        SetPadding(10);
    }

    public override void OnInitialize()
    {
        int y = 0;
        
        var title = new UIText(Localize("Title"));
        title.Width.Set(0, 1);
        title.Height.Set(25, 0);
        title.TextOriginX = 0.5f;
        title.TextOriginY = 0.5f;
        Append(title);
        
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

        _helpPanel = new PanelWidget();
        _helpPanel.Left.Set(-10, 0);
        _helpPanel.Width.Set(20, 1);
        _helpPanel.MaxWidth.Set(float.MaxValue, 0);
        _helpPanel.BackgroundColor = Color.LightSeaGreen;
        _helpPanel.BorderColor = Color.SeaGreen;
        _helpPanel.SetPadding(10);

        _helpText = new UIText(LocalizedText.Empty);
        _helpText.Width.Set(0, 1);
        _helpText.Height.Set(0, 1);
        _helpText.TextOriginX = 0;
        _helpPanel.Append(_helpText);

        _helpButton = new ColoredImageButton(Main.Assets.Request<Texture2D>("Images/UI/Bestiary/Icon_Locked", AssetRequestMode.ImmediateLoad));
        _helpButton.ResizeToFitHeight(24);
        _helpButton.Left.Set(-24 - 10, 0);
        _helpButton.HAlign = 1;
        _helpButton.OnMouseOver += (evt, elem) =>
        {
            Append(_helpPanel);
        };
        _helpButton.OnMouseOut += (evt, elem) =>
        {
            _helpPanel.Remove();
        };
        Append(_helpButton);
        
        y += 25 + 10;

        _menuButtons = new OptionButton<int>[4];
        
        _menuButtons[0] = new OptionButton<int>(0, Localize("CategoryBestiary"), LocalizedText.Empty, Color.White, "Images/UI/WorldCreation/IconDifficultyNormal");
        _menuButtons[0].Top.Set(y, 0);
        _menuButtons[0].Width.Set(-5, 0.25f);
        _menuButtons[0].HAlign = 1f / 3 * 0;
        _menuButtons[0].OnLeftClick += (evt, elem) => SelectCategory(0);
        Append(_menuButtons[0]);
        
        _menuButtons[1] = new OptionButton<int>(1, Localize("CategoryItem"), LocalizedText.Empty, Color.White, "Images/Item_9");
        _menuButtons[1].Top.Set(y, 0);
        _menuButtons[1].Width.Set(-5, 0.25f);
        _menuButtons[1].HAlign = 1f / 3 * 1;
        _menuButtons[1].OnLeftClick += (evt, elem) => SelectCategory(1);
        Append(_menuButtons[1]);
        
        _menuButtons[2] = new OptionButton<int>(2, Localize("CategoryMisc"), LocalizedText.Empty, Color.White, "Images/UI/Bestiary/Icon_Rank_Light");
        _menuButtons[2].Top.Set(y, 0);
        _menuButtons[2].Width.Set(-5, 0.25f);
        _menuButtons[2].HAlign = 1f / 3 * 2;
        _menuButtons[2].OnLeftClick += (evt, elem) => SelectCategory(2);
        Append(_menuButtons[2]);
        
        _menuButtons[3] = new OptionButton<int>(3, Localize("CategoryWeb"), LocalizedText.Empty, Color.White, "Images/UI/Workshop/PublicityPublic");
        _menuButtons[3].Top.Set(y, 0);
        _menuButtons[3].Width.Set(-5, 0.25f);
        _menuButtons[3].HAlign = 1f / 3 * 3;
        _menuButtons[3].OnLeftClick += (evt, elem) => SelectCategory(3);
        Append(_menuButtons[3]);

        y += 37 + 20;
        
        _bestiaryPicker = new BestiaryIconPicker();
        _bestiaryPicker.Top.Set(y, 0);
        _bestiaryPicker.Width.Set(0, 1);
        _bestiaryPicker.Height.Set(0, 1);
        
        _itemPicker = new ItemIconPicker();
        _itemPicker.Top.Set(y, 0);
        _itemPicker.Width.Set(0, 1);
        _itemPicker.Height.Set(0, 1);

        _miscPicker = new MiscIconPicker();
        _miscPicker.Top.Set(y, 0);
        _miscPicker.Width.Set(0, 1);
        _miscPicker.Height.Set(0, 1);
        
        _urlPicker = new UrlIconPicker();
        _urlPicker.Top.Set(y, 0);
        _urlPicker.Width.Set(0, 1);
        _urlPicker.Height.Set(0, 1);
        
        SelectCategory(0);
    }

    public void SelectCategory(int index)
    {
        _activeCategory = index;
        for (int i = 0; i < _menuButtons.Length; i++)
        {
            _menuButtons[i].SetCurrentOption(index);
        }
        
        IconPickerMenu newPicker = null;
        switch (index)
        {
            case 0:
                newPicker = _bestiaryPicker;
                break;
            case 1:
                newPicker = _itemPicker;
                break;
            case 2:
                newPicker = _miscPicker;
                break;
            case 3:
                newPicker = _urlPicker;
                break;
        }
        
        if (_activePicker == newPicker) return;

        if (_activePicker != null)
        {
            _activePicker.Deactivate();
            _activePicker.OnIconSelected -= IconSelected;
            _activePicker.OnDesiredSizeChanged -= DesiredSizeChanged;
            _activePicker.Remove();
        }

        _activePicker = newPicker;

        if (_activePicker != null)
        {
            Append(_activePicker);
            _activePicker.OnIconSelected += IconSelected;
            _activePicker.OnDesiredSizeChanged += DesiredSizeChanged;
            _activePicker.Activate();
        }
        
        DesiredSizeChanged();

        Recalculate();
    }

    private void IconSelected(IconSystem.IconProvider icon)
    {
        OnIconSelected?.Invoke(icon);
    }

    private void DesiredSizeChanged()
    {
        LocalizedText helpText = LocalizedText.Empty;
        switch (_activeCategory)
        {
            case 0:
                helpText = Localize("HelpBestiary");
                break;
            case 1:
                helpText = Localize("HelpItem");
                break;
            case 2:
                helpText = Localize("HelpMisc");
                break;
            case 3:
                helpText = Localize("HelpURL");
                break;
        }
        
        if (_activePicker == null || !_activePicker.HasDesiredSize())
        {
            Width.Set(IconPickerMenu.CalculateSizeForGrid(IconPickerMenu.NumColumns, 40, IconPickerMenu.ColumnSpace) + 20, 0);
            Height.Set(10 + 25 + 10 + 37 + 10, 0);
        }
        else
        {
            Point desiredSize = _activePicker.GetDesiredSize();
            Width.Set(Math.Max(desiredSize.X + 20, 500), 0);
            Height.Set(10 + 25 + 10 + 37 + 20 + desiredSize.Y + 10, 0);
        }
        
        var wrappedHelpText = FontAssets.MouseText.Value.CreateWrappedText(helpText.Value, Width.Pixels - 20);
        var helpTextHeight = ChatManager.GetStringSize(FontAssets.MouseText.Value, wrappedHelpText, new Vector2(1)).Y;
        _helpText.SetText(wrappedHelpText);
        _helpText.Height.Set(helpTextHeight, 0);
        _helpPanel.Height.Set(helpTextHeight + 20, 0);
        _helpPanel.Top.Set(-helpTextHeight - 20 - 20, 0);
    }
    
    private LocalizedText Localize(string key)
    {
        return Language.GetText($"Mods.ZoneTitles.UI.IconPicker.{key}");
    }
}
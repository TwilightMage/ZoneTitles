using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent.UI.Elements;
using Terraria.GameContent.UI.States;
using Terraria.GameInput;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;
using Terraria.UI;
using ZoneTitles.Common.Systems;
using ZoneTitles.Common.UI.Inputs;
using ZoneTitles.Common.UI.Views;

namespace ZoneTitles.Common.UI.Elements;

[IconProvider(SourceMarker = "item")]
public class ItemIconProvider : IconSystem.IconProvider
{
    private Texture2D _cachedTexture;
    private string _name;
    private Item _sourceItem;
    private int? _frameIndex;
    private bool _transient;

    public Item SourceItem => _sourceItem;
    public int? Frame => _frameIndex;

    public override Texture2D GetTexture()
    {
        if (_cachedTexture == null && _sourceItem != null)
        {
            _cachedTexture = _sourceItem.BakeToTexture(_frameIndex ?? 0);
        }

        return _cachedTexture;
    }

    public override string GetName()
    {
        return _name;
    }

    public override void Serialize(TagCompound tag)
    {
        base.Serialize(tag);

        tag.Set("item", _sourceItem.ModItem?.FullName ?? _sourceItem.type.ToString());
        tag.Set("frame", _frameIndex);
    }

    public override void Deserialize(TagCompound tag)
    {
        _cachedTexture = null;
        _name = null;

        string id = tag.Get<string>("item");

        if (int.TryParse(id, out int type))
        {
            _sourceItem = ContentSamples.ItemsByType[type];
        }
        else if (ModContent.TryFind(id, out ModItem modItem))
        {
            _sourceItem = modItem.Item;
        }
        else
        {
            _sourceItem = null;
        }

        _frameIndex = tag.Get<int>("frame");

        ApplyFromItem(_sourceItem, _frameIndex);
    }

    public override void SerializeBinary(BinaryWriter writer)
    {
        base.SerializeBinary(writer);

        writer.Write(_sourceItem.ModItem?.FullName ?? _sourceItem.type.ToString());
        writer.Write(_frameIndex ?? -1);
    }

    public override void DeserializeBinary(BinaryReader reader)
    {
        _cachedTexture = null;
        _name = null;

        string id = reader.ReadString();

        if (int.TryParse(id, out int type))
        {
            _sourceItem = ContentSamples.ItemsByType[type];
        }
        else if (ModContent.TryFind(id, out ModItem modItem))
        {
            _sourceItem = modItem.Item;
        }
        else
        {
            _sourceItem = null;
        }

        _frameIndex = reader.ReadInt32();
        if (_frameIndex == -1) _frameIndex = null;

        ApplyFromItem(_sourceItem, _frameIndex);
    }

    public override void DeserializeBinaryFake(BinaryReader reader)
    {
        reader.ReadString();
        reader.ReadInt32();
    }

    public override void Draw(SpriteBatch spriteBatch, Vector2 position)
    {
        if (_sourceItem != null)
        {
            _sourceItem.DrawSimple(spriteBatch, position, frame: _frameIndex);
        }
    }

    private static string IdFromItem(Item item, int? frame)
    {
        return $"misc_{item.type}_{frame}";
    }

    private void ApplyFromItem(Item item, int? frame)
    {
        if (item != null)
        {
            _id = IdFromItem(item, frame);
            _sourceItem = item;
            _frameIndex = frame;

            _name = item.Name;
        }
    }

    public void ApplyTransientFromItem(Item item, int? frame)
    {
        if (_transient)
        {
            ApplyFromItem(item, frame);
        }
    }

    public static ItemIconProvider CreateFromItem(Item item, int? frame)
    {
        if (item != null)
        {
            string id = IdFromItem(item, frame);

            ItemIconProvider instance = (ItemIconProvider)GetProviderInstance(id);

            if (instance == null)
            {
                instance = new ItemIconProvider();
                instance.ApplyFromItem(item, frame);
                RegisterInstance(instance);
            }

            return instance;
        }

        return null;
    }

    public static ItemIconProvider CreateTransient()
    {
        var instance = new ItemIconProvider();
        instance._transient = true;

        return instance;
    }
}

public class ItemIconPicker : IconPickerMenu
{
    private const int NumRowsVisible = 10;

    private UIPanel _searchPanel;
    private UISearchBar _searchBar;
    private string _searchString;
    private bool _justSearchMouseLeftDown;

    private ClippedContainer _listView;
    private IconButton[] _iconButtonPool = new IconButton[(NumRowsVisible + 1) * NumColumns];
    private UIScrollbar _scrollbar;
    private Item[] _currentItemSet = new Item[ContentSamples.ItemsByType.Count - 1];
    private int _currentItemSetSize;
    private bool _filterRequired;
    private bool _filteringFinished = true;
    private bool _filteringJustFinished;
    private double _lastFilterSeconds;
    private int _oldFirstVisibleRow;
    private float _oldScroll;

    private Widget _frameSelector;
    private List<IconButton> _frameButtons = new List<IconButton>();
    private Pool<IconButton> _frameButtonsPool = new Pool<IconButton>(() => new IconButton());
    private bool FrameSelectorVisible => _frameSelector.Parent == this;

    public ItemIconPicker()
    {
        ClickTransparent = true;
    }

    public override void OnInitialize()
    {
        base.OnInitialize();

        // Search panel
        _searchPanel = new UIPanel();
        _searchPanel.Width.Set(0, 1);
        _searchPanel.Height.Set(27, 0);
        _searchPanel.BackgroundColor = new Color(35, 40, 83);
        _searchPanel.BorderColor = new Color(35, 40, 83);
        _searchPanel.SetPadding(0.0f);
        _searchPanel.OnLeftMouseDown += (evt, elem) =>
        {
            _justSearchMouseLeftDown = true;
        };
        _searchPanel.OnLeftClick += (evt, elem) =>
        {
            if (!_searchBar.IsWritingText)
            {
                _searchBar.ToggleTakingText();
            }
        };
        _searchPanel.OnRightClick += (evt, elem) =>
        {
            _searchBar.SetContents(null, true);
            if (!_searchBar.IsWritingText)
            {
                _searchBar.ToggleTakingText();
            }
        };
        Append(_searchPanel);

        _searchBar = new UISearchBar(Localize("SearchBarPlaceholder"), 0.8f);
        _searchBar.Width.Set(0, 1);
        _searchBar.Height.Set(0, 1);
        _searchBar.IgnoresMouseInteraction = true;
        _searchBar.OnContentsChanged += (contents) =>
        {
            if (_searchString == contents) return;

            _searchString = contents;
            Task.Run(FilterItemSet);
        };
        _searchBar.OnStartTakingInput += () => _searchPanel.BorderColor = Main.OurFavoriteColor;
        _searchBar.OnEndTakingInput += () => _searchPanel.BorderColor = new Color(35, 40, 83);
        _searchBar.OnNeedingVirtualKeyboard += () =>
        {
            int length = 40;
            UIVirtualKeyboard uiVirtualKeyboard = new UIVirtualKeyboard(
                Localize("SearchBarPlaceholder").Value,
                _searchString,
                (name) =>
                {
                    _searchBar.SetContents(name.Trim());
                    GoBackHere();
                },
                GoBackHere,
                3,
                true);
            uiVirtualKeyboard.SetMaxInputLength(length);
            uiVirtualKeyboard.CustomEscapeAttempt = () =>
            {
                IngameFancyUI.Close();
                Main.playerInventory = true;
                if (_searchBar.IsWritingText)
                {
                    _searchBar.ToggleTakingText();
                }

                return true;
            };
            IngameFancyUI.OpenUIState(uiVirtualKeyboard);
        };
        _searchBar.OnCanceledTakingInput += () => _searchPanel.BorderColor = new Color(35, 40, 83);
        _searchPanel.Append(_searchBar);

        UIImageButton search_cancel = new UIImageButton(Main.Assets.Request<Texture2D>("Images/UI/SearchCancel", AssetRequestMode.ImmediateLoad));
        search_cancel.HAlign = 1f;
        search_cancel.VAlign = 0.5f;
        search_cancel.Left.Set(-2, 0);
        ;
        search_cancel.OnMouseOver += (evt, elem) =>
        {
            SoundEngine.PlaySound(SoundID.MenuTick);
        };
        search_cancel.OnLeftClick += (evt, elem) =>
        {
            if (_searchBar.HasContents)
            {
                _searchBar.SetContents(null, true);
                SoundEngine.PlaySound(SoundID.MenuClose);
            }
            else
            {
                SoundEngine.PlaySound(SoundID.MenuTick);
            }
        };
        _searchPanel.Append(search_cancel);

        // List view
        _listView = new ClippedContainer();
        _listView.Width.Set(0, 1);
        _listView.Height.Set(CalculateSizeForGrid(NumRowsVisible, 40, ColumnSpace), 0);
        _listView.Top.Set(_searchPanel.Top.Pixels + _searchPanel.Height.Pixels + 10, 0);
        _listView.OnMouseOver += (evt, elem) =>
        {
            PlayerInput.LockVanillaMouseScroll("ZoneTitles/IconList");
        };
        _listView.OnScrollWheel += (evt, elem) =>
        {
            _scrollbar.ViewPosition -= evt.ScrollWheelValue;
        };
        Append(_listView);

        for (int i = 0; i < _iconButtonPool.Length; i++)
        {
            _iconButtonPool[i] = new IconButton();
            _iconButtonPool[i].Icon = ItemIconProvider.CreateTransient();
            _iconButtonPool[i].Left.Set(CalculateXInGrid(i, NumColumns, 40, ColumnSpace), 0);
            _iconButtonPool[i].Top.Set(CalculateYInGrid(i, NumColumns, 40, ColumnSpace), 0);
            _iconButtonPool[i].OnLeftClick += IconClicked;
        }

        _scrollbar = new UIScrollbar();
        _scrollbar.Height.Set(CalculateSizeForGrid(NumRowsVisible, 40, ColumnSpace) - 10, 0);
        _scrollbar.HAlign = 1;
        _scrollbar.VAlign = 0.5f;
        _listView.Append(_scrollbar);

        // Frame selector
        _frameSelector = new Widget();
        _frameSelector.Top.Set(_listView.Top.Pixels + _listView.Height.Pixels + 10, 0);
        _frameSelector.Width.Set(0, 1);
        _frameSelector.Height.Set(20 + 10 + 40, 0);
        _frameSelector.ClickTransparent = true;

        var frameSelectorTitle = new UIText(Localize("FrameSelectorTitle"));
        frameSelectorTitle.Width.Set(0, 1);
        frameSelectorTitle.Height.Set(20, 0);
        frameSelectorTitle.TextOriginY = 0.5f;
        _frameSelector.Append(frameSelectorTitle);

        _filterRequired = true;
    }

    private void AddFrameToSelector(Item item, int frame)
    {
        IconButton button = _frameButtonsPool.Allocate();

        var icon = ItemIconProvider.CreateTransient();
        icon.ApplyTransientFromItem(item, frame);

        button.Left.Set(CalculateXInGrid(frame, int.MaxValue, 40, ColumnSpace), 0);
        button.Top.Set(20 + 10, 0);
        button.Icon = icon;
        button.OnLeftClick += SelectorFrameClicked;
        _frameSelector.Append(button);

        _frameButtons.Add(button);
    }

    private void ClearFrameSelector()
    {
        foreach (var button in _frameButtons)
        {
            button.Remove();
            button.Icon = null;
            button.OnLeftClick -= SelectorFrameClicked;

            _frameButtonsPool.Free(button);
        }

        _frameButtons.Clear();
    }

    private void SelectorFrameClicked(UIMouseEvent evt, UIElement element)
    {
        var button = (IconButton)element;
        var icon = (ItemIconProvider)button.Icon;
        SelectIcon(ItemIconProvider.CreateFromItem(icon.SourceItem, icon.Frame));
    }

    private void GoBackHere()
    {
        IngameFancyUI.Close();
        _searchBar.ToggleTakingText();
    }

    private void FilterItemSet()
    {
        var filterStart = DateTime.Now;
        _filteringFinished = false;
        if (string.IsNullOrWhiteSpace(_searchString))
        {
            _currentItemSetSize = 0;
            for (int i = 1; i < ContentSamples.ItemsByType.Count && !_filterRequired; i++)
            {
                if (ContentSamples.ItemsByType[i].type != ItemID.None)
                {
                    _currentItemSet[_currentItemSetSize++] = ContentSamples.ItemsByType[i];
                }
            }

            _filteringFinished = !_filterRequired;
        }
        else
        {
            bool filterById = false;
            string pattern = _searchString.ToLower();

            if (pattern.StartsWith("#"))
            {
                pattern = pattern.Substring(1);
                filterById = true;
            }

            _currentItemSetSize = 0;
            for (int i = 1; i < ContentSamples.ItemsByType.Count && !_filterRequired; i++)
            {
                if (ContentSamples.ItemsByType[i].type != ItemID.None &&
                    filterById
                        ? ContentSamples.ItemsByType[i].type.ToString().StartsWith(pattern)
                        : ContentSamples.ItemsByType[i].Name.ToLower().Contains(pattern))
                {
                    _currentItemSet[_currentItemSetSize++] = ContentSamples.ItemsByType[i];
                }
            }

            _filteringFinished = !_filterRequired;
        }

        _lastFilterSeconds = (DateTime.Now - filterStart).TotalSeconds;

        _filteringJustFinished = _filteringFinished;
    }

    public override void Update(GameTime gameTime)
    {
        base.Update(gameTime);

        if (UISystem.JustMouseLeftDown && !_justSearchMouseLeftDown && _searchBar.IsWritingText)
        {
            _searchBar.ToggleTakingText();
        }

        _justSearchMouseLeftDown = false;

        if (_filterRequired)
        {
            _filterRequired = false;

            FilterItemSet();
        }

        int firstVisibleRow = (int)(_scrollbar.ViewPosition / (40 + ColumnSpace));
        int firstVisibleIndex = firstVisibleRow * NumColumns;

        if (_filteringJustFinished || _oldFirstVisibleRow != firstVisibleRow)
        {
            _filteringJustFinished = false;
            _oldFirstVisibleRow = firstVisibleRow;

            _scrollbar.SetView(CalculateSizeForGrid(NumRowsVisible, 40, ColumnSpace), CalculateHeightForGrid(_currentItemSetSize, NumColumns, 40, ColumnSpace));

            for (int i = 0; i < _iconButtonPool.Length; i++)
            {
                int j = i + firstVisibleIndex;
                if (j < _currentItemSetSize)
                {
                    if (_iconButtonPool[i].Parent != _listView) _listView.Append(_iconButtonPool[i]);
                    ((ItemIconProvider)_iconButtonPool[i].Icon).ApplyTransientFromItem(_currentItemSet[j], null);
                }
                else
                {
                    _iconButtonPool[i].Remove();
                }
            }
        }

        if (_oldScroll != _scrollbar.ViewPosition)
        {
            _oldScroll = _scrollbar.ViewPosition;

            float offset = _scrollbar.ViewPosition % (40 + 10);

            for (int i = 0; i < _iconButtonPool.Length; i++)
            {
                _iconButtonPool[i].MarginTop = -offset;
                _iconButtonPool[i].Recalculate();
            }
        }
    }

    private void IconClicked(UIMouseEvent evt, UIElement element)
    {
        if (_searchBar.IsWritingText)
        {
            _searchBar.ToggleTakingText();
        }

        var button = (IconButton)element;
        var icon = (ItemIconProvider)button.Icon;
        SetFrameSelectorTarget(icon.SourceItem);

        if (Main.itemAnimations[icon.SourceItem.type] == null)
        {
            SelectIcon(ItemIconProvider.CreateFromItem(icon.SourceItem, icon.Frame));
        }
    }

    private void SetFrameSelectorTarget(Item newTargetItem)
    {
        var animation = Main.itemAnimations[newTargetItem.type];
        bool newState = animation != null;

        ClearFrameSelector();

        if (newState)
        {
            if (!FrameSelectorVisible)
            {
                Append(_frameSelector);
            }

            for (int i = 0; i < animation.FrameCount; i++)
            {
                AddFrameToSelector(newTargetItem, i);
            }
        }
        else
        {
            _frameSelector.Remove();
        }

        DesiredSizeChanged();
    }

    private LocalizedText Localize(string key)
    {
        return Language.GetText($"Mods.ZoneTitles.UI.ItemIconPicker.{key}");
    }

    public override Point GetDesiredSize() => new Point(CalculateSizeForGrid(NumColumns, 40, ColumnSpace) + 10 + 20, (int)_searchPanel.Height.Pixels + 10 + (int)_listView.Height.Pixels + (FrameSelectorVisible ? 10 + (int)_frameSelector.Height.Pixels : 0));
    public override bool HasDesiredSize() => true;
}
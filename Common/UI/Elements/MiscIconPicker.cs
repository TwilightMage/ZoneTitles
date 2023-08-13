using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Terraria;
using Terraria.ModLoader.IO;
using Terraria.UI;
using ZoneTitles.Common.Systems;
using ZoneTitles.Common.UI.Inputs;
using ZoneTitles.Common.UI.Views;

namespace ZoneTitles.Common.UI.Elements;

[IconProvider(SourceMarker = "misc")]
public class MiscIconProvider : IconSystem.IconProvider
{
    private class MiscIconData
    {
        public string Path;
        public Rectangle? Frame;
        public string Name;
    }

    private static Dictionary<string, MiscIconData> _miscEntries = new Dictionary<string, MiscIconData>();

    private Texture2D _sourceTexture;
    private Rectangle _sourceFrame;
    private Texture2D _cachedClippedTexture;
    private string _name;
    private string _key;
    private float _scale;

    static MiscIconProvider()
    {
        _miscEntries.Add("camera"          , new MiscIconData{Frame = null, Name = "Camera"          , Path = "Images/UI/Camera_0"});
        _miscEntries.Add("settings"        , new MiscIconData{Frame = null, Name = "Settings"        , Path = "Images/UI/Camera_1"});
        _miscEntries.Add("border_corners"  , new MiscIconData{Frame = null, Name = "Border Corners"  , Path = "Images/UI/Camera_2"});
        _miscEntries.Add("border_edge"     , new MiscIconData{Frame = null, Name = "Border Edge"     , Path = "Images/UI/Camera_3"});
        _miscEntries.Add("camera_shut"     , new MiscIconData{Frame = null, Name = "Camera Shut"     , Path = "Images/UI/Camera_4"});
        _miscEntries.Add("quit"            , new MiscIconData{Frame = null, Name = "Quit"            , Path = "Images/UI/Camera_5"});
        _miscEntries.Add("folder"          , new MiscIconData{Frame = null, Name = "Folder"          , Path = "Images/UI/Camera_6"});
        _miscEntries.Add("camera_flash"    , new MiscIconData{Frame = null, Name = "Camera Flash"    , Path = "Images/UI/Camera_7"});
        _miscEntries.Add("craft_grid"      , new MiscIconData{Frame = null, Name = "Craft Grid"      , Path = "Images/UI/Craft_Toggle_0"});
        _miscEntries.Add("craft_rows"      , new MiscIconData{Frame = null, Name = "Craft Rows"      , Path = "Images/UI/Craft_Toggle_2"});
        _miscEntries.Add("grappling_hook"  , new MiscIconData{Frame = null, Name = "Grappling Hook"  , Path = "Images/UI/DisplaySlots_3"});
        _miscEntries.Add("house"           , new MiscIconData{Frame = null, Name = "House"           , Path = "Images/UI/DisplaySlots_5"});
        _miscEntries.Add("open_camera"     , new MiscIconData{Frame = null, Name = "Open Camera"     , Path = "Images/UI/DisplaySlots_10"});
        _miscEntries.Add("emote"           , new MiscIconData{Frame = new Rectangle(0, 0, 30, 30), Name = "Emote", Path = "Images/UI/Emotes"});
        _miscEntries.Add("hammer"          , new MiscIconData{Frame = null, Name = "Hammer"          , Path = "Images/UI/Reforge_0"});
        _miscEntries.Add("sort"            , new MiscIconData{Frame = null, Name = "Sort"            , Path = "Images/UI/Sort_0"});
        _miscEntries.Add("exclamation_mark", new MiscIconData{Frame = null, Name = "Exclamation Mark", Path = "Images/UI/UI_quickicon1"});
        _miscEntries.Add("male"            , new MiscIconData{Frame = null, Name = "Male"            , Path = "Images/UI/CharCreation/ClothStyleMale"});
        _miscEntries.Add("female"          , new MiscIconData{Frame = null, Name = "Female"          , Path = "Images/UI/CharCreation/ClothStyleFemale"});
        _miscEntries.Add("eye"             , new MiscIconData{Frame = null, Name = "Eye"             , Path = "Images/UI/CharCreation/ColorEyeBack"});
        _miscEntries.Add("journey"         , new MiscIconData{Frame = null, Name = "Journey"         , Path = "Images/UI/Creative/Journey_Toggle"});
        _miscEntries.Add("heart"           , new MiscIconData{Frame = null, Name = "Heart"           , Path = "Images/UI/PlayerResourceSets/FancyClassic/Heart_Fill"});
        _miscEntries.Add("golden_heart"    , new MiscIconData{Frame = null, Name = "Golden Heart"    , Path = "Images/UI/PlayerResourceSets/FancyClassic/Heart_Fill_B"});
        _miscEntries.Add("mana_star"       , new MiscIconData{Frame = null, Name = "Mana Star"       , Path = "Images/UI/PlayerResourceSets/FancyClassic/Star_Fill"});
        _miscEntries.Add("friends"         , new MiscIconData{Frame = null, Name = "Friends"         , Path = "Images/UI/Workshop/PublicityFriendsOnly"});
        _miscEntries.Add("lock"            , new MiscIconData{Frame = null, Name = "Lock"            , Path = "Images/UI/Workshop/PublicityPrivate"});
        _miscEntries.Add("public"          , new MiscIconData{Frame = null, Name = "Public"          , Path = "Images/UI/Workshop/PublicityPublic"});
        _miscEntries.Add("upload"          , new MiscIconData{Frame = null, Name = "Upload"          , Path = "Images/UI/Workshop/Publish"});
        _miscEntries.Add("tag"             , new MiscIconData{Frame = null, Name = "Tag"             , Path = "Images/UI/Workshop/Tags"});

        _miscEntries.Add("peace_neutral"   , new MiscIconData{Frame = new Rectangle(0, 0, 36, 36), Name = "Peace Neutral", Path = "Images/UI/PVP_0"});
        _miscEntries.Add("peace_red"       , new MiscIconData{Frame = new Rectangle(0, 38, 36, 36), Name = "Peace Red", Path = "Images/UI/PVP_0"});
        _miscEntries.Add("peace_green"     , new MiscIconData{Frame = new Rectangle(0, 76, 36, 36), Name = "Peace Green", Path = "Images/UI/PVP_0"});
        _miscEntries.Add("peace_blue"      , new MiscIconData{Frame = new Rectangle(0, 114, 36, 36), Name = "Peace Blue", Path = "Images/UI/PVP_0"});
        _miscEntries.Add("peace_yellow"    , new MiscIconData{Frame = new Rectangle(0, 152, 36, 36), Name = "Peace Yellow", Path = "Images/UI/PVP_0"});
        _miscEntries.Add("peace_purple"    , new MiscIconData{Frame = new Rectangle(0, 190, 36, 36), Name = "Peace Purple", Path = "Images/UI/PVP_0"});

        _miscEntries.Add("war_neutral"     , new MiscIconData{Frame = new Rectangle(76, 0, 36, 36), Name = "War Neutral", Path = "Images/UI/PVP_0"});
        _miscEntries.Add("war_red"         , new MiscIconData{Frame = new Rectangle(76, 38, 36, 36), Name = "War Red", Path = "Images/UI/PVP_0"});
        _miscEntries.Add("war_green"       , new MiscIconData{Frame = new Rectangle(76, 76, 36, 36), Name = "War Green", Path = "Images/UI/PVP_0"});
        _miscEntries.Add("war_blue"        , new MiscIconData{Frame = new Rectangle(76, 114, 36, 36), Name = "War Blue", Path = "Images/UI/PVP_0"});
        _miscEntries.Add("war_yellow"      , new MiscIconData{Frame = new Rectangle(76, 152, 36, 36), Name = "War Yellow", Path = "Images/UI/PVP_0"});
        _miscEntries.Add("war_purple"      , new MiscIconData{Frame = new Rectangle(76, 190, 36, 36), Name = "War Purple", Path = "Images/UI/PVP_0"});

        _miscEntries.Add("team_neutral"    , new MiscIconData{Frame = new Rectangle(0, 0, 16, 16), Name = "Team Neutral", Path = "Images/UI/PVP_1"});
        _miscEntries.Add("team_red"        , new MiscIconData{Frame = new Rectangle(18, 0, 16, 16), Name = "Team Red", Path = "Images/UI/PVP_1"});
        _miscEntries.Add("team_green"      , new MiscIconData{Frame = new Rectangle(36, 0, 16, 16), Name = "Team Green", Path = "Images/UI/PVP_1"});
        _miscEntries.Add("team_blue"       , new MiscIconData{Frame = new Rectangle(54, 0, 16, 16), Name = "Team Blue", Path = "Images/UI/PVP_1"});
        _miscEntries.Add("team_yellow"     , new MiscIconData{Frame = new Rectangle(72, 0, 16, 16), Name = "Team Yellow", Path = "Images/UI/PVP_1"});
        _miscEntries.Add("team_purple"     , new MiscIconData{Frame = new Rectangle(90, 0, 16, 16), Name = "Team Purple", Path = "Images/UI/PVP_1"});
    }

    public static int GetNumKeys() => _miscEntries.Count;
    public static List<string> GetKeysCopy() => _miscEntries.Keys.ToList();

    public override Texture2D GetTexture()
    {
        if (_cachedClippedTexture == null && _sourceTexture != null)
        {
            _cachedClippedTexture = _sourceTexture.ClipRect(_sourceFrame);
            if (_scale != 1)
            {
                _cachedClippedTexture = _cachedClippedTexture.Resize(new Point((int)(_cachedClippedTexture.Width * _scale), (int)(_cachedClippedTexture.Height * _scale)));
            }
        }

        return _cachedClippedTexture;
    }

    public override string GetName()
    {
        return _name;
    }

    public override void Serialize(TagCompound tag)
    {
        base.Serialize(tag);
        
        tag.Set("key", _key);
    }

    public override void Deserialize(TagCompound tag)
    {
        _cachedClippedTexture = null;
        _name = null;
        _sourceTexture = null;
        
        _key = tag.Get<string>("key");
        
        ApplyFromKey(_key);
    }

    public override void SerializeBinary(BinaryWriter writer)
    {
        base.SerializeBinary(writer);
        
        writer.Write(_key);
    }

    public override void DeserializeBinary(BinaryReader reader)
    {
        _cachedClippedTexture = null;
        _name = null;
        _sourceTexture = null;
        
        _key = reader.ReadString();
        
        ApplyFromKey(_key);
    }

    public override void DeserializeBinaryFake(BinaryReader reader)
    {
        reader.ReadString();
    }

    public override void Draw(SpriteBatch spriteBatch, Vector2 position)
    {
        if (_sourceTexture != null)
        {
            spriteBatch.Draw(_sourceTexture, position, _sourceFrame, Color.White, 0, new Vector2(_sourceFrame.Width / 2f, _sourceFrame.Height / 2f), _scale, SpriteEffects.None, 0);
        }
    }
    
    private static string IdFromKey(string key)
    {
        return $"misc_{key.GetHashCode()}";
    }

    private void ApplyFromKey(string key)
    {
        if (_miscEntries.TryGetValue(key, out MiscIconData entry))
        {
            _id = IdFromKey(key);
            _key = key;

            _name = entry.Name;
            _sourceTexture = Main.Assets.Request<Texture2D>(entry.Path, AssetRequestMode.ImmediateLoad).Value;
            _sourceFrame = entry.Frame ?? _sourceTexture.Bounds;

            _scale = _sourceFrame.Height > 28 ? 28f / _sourceFrame.Height : 1;
        }
    }

    public static MiscIconProvider CreateFromKey(string key)
    {
        if (_miscEntries.ContainsKey(key))
        {
            string id = IdFromKey(key);

            MiscIconProvider instance = (MiscIconProvider)GetProviderInstance(id);

            if (instance == null)
            {
                instance = new MiscIconProvider();
                instance.ApplyFromKey(key);
                RegisterInstance(instance);
            }

            return instance;
        }

        return null;
    }
}

public class MiscIconPicker : IconPickerMenu
{
    public MiscIconPicker()
    {
        ClickTransparent = true;
    }
    
    public override void OnInitialize()
    {
        base.OnInitialize();

        var keys = MiscIconProvider.GetKeysCopy();
        for (int i = 0; i < keys.Count; i++)
        {
            var iconButton = new IconButton();
            iconButton.Icon = MiscIconProvider.CreateFromKey(keys[i]);
            iconButton.Left.Set(CalculateXInGrid(i, NumColumns, 40, ColumnSpace), 0);
            iconButton.Top.Set(CalculateYInGrid(i, NumColumns, 40, ColumnSpace), 0);
            iconButton.OnClick += IconClicked;
            Append(iconButton);
        }
    }

    private void IconClicked(UIMouseEvent evt, UIElement listeningElement)
    {
        if (evt.Target is IconButton button)
        {
            SelectIcon(button.Icon);
        }
    }

    public override Point GetDesiredSize() => new Point(CalculateSizeForGrid(NumColumns, 40, ColumnSpace), CalculateHeightForGrid(MiscIconProvider.GetNumKeys(), NumColumns, 40, ColumnSpace));
    public override bool HasDesiredSize() => true;
}
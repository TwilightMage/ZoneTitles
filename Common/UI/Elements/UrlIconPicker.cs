using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.OS;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Terraria;
using Terraria.GameContent;
using Terraria.GameContent.UI.Elements;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;
using Terraria.UI;
using ZoneTitles.Common.Configs;
using ZoneTitles.Common.Systems;
using ZoneTitles.Common.UI.Inputs;
using ZoneTitles.Common.UI.Views;

namespace ZoneTitles.Common.UI.Elements;

[IconProvider(SourceMarker = "url")]
public class UrlIconProvider : IconSystem.IconProvider
{
    private static List<UrlIconProvider> _recent = new List<UrlIconProvider>();
 
    public static event Action<UrlIconProvider> OnRecentAdded;
    public static event Action<UrlIconProvider> OnRecentRemoved;
    
    private Texture2D _cachedTexture;
    private string _cachedName;
    private string _url;

    private Mutex _errorMutex = new Mutex();

    public bool HaveTextureLoaded => _cachedTexture != null;
    public bool HaveError => Error != null;

    public event Action<UrlIconProvider> OnStatusUpdated;

    public string Error
    {
        get => _error;
        private set
        {
            _errorMutex.WaitOne();
            
            _error = value;
            OnStatusUpdated?.Invoke(this);

            if (value != null && _recent.Remove(this))
            {
                OnRecentRemoved?.Invoke(this);
            }
            
            _errorMutex.ReleaseMutex();
        }
    }
    private string _error;

    public override Texture2D GetTexture()
    {
        return _cachedTexture;
    }

    public override string GetName()
    {
        return _cachedName;
    }

    public static int GetNumRecent()
    {
        return _recent.Count;
    }

    public static UrlIconProvider GetRecent(int index)
    {
        return _recent[index];
    }

    public override void Serialize(TagCompound tag)
    {
        base.Serialize(tag);
        
        tag.Set("url", _url);
    }

    public override void Deserialize(TagCompound tag)
    {
        _cachedTexture = null;
        
        _url = tag.Get<string>("url");
        
        ApplyFromUrl(_url);

        if (!_recent.Contains(this) && !HaveError)
        {
            _recent.Add(this);
            OnRecentAdded?.Invoke(this);
        }
    }

    public override void SerializeBinary(BinaryWriter writer)
    {
        base.SerializeBinary(writer);
        
        writer.Write(_url);
    }

    public override void DeserializeBinary(BinaryReader reader)
    {
        _cachedTexture = null;
        
        _url = reader.ReadString();
        
        ApplyFromUrl(_url);

        if (!_recent.Contains(this) && Error == null)
        {
            _recent.Add(this);
            OnRecentAdded?.Invoke(this);
        }
    }

    public override void DeserializeBinaryFake(BinaryReader reader)
    {
        reader.ReadString();
    }

    private static string IdFromUrl(string url)
    {
        return $"url_{url.GetHashCode()}";
    }
    
    private void ApplyFromUrl(string url)
    {
        if (string.IsNullOrWhiteSpace(url)) return;

        _id = IdFromUrl(url);
        _url = url;
        _cachedTexture = null;
        Error = null;

        var config = ModContent.GetInstance<ClientConfig>();
        if (!config.AllowURLIcons)
        {
            Error = Localize("Disabled").Value;
            return;
        }

        if (!Uri.IsWellFormedUriString(url, UriKind.Absolute))
        {
            Error = Localize("InvalidUrl").Value;
            return;
        }
        
        Uri uri = new Uri(url);
        _cachedName = Path.GetFileName(uri.LocalPath).Replace("_", " ");

        Task.Run(() =>
        {
            try
            {
                if (config.LimitURLIconsSize)
                {
                    if (WebRequest.Create(uri) is HttpWebRequest sizeRequest)
                    {
                        sizeRequest.UserAgent = "ZoneTitlesMod";
                        sizeRequest.UseDefaultCredentials = true;
                        sizeRequest.Method = "HEAD";
                        using (WebResponse resp = sizeRequest.GetResponse())
                        {
                            if (long.TryParse(resp.Headers.Get("Content-Length"), out long contentLength))
                            {
                                if (contentLength > config.URLIconSizeLimitKB * 1024)
                                {
                                    Error = Localize("TooBig").Value;
                                    return;
                                }
                            }
                            else
                            {
                                Error = Localize("SizeFetchFailed").Value;
                                return;
                            }
                        }
                    }
                    else
                    {
                        Error = Localize("NotHttp").Value;
                        return;
                    }
                }

                if (WebRequest.Create(uri) is HttpWebRequest imageRequest)
                {
                    imageRequest.UserAgent = "ZoneTitlesMod";
                    imageRequest.UseDefaultCredentials = true;

                    using (HttpWebResponse resp = (HttpWebResponse)imageRequest.GetResponse())
                    {
                        if ((resp.StatusCode != HttpStatusCode.OK &&
                             resp.StatusCode != HttpStatusCode.Moved &&
                             resp.StatusCode != HttpStatusCode.Redirect))
                        {
                            Error = $"{resp.StatusCode} - {resp.StatusDescription}";
                            return;
                        }

                        if (!resp.ContentType.StartsWith("image", StringComparison.OrdinalIgnoreCase))
                        {
                            Error = Localize("NotImage").Value;
                            return;
                        }

                        MemoryStream ms = new MemoryStream();
                        resp.GetResponseStream()?.CopyTo(ms);

                        Main.RunOnMainThread(() =>
                        {
                            try
                            {
                                _cachedTexture = Texture2D.FromStream(Main.graphics.GraphicsDevice, ms);
                                if (_cachedTexture != null)
                                {
                                    if (_cachedTexture.Height > 28)
                                    {
                                        float scale = 28f / _cachedTexture.Height;
                                        _cachedTexture = _cachedTexture.Resize(new Point((int)(_cachedTexture.Width * scale), (int)(_cachedTexture.Height * scale)));
                                    }
                                }
                            }
                            catch (Exception e)
                            {
                                _cachedTexture = null;
                                Error = Localize("CreateTextureFailed").Value;
                            }
                        });
                    }
                }
                else
                {
                    Error = Localize("NotHttp").Value;
                    return;
                }
            }
            catch (Exception e)
            {
                Error = Localize("GenericError").Value;
            }
        });
    }

    public static UrlIconProvider CreateFromUrl(string url)
    {
        if (!string.IsNullOrWhiteSpace(url))
        {
            string id = IdFromUrl(url);

            UrlIconProvider instance = (UrlIconProvider)GetProviderInstance(id);

            if (instance == null)
            {
                instance = new UrlIconProvider();
                _recent.Add(instance);
                OnRecentAdded?.Invoke(instance);
                instance.ApplyFromUrl(url);
                RegisterInstance(instance);
            }

            return instance;
        }

        return null;
    }

    public override void Draw(SpriteBatch spriteBatch, Vector2 position)
    {
        if (_cachedTexture != null)
        {
            spriteBatch.Draw(_cachedTexture, position, _cachedTexture.Bounds, Color.White, 0, new Vector2(_cachedTexture.Width / 2f, _cachedTexture.Height / 2f), Vector2.One, SpriteEffects.None, 0);
        }
        else if (Error == null)
        {
            ZoneUtils.DrawSunflowerLoading(spriteBatch, position, scale: 0.5f);
        }
    }
    
    private LocalizedText Localize(string key)
    {
        return Language.GetText($"Mods.ZoneTitles.UI.UrlIconProvider.{key}");
    }

    public static void ReloadIcons()
    {
        var icons = IconSystem.GetLoadedIcons();
        foreach (var icon in icons)
        {
            if (icon is UrlIconProvider urlIcon)
            {
                if (urlIcon.Error == null) continue;
                
                _recent.Add(urlIcon);
                OnRecentAdded?.Invoke(urlIcon);
                urlIcon.ApplyFromUrl(urlIcon._url);
            }
        }
    }
}

public class UrlIconPicker : IconPickerMenu
{
    private IconButton _iconButton;
    private UIText _errorText;
    private List<IconButton> _recentList = new List<IconButton>();

    public UrlIconPicker()
    {
        ClickTransparent = true;
    }

    ~UrlIconPicker()
    {
        UrlIconProvider.OnRecentAdded -= RecentAdded;
        UrlIconProvider.OnRecentRemoved -= RecentRemoved;
    }
    
    public override void OnInitialize()
    {
        var pasteLabel = Localize("PasteUrl");
        
        var pasteButton = new IconTextButton(pasteLabel, contentAlignmentX: 0.5f);
        pasteButton.Width.Set(FontAssets.MouseText.Value.MeasureString(pasteLabel.Value).X + 20, 0);
        pasteButton.HAlign = 0.5f;
        pasteButton.OnLeftClick += PasteClicked;
        Append(pasteButton);

        _iconButton = new IconButton();
        _iconButton.Top.Set(37 + 10, 0);
        _iconButton.HAlign = 0.5f;
        _iconButton.OnLeftClick += (evt, elem) =>
        {
            SelectIcon(_iconButton.Icon);
        };
        Append(_iconButton);

        _errorText = new UIText(LocalizedText.Empty);
        _errorText.Top.Set(37 + 10 + 40 + 10, 0);
        _errorText.Height.Set(30, 0);
        _errorText.HAlign = 0.5f;
        _errorText.TextOriginX = 0.5f;
        _errorText.TextOriginY = 0.5f;
        _errorText.TextColor = Color.Red;
        Append(_errorText);

        for (int i = 0; i < UrlIconProvider.GetNumRecent(); i++)
        {
            var iconButton = new IconButton();
            iconButton.Icon = UrlIconProvider.GetRecent(i);
            iconButton.Left.Set(CalculateXInGrid(i, NumColumns, 40, ColumnSpace), 0);
            iconButton.Top.Set(37 + 10 + 40 + 10 + 30 + 10 + CalculateYInGrid(i, NumColumns, 40, ColumnSpace), 0);
            iconButton.OnLeftClick += IconClicked;
            Append(iconButton);
        }

        UrlIconProvider.OnRecentAdded += RecentAdded;
        UrlIconProvider.OnRecentRemoved += RecentRemoved;
    }

    private void PasteClicked(UIMouseEvent evt, UIElement listeningElement)
    {
        if (_iconButton.Icon != null)
        {
            ((UrlIconProvider)_iconButton.Icon).OnStatusUpdated -= LatestIconStatusUpdated;
        }
        
        string url = Platform.Get<IClipboard>().Value;
        var icon = UrlIconProvider.CreateFromUrl(url);
        _iconButton.Icon = icon;
        LatestIconStatusUpdated(icon);

        if (icon != null)
        {
            icon.OnStatusUpdated += LatestIconStatusUpdated;
        }
    }
    
    private void IconClicked(UIMouseEvent evt, UIElement listeningElement)
    {
        if (evt.Target is IconButton button && button.Icon is UrlIconProvider icon && icon.HaveTextureLoaded)
        {
            SelectIcon(button.Icon);
        }
    }

    private void RecentAdded(UrlIconProvider added)
    {
        var iconButton = new IconButton();
        iconButton.Icon = added;
        iconButton.Left.Set(CalculateXInGrid(_recentList.Count, NumColumns, 40, ColumnSpace), 0);
        iconButton.Top.Set(37 + 10 + 40 + 10 + 30 + 10 + CalculateYInGrid(_recentList.Count, NumColumns, 40, ColumnSpace), 0);
        iconButton.OnLeftClick += IconClicked;
        Append(iconButton);
        _recentList.Add(iconButton);
        
        DesiredSizeChanged();
    }

    private void RecentRemoved(UrlIconProvider removed)
    {
        var button = _recentList.FirstOrDefault(button => button.Icon == removed);
        if (button != null)
        {
            button.Remove();
            _recentList.Remove(button);

            int i = 0;
            foreach (var iconButton in _recentList)
            {
                iconButton.Left.Set(CalculateXInGrid(i, NumColumns, 40, ColumnSpace), 0);
                iconButton.Top.Set(37 + 10 + 40 + 10 + 30 + 10 + CalculateYInGrid(i, NumColumns, 40, ColumnSpace), 0);
                i++;
            }
            
            DesiredSizeChanged();
        }
    }

    private void LatestIconStatusUpdated(UrlIconProvider icon)
    {
        _errorText.SetText(icon?.Error ?? "");
    }

    private LocalizedText Localize(string key)
    {
        return Language.GetText($"Mods.ZoneTitles.UI.UrlIconPicker.{key}");
    }

    public override Point GetDesiredSize() => new Point(CalculateSizeForGrid(NumColumns, 40, ColumnSpace), 37 + 10 + 40 + 10 + 30 + (UrlIconProvider.GetNumRecent() == 0 ? 0 : 10 + CalculateHeightForGrid(UrlIconProvider.GetNumRecent(), NumColumns, 40, ColumnSpace)));
    public override bool HasDesiredSize() => true;
}
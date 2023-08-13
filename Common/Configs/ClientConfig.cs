using Newtonsoft.Json;
using System;
using System.ComponentModel;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader.Config;
using ZoneTitles.Common.UI;
using ZoneTitles.Common.UI.Elements;

namespace ZoneTitles.Common.Configs;

public class ClientConfig : ModConfig
{
    public override ConfigScope Mode => ConfigScope.ClientSide;
    
    [Header("[i:4723] Icons")]
    
    [Label("Allow URL icons")]
    [Description("Allow URL icons to download data from WEB")]
    [DefaultValue(true)]
    public bool AllowURLIcons;
    
    [Label("Limit URL icons size")]
    [Description("Enable size limit for each URL icon")]
    [DefaultValue(false)]
    public bool LimitURLIconsSize;
    
    [Label("URL icon size limit (KB)")]
    [Description("Size limit for each icon in kilobytes")]
    [DefaultValue(1024)]
    public int URLIconSizeLimitKB;

    [JsonIgnore]
    [Label("Reload all URL icons")]
    public object ReloadAllURLIcons
    {
        get => null;
        set
        {
            SoundEngine.PlaySound(SoundID.Duck);
            UrlIconProvider.ReloadIcons();
        }
    }
}
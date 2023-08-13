using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Utilities;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace ZoneTitles.Common.Systems;

[AttributeUsage(AttributeTargets.Class)]
public class IconProviderAttribute : Attribute
{
    public string SourceMarker;
    public Func<string, IconSystem.IconProvider> CreateFromRawString;
}

public class IconSystem : ModSystem
{
    private class Module
    {
        public Func<IconProvider> Create;
        public Func<string, IconProvider> CreateFromRawString;
    }
    
    private static readonly List<IconProvider> _loadedInstances = new List<IconProvider>();
    private static readonly Dictionary<string, Module> _modules = new Dictionary<string, Module>();

    public static ReadOnlyCollection<IconProvider> GetLoadedIcons() => new ReadOnlyCollection<IconProvider>(_loadedInstances);

    public abstract class IconProvider
    {
        protected string _id;
    
        public abstract Texture2D GetTexture();
        public abstract string GetName();

        public virtual void Serialize(TagCompound tag)
        {
            tag.Set("id", _id);
            tag.Set("source", GetType().GetAttribute<IconProviderAttribute>().SourceMarker);
        }
    
        public abstract void Deserialize(TagCompound tag);

        public virtual void SerializeBinary(BinaryWriter writer)
        {
            writer.Write(_id);
            writer.Write(GetType().GetAttribute<IconProviderAttribute>().SourceMarker);
        }
    
        public abstract void DeserializeBinary(BinaryReader reader);
        public abstract void DeserializeBinaryFake(BinaryReader reader);
    
        public abstract void Draw(SpriteBatch spriteBatch, Vector2 position);

        public static IconProvider CreateFromRawString(string rawString)
        {
            if (!string.IsNullOrWhiteSpace(rawString))
            {
                var source = rawString.FirstWord();
                
                if (_modules.TryGetValue(source.Key, out Module module))
                {
                    var instance = module.CreateFromRawString(source.Value);
                    
                    RegisterInstance(instance);

                    return instance;
                }
            }

            return null;
        }
        
        public static IconProvider CreateFromTag(TagCompound tag)
        {
            string id = tag.Get<string>("id");
            var instance = GetProviderInstance(id);
            if (instance == null)
            {
                string source = tag.Get<string>("source");
                if (_modules.TryGetValue(source, out Module module))
                {
                    instance = module.Create();
                    instance.Deserialize(tag);
                    instance._id = id;
                    
                    RegisterInstance(instance);
                }
            }
    
            return instance;
        }

        public static IconProvider CreateFromBinary(BinaryReader reader)
        {
            string id = reader.ReadString();
            string source = reader.ReadString();
            
            var instance = GetProviderInstance(id);
            if (instance == null)
            {
                _modules.TryGetValue(source, out Module module);
                instance = module.Create();
                instance.DeserializeBinary(reader);
                instance._id = id;
                    
                RegisterInstance(instance);
            }
            else
            {
                instance.DeserializeBinaryFake(reader);
            }
    
            return instance;
        }
    
        public static IconProvider GetProviderInstance(string id)
        {
            return _loadedInstances.FirstOrDefault(instance => instance._id == id);
        }
    
        protected static bool RegisterInstance(IconProvider iconProvider)
        {
            var existingInstanceIndex = _loadedInstances.FindIndex(instance => instance._id == iconProvider._id);
            if (existingInstanceIndex == -1)
            {
                _loadedInstances.Add(iconProvider);
                return true;
            }
    
            return false;
        }
    }

    public override void OnModLoad()
    {
        GetType().Assembly.GetTypes()
            .Where(type => type.IsSubclassOf(typeof(IconProvider)) && !type.IsAbstract)
            .Select(type => new
            {
                Attribute = type.GetAttribute<IconProviderAttribute>(),
                ProviderType = type
            })
            .Where(provider => !string.IsNullOrWhiteSpace(provider.Attribute.SourceMarker))
            .ToList()
            .ForEach(provider =>
            {
                _modules.Add(provider.Attribute.SourceMarker, new Module
                {
                    Create = () => (IconProvider)Activator.CreateInstance(provider.ProviderType),
                    CreateFromRawString = provider.Attribute.CreateFromRawString
                });
            });
    }

    public override void OnModUnload()
    {
        _loadedInstances.Clear();
        _modules.Clear();
    }
}
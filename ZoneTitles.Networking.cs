using System.IO;
using System.Linq;
using Terraria;
using Terraria.ID;
using ZoneTitles.Common;
using ZoneTitles.Common.Systems;

namespace ZoneTitles
{
    partial class ZoneTitlesMod
    {
        internal enum MessageType : byte
        {
            AddOrUpdateZone,
            ChangeZoneRect,
            ChangeZoneVisual,
            RemoveZone
        }

        public override void HandlePacket(BinaryReader reader, int sender)
        {
            MessageType msgType = (MessageType)reader.ReadByte();

            switch (msgType)
            {
                case MessageType.AddOrUpdateZone:
                {
                    long Id = reader.ReadInt64();
                    Zone zone = ZonesSystem.GetZoneById(Id);
                    bool isNew = false;
                    if (zone == null)
                    {
                        zone = Zone.CreateWithId(Id);
                        isNew = true;
                    }

                    zone.LoadBinaryRect(reader);
                    zone.LoadBinaryVisual(reader);

                    if (isNew)
                    {
                        ZonesSystem.AddZoneNoSync(zone);
                    }

                    if (Main.netMode == NetmodeID.Server)
                    {
                        zone.SendAddOrUpdate(-1, sender);
                    }
                }
                    break;
                case MessageType.ChangeZoneRect:
                {
                    long id = reader.ReadInt64();
                    Zone zone = ZonesSystem.GetZoneById(id);
                    if (zone != null)
                    {
                        zone.LoadBinaryRect(reader);
                        
                        ZonesSystem.RebuildAABB();
                        
                        if (Main.netMode == NetmodeID.Server)
                        {
                            zone.SendRect(-1, sender);
                        }
                    }
                }
                    break;
                case MessageType.ChangeZoneVisual:
                {
                    long id = reader.ReadInt64();
                    Zone zone = ZonesSystem.GetZoneById(id);
                    if (zone != null)
                    {
                        zone.LoadBinaryVisual(reader);
                        
                        if (Main.netMode == NetmodeID.Server)
                        {
                            zone.SendVisualData(-1, sender);
                        }
                    }
                }
                    break;
                case MessageType.RemoveZone:
                {
                    long id = reader.ReadInt64();
                    var zone = ZonesSystem.GetZoneById(id);
                    if (zone != null)
                    {
                        if (Main.netMode == NetmodeID.Server)
                        {
                            zone.SendRemove(-1, sender);
                        }
                        
                        ZonesSystem.RemoveZoneNoSync(zone);
                    }
                }
                    break;
                default:
                    Logger.WarnFormat($"{nameof(ZoneTitlesMod)}: Unknown Message type: {0}", msgType);
                    break;
            }
        }
    }
}
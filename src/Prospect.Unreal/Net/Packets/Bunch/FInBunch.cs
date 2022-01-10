using Prospect.Unreal.Core.Names;
using Prospect.Unreal.Net.Channels;
using Prospect.Unreal.Serialization;

namespace Prospect.Unreal.Net.Packets.Bunch
{
    public class FInBunch : FNetBitReader
    {
        public FInBunch(UNetConnection inConnection, byte[]? src = null, int countBits = 0) : base(inConnection.PackageMap, src, countBits)
        {
            PacketId = 0;
            Next = null;
            Connection = inConnection;
            ChIndex = 0;
            ChType = 0; // TODO: CHTYPE_None
            ChName = UnrealNames.FNames[UnrealNameKey.None]; // TODO: NAME_None
            ChSequence = 0;
            bOpen = false;
            bClose = false;
            bDormant = false;
            bReliable = false;
            bPartial = false;
            bPartialInitial = false;
            bPartialFinal = false;
            bHasPackageMapExports = false;
            bHasMustBeMappedGUIDs = false;
            bIgnoreRPCs = false;
            CloseReason = EChannelCloseReason.Destroyed;
            
            // TODO: SetByteSwapping(Connection->bNeedsByteSwapping);
            
            SetEngineNetVer(Connection.EngineNetworkProtocolVersion);
            SetGameNetVer(Connection.GameNetworkProtocolVersion);
        }

        public int PacketId { get; set; }
        
        public FInBunch? Next { get; set; }

        public UNetConnection Connection { get; }

        public int ChIndex { get; set; }

        public int ChType { get; set; }
        
        public FName ChName { get; set; }

        public int ChSequence { get; set; }

        public bool bOpen { get; set; }

        public bool bClose { get; set; }

        /// <summary>
        ///     Close, but go dormant.
        /// </summary>
        public bool bDormant { get; set; }

        /// <summary>
        ///     Replication on this channel is being paused by the server.
        /// </summary>
        public bool bIsReplicationPaused { get; set; }

        public bool bReliable { get; set; }

        /// <summary>
        ///     Not a complete bunch
        /// </summary>
        public bool bPartial { get; set; }

        /// <summary>
        ///     The first bunch of a partial bunch
        /// </summary>
        public bool bPartialInitial { get; set; }

        /// <summary>
        ///     The final bunch of a partial bunch
        /// </summary>
        public bool bPartialFinal { get; set; }
        
        /// <summary>
        ///     This bunch has networkGUID name/id pairs.
        /// </summary>
        public bool bHasPackageMapExports { get; set; }

        /// <summary>
        ///     This bunch has guids that must be mapped before we can process this bunch.
        /// </summary>
        public bool bHasMustBeMappedGUIDs { get; set; }
        
        public bool bIgnoreRPCs { get; set; }
        
        public EChannelCloseReason CloseReason { get; set; }
    }
}
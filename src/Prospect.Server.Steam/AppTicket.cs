using System.Net;

namespace Prospect.Server.Steam;

public class AppTicket
{
    private AppTicket()
    {
            
    }

    /// <summary>
    ///     Full appticket, with a GC token and session header.
    /// </summary>
    public byte[] AuthTicket { get; set; }
        
    public ulong GcToken { get; set; }
        
    public DateTimeOffset TokenGenerated { get; set; }
        
    private IPAddress SessionExternalIp { get; set; }
        
    /// <summary>
    ///     Time the client has been connected to Steam in ms.
    /// </summary>
    public uint ClientConnectionTime { get; set; }
        
    /// <summary>
    ///     How many servers the client has connected to.
    /// </summary>
    public uint ClientConnectionCount { get; set; }
        
    public uint Version { get; set; }
        
    public ulong SteamId { get; set; }
        
    public uint AppId { get; set; }
        
    public IPAddress OwnershipTicketExternalIp { get; set; }
        
    public IPAddress OwnershipTicketInternalIp { get; set; }
        
    public uint OwnershipFlags { get; set; }
        
    public DateTimeOffset OwnershipTicketGenerated { get; set; }
        
    public DateTimeOffset OwnershipTicketExpires { get; set; }
        
    public List<uint> Licenses { get; set; }
        
    public List<AppDlc> Dlc { get; set; }

    public byte[] Signature { get; set; }
        
    public bool IsExpired { get; set; }
        
    public bool HasValidSignature { get; set; }
        
    public bool IsValid { get; set; }
        
    public static AppTicket Parse(string ticketHex)
    {
        var result = new AppTicket();
        var ticketBytes = Convert.FromHexString(ticketHex);

        using (var stream = new MemoryStream(ticketBytes))
        using (var ticketReader = new BinaryReader(stream))
        {
            // AuthTicket
            var initialLength = ticketReader.ReadUInt32();
            if (initialLength == 20)
            {
                result.AuthTicket = ticketBytes.AsSpan(0, 52).ToArray();
                result.GcToken = ticketReader.ReadUInt64();

                ticketReader.BaseStream.Position += 8;

                result.TokenGenerated = DateTimeOffset.FromUnixTimeSeconds(ticketReader.ReadUInt32());

                if (ticketReader.ReadUInt32() != 24)
                {
                    return null;
                }

                ticketReader.BaseStream.Position += 8;
                    
                result.SessionExternalIp = new IPAddress(BitConverter.GetBytes(ticketReader.ReadUInt32()));
                    
                ticketReader.BaseStream.Position += 4;

                result.ClientConnectionTime = ticketReader.ReadUInt32();
                result.ClientConnectionCount = ticketReader.ReadUInt32();

                if (ticketReader.ReadUInt32() + ticketReader.BaseStream.Position != ticketReader.BaseStream.Length)
                {
                    return null;
                }
            }
            else
            {
                ticketReader.BaseStream.Position -= 4;
            }
                
            // Ownership ticket
            var ownershipTicketOffset = ticketReader.BaseStream.Position;
            var ownershipTicketLength = ticketReader.ReadUInt32();
            if (ownershipTicketOffset + ownershipTicketLength != ticketReader.BaseStream.Length &&
                ownershipTicketOffset + ownershipTicketLength + 128 != ticketReader.BaseStream.Length)
            {
                return null;
            }

            result.Version = ticketReader.ReadUInt32();
            result.SteamId = ticketReader.ReadUInt64();
            result.AppId = ticketReader.ReadUInt32();
            result.OwnershipTicketExternalIp = new IPAddress(BitConverter.GetBytes(ticketReader.ReadUInt32()));
            result.OwnershipTicketInternalIp = new IPAddress(BitConverter.GetBytes(ticketReader.ReadUInt32()));
            result.OwnershipFlags = ticketReader.ReadUInt32();
            result.OwnershipTicketGenerated  = DateTimeOffset.FromUnixTimeSeconds(ticketReader.ReadUInt32());
            result.OwnershipTicketExpires  = DateTimeOffset.FromUnixTimeSeconds(ticketReader.ReadUInt32());
            result.Licenses = new List<uint>();

            var licenseCount = ticketReader.ReadUInt16();
            for (var i = 0; i < licenseCount; i++)
            {
                result.Licenses.Add(ticketReader.ReadUInt32());
            }

            result.Dlc = new List<AppDlc>();
                
            var dlcCount = ticketReader.ReadUInt16();
            for (var i = 0; i < dlcCount; i++)
            {
                var dlc = new AppDlc();

                dlc.AppId = ticketReader.ReadUInt32();
                dlc.Licenses = new List<uint>();

                var dlcLicenseCount = ticketReader.ReadUInt16();
                for (var j = 0; j < dlcLicenseCount; j++)
                {
                    dlc.Licenses.Add(ticketReader.ReadUInt32());
                }
                    
                result.Dlc.Add(dlc);
            }

            ticketReader.ReadUInt16();

            if (ticketReader.BaseStream.Position + 128 == ticketReader.BaseStream.Length)
            {
                result.Signature = ticketBytes.AsSpan((int)ticketReader.BaseStream.Position, 128).ToArray();
            }

            var date = DateTimeOffset.UtcNow;
            result.IsExpired = result.OwnershipTicketExpires < date;
            result.HasValidSignature = result.Signature != null && SteamCrypto.VerifySignature(ticketBytes.AsSpan((int)ownershipTicketOffset, (int)ownershipTicketLength).ToArray(), result.Signature);
            result.IsValid = !result.IsExpired && (result.Signature == null || result.HasValidSignature);
        }
            
        return result;
    }
}
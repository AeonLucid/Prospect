using System.Security.Cryptography;

namespace Prospect.Steam;

internal static class SteamCrypto
{
    private const string SystemCertificate = "-----BEGIN PUBLIC KEY-----\n" +
                                             "MIGdMA0GCSqGSIb3DQEBAQUAA4GLADCBhwKBgQDf7BrWLBBmLBc1OhSwfFkRf53T\n" +
                                             "2Ct64+AVzRkeRuh7h3SiGEYxqQMUeYKO6UWiSRKpI2hzic9pobFhRr3Bvr/WARvY\n" +
                                             "gdTckPv+T1JzZsuVcNfFjrocejN1oWI0Rrtgt4Bo+hOneoo3S57G9F1fOpn5nsQ6\n" +
                                             "6WOiu4gZKODnFMBCiQIBEQ==\n" +
                                             "-----END PUBLIC KEY-----" ;
        
    public static bool VerifySignature(byte[] data, byte[] signature)
    {
        var rsa = new RSACryptoServiceProvider();
            
        rsa.ImportFromPem(SystemCertificate);
            
        var dataOk = rsa.VerifyData(data, CryptoConfig.MapNameToOID("SHA1")!, signature);
        if (!dataOk)
        {
            return false;
        }
            
        var dataHashed = SHA1.HashData(data);
        var dataVerified = rsa.VerifyHash(dataHashed, CryptoConfig.MapNameToOID("SHA1")!, signature);

        return dataVerified;
    }
}
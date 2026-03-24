using System.Security.Cryptography;
using Microsoft.AspNetCore.DataProtection;

namespace AuthServer.Infrastructure.Security;

public sealed class DataProtectionTotpSecretProtector : ITotpSecretProtector
{
    private readonly IDataProtector _protector;

    public DataProtectionTotpSecretProtector(IDataProtectionProvider dataProtectionProvider)
    {
        _protector = dataProtectionProvider.CreateProtector("AuthServer.Infrastructure.Security.TotpSecret");
    }

    public string Protect(string plainText)
    {
        return _protector.Protect(plainText);
    }

    public string Unprotect(string protectedText)
    {
        try
        {
            return _protector.Unprotect(protectedText);
        }
        catch (CryptographicException)
        {
            return protectedText;
        }
    }
}

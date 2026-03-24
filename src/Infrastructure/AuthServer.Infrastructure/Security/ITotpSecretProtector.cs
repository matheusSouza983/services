namespace AuthServer.Infrastructure.Security;

public interface ITotpSecretProtector
{
    string Protect(string plainText);
    string Unprotect(string protectedText);
}

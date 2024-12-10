namespace FileMonitor.Infrastructure.Security;

public class ImpersonationService : IImpersonationService, IDisposable
{
    private readonly ConcurrentDictionary<string, WindowsIdentity> _identityCache = new();

    public async Task<TResult> RunImpersonatedAsync<TResult>(NetworkCredential credential, Func<Task<TResult>> action)
    {
        var identity = GetOrCreateIdentity(credential);
        return await WindowsIdentity.RunImpersonated(identity.AccessToken, action);
    }

    public async Task RunImpersonatedAsync(NetworkCredential credential, Func<Task> action)
    {
        var identity = GetOrCreateIdentity(credential);
        await WindowsIdentity.RunImpersonated(identity.AccessToken, action);
    }

    public TResult RunImpersonated<TResult>(NetworkCredential credential, Func<TResult> action)
    {
        var identity = GetOrCreateIdentity(credential);
        return WindowsIdentity.RunImpersonated(identity.AccessToken, action);
    }

    public void RunImpersonated(NetworkCredential credential, Action action)
    {
        var identity = GetOrCreateIdentity(credential);
        WindowsIdentity.RunImpersonated(identity.AccessToken, action);
    }

    private WindowsIdentity GetOrCreateIdentity(NetworkCredential credential)
    {
        var cacheKey = $"{credential.Domain}\\{credential.UserName}";

        return _identityCache.GetOrAdd(cacheKey, _ =>
        {
            var securePassword = new SecureString();
            foreach (char c in credential.Password)
            {
                securePassword.AppendChar(c);
            }

            if (!LogonUser(credential.UserName, credential.Domain, securePassword, LOGON32_LOGON_NEW_CREDENTIALS, LOGON32_PROVIDER_DEFAULT, out var tokenHandle))
            {
                var errorCode = Marshal.GetLastWin32Error();
                throw new InvalidOperationException($"Failed to impersonate user. Error code: {errorCode}");
            }

            return new WindowsIdentity(tokenHandle);
        });
    }

    public void Dispose()
    {
        foreach (var identity in _identityCache.Values)
        {
            identity.Dispose();
        }
        _identityCache.Clear();
    }

    [DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern bool LogonUser(
        string lpszUsername,
        string lpszDomain,
        SecureString lpszPassword,
        int dwLogonType,
        int dwLogonProvider,
        out IntPtr phToken);

    private const int LOGON32_LOGON_NEW_CREDENTIALS = 9;
    private const int LOGON32_PROVIDER_DEFAULT = 0;
}

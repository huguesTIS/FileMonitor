namespace FileMonitor.Core.Interfaces;

public interface IImpersonationService
{
    Task<TResult> RunImpersonatedAsync<TResult>(NetworkCredential credential, Func<Task<TResult>> action);
    Task RunImpersonatedAsync(NetworkCredential credential, Func<Task> action);
    TResult RunImpersonated<TResult>(NetworkCredential credential, Func<TResult> action);
    void RunImpersonated(NetworkCredential credential, Action action);
}


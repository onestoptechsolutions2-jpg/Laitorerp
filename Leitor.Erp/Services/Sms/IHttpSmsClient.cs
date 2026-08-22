using System.Threading;
using System.Threading.Tasks;

namespace Leitor.Erp.Services.Sms;

public interface IHttpSmsClient
{
    Task<bool> IsConfiguredAsync();

    Task<HttpSmsSendResult> SendAsync(string toE164, string content, CancellationToken cancellationToken = default);
}

using Microsoft.Extensions.Options;
using SmartTaxi.Application.Identity.Documents.Abstractions;
using SmartTaxi.Infrastructure.Identity.Options;

namespace SmartTaxi.Infrastructure.Identity.Services;

internal sealed class DocumentExpirationPolicy : IDocumentExpirationPolicy
{
    private readonly DocumentExpirationOptions _options;

    public DocumentExpirationPolicy(IOptions<DocumentExpirationOptions> options)
    {
        _options = options.Value;
    }

    public int ReminderLeadDays => _options.ReminderLeadDays;
}

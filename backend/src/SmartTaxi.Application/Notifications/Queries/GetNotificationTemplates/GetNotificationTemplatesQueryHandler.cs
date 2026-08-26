using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Application.Notifications.Abstractions;
using SmartTaxi.Domain.Notifications.Entities;

namespace SmartTaxi.Application.Notifications.Queries.GetNotificationTemplates;

public sealed class GetNotificationTemplatesQueryHandler : IQueryHandler<GetNotificationTemplatesQuery, IReadOnlyCollection<NotificationTemplate>>
{
    private readonly INotificationTemplateRepository _templateRepository;

    public GetNotificationTemplatesQueryHandler(INotificationTemplateRepository templateRepository)
    {
        _templateRepository = templateRepository;
    }

    public Task<IReadOnlyCollection<NotificationTemplate>> Handle(GetNotificationTemplatesQuery query, CancellationToken cancellationToken) =>
        _templateRepository.GetAllAsync(cancellationToken);
}

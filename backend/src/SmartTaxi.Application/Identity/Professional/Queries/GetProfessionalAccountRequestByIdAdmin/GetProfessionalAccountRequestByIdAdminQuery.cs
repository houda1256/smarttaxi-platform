using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Application.Identity.Professional;

namespace SmartTaxi.Application.Identity.Professional.Queries.GetProfessionalAccountRequestByIdAdmin;

public sealed record GetProfessionalAccountRequestByIdAdminQuery(Guid RequestId)
    : IQuery<Result<ProfessionalAccountRequestSummary>>;

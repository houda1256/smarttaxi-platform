using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Application.Identity.Documents;

namespace SmartTaxi.Application.Identity.DataRequests.Queries.GetMyPersonalDataExportContent;

public sealed record GetMyPersonalDataExportContentQuery(Guid UserId, Guid RequestId) : IQuery<Result<DocumentContentResult>>;

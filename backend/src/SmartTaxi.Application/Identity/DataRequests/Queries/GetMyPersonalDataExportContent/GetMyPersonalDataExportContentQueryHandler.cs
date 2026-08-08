using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;
using SmartTaxi.Application.Identity.DataRequests.Abstractions;
using SmartTaxi.Application.Identity.Documents;
using SmartTaxi.Application.Identity.Documents.Abstractions;
using SmartTaxi.Domain.Identity.DataRequests.Enums;

namespace SmartTaxi.Application.Identity.DataRequests.Queries.GetMyPersonalDataExportContent;

public sealed class GetMyPersonalDataExportContentQueryHandler
    : IQueryHandler<GetMyPersonalDataExportContentQuery, Result<DocumentContentResult>>
{
    private const string NotFoundError = "Export introuvable.";

    private readonly IPersonalDataRequestRepository _repository;
    private readonly IFileStorageService _fileStorage;

    public GetMyPersonalDataExportContentQueryHandler(
        IPersonalDataRequestRepository repository, IFileStorageService fileStorage)
    {
        _repository = repository;
        _fileStorage = fileStorage;
    }

    public async Task<Result<DocumentContentResult>> Handle(
        GetMyPersonalDataExportContentQuery query, CancellationToken cancellationToken)
    {
        var request = await _repository.GetByIdAsync(query.RequestId, cancellationToken);

        if (request is null || request.UserId != query.UserId
            || request.RequestType != PersonalDataRequestType.Export
            || request.Status != PersonalDataRequestStatus.Completed
            || request.ResultReference is null)
        {
            return Result<DocumentContentResult>.Failure(NotFoundError, ErrorType.NotFound);
        }

        var stream = await _fileStorage.OpenReadAsync(request.ResultReference, cancellationToken);

        return Result<DocumentContentResult>.Success(new DocumentContentResult(stream, "application/json", "export.json"));
    }
}

using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;

namespace SmartTaxi.Application.Advertising.Commands.UpdateAdvertiserProfile;

public sealed record UpdateAdvertiserProfileCommand(
    Guid UserId, string BusinessName, string LegalName, string TaxIdentifier, string City, string Address, string ContactEmail,
    string? ContactPhone) : ICommand<Result>;

using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;

namespace SmartTaxi.Application.Advertising.Commands.RegisterAdvertiserProfile;

public sealed record RegisterAdvertiserProfileCommand(
    Guid UserId, string BusinessName, string LegalName, string TaxIdentifier, string City, string Address, string ContactEmail,
    string? ContactPhone) : ICommand<Result<Guid>>;

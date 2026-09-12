using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;

namespace SmartTaxi.Application.Fleet.Owners.Commands.CreateIndividualOwnerProfile;

public sealed record CreateIndividualOwnerProfileCommand(
    Guid UserId,
    string FirstName,
    string LastName,
    string NationalId,
    string Street,
    string City,
    string? PostalCode,
    string Country,
    string BankName,
    string AccountHolderName,
    string AccountNumber,
    string? SwiftOrBic) : ICommand<Result<Guid>>;

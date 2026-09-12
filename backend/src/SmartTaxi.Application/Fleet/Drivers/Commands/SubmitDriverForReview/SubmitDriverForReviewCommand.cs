using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;

namespace SmartTaxi.Application.Fleet.Drivers.Commands.SubmitDriverForReview;

public sealed record SubmitDriverForReviewCommand(Guid RequestingUserId, Guid DriverProfileId) : ICommand<Result>;

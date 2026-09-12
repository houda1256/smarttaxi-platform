using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;

namespace SmartTaxi.Application.Identity.Commands.RefreshToken;

public sealed record RefreshTokenCommand(string RawRefreshToken) : ICommand<Result<RefreshTokenResult>>;

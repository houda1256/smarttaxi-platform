using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;

namespace SmartTaxi.Application.Advertising.Commands.ProcessScheduledCampaigns;

public sealed record ProcessScheduledCampaignsCommand : ICommand<Result<int>>;

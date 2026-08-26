using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;

namespace SmartTaxi.Application.Advertising.Commands.ProcessCompletedCampaigns;

public sealed record ProcessCompletedCampaignsCommand : ICommand<Result<int>>;

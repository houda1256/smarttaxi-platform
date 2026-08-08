using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;

namespace SmartTaxi.Application.Rides.Commands.DismissRideComplaint;

public sealed record DismissRideComplaintCommand(Guid ComplaintId, string Resolution) : ICommand<Result>;

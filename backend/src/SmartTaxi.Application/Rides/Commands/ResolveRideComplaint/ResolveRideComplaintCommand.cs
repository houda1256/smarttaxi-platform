using SmartTaxi.Application.Common;
using SmartTaxi.Application.Common.Messaging;

namespace SmartTaxi.Application.Rides.Commands.ResolveRideComplaint;

/// <summary>No ownership check — gated purely by an admin/support permission at the API layer, same convention as CancelRideByAdmin.</summary>
public sealed record ResolveRideComplaintCommand(Guid ComplaintId, string Resolution) : ICommand<Result>;

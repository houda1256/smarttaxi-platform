namespace SmartTaxi.API.Correlation;

public static class CorrelationHttpContextExtensions
{
    /// <summary>Only populated once CorrelationMiddleware has run for this request.</summary>
    public static string? GetCorrelationId(this HttpContext context) =>
        context.Items.TryGetValue(CorrelationConstants.HttpContextItemKey, out var value) ? value as string : null;
}

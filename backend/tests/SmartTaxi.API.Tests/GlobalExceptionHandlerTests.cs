using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using SmartTaxi.API.Correlation;
using SmartTaxi.API.ErrorHandling;

namespace SmartTaxi.API.Tests;

/// <summary>
/// GlobalExceptionHandler bypasses IProblemDetailsService entirely (it writes
/// its own ProblemDetails), so its correlation-id inclusion is unit-tested
/// directly rather than through a synthetic unhandled-exception HTTP path,
/// which would require adding a throwing endpoint to production code.
/// </summary>
public class GlobalExceptionHandlerTests
{
    [Fact]
    public async Task TryHandleAsync_IncludesCorrelationIdAndNeverLeaksExceptionDetails()
    {
        var handler = new GlobalExceptionHandler(NullLogger<GlobalExceptionHandler>.Instance);
        var httpContext = new DefaultHttpContext();
        httpContext.Items[CorrelationConstants.HttpContextItemKey] = "unit-test-correlation-id";
        var responseBody = new MemoryStream();
        httpContext.Response.Body = responseBody;

        var handled = await handler.TryHandleAsync(
            httpContext, new InvalidOperationException("sensitive internal detail"), CancellationToken.None);

        Assert.True(handled);
        Assert.Equal(StatusCodes.Status500InternalServerError, httpContext.Response.StatusCode);

        responseBody.Seek(0, SeekOrigin.Begin);
        var problem = await JsonSerializer.DeserializeAsync<JsonElement>(responseBody);

        Assert.Equal("unit-test-correlation-id", problem.GetProperty("correlationId").GetString());
        Assert.Equal(500, problem.GetProperty("status").GetInt32());

        var raw = System.Text.Encoding.UTF8.GetString(responseBody.ToArray());
        Assert.DoesNotContain("sensitive internal detail", raw);
        Assert.DoesNotContain("InvalidOperationException", raw);
        Assert.DoesNotContain("StackTrace", raw, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task TryHandleAsync_WithNoCorrelationIdOnContext_OmitsTheExtension()
    {
        var handler = new GlobalExceptionHandler(NullLogger<GlobalExceptionHandler>.Instance);
        var httpContext = new DefaultHttpContext();
        var responseBody = new MemoryStream();
        httpContext.Response.Body = responseBody;

        await handler.TryHandleAsync(httpContext, new InvalidOperationException("boom"), CancellationToken.None);

        responseBody.Seek(0, SeekOrigin.Begin);
        var problem = await JsonSerializer.DeserializeAsync<JsonElement>(responseBody);
        Assert.False(problem.TryGetProperty("correlationId", out _));
    }
}

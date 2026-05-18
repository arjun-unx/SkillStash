using System.Diagnostics;
using MediatR;

namespace PromptStash.Api.Common.Behaviors;

public sealed class LoggingBehavior<TRequest, TResponse>(ILogger<LoggingBehavior<TRequest, TResponse>> logger)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken ct)
    {
        var name = typeof(TRequest).Name;
        var sw = Stopwatch.StartNew();
        logger.LogInformation("Handling {RequestName}", name);
        try
        {
            var response = await next();
            sw.Stop();
            logger.LogInformation("Handled {RequestName} in {ElapsedMs} ms", name, sw.ElapsedMilliseconds);
            return response;
        }
        catch
        {
            sw.Stop();
            logger.LogWarning("Failed {RequestName} after {ElapsedMs} ms", name, sw.ElapsedMilliseconds);
            throw;
        }
    }
}

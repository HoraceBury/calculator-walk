public class LogMiddleware
{
    private readonly ILogger<LogMiddleware> logger;
    private readonly RequestDelegate next;

    public LogMiddleware(RequestDelegate _next, ILogger<LogMiddleware> _logger)
    {
        next = _next;
        logger = _logger;
    }

    public async Task Invoke(HttpContext context)
    {
        logger.LogInformation($"Invoked middleware. Auth header isnull: '{string.IsNullOrEmpty(context.Request.Headers.Authorization)}'");
        await next(context);
    }
}
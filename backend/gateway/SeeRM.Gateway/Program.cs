using System.Text.Json;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using SeeRM.Gateway;
using Yarp.ReverseProxy.Model;

// API gateway: the single entry point for the React app.
//   /api/leads/**                                   -> Leads service    (localhost:5080)
//   /api/customers|deals|tasks|interactions|dashboard/** -> Customer service (localhost:5067)
// Routes and service addresses live in appsettings.json ("ReverseProxy"). CORS is handled here only;
// the services themselves no longer enable CORS (duplicate CORS headers would be rejected by browsers).

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"))
    // Fail fast when a service is down: on Windows a refused connection otherwise takes ~2s per address.
    .ConfigureHttpClient((_, handler) => handler.ConnectTimeout = TimeSpan.FromSeconds(1));

var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
builder.Services.AddCors(o => o.AddDefaultPolicy(p =>
    p.WithOrigins(allowedOrigins).AllowAnyHeader().AllowAnyMethod()));

// ---- Health: gateway + each downstream service -----------------------------------
builder.Services.AddHttpClient("downstream-health", c => c.Timeout = TimeSpan.FromSeconds(3))
    .ConfigurePrimaryHttpMessageHandler(() => new SocketsHttpHandler { ConnectTimeout = TimeSpan.FromSeconds(1) });

var health = builder.Services.AddHealthChecks();
foreach (var cluster in builder.Configuration.GetSection("ReverseProxy:Clusters").GetChildren())
{
    var address = cluster["Destinations:primary:Address"];
    if (address is null) continue;
    health.Add(new HealthCheckRegistration(
        name: cluster.Key,
        factory: sp => new DownstreamHealthCheck(sp.GetRequiredService<IHttpClientFactory>(), address),
        failureStatus: null,
        tags: null));
}

var app = builder.Build();

app.UseCors();

app.MapHealthChecks("/health", new HealthCheckOptions
{
    // JSON body so you can see which service is down, e.g. {"status":"Degraded","services":{"leads":"Degraded: ..."}}
    ResponseWriter = (ctx, report) =>
    {
        ctx.Response.ContentType = "application/json";
        return ctx.Response.WriteAsync(JsonSerializer.Serialize(new
        {
            status = report.Status.ToString(),
            services = report.Entries.ToDictionary(
                e => e.Key,
                e => new { status = e.Value.Status.ToString(), detail = e.Value.Description }),
        }));
    },
});

// One Swagger UI for both services. The OpenAPI documents are proxied from each service, and
// "Try it out" sends requests through the gateway.
if (app.Environment.IsDevelopment())
{
    app.UseSwaggerUI(c =>
    {
        c.RoutePrefix = "swagger";
        c.SwaggerEndpoint("/docs/customer/swagger.json", "Customer service");
        c.SwaggerEndpoint("/docs/leads/swagger.json", "Leads service");
        c.DocumentTitle = "SeeRM API Gateway";
    });
    app.MapGet("/", () => Results.Redirect("/swagger")).ExcludeFromDescription();
}

app.MapReverseProxy(proxy =>
{
    // When a service can't be reached, YARP returns 502/503 with an empty body. Replace it with
    // ProblemDetails naming the service, so the UI can show a useful message.
    proxy.Use(async (ctx, next) =>
    {
        await next();

        var error = ctx.GetForwarderErrorFeature();
        if (error is null || ctx.Response.HasStarted) return;

        // Connect failures (including the 1s connect timeout) mean the service is down → 503.
        var service = ctx.GetReverseProxyFeature().Cluster.Config.ClusterId;
        const int status = StatusCodes.Status503ServiceUnavailable;

        ctx.Response.StatusCode = status;
        await Results.Problem(
            statusCode: status,
            title: "Service Unavailable",
            detail: $"The {service} service is unavailable right now. Please try again shortly.")
            .ExecuteAsync(ctx);
    });
});

app.Run();

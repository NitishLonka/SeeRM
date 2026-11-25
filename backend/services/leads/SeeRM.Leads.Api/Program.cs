using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Http.Resilience;
using Microsoft.OpenApi;
using Npgsql;
using SeeRM.Leads.Data;
using SeeRM.Leads.Infrastructure;
using SeeRM.Leads.Integration;
using SeeRM.Leads.Services;

var builder = WebApplication.CreateBuilder(args);

// ---- Database (owned only by this service) ------------------------------------
// Connection details come from ConnectionStrings:Leads; the password is kept out of source
// control in Database:Password (user-secrets or the Database__Password env var).
var csb = new NpgsqlConnectionStringBuilder(
    builder.Configuration.GetConnectionString("Leads")
    ?? throw new InvalidOperationException("Missing connection string 'ConnectionStrings:Leads'."));
if (builder.Configuration["Database:Password"] is { Length: > 0 } dbPassword)
    csb.Password = dbPassword;

builder.Services.AddDbContext<LeadsDbContext>(o =>
{
    o.UseNpgsql(csb.ConnectionString);
    if (builder.Environment.IsDevelopment()) o.EnableDetailedErrors();
});

// ---- Application services ---------------------------------------------------
builder.Services.AddScoped<LeadService>();
builder.Services.AddScoped<LeadConversionService>();

// ---- Customer service client (sync REST, called directly, not via the gateway) ----
var customerBaseUrl = builder.Configuration["Services:Customer:BaseUrl"]
    ?? throw new InvalidOperationException("Missing configuration 'Services:Customer:BaseUrl'.");
builder.Services.AddHttpClient<CustomerServiceClient>(c =>
        c.BaseAddress = new Uri(customerBaseUrl.TrimEnd('/') + "/"))
    // Fail fast when the Customer service is down: on Windows a refused connection otherwise takes ~2s per attempt.
    .ConfigurePrimaryHttpMessageHandler(() => new SocketsHttpHandler { ConnectTimeout = TimeSpan.FromSeconds(1) })
    .AddStandardResilienceHandler(o =>
    {
        o.AttemptTimeout.Timeout = TimeSpan.FromSeconds(3);
        o.TotalRequestTimeout.Timeout = TimeSpan.FromSeconds(8);
        o.CircuitBreaker.SamplingDuration = TimeSpan.FromSeconds(10); // must be >= 2x attempt timeout
        o.Retry.MaxRetryAttempts = 1;
        o.Retry.Delay = TimeSpan.FromMilliseconds(300);
        // Never auto-retry POSTs; conversion is made safe to retry at the business level instead.
        o.Retry.DisableForUnsafeHttpMethods();
    });

builder.Services.AddControllers();
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<ApiExceptionHandler>();
builder.Services.AddHealthChecks()
    .AddCheck<DatabaseHealthCheck>("leads-db")
    .AddCheck<CustomerServiceHealthCheck>("customer-service");

// No CORS here: browsers reach this service only through the API gateway (backend/gateway), which handles CORS.

// ---- Swagger ----------------------------------------------------------------
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "SeeRM Leads API",
        Version = "v1",
        Description = "Leads microservice: capture, qualify and convert prospects. Owns the 'leads' database.",
    });
    var xml = Path.Combine(AppContext.BaseDirectory, $"{Assembly.GetExecutingAssembly().GetName().Name}.xml");
    c.IncludeXmlComments(xml);
});

var app = builder.Build();

app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "SeeRM Leads API v1");
        c.DocumentTitle = "SeeRM Leads API";
    });
    app.MapGet("/", () => Results.Redirect("/swagger")).ExcludeFromDescription();
}
else
{
    app.UseHttpsRedirection();
}

app.MapControllers();
app.MapHealthChecks("/health");

app.Run();

using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi;
using Npgsql;
using SeeRM.Api.Data;
using SeeRM.Api.Infrastructure;
using SeeRM.Api.Services;

var builder = WebApplication.CreateBuilder(args);

// ---- Database -------------------------------------------------------------
// Connection details come from ConnectionStrings:SeeRm (appsettings.*.json); the password is
// kept out of source control in Database:Password (user-secrets or the Database__Password env var).
var csb = new NpgsqlConnectionStringBuilder(
    builder.Configuration.GetConnectionString("SeeRm")
    ?? throw new InvalidOperationException("Missing connection string 'ConnectionStrings:SeeRm'."));
if (builder.Configuration["Database:Password"] is { Length: > 0 } dbPassword)
    csb.Password = dbPassword;

builder.Services.AddDbContext<SeeRmDbContext>(o =>
{
    o.UseNpgsql(csb.ConnectionString);
    if (builder.Environment.IsDevelopment()) o.EnableDetailedErrors();
});

// ---- Application services ---------------------------------------------------
builder.Services.AddScoped<CustomerService>();
builder.Services.AddScoped<DealService>();
builder.Services.AddScoped<TaskService>();
builder.Services.AddScoped<InteractionService>();
builder.Services.AddScoped<DashboardService>();

builder.Services.AddControllers();
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<ApiExceptionHandler>();
builder.Services.AddHealthChecks().AddCheck<DatabaseHealthCheck>("customer-db");

// No CORS here: browsers reach this service only through the API gateway (backend/gateway), which handles CORS.

// ---- Swagger ----------------------------------------------------------------
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "SeeRM API",
        Version = "v1",
        Description = "CRUD API for the SeeRM CRM: customers, deals, tasks, interactions and dashboard KPIs.",
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
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "SeeRM API v1");
        c.DocumentTitle = "SeeRM API";
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

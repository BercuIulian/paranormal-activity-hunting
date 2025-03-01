using Microsoft.EntityFrameworkCore;
using SessionManagement.API.Data;
using Prometheus;
using System.Diagnostics;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddDbContext<SessionDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Apply migrations at startup
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<SessionDbContext>();
    try
    {
        Console.WriteLine("Applying migrations...");
        dbContext.Database.Migrate();
        Console.WriteLine("Migrations applied successfully");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"An error occurred while applying migrations: {ex.Message}");
        // Optionally, you might want to rethrow the exception if you want the application to fail on migration error
        // throw;
    }
}

app.UseAuthorization();
app.MapControllers();

var sessionCounter = Metrics.CreateCounter("session_operations_total", "Number of session operations",
    new CounterConfiguration
    {
        LabelNames = new[] { "operation", "status" }
    });

var sessionDuration = Metrics.CreateHistogram("session_operation_duration_seconds",
    "Histogram of session operation processing durations.",
    new HistogramConfiguration
    {
        LabelNames = new[] { "operation" },
        Buckets = new[] { .005, .01, .025, .05, .075, .1, .25, .5, .75, 1, 2.5, 5, 7.5, 10 }
    });

var activeSessionsGauge = Metrics.CreateGauge("active_sessions",
    "Number of currently active sessions");

app.UseRouting();
app.UseMetricServer();
app.UseHttpMetrics();

// Add a middleware to track operation duration
app.Use(async (context, next) =>
{
    var path = context.Request.Path.Value;
    var method = context.Request.Method;
    var operation = $"{method}:{path}";

    var sw = Stopwatch.StartNew();
    try
    {
        await next();
        sessionCounter.WithLabels(operation, context.Response.StatusCode.ToString()).Inc();
    }
    catch
    {
        sessionCounter.WithLabels(operation, "error").Inc();
        throw;
    }
    finally
    {
        sw.Stop();
        sessionDuration.WithLabels(operation).Observe(sw.Elapsed.TotalSeconds);
    }
});

app.UseEndpoints(endpoints =>
{
    endpoints.MapMetrics();
});

app.Urls.Add("http://0.0.0.0:8080");

app.Run();


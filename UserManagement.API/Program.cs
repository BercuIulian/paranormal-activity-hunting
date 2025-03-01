using Microsoft.Extensions.Configuration;
using MongoDB.Driver;
using UserManagement.API.Configuration;
using Prometheus;
using System.Diagnostics;

var builder = WebApplication.CreateBuilder(args);

// Configure MongoDB
var mongoDbSettings = builder.Configuration.GetSection("MongoDbSettings").Get<MongoDbSettings>();
builder.Services.AddSingleton<IMongoDatabase>(sp =>
{
    var client = new MongoClient(mongoDbSettings.ConnectionString);
    return client.GetDatabase(mongoDbSettings.DatabaseName);
});

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthorization();
app.MapControllers();

var userCounter = Metrics.CreateCounter("user_operations_total", "Number of user operations",
    new CounterConfiguration
    {
        LabelNames = new[] { "operation", "status" }
    });

var userDuration = Metrics.CreateHistogram("user_operation_duration_seconds",
    "Histogram of user operation processing durations.",
    new HistogramConfiguration
    {
        LabelNames = new[] { "operation" },
        Buckets = new[] { .005, .01, .025, .05, .075, .1, .25, .5, .75, 1, 2.5, 5, 7.5, 10 }
    });

var activeUsersGauge = Metrics.CreateGauge("active_users",
    "Number of currently active users");

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
        userCounter.WithLabels(operation, context.Response.StatusCode.ToString()).Inc();
    }
    catch
    {
        userCounter.WithLabels(operation, "error").Inc();
        throw;
    }
    finally
    {
        sw.Stop();
        userDuration.WithLabels(operation).Observe(sw.Elapsed.TotalSeconds);
    }
});

app.UseEndpoints(endpoints =>
{
    endpoints.MapMetrics();
});

app.Urls.Add("http://0.0.0.0:8080");

app.Run();


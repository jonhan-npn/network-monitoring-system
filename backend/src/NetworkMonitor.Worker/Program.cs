// using NetworkMonitor.Worker;

// var builder = Host.CreateApplicationBuilder(args);
// builder.Services.AddHostedService<Worker>();

// var host = builder.Build();
// host.Run();
using NetworkMonitor.Application;
using NetworkMonitor.Application.Interfaces;
using NetworkMonitor.Infrastructure;
using NetworkMonitor.Worker;
using NetworkMonitor.Worker.Alerts;
using NetworkMonitor.Worker.Jobs;
using NetworkMonitor.Worker.Monitoring;
using NetworkMonitor.Worker.Services;

var builder = Host.CreateApplicationBuilder(args);

// Same two calls the API's Program.cs makes — see docs/architecture.md for
// why both processes share this exact DI wiring.
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

builder.Services.Configure<MonitoringWorkerOptions>(
    builder.Configuration.GetSection(MonitoringWorkerOptions.SectionName));

// Worker-only registrations that don't belong in the shared
// Infrastructure.AddInfrastructure (that one is also called by the API,
// which registers its own ICurrentUserService backed by IHttpContextAccessor
// instead of this one).
builder.Services.AddSingleton<ICurrentUserService, SystemCurrentUserService>();
builder.Services.AddSingleton<ConsecutiveCounterTracker>();
builder.Services.AddScoped<AlertThresholdEngine>();

builder.Services.AddHostedService<PingBackgroundService>();
builder.Services.AddHostedService<SnmpBackgroundService>();
builder.Services.AddHostedService<SshInventoryBackgroundService>();
builder.Services.AddHostedService<DataRetentionJob>();

var host = builder.Build();
host.Run();

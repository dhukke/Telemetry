using ContractsTelemetry;
using MassTransit;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using System.Diagnostics;

const string SourceName = "WebTelemetry";

// Custom ActivitySource for the application
var source = new ActivitySource(SourceName);

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddMassTransit(x =>
{
    //x.AddConsumer<MessageConsumer>();

    x.UsingRabbitMq((context, cfg) =>
    {
        cfg.ConfigureEndpoints(context);
        
    });
});

var openTelemetry = builder.Services.AddOpenTelemetry();

openTelemetry
    .ConfigureResource(otelBuilder => otelBuilder
    .AddService(serviceName: SourceName));

openTelemetry.WithTracing(options =>
     options
    .AddSource(SourceName)
    .AddSource("MassTransit")
    .SetResourceBuilder(ResourceBuilder.CreateDefault().AddService(SourceName).AddTelemetrySdk())
    //.AddSqlClientInstrumentation(options =>
    //{
    //    options.SetDbStatementForText = true;
    //    options.RecordException = true;
    //})
    .AddAspNetCoreInstrumentation()
    .AddHttpClientInstrumentation()
    .AddConsoleExporter()
    .AddOtlpExporter(otlpOptions =>
    {
        otlpOptions.Endpoint = new Uri(builder.Configuration.GetValue<string>("AppSettings:OtelEndpoint"));
    })

);

openTelemetry.WithMetrics(options =>
    options.AddHttpClientInstrumentation()
     .SetResourceBuilder(ResourceBuilder.CreateDefault().AddService(SourceName).AddTelemetrySdk())
     // Metrics provider from OpenTelemetry
     .AddAspNetCoreInstrumentation()
     //.AddMeter(greeterMeter.Name)
     // Metrics provides by ASP.NET Core in .NET 8
     .AddMeter("Microsoft.AspNetCore.Hosting")
     .AddMeter("Microsoft.AspNetCore.Server.Kestrel")
     .AddOtlpExporter(otlpOptions =>
     {
         var uri = builder.Configuration.GetValue<string>("AppSettings:OtelEndpoint");
         otlpOptions.Endpoint = new Uri(uri);
     }));

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

var summaries = new[]
{
    "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
};

app.MapGet("/weatherforecast", async (IBus _bus, CancellationToken stoppingToken) =>
{
    using var getWeatherforecastActivity = source.StartActivity(SourceName, ActivityKind.Internal)!;

    var gettingForecastActivity = source.StartActivity(SourceName, ActivityKind.Consumer, getWeatherforecastActivity.Context)!;
    gettingForecastActivity.DisplayName = "Getting forecast";

    await Task.Delay(1000, stoppingToken);

    var forecast = Enumerable.Range(1, 5).Select(index =>
        new WeatherForecast
        (
            DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
            Random.Shared.Next(-20, 55),
            summaries[Random.Shared.Next(summaries.Length)]
        ))
        .ToArray();

    gettingForecastActivity.Stop();

    await Task.Delay(500, stoppingToken);

    var sendForecast = source.StartActivity(SourceName, ActivityKind.Consumer, getWeatherforecastActivity.Context)!;
    sendForecast.DisplayName = "Send forecast";

    await _bus.Publish(new Message { Text = $"The time is {DateTimeOffset.Now}" }, stoppingToken);

    sendForecast.Stop();

    return forecast;
})
.WithName("GetWeatherForecast")
.WithOpenApi();

app.Run();

internal record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}

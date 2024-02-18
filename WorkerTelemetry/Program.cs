using MassTransit;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using WorkerTelemetry;

const string SourceName = "WorkerTelemetry";

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddMassTransit(x =>
{
    x.AddConsumer<MessageConsumer>();

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
    .AddAspNetCoreInstrumentation()
    .AddHttpClientInstrumentation()
    .AddConsoleExporter()
    .AddOtlpExporter(otlpOptions =>
    {
        otlpOptions.Endpoint = new Uri(builder.Configuration.GetValue<string>("AppSettings:OtelEndpoint"));
    })
);


var host = builder.Build();

host.Run();

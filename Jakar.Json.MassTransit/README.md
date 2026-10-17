# Jakar.Json.MassTransit

[Jakar.Json](https://github.com/Jakar510/Jakar.Json/blob/master/Jakar.Json/README.md) for [MassTransit](https://masstransit.io) 8.

```csharp
services.AddMassTransit(x =>
{
    x.AddConsumer<OrderSubmittedConsumer>();
    x.UsingRabbitMq((context, cfg) =>
    {
        cfg.UseJakarJson();
        cfg.ConfigureEndpoints(context);
    });
});
```

- **Messages** of `[JsonModel]` types, and arrays, lists and collection interfaces of them, are written and read by Jakar.Json, inside MassTransit's standard JSON envelope.
- **The envelope**, the headers and every other message type keep System.Text.Json. Messages stay compatible with other MassTransit services on the wire, including ones without Jakar.Json.
- It works through MassTransit's System.Text.Json options, using [Jakar.Json.SystemTextJson](https://github.com/Jakar510/Jakar.Json/blob/master/Jakar.Json.SystemTextJson/README.md)'s converter. Those options are process-wide, so `UseJakarJson()` applies to every bus in the process.
- **Native AOT:** list the message types with `cfg.UseJakarJson(f => f.Add<OrderSubmitted>())`.

Targets MassTransit 8.x (Apache 2.0).

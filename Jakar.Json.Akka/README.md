# Jakar.Json.Akka

[Jakar.Json](https://github.com/Jakar510/Jakar.Json/blob/master/Jakar.Json/README.md) as an [Akka.NET](https://getakka.net) serializer, for remote messages, cluster messages and persisted events of `[JsonModel]` types.

```csharp
// In code
ActorSystem system = ActorSystem.Create("app", BootstrapSetup.Create().And(JakarJsonAkka.Setup(typeof(OrderPlaced), typeof(OrderShipped))));
```

```hocon
# Or in configuration: JakarJsonAkka.Hocon registers the serializer, then bind your types
akka.actor.serializers.jakar-json = "Jakar.Json.Akka.JakarJsonAkkaSerializer, Jakar.Json.Akka"
akka.actor.serialization-bindings { "MyApp.OrderPlaced, MyApp" = jakar-json }
```

- **Manifest:** Akka records the message type as the manifest, and Jakar.Json reads the bytes back as that type.
- **Bindings** cover a type and its subtypes. Bind a common base type or interface to cover a family of messages.
- **Id:** the serializer's id is `3710525`. Events persisted with it need that id to stay registered.

// Jakar.Json.Akka
// 10/17/2026

using System.Collections.Immutable;
using Akka.Actor;
using Akka.Serialization;



namespace Jakar.Json.Akka;


/// <summary>
///     An Akka.NET serializer on Jakar.Json, for messages and persisted events of <c>[JsonModel]</c> types (and arrays, lists and collection interfaces of them). Akka records the type as the
///     manifest. Bind it to your types in configuration (<see cref="JakarJsonAkka.Hocon"/>) or with <see cref="JakarJsonAkka.Setup"/>.
/// </summary>
public sealed class JakarJsonAkkaSerializer( ExtendedActorSystem system ) : Serializer(system)
{
    /// <summary> The serializer's id; Akka reserves 0-40 for its own. Messages persisted with it must keep this id. </summary>
    public const int ID = 3_710_525;


    public override int  Identifier      => ID;
    public override bool IncludeManifest => true;


    public override byte[] ToBinary( object obj )
    {
        ArgumentNullException.ThrowIfNull(obj);

        using PooledBufferWriter buffer = JsonModels.RequiredCodec(obj.GetType()).WriteToBuffer(obj);
        return buffer.ToArray();
    }

    public override object FromBinary( byte[] bytes, Type type )
    {
        ArgumentNullException.ThrowIfNull(bytes);
        ArgumentNullException.ThrowIfNull(type);
        return JsonModels.RequiredCodec(type).Read(bytes)!;
    }
}



public static class JakarJsonAkka
{
    /// <summary> The configuration name of <see cref="JakarJsonAkkaSerializer"/>. </summary>
    public const string NAME = "jakar-json";

    /// <summary> HOCON registering the serializer; add <c>serialization-bindings</c> for your types: <c>"MyApp.OrderPlaced, MyApp" = jakar-json</c>. </summary>
    public const string Hocon = """
                                akka.actor.serializers.jakar-json = "Jakar.Json.Akka.JakarJsonAkkaSerializer, Jakar.Json.Akka"
                                akka.actor.serialization-identifiers."Jakar.Json.Akka.JakarJsonAkkaSerializer, Jakar.Json.Akka" = 3710525
                                """;

    /// <summary> Binds <paramref name="types"/> (and their subtypes) to <see cref="JakarJsonAkkaSerializer"/> in code: <c>ActorSystem.Create("app", BootstrapSetup.Create().And(JakarJsonAkka.Setup(typeof(OrderPlaced))))</c>. </summary>
    public static SerializationSetup Setup( params Type[] types ) => SerializationSetup.Create(system => ImmutableHashSet.Create(SerializerDetails.Create(NAME, new JakarJsonAkkaSerializer(system), types.ToImmutableHashSet())));
}

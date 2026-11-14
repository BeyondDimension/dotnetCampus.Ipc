#if NET6_0_OR_GREATER
using System.Collections.Concurrent;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using dotnetCampus.Ipc.CompilerServices.GeneratedProxies.Models;

namespace dotnetCampus.Ipc.Serialization;

/// <summary>
/// Serializes IPC envelopes and explicitly registered contract metadata without reflection.
/// Register each application's source-generated contexts before starting its IPC providers.
/// </summary>
public sealed class GeneratedIpcJsonSerializer : IIpcObjectSerializer
{
    private static readonly ConcurrentBag<JsonSerializerContext> Contexts = new();
    private static readonly ConcurrentDictionary<Type, JsonTypeInfo> TypeInfos = new();

    /// <summary>Gets the process-wide serializer shared by IPC providers.</summary>
    public static GeneratedIpcJsonSerializer Instance { get; } = new();

    /// <summary>Registers source-generated metadata for a statically linked contract.</summary>
    public static void RegisterContext(JsonSerializerContext context)
    {
        if (context is null) throw new ArgumentNullException(nameof(context));
        Contexts.Add(context);
    }

    private static JsonTypeInfo GetTypeInfo(Type type) => TypeInfos.GetOrAdd(type, static type =>
    {
        foreach (var context in Contexts)
        {
            if (context.GetTypeInfo(type) is { } info)
                return info;
        }
        return IpcInternalJsonSerializerContext.Default.GetTypeInfo(type)
            ?? throw new NotSupportedException($"No generated IPC JSON metadata is registered for {type.FullName}.");
    });

    /// <inheritdoc />
    public byte[] Serialize(object value) => value is null
        ? "{}"u8.ToArray()
        : JsonSerializer.SerializeToUtf8Bytes(value, GetTypeInfo(value.GetType()));

    /// <inheritdoc />
    public void Serialize(Stream stream, object? value)
    {
        if (value is null)
        {
            stream.Write("{}"u8);
            return;
        }
        JsonSerializer.Serialize(stream, value, GetTypeInfo(value.GetType()));
    }

    /// <inheritdoc />
    public IpcJsonElement SerializeToElement(object? value) => value is null
        ? default
        : SerializeToElement(value, value.GetType());

    /// <summary>
    /// Uses the declared contract type, not a compiler-generated iterator or collection implementation.
    /// </summary>
    public IpcJsonElement SerializeToElement(object? value, Type contractType) => value is null
        ? default
        : new IpcJsonElement { RawValueOnSystemTextJson = JsonSerializer.SerializeToElement(value, GetTypeInfo(contractType)) };

    /// <inheritdoc />
    public T? Deserialize<T>(byte[] data, int start, int length)
        => JsonSerializer.Deserialize(data.AsSpan(start, length), (JsonTypeInfo<T>)GetTypeInfo(typeof(T)));

    /// <inheritdoc />
    public T? Deserialize<T>(Stream stream)
        => JsonSerializer.Deserialize(stream, (JsonTypeInfo<T>)GetTypeInfo(typeof(T)));

    /// <inheritdoc />
    public T? Deserialize<T>(IpcJsonElement jsonElement)
        => jsonElement.RawValueOnSystemTextJson is { } element
            ? element.Deserialize((JsonTypeInfo<T>)GetTypeInfo(typeof(T)))
            : default;
}
#endif

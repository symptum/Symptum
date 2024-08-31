// Code from https://github.com/dotnet/runtime/issues/29538#issuecomment-1330494636

using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;

namespace Symptum.Core.Serialization;

public static class JsonSerializerEx
{
    // Dynamically attach a JsonSerializerOptions copy that is configured using PopulateTypeInfoResolver
    private readonly static ConditionalWeakTable<JsonSerializerOptions, JsonSerializerOptions> s_populateMap = [];

    public static void PopulateObject<T>(T destination, string json, JsonSerializerOptions? options = null)
    {
        if (destination == null || string.IsNullOrEmpty(json) ||
            string.IsNullOrWhiteSpace(json)) return;
        PopulateObject(json, destination.GetType(), destination, options);
    }

    private static void PopulateObject(string json, Type returnType, object? destination, JsonSerializerOptions? options = null)
    {
        options = GetOptionsWithPopulateResolver(options);
        PopulateTypeInfoResolver.t_populateObject = destination;
        try
        {
            object? result = JsonSerializer.Deserialize(json, returnType, options);
        }
        finally
        {
            PopulateTypeInfoResolver.t_populateObject = null;
        }
    }

    private static JsonSerializerOptions GetOptionsWithPopulateResolver(JsonSerializerOptions? options)
    {
        options ??= JsonSerializerOptions.Default;

        if (!s_populateMap.TryGetValue(options, out JsonSerializerOptions? populateResolverOptions))
        {
            JsonSerializer.Serialize(value: 0, options); // Force a serialization to mark options as read-only

            populateResolverOptions = new JsonSerializerOptions(options)
            {
                TypeInfoResolver = new PopulateTypeInfoResolver(options.TypeInfoResolver)
            };

            s_populateMap.TryAdd(options, populateResolverOptions);
        }

        return populateResolverOptions;
    }

    private class PopulateTypeInfoResolver : IJsonTypeInfoResolver
    {
        private readonly IJsonTypeInfoResolver _jsonTypeInfoResolver;

        [ThreadStatic]
        internal static object? t_populateObject;

        public PopulateTypeInfoResolver(IJsonTypeInfoResolver jsonTypeInfoResolver)
        {
            _jsonTypeInfoResolver = jsonTypeInfoResolver;
        }

        public JsonTypeInfo? GetTypeInfo(Type type, JsonSerializerOptions options)
        {
            var typeInfo = _jsonTypeInfoResolver.GetTypeInfo(type, options);
            if (typeInfo != null && typeInfo.Kind != JsonTypeInfoKind.None)
            {
                Func<object>? defaultCreateObjectDelegate = typeInfo.CreateObject;
                typeInfo.CreateObject = () =>
                {
                    object? result = t_populateObject;
                    if (result != null)
                    {
                        // clean up to prevent reuse in recursive scenario
                        t_populateObject = null;
                    }
                    else
                    {
                        // fall back to the default delegate
                        result = defaultCreateObjectDelegate?.Invoke();
                    }

                    return result!;
                };
            }

            return typeInfo;
        }
    }
}

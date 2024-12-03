using System.Collections;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using Symptum.Core.Management.Resources;
using static Symptum.Core.Helpers.FileHelper;

namespace Symptum.Core.Serialization;

/// <summary>
/// Converts derivates of <see cref="IMetadataResource"/> to or from JSON.
/// This converter will check the <see cref="IMetadataResource.SplitMetadata"/> property.
/// If it's set to <see langword="true"/>, then the converter will use a path to another JSON file where it will be stored.
/// </summary>
/// <typeparam name="T">Derivate of <see cref="IMetadataResource"/></typeparam>
public class MetadataResourceConverter<T> : JsonConverter<T> where T : IMetadataResource
{
    public override T? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.String)
        {
            string? json = reader.GetString();
            (Type? derivedType, string? filePath) = MetadataSerializationHelper.ParseSplitJson<T>(json);
            if (derivedType != null && Activator.CreateInstance(derivedType) is T obj)
            {
                obj.SplitMetadata = true;
                obj.MetadataPath = filePath;
                obj.IsMetadataLoaded = false;
                return obj;
            }
        }

        return JsonSerializer.Deserialize<T>(ref reader, options);
    }

    public override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options)
    {
        if (value == null) return;

        if (value.SplitMetadata)
        {
            string filePath = ResourceManager.GetResourceFilePath(value, JsonFileExtension);
            string json = MetadataSerializationHelper.GetStronglyTypedJsonFilePath(value, filePath);
            writer.WriteStringValue(json);
            value.MetadataPath = filePath;
        }
        else
        {
            JsonSerializer.Serialize(writer, value, options);
        }
    }
}

/// <summary>
/// Converts a list of derivates of <see cref="IMetadataResource"/> to or from JSON.
/// This converter will check the <see cref="IMetadataResource.SplitMetadata"/> property of each item in a list.
/// If it's set to <see langword="true"/>, then the converter will use a path to another JSON file where it will be stored.
/// </summary>
/// <typeparam name="TList"><typeparamref name="TList"/> is <see cref="IList{TResource}"/></typeparam>
/// <typeparam name="TResource"><typeparamref name="TResource"/> is <see cref="IMetadataResource"/></typeparam>
public class ListOfMetadataResourceConverter<TList, TResource> : JsonConverter<TList> where TList : IList<TResource> where TResource : IMetadataResource
{
    public override TList? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.StartArray)
        {
            throw new JsonException();
        }

        if (Activator.CreateInstance(typeToConvert) is TList list)
        {
            while (reader.Read())
            {
                if (reader.TokenType == JsonTokenType.EndArray)
                {
                    return list;
                }

                if (reader.TokenType == JsonTokenType.String)
                {
                    string? json = reader.GetString();
                    (Type? derivedType, string? filePath) = MetadataSerializationHelper.ParseSplitJson<TResource>(json);
                    if (derivedType != null && Activator.CreateInstance(derivedType) is TResource obj)
                    {
                        obj.SplitMetadata = true;
                        obj.MetadataPath = filePath;
                        obj.IsMetadataLoaded = false;
                        list.Add(obj);
                    }
                }
                else if (JsonSerializer.Deserialize<TResource>(ref reader, options) is TResource obj)
                {
                    list.Add(obj);
                }
            }
        }


        throw new JsonException();
    }

    public override void Write(Utf8JsonWriter writer, TList value, JsonSerializerOptions options)
    {
        writer.WriteStartArray();

        foreach (TResource item in value)
        {
            if (item == null) return;

            if (item.SplitMetadata)
            {
                string filePath = ResourceManager.GetResourceFilePath(item, JsonFileExtension);
                string json = MetadataSerializationHelper.GetStronglyTypedJsonFilePath(item, filePath);
                writer.WriteStringValue(json);
                item.MetadataPath = filePath;
            }
            else
            {
                JsonSerializer.Serialize(writer, item, options);
            }
        }

        writer.WriteEndArray();
    }
}

/// <summary>
/// Specifies the property is a derivate of <see cref="IMetadataResource"/>.
/// Must be placed on all properties of derivates of <see cref="IMetadataResource"/> to support splitting Metadata.
/// </summary>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
public class MetadataResourceAttribute : JsonConverterAttribute
{
    public override JsonConverter? CreateConverter(Type typeToConvert)
    {
        if (!typeof(IMetadataResource).IsAssignableFrom(typeToConvert))
            throw new NotSupportedException();

        return Activator.CreateInstance(typeof(MetadataResourceConverter<>).MakeGenericType([typeToConvert])) as JsonConverter;
    }
}

/// <summary>
/// Specifies the property is a <see cref="IList{T}"/> where T is a derivate of <see cref="MetadataResource"/>.
/// Must be placed on all properties of <see cref="IList{T}"/> where T is a derivate of <see cref="MetadataResource"/> to support splitting Metadata.
/// </summary>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
public class ListOfMetadataResourceAttribute : JsonConverterAttribute
{
    public override JsonConverter? CreateConverter(Type typeToConvert)
    {
        if (!typeToConvert.IsGenericType)
            throw new NotSupportedException();

        if (!typeof(IList).IsAssignableFrom(typeToConvert))
            throw new NotSupportedException();

        Type elementType = typeToConvert.GetGenericArguments()[0];

        if (!typeof(IMetadataResource).IsAssignableFrom(elementType))
            throw new NotSupportedException();

        return Activator.CreateInstance(typeof(ListOfMetadataResourceConverter<,>).MakeGenericType([typeToConvert, elementType])) as JsonConverter;
    }
}

internal static class MetadataSerializationHelper
{
    public const char TypeDiscriminatorChar = '$';

    private static Dictionary<Type, string?> derivedTypeDiscriminators = [];

    public static Dictionary<Type, string?> DerivedTypeDiscriminators { get => derivedTypeDiscriminators; }

    static MetadataSerializationHelper()
    {
        var attrs = typeof(MetadataResource).GetCustomAttributes<JsonDerivedTypeAttribute>();
        foreach (var attr in attrs)
        {
            derivedTypeDiscriminators.Add(attr.DerivedType, attr.TypeDiscriminator as string);
        }
    }

    public static string GetStronglyTypedJsonFilePath<TResource>(TResource item, string filePath) where TResource : IMetadataResource
    {
        if (typeof(TResource) == typeof(MetadataResource)
            || typeof(TResource) == typeof(IMetadataResource)) // Not strongly typed
        {
            if (DerivedTypeDiscriminators.TryGetValue(item.GetType(), out string? discriminator))
            {
                return TypeDiscriminatorChar + discriminator + filePath;
            }
        }

        return filePath;
    }

    public static (Type? derivedType, string? filePath) ParseSplitJson<TResource>(string? json) where TResource : IMetadataResource
    {
        if (!string.IsNullOrWhiteSpace(json))
        {
            if (json.StartsWith(TypeDiscriminatorChar))
            {
                int i = json.IndexOf(PathSeparator);
                if (i > 0)
                {
                    string typeDiscriminator = json[1..i];
                    string filePath = json[i..];

                    return (DerivedTypeDiscriminators.FirstOrDefault(x => x.Value == typeDiscriminator).Key, filePath);
                }
            }
            else if (json.StartsWith(PathSeparator)) // Only filePath is present.
            {
                return (typeof(TResource), json);
            }
        }

        return (null, null);
    }
}

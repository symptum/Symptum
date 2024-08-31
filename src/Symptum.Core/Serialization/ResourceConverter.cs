using System.Collections;
using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Serialization;
using Symptum.Core.Management.Resources;
using static Symptum.Core.Helpers.FileHelper;

namespace Symptum.Core.Serialization;

/// <summary>
/// Converts derivates of <see cref="MetadataResource"/> to or from JSON.
/// This converter will check the <see cref="MetadataResource.SplitMetadata"/> property.
/// If it's set to <see langword="true"/>, then the converter will use a path to another JSON file where it will be stored.
/// </summary>
/// <typeparam name="T">Derivate of <see cref="MetadataResource"/></typeparam>
public class MetadataResourceConverter<T> : JsonConverter<T> where T : MetadataResource
{
    public override T? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.String)
        {
            string json = reader.GetString() ?? string.Empty;
            if (json.StartsWith(PathSeparator))
            {
                string filePath = json;
                Debug.WriteLine(filePath);
                if (Activator.CreateInstance(typeof(T)) is T obj)
                {
                    obj.SplitMetadata = true;
                    obj.MetadataPath = filePath;
                    obj.IsMetadataLoaded = false;
                    return obj;
                }
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
            writer.WriteStringValue(filePath);
            value.MetadataPath = filePath;
        }
        else
        {
            JsonSerializer.Serialize(writer, value, options);
        }
    }
}

/// <summary>
/// Converts a list of derivates of <see cref="MetadataResource"/> to or from JSON.
/// This converter will check the <see cref="MetadataResource.SplitMetadata"/> property of each item in a list.
/// If it's set to <see langword="true"/>, then the converter will use a path to another JSON file where it will be stored.
/// </summary>
/// <typeparam name="TList"><typeparamref name="TList"/> is <see cref="IList{TResource}"/></typeparam>
/// <typeparam name="TResource"><typeparamref name="TResource"/> is <see cref="MetadataResource"/></typeparam>
public class ListOfMetadataResourceConverter<TList, TResource> : JsonConverter<TList> where TList : IList<TResource> where TResource : MetadataResource
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
                    string json = reader.GetString() ?? string.Empty;
                    if (json.StartsWith(PathSeparator))
                    {
                        string filePath = json;
                        Debug.WriteLine(filePath);
                        if (Activator.CreateInstance(typeof(TResource)) is TResource obj)
                        {
                            obj.SplitMetadata = true;
                            obj.MetadataPath = filePath;
                            obj.IsMetadataLoaded = false;
                            list.Add(obj);
                        }
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
                writer.WriteStringValue(filePath);
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
/// Specifies the property is a derivate of <see cref="MetadataResource"/>.
/// Must be placed on all properties of derivates of <see cref="MetadataResource"/> to support splitting Metadata.
/// </summary>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
public class MetadataResourceAttribute : JsonConverterAttribute
{
    public override JsonConverter? CreateConverter(Type typeToConvert)
    {
        if (!typeof(MetadataResource).IsAssignableFrom(typeToConvert))
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

        if (!typeof(MetadataResource).IsAssignableFrom(elementType))
            throw new NotSupportedException();

        return Activator.CreateInstance(typeof(ListOfMetadataResourceConverter<,>).MakeGenericType([typeToConvert, elementType])) as JsonConverter;
    }
}

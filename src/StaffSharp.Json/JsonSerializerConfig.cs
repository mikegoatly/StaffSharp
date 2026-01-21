namespace StaffSharp.Json;

using System.Collections;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;

using StaffSharp.Notation;

/// <summary>
/// Custom type resolver that configures polymorphism for INotationEvent
/// and omits default/empty values without modifying Core library types.
/// </summary>
internal static class JsonSerializerConfig
{
    public static void ConfigureContext(JsonTypeInfo typeInfo)
    {
        // Polymorphism support for INotationEvent
        if (typeInfo.Type == typeof(INotationEvent))
        {
            typeInfo.PolymorphismOptions = new JsonPolymorphismOptions
            {
                TypeDiscriminatorPropertyName = "$type",
                IgnoreUnrecognizedTypeDiscriminators = false,
                UnknownDerivedTypeHandling = JsonUnknownDerivedTypeHandling.FailSerialization,
                DerivedTypes =
                {
                    new JsonDerivedType(typeof(NotationNote), "note"),
                    new JsonDerivedType(typeof(Chord), "chord"),
                    new JsonDerivedType(typeof(Rest), "rest")
                }
            };
        }
        
        if (typeInfo.Kind == JsonTypeInfoKind.Object)
        {
            // Omit Empty Collections for concisness
            foreach (var property in typeInfo.Properties)
            {
                // We check if it is IEnumerable, but NOT a string (strings are IEnumerable<char>)
                if (typeof(IEnumerable).IsAssignableFrom(property.PropertyType) &&
                    property.PropertyType != typeof(string))
                {
                    property.ShouldSerialize = (obj, value) =>
                    {
                        // Omit if Null
                        if (value is null)
                        {
                            return false;
                        }

                        // Fast check for standard collections (List<T>, Arrays, etc)
                        if (value is ICollection collection)
                        {
                            return collection.Count > 0;
                        }

                        // Fallback for IReadOnlyList<T>
                        if (value is IList enumerable)
                        {
                            var enumerator = enumerable.GetEnumerator();
                            // If we can move to the first item, it's not empty
                            return enumerator.MoveNext();
                        }

                        return false;
                    };
                }
            }
        }
    }
}
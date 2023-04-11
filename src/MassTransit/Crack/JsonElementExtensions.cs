using System;
using System.Collections.Generic;
using System.Text.Json;

namespace MassTransit.Crack
{
    internal static class JsonElementExtensions
    {
        internal static T DeserializeAsDynamic<T>(this JsonElement jsonElement, JsonSerializerOptions options)
        {
            var payload = jsonElement.Deserialize<Dictionary<string, object>>(options);

            return (T)Activator.CreateInstance(typeof(DynamicJsonObject), payload);
        }
    }
}

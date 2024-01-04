using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace SimpleECS
{
    public static class Serialization
    {
        public static string SerializeComponent(Component obj)
        {
            if (settings == null)
            {
                settings = CreateSettings();
            }
            
            return JsonConvert.SerializeObject(obj,
                typeof (Component),
                Formatting.None,
                settings
                );
        }

        public static T DeserializeJSON<T>(string data) where T : Component
        {
            if (settings == null)
            {
                settings = CreateSettings();
            }
            
            var obj = JsonConvert.DeserializeObject(data, typeof(Component), settings);
            return (T)obj;
        }

        private static JsonSerializerSettings settings;
        private static JsonSerializerSettings CreateSettings() => new JsonSerializerSettings
        {
            TypeNameHandling = TypeNameHandling.Auto,
            ContractResolver = new WritablePropertiesOnlyResolver()
        };
        
        private class WritablePropertiesOnlyResolver : DefaultContractResolver
        {
            protected override IList<JsonProperty> CreateProperties(Type type, MemberSerialization memberSerialization)
            {
                IList<JsonProperty> props = base.CreateProperties(type, memberSerialization);
                return props.Where(p => p.Writable).ToList();
            }
        }
    }
}
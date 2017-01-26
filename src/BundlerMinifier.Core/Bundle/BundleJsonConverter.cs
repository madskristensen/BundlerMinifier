using System;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Reflection;

namespace BundlerMinifier
{
    class BundleJsonConverter : JsonConverter
    {
        public JObject DefaultSettings;

        public BundleJsonConverter(string configFile)
        {
            string defaultFile = configFile + ".defaults";

            if (File.Exists(defaultFile))
            {
                DefaultSettings = JObject.Parse(File.ReadAllText(defaultFile));
            }
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            // Load JObject from stream
            JObject jObject = JObject.Load(reader);
            if (DefaultSettings != null)
            {
                jObject.Merge(JObject.FromObject(DefaultSettings));
            }

            // Create target object based on JObject
            Bundle target = new Bundle();

            // Populate the object properties
            serializer.Populate(jObject.CreateReader(), (object)target);

            return target;
        }

        public override bool CanConvert(Type objectType)
        {
            return typeof(Bundle).IsAssignableFrom(objectType);
        }
    }
}
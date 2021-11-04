using System;
using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Prospect.Server.Api.Converters
{
    // By default System.Text.Json outputs and we want:
    //  Output: 2021-11-04T04:58:20.4232184Z
    //  Want  : 2021-10-24T02:54:56.652Z
    public class DateTimeConverter : JsonConverter<DateTime>
    {
        public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            Debug.Assert(typeToConvert == typeof(DateTime));
            return DateTime.Parse(reader.GetString());
        }

        public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options)
        {
            writer.WriteStringValue(value.ToString("yyyy'-'MM'-'dd'T'HH':'mm':'ss'.'fffK"));
        }
    }
}
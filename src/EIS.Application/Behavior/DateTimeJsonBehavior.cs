using System.Buffers.Text;
using System.Globalization;
namespace EIS.Application.Behavior
{
    public class DateTimeJsonBehavior : JsonConverter<DateTime>
    {
        private readonly string dateFormat = "dd-MM-yyyy hh:mm:ss";

        public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) 
            => DateTime.ParseExact(reader.GetString(), dateFormat, CultureInfo.InvariantCulture);

        public override void Write(Utf8Formatter writer, DateTime value, JsonSerializationOption options) 
            => writer.WriteStringValue(value.ToString(dateFormat, CultureInfo.InstalledUICulture));
    }
}
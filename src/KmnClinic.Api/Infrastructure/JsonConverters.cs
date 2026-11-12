using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace KmnClinic.Api.Infrastructure;

/// <summary>Giờ dạng "HH:mm" (ô nhập giờ của trình duyệt) hoặc "HH:mm:ss"; trả ra dạng "HH:mm".</summary>
public class GioJsonConverter : JsonConverter<TimeOnly>
{
    private static readonly string[] DinhDang = { "HH:mm", "HH:mm:ss", "H:mm" };

    public override TimeOnly Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var s = reader.GetString();
        if (s is not null && TimeOnly.TryParseExact(s, DinhDang, CultureInfo.InvariantCulture, DateTimeStyles.None, out var t))
            return t;
        throw new JsonException("Giờ không hợp lệ.");
    }

    public override void Write(Utf8JsonWriter writer, TimeOnly value, JsonSerializerOptions options)
        => writer.WriteStringValue(value.ToString("HH:mm", CultureInfo.InvariantCulture));
}

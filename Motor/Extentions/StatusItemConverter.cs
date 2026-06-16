using System.Text.Json;
using System.Text.Json.Serialization;
using XOSC.Motor.UI;


namespace XOSC.Motor.Extentions;

public class StatusItemConverter : JsonConverter<List<StatusItem>>
{
    public override List<StatusItem> Read(ref Utf8JsonReader r,
        Type t,
        JsonSerializerOptions o)
    {
        if (r.TokenType == JsonTokenType.StartArray)
        {
            var l = new List<StatusItem>();
            while (r.Read() && r.TokenType != JsonTokenType.EndArray)
            {
                if (r.TokenType == JsonTokenType.String)
                    l.Add(new StatusItem
                    {
                        Text = r.GetString()
                    });
                else if (r.TokenType == JsonTokenType.StartObject)
                    l.Add(JsonSerializer.Deserialize<StatusItem>(ref r,
                        o));
            }

            return l;
        }

        return new List<StatusItem>();
    }

    public override void Write(Utf8JsonWriter w,
        List<StatusItem> v,
        JsonSerializerOptions o) =>
        JsonSerializer.Serialize(w,
            v,
            o);
}
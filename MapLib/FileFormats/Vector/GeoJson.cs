using System.IO;
using System.Text.Json;

namespace MapLib.FileFormats.Vector;

internal class GeoJson : IVectorFormatReader
{
    private static ReadOnlySpan<byte> UTF8_BOM => new byte[] { 0xEF, 0xBB, 0xBF };

    public VectorData ReadFile(string filename)
    {
        //VectorData features = new();

        // Read raw data
        ReadOnlySpan<byte> rawData = File.ReadAllBytes(filename);
        if (rawData.StartsWith(UTF8_BOM)) // skip past UTF8 byte marker (if exists)
            rawData = rawData.Slice(UTF8_BOM.Length);

        Utf8JsonReader reader = new(rawData, new JsonReaderOptions
        {
            AllowTrailingCommas = true,
            CommentHandling = JsonCommentHandling.Skip
        });

        while (reader.Read())
        {
            JsonTokenType tokenType = reader.TokenType;
            if (tokenType == JsonTokenType.PropertyName)
            {
                string? name = reader.GetString();
                switch (name)
                {
                    case "type":
                        // expect "FeatureCollecton"
                        reader.Read();
                        if (reader.TokenType == JsonTokenType.String)
                        {
                            string? value = reader.GetString();
                            if (value != "FeatureCollection")
                            {
                                throw new ApplicationException(
                                    "Expected value: \"FeatureCollection\"");
                            }
                        }
                        else
                        {
                            throw new ApplicationException(
                                "Expected value: \"FeatureCollection\"");
                        }
                        break;
                    case "features":
                        break;
                    default:
                        break;
                }
            }
        }

        //JsonSerializer.Deserialize(rawData);


        // Work in progress 

        throw new NotImplementedException();
    }
}

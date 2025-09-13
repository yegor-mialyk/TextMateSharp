using System.Text.Json;

namespace TextMateSharp.Internal.Parser.Json;

public class JsonpListParser<T>
{
    private readonly bool _isTheme;

    public JsonpListParser(bool isTheme)
    {
        _isTheme = isTheme;
    }

    public T Parse(StreamReader contents)
    {
        var pList = new PList<T>(_isTheme);

        var buffer = new byte[2048];

        var options = new JsonReaderOptions
        {
            AllowTrailingCommas = true,
            CommentHandling = JsonCommentHandling.Skip
        };

        var size = contents.BaseStream.Read(buffer, 0, buffer.Length);
        var reader = new Utf8JsonReader(buffer.AsSpan(0, size), false, new(options));

        while (true)
        {
            var bytesRead = -1;

            while (!reader.Read())
            {
                bytesRead = GetMoreBytesFromStream(contents.BaseStream, ref buffer, ref reader);

                if (bytesRead == 0)
                    break;
            }

            if (bytesRead == 0)
                break;

            var nextToken = reader.TokenType;
            switch (nextToken)
            {
                case JsonTokenType.StartArray:
                    pList.StartElement("array");
                    break;
                case JsonTokenType.EndArray:
                    pList.EndElement("array");
                    break;
                case JsonTokenType.StartObject:
                    pList.StartElement("dict");
                    break;
                case JsonTokenType.EndObject:
                    pList.EndElement("dict");
                    break;
                case JsonTokenType.PropertyName:
                    pList.StartElement("key");
                    pList.AddString(reader.GetString());
                    pList.EndElement("key");
                    break;
                case JsonTokenType.String:
                    pList.StartElement("string");
                    pList.AddString(reader.GetString());
                    pList.EndElement("string");
                    break;
            }
        }

        return pList.GetResult();
    }

    private static int GetMoreBytesFromStream(Stream stream, ref byte[] buffer, ref Utf8JsonReader reader)
    {
        int bytesRead;

        if (reader.BytesConsumed < buffer.Length)
        {
            ReadOnlySpan<byte> leftover = buffer.AsSpan((int) reader.BytesConsumed);

            if (leftover.Length == buffer.Length)
                Array.Resize(ref buffer, buffer.Length * 2);

            leftover.CopyTo(buffer);

            bytesRead = stream.Read(buffer, leftover.Length, buffer.Length - leftover.Length);

            reader = new(buffer.AsSpan(0, bytesRead + leftover.Length), bytesRead == 0, reader.CurrentState);
        }
        else
        {
            bytesRead = stream.Read(buffer, 0, buffer.Length);

            reader = new(buffer.AsSpan(0, bytesRead), bytesRead == 0, reader.CurrentState);
        }

        return bytesRead;
    }
}

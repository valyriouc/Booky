using System.Text;

namespace Booky.Core;

public enum BPStatusCode
{
    UnkownMethod = 1,
    IncorrectFormat = 2,
    HeaderError = 3,
    DeadParser = 4,

    Success = 100,

    ServerError = 200
}

public struct BPResponse
{
    public BPStatusCode StatusCode { get; init; }

    public List<(string, string)> Headers { get; init; } 

    public Stream Body { get; init; }

    public BPResponse(BPStatusCode statusCode, Stream body)
    {
        StatusCode = statusCode;    
        Body = body;
        Headers = new List<(string, string)>(); 
    }

    public static BPResponse FromParserErrors(ParserError error) => 
        new BPResponse(error.Type.FromParserErrorType(), new MemoryStream(Encoding.UTF8.GetBytes(error.Message)));
   
    public static BPResponse ServerError(string message) =>
        new BPResponse(BPStatusCode.ServerError, new MemoryStream(Encoding.UTF8.GetBytes(message)));    

    public IEnumerable<byte> GetBytes()
    {
        yield return (byte)StatusCode;

        yield return (byte)0x20;
        yield return (byte)'\r';
        yield return (byte)'\n';

        foreach ((string, string) header in Headers)
        {
            foreach (var headerByte in Encoding.UTF8.GetBytes(header.Item1 + ":" + header.Item2))
            {
                yield return headerByte;
            }

            yield return (byte)'\r';
            yield return (byte)'\n';
        }

        yield return (byte)'-';
        yield return (byte)'\r';
        yield return (byte)'\n';

        Body.Position = 0;
        
        while(true)
        {
            int b = Body.ReadByte();

            if (b == -1) break;

            yield return (byte)b;
        }
    }
}


file static class LocalExtensions
{
    public static BPStatusCode FromParserErrorType(this ParserErrorType self) => self switch
    {
        ParserErrorType.UnkownMethod => BPStatusCode.UnkownMethod,
        ParserErrorType.Header => BPStatusCode.HeaderError,
        ParserErrorType.Format => BPStatusCode.IncorrectFormat,
        ParserErrorType.ParserIsDead => BPStatusCode.DeadParser,
        _ => throw new NotImplementedException()
    };
}

internal static class BPStatusCodeExtensions
{
    public static string GetStringRepres(this BPStatusCode self) => self switch
    {
        BPStatusCode.UnkownMethod => "What should I do?!",
        BPStatusCode.HeaderError => "Please what?",
        BPStatusCode.IncorrectFormat => "Take a look at the words you said!",
        BPStatusCode.DeadParser => "I died for some reason!",
        BPStatusCode.Success => "Huh that worked!",
        BPStatusCode.ServerError => "Complete system failure!",
        _ => throw new NotImplementedException()
    };
}
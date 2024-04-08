namespace Booky.Core;

public enum BPStatusCode
{
    UnkownMethod = 1,
    IncorrectFormat = 2,
    HeaderError = 3,
    DeadParser = 4,

    Success = 100,
}

public struct BPResponse
{
    public BPStatusCode StatusCode { get; set; }

    public Stream Body { get; set; }

    public BPResponse(BPStatusCode statusCode, Stream body)
    {
        StatusCode = statusCode;    
        Body = body;
    }
}

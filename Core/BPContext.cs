namespace Booky.Core;

public struct BPContext
{

    public BPMethod Method { get; }

    public string Resource { get; }

    public List<(string, string)> Headers { get; }

    public Stream Body { get; }

    internal BPContext(
        BPMethod method,
        string resource,
        List<(string, string)> headers,
        Stream body)
    {
        Method = method;
        Resource = resource;
        Headers = headers;
        Body = body;
    }

    public void SendResponse()
}

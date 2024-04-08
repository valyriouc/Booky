

namespace Booky.Core;

public class BPContextBuilder
{
    private BPMethod Method { get; set; }

    private string Resource { get; set; }

    private List<(string, string)> Headers { get; set; }

    private Stream Body { get; set; }

    public BPContextBuilder()
    {
        Method = BPMethod.Unkown;
        Resource = string.Empty;
        Headers = new List<(string, string)>();
        Body = new MemoryStream();
    }

    public BPContextBuilder WithMethod(BPMethod method)
    {
        Method = method;
        return this;
    }

    public BPContextBuilder WithResource(string resource)
    {
        Resource = resource;
        return this;
    }

    public BPContextBuilder WithHeader((string, string) header)
    {
        Headers.Add(header);
        return this;
    }

    public BPContextBuilder WithBodyData(ReadOnlySpan<byte> bytes)
    {
        Body.Write(bytes);
        return this;
    }

    public BPRequest Build() => new BPRequest(Method, Resource, Headers, Body);
    

    public static BPRequest BuildErrorContext() => new BPRequest();
}

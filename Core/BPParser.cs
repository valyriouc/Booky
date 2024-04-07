using System.Text;

namespace Booky.Core;

public enum BPMethod
{
    SYC,
    UPL,
    DWN,
    Unkown,
}

public enum ParserErrorType
{
    UnkownMethod,
    Format,
    Header,
    ParserIsDead
}

public readonly struct ParserError
{
    public ParserErrorType Type { get; }

    public string Message { get; }

    public ParserError(
        ParserErrorType type, 
        string message)
    {
        Type = type;
        Message = message;
    }
}

public delegate void OnParserSuccess(
    BPContext context);

public delegate void OnParserError(
    BPContext context,
    IEnumerable<ParserError> errors);

public class BPParser
{
    private enum ParserState
    {
        Unkown,
        Finished,
        Needed,
        NotStarted,
        Parsing
    }

    private enum ProtocolState
    {
        Method,
        Resource,
        Header,
        Body
    }

    // State handling 
    private OnParserSuccess SuccessHandler { get; set; }

    private OnParserError ErrorHandler { get; set;  } 

    private ParserState State { get; set; }

    private ProtocolState ProtState { get; set; }

    private List<ParserError> Errors { get; init; }  

    // Byte properties 
    public Stream Stream { get; init; }

    private byte[] Buffer { get; set; }

    private int Position { get; set; }

    private int BufferPos { get; set; } = 0;

    // Parser result 
    public BPContextBuilder Builder { get; init; }

    public BPParser(Stream stream)
    {
        State = ParserState.NotStarted;
        ProtState = ProtocolState.Method;

        Stream = stream;
        Buffer = new byte[512];
        Position = 0;

        Errors = new List<ParserError>();
        Builder = new BPContextBuilder();
    }

    private void ManageState()
    {
        switch (State)
        {
            case ParserState.Unkown:
                InvokeFailure();
                break;
            case ParserState.Finished:
                InvokeSuccess();
                break;
            case ParserState.Needed:
            case ParserState.NotStarted:
                // Fill up the internal buffer
                FillUp();
                Parse();
                break;
            default:
                Parse();
                break;
        }
    }

    private bool fillupNeeded = true;
    private void FillUp()
    {
        if (BufferPos == Buffer.Length - 1 || fillupNeeded)
        {
            Position = Stream.Read(Buffer, Position, Buffer.Length);
            BufferPos = 0;
            fillupNeeded = false;
            State = ParserState.Parsing;
        }
    }

    public void Parse()
    {
        FillUp();

        if (State == ParserState.Unkown ||
            State == ParserState.Finished)
        {
            return;
        }

        switch (ProtState)
        {
            case ProtocolState.Method:
                ReadMethod();
                break;
            case ProtocolState.Resource:
                ReadResource();
                break;
            case ProtocolState.Header:
                ReadHeader();
                break;
            case ProtocolState.Body:
                ReadBody();
                break;
            default:
                State = ParserState.Unkown;
                break;
        }

        ManageState();
    }

    private bool IsCorrectLineEnding() =>
        Buffer[BufferPos] == (byte)'\r' &&
            Buffer[BufferPos + 1] == (byte)'\n';

    private void ReadMethod()
    {
        if (ProtState != ProtocolState.Method)
        {
            Errors.Add(new ParserError(ParserErrorType.ParserIsDead, "Something is wrong with the parser!"));
            State = ParserState.Unkown;
            return;
        }

        ReadOnlySpan<byte> data = Buffer[..3];

        BPMethod method = ExtractMethod(Encoding.UTF8.GetString(data));

        if (method is BPMethod.Unkown)
        {
            Errors.Add(new ParserError(
                ParserErrorType.UnkownMethod, 
                "This method is not supported!"));
            State = ParserState.Unkown;
            return;
        }

        if (Buffer[3] != 0x20)
        {
            Errors.Add(new ParserError(
                ParserErrorType.Format, 
                "Expected space after method!"));
            State = ParserState.Unkown;
            return;
        }

        ProtState = ProtocolState.Resource;
        BufferPos = 4;
        Builder.WithMethod(method);
    }

    private static BPMethod ExtractMethod(string method) => method switch
    {
        "SYC" => BPMethod.SYC,
        "UPL" => BPMethod.UPL,
        "DWN" => BPMethod.DWN,
        _ => BPMethod.Unkown
    };

    private void ReadResource()
    {
        if (ProtState != ProtocolState.Resource)
        {
            Errors.Add(new ParserError(ParserErrorType.ParserIsDead, "Something is wrong with the parser!"));
            State = ParserState.Unkown;
            return;
        }

        ReadOnlySpan<byte> span = Buffer[BufferPos..];
        int index = span.IndexOf((byte)'\r');

        string resource = Encoding.UTF8.GetString(span[..index]);
        BufferPos += index;

        if (!IsCorrectLineEnding())
        {
            Errors.Add(new ParserError(
                ParserErrorType.Format,
                "Expected newline after resource!"));
            State = ParserState.Unkown;
            return;
        }

        BufferPos += 2;
        ProtState = ProtocolState.Header;
        Builder.WithResource(resource);
    }

    private void ReadHeader()
    {
        if (ProtState != ProtocolState.Header)
        {
            Errors.Add(new ParserError(ParserErrorType.ParserIsDead, "Something is wrong with the parser!"));
            State = ParserState.Unkown;
            return;
        }

        ReadOnlySpan<byte> span = Buffer[BufferPos..];
        if (span[0] == (byte)'-')
        {
            BufferPos += 1;

            if (!IsCorrectLineEnding())
            {
                Errors.Add(new ParserError(
                    ParserErrorType.Format,
                    "Expected newline after resource!"));
                State = ParserState.Unkown;
                return;
            }

            ProtState = ProtocolState.Body;
            BufferPos += 2;
            return;
        }

        int index = span.IndexOf((byte)'\r');
        (string, string) header = ExtractHeader(span[..index]);
        BufferPos += index;
        if (!IsCorrectLineEnding())
        {
            Errors.Add(new ParserError(
                ParserErrorType.Format,
                "Expected newline after header!"));

            State = ParserState.Unkown;
            return;
        }

        BufferPos += 2;
        Builder.WithHeader(header);
    }

    private static (string, string) ExtractHeader(ReadOnlySpan<byte> data)
    {
        string[] splitted = Encoding.UTF8.GetString(data)
            .Split(":")
            .Select(x => x.Trim())
            .ToArray();

        if (splitted.Count() != 2)
        {
            throw new FormatException("Header must contain only one ':'!");
        }

        return (splitted[0], splitted[1]);
    }

    private void ReadBody()
    {
        if (ProtState != ProtocolState.Body)
        {
            Errors.Add(new ParserError(ParserErrorType.ParserIsDead, "Something is wrong with the parser!"));
            State = ParserState.Unkown;
            return;
        }

        ReadOnlySpan<byte> span = Buffer[BufferPos..];
        if (span[0] == 0x00)
        {
            State = ParserState.Finished;
            return;
        }

        int index = span.IndexOf((byte)0x00);
        if (index == -1)
        {
            Builder.WithBodyData(span);
            State = ParserState.Needed;
            return;
        }

        Builder.WithBodyData(span[..index]);
        State = ParserState.Finished;
    }

    public BPParser WithSuccessHandler(OnParserSuccess successHandler)
    {
        SuccessHandler += successHandler;
        return this;
    }

    public BPParser WithErrorHandler(OnParserError errorHandler)
    {
        ErrorHandler += errorHandler;
        return this;
    }

    private void InvokeSuccess()
    {
        SuccessHandler?.Invoke(Builder.Build());
    }

    private void InvokeFailure()
    {
        ErrorHandler?.Invoke(BPContextBuilder.BuildErrorContext(), Errors); 
    }
}
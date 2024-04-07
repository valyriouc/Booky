namespace Booky.Core;

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
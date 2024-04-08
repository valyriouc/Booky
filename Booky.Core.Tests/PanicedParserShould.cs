using System.Text;

namespace Booky.Core.Tests;

public class PanicedParserShould
{
    [Fact]
    public void ExecuteFailureCallback()
    {
        string payload =
            """
            DEL payload.pdf
            Some: header
            -
            {
                "test": 124
            }
            """;

        BPParser parser = new BPParser(new MemoryStream(Encoding.UTF8.GetBytes(payload)));

        bool errorCalled = false;
        parser.WithErrorHandler(async (errors) =>
        {
            errorCalled = true;
            await Task.CompletedTask;
        });

        bool successCalled = false;

        parser.WithSuccessHandler(async (context) =>
        {
            successCalled = true;

            await Task.CompletedTask;
        });

        parser.Parse();

        Assert.True(errorCalled, "Error callback not called!");
        Assert.False(successCalled, "Success callback was called!");
    }

    [Fact]
    public void RunIntoFailureBecauseMethodUnkown()
    {
        string payload =
            """
            DEL payload.pdf
            Some: header
            -
            {
                "test": 124
            }
            """;

        BPParser parser = new BPParser(new MemoryStream(Encoding.UTF8.GetBytes(payload)));

        parser.WithErrorHandler(async (errors) =>
        {
            Assert.Equal(ParserErrorType.UnkownMethod, errors[0].Type);
            Assert.Equal("This method is not supported!", errors[0].Message);

            await Task.CompletedTask;
        });

        parser.Parse();
    }

    [Fact]
    public void RunIntoFailureBecauseMissingSpaceBetweenMethodAndResource()
    {
        string payload =
            """
            SYC-payload.pdf
            Some: header
            -
            {
                "test": 124
            }
            """;

        BPParser parser = new BPParser(new MemoryStream(Encoding.UTF8.GetBytes(payload)));

        parser.WithErrorHandler(async (errors) =>
        {
            Assert.Equal(ParserErrorType.Format, errors[0].Type);
            Assert.Equal("Expected space after method!", errors[0].Message);

            await Task.CompletedTask;
        });

        parser.Parse();
    }

    [Fact]
    public void RunIntoFailureBecauseMissingNewLineAfterResource()
    {
        string payload =
            """
            SYC payload.pdf sdfd
            Some: header
            -
            {
                "test": 124
            }
            """;

        BPParser parser = new BPParser(new MemoryStream(Encoding.UTF8.GetBytes(payload)));

        parser.WithErrorHandler(async (context, errors) =>
        {
            Assert.Equal(ParserErrorType.Format, errors[0].Type);
            Assert.Equal("Expected newline after resource!", errors[0].Message);

            await Task.CompletedTask;
        });

        parser.Parse();
    }

    [Fact]
    public void RunIntoFailureBecauseMissingNewLineAfterHeaderBlockEnding()
    {
        string payload =
            """
            SYC payload.pdf sdfd
            Some: header
            -dfasd df 
            {
                "test": 124
            }
            """;

        BPParser parser = new BPParser(new MemoryStream(Encoding.UTF8.GetBytes(payload)));

        parser.WithErrorHandler(async (errors) =>
        {
            Assert.Equal(ParserErrorType.Format, errors[0].Type);
            Assert.Equal("Expected newline after header end!", errors[0].Message);

            await Task.CompletedTask;
        });

        parser.Parse();
    }

    [Fact]
    public void RunIntoFailureBecauseMissingNewLineAfterHeaderEnding()
    {
        string payload =
            """
            SYC payload.pdf sdfd
            Some: header safsadf
            -
            {
                "test": 124
            }
            """;

        BPParser parser = new BPParser(new MemoryStream(Encoding.UTF8.GetBytes(payload)));

        parser.WithErrorHandler(async (errors) =>
        {
            Assert.Equal(ParserErrorType.Format, errors[0].Type);
            Assert.Equal("Expected newline after header!", errors[0].Message);

            await Task.CompletedTask;
        });

        parser.Parse();
    }
}

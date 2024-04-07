using System.Text;

namespace Booky.Core.Tests;

public class BPParserSuccessShould
{


    [Fact]
    public void CallSuccessCallback()
    {
        const string payload =
        """
        SYC test/testing.pdf
        Encoding: UTF8
        Content-Type: json
        -
        {
            "test": 123,
            "another": "hello"
        }
        """;

        BPParser parser = new BPParser(new MemoryStream(Encoding.UTF8.GetBytes(payload)));

        bool calledSuccess = false;
        parser.WithSuccessHandler((context) =>
        {
            calledSuccess = true;
        });

        bool calledFailure = false;
        parser.WithErrorHandler((context, errors) =>
        {
            calledFailure = true;
        });

        parser.Parse();

        Assert.False(calledFailure, "Failure callback was called!");
        Assert.True(calledSuccess, "Success callback was not called!");
    }

    [Theory]
    [InlineData("SYC")]
    [InlineData("UPL")]
    [InlineData("DWN")]
    public void DetectCorrectMethod(string method)
    {
        string payload =
        $$"""
        {{method}} test/testing.pdf
        Encoding: UTF8
        Content-Type: json
        -
        {
            "test": 123,
            "another": "hello"
        }
        """;

        BPParser parser = new BPParser(new MemoryStream(Encoding.UTF8.GetBytes(payload)));

        bool calledSuccess = false;
        parser.WithSuccessHandler((context) =>
        {
            calledSuccess = true;
            Assert.Equal(Enum.Parse<BPMethod>(method), context.Method);
        });

        parser.Parse();

        Assert.True(calledSuccess, "Success callback was not called!");
    }

    [Theory]
    [InlineData("payload.pdf")]
    [InlineData("test/test.pdf")]
    [InlineData("what/the/matter.md")]
    public void RetrieveCorrectResource(string resource)
    {
        string payload =
        $$"""
        SYC {{resource}}
        Encoding: UTF8
        Content-Type: json
        -
        {
            "test": 123,
            "another": "hello"
        }
        """;

        BPParser parser = new BPParser(new MemoryStream(Encoding.UTF8.GetBytes(payload)));

        bool calledSuccess = false;
        parser.WithSuccessHandler((context) =>
        {
            calledSuccess = true;
            Assert.Equal(resource, context.Resource);
        });

        parser.Parse();

        Assert.True(calledSuccess, "Success callback was not called!");
    }

    [Fact]
    public void RetrieveCorrectHeaders()
    {
        string payload =
        """
        SYC test.pdf
        Encoding: UTF8
        Custom: header
        Key: value
        Content-Type: json
        -
        {
            "test": 123,
            "another": "hello"
        }
        """;

        BPParser parser = new BPParser(new MemoryStream(Encoding.UTF8.GetBytes(payload)));

        bool calledSuccess = false;
        parser.WithSuccessHandler((context) =>
        {
            calledSuccess = true;
            Assert.Contains(("Encoding", "UTF8"), context.Headers);
            Assert.Contains(("Custom", "header"), context.Headers);
            Assert.Contains(("Key", "value"), context.Headers);
            Assert.Contains(("Content-Type", "json"), context.Headers);

        });

        parser.Parse();

        Assert.True(calledSuccess, "Success callback was not called!");
    }

    [Fact]
    public void RetrieveCorrectBody()
    {
        string payload =
        """
        SYC test.pdf
        Encoding: UTF8
        Custom: header
        Key: value
        Content-Type: json
        -
        {
            "test": 123,
            "another": "hello"
        }
        """;

        BPParser parser = new BPParser(new MemoryStream(Encoding.UTF8.GetBytes(payload)));

        string expected =
            """
            {
                "test": 123,
                "another": "hello"
            }
            """;

        bool calledSuccess = false;
        parser.WithSuccessHandler((context) =>
        {
            calledSuccess = true;

            context.Body.Position = 0;

            using StreamReader sr = new(context.Body);
            
            Assert.Equal(expected, sr.ReadToEnd());
        });

        parser.Parse();

        Assert.True(calledSuccess, "Success callback was not called!");
    }
}

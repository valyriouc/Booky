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
}

using Booky.Core;
using System.Net;
using System.Net.Sockets;

namespace Booky.Server;

internal class Program
{
    static (string, int) ParseArgs(string[] args)
    {
        if (args.Length != 4)
        {
            Console.WriteLine("Please specify address and port!");
            Environment.Exit(1);
        }

        string address = string.Empty;
        int port = -1;
        for (int i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "-p":
                    port = int.Parse(args[i + 1]);
                    i += 1;
                    break;
                case "-a":
                    address = args[i + 1];
                    i += 1;
                    break;
                default:
                    Console.WriteLine("This is not a valid args!");
                    Environment.Exit(1);
                    break;
            }
        }

        return (address, port);
    }

    static async Task Main(string[] args)
    {
        (string address, int port) = ParseArgs(args);

        IPEndPoint endpoint = new(IPAddress.Parse(address), port);
        using TcpListener listener = new(endpoint);

        Console.WriteLine($"Starting listener on port {port}");
        listener.Start();

        List<Task> tasks = new List<Task>();    
        while(true)
        {
            Console.WriteLine("Waiting for a connection...");
            TcpClient tcpClient = await listener.AcceptTcpClientAsync();
            Console.WriteLine("Received a connection!");

            Task task = Task.Run(() => HandleRequest(tcpClient));

            tasks.Add(task);    
        }

        await Task.WhenAll(tasks);
    }

    static async Task HandleRequest(TcpClient client)
    {
        BPParser parser = new BPParser(client.GetStream());

        parser.WithErrorHandler(HandleFailure);
        parser.WithSuccessHandler(HandleSuccess);

        parser.Parse();

        client.Close();
    }

    static void HandleFailure(BPContext context, List<ParserError> errors)
    {

    }

    static void HandleSuccess(BPContext context)
    {

    }
}

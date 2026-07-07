using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Godot;
using ServerSystem.ClientHandler;


//  NOTE: GOALS
//  1. Seperate Input and Output System
//  2. Check whether Message Queue Works


namespace ServerSystem;

public partial class Server : Node
{
    private static TcpListener server { get; set; }
    public static bool isServerRunning { get; set; }
    private static List<ClientData> ClientList { get => clientList; set => clientList = value; }

    private static readonly int portNumber = 4001;
    private static IPAddress host = IPAddress.Parse("127.0.0.1");
    private static List<ClientData> clientList = [];

    private static readonly Lock _lock = new();


    public static async Task StartServer()
    {
        server = new TcpListener(host, portNumber);
        server.Start();
        GD.Print(" --- Server started --- ");
        isServerRunning = true;

        while (true) {
            var client = await server.AcceptTcpClientAsync();
            GD.Print(" --- Accept New Client --- ");
            var currentTask = StartConnectionAsync(client);
            if (currentTask.IsFaulted) {
                currentTask.Wait();
            }
        }
    }

    
    private async static Task StartConnectionAsync(TcpClient client) 
    {
        ClientData clientData = new(client);
        lock (_lock)
        {
            ClientList.Add(clientData); 
        }
        await clientData.SaveID();

        //  TODO: Await for a Input Handler Function, then queue to ClientRef a message queue
        try {
            if (!string.IsNullOrEmpty(clientData.ID) && clientData.ID != "NONE") {
                await TestMessageQueueAsync(clientData);
                // await EchoClientAsync(clientData);
                // await LinkClientsAsync(clientData);
                // await PushtoAllClientsAsync();
            }
        }
        catch (Exception e) {
            GD.Print(e);
        }
        finally {
            lock (_lock) {
                ClientList.Remove(clientData);
            }
            clientData.Dispose();
        }
    }

    
    private static async Task GrabAsyncInput(ClientData clientData)
    {
        while (ClientList.Count >= 1) {

        }
    }








    //  TODO: Make sure Message Queue works
    private static async Task TestMessageQueueAsync(ClientData clientData)
    {
        while (ClientList.Count >= 1)
        {
            string clientMsg = await clientData.streamReader.ReadLineAsync();

            if (clientMsg != null) {
                if (clientMsg.StartsWith("MSG:")) {
                    // clientData.messageQueue.EnqueueMessage($"{clientData.ID}: {clientMsg.Substring(4)}");
                } else {
                    await clientData.streamWriter.WriteLineAsync($"---  {clientData.ID} -- QUIT SUCCESFULLY  ---");
                    await clientData.streamWriter.FlushAsync();
                    break;
                }
            }
        }
    }


    //  TODO: Make Push to All Clients Seperate from input and output
    private static async Task PushtoAllClientsAsync()
    {
        while (ClientList.Count >= 1)
        {
            Console.WriteLine(" > ");
            string serverMsg = Console.ReadLine();

            if (serverMsg != null) {
                if (serverMsg != "q") {
                    for (int i = 0; i < ClientList.Count; i++)
                    {
                        await ClientList[i].streamWriter.WriteLineAsync($"---  Server: {serverMsg}  ---");
                        await ClientList[i].streamWriter.FlushAsync();
                    }
                } else {
                    for (int i = 0; i < ClientList.Count; i++)
                    {
                        await ClientList[i].streamWriter.WriteLineAsync($"QUIT---  {ClientList[i].ID} -- FORCE QUIT FROM SERVER  ---");
                        await ClientList[i].streamWriter.FlushAsync();
                    }
                    break;
                }
            }
        }
    }

    //  TODO: Linking should be done within a seperate function based on message type
    private static async Task LinkClientsAsync(ClientData clientData)
    {
        while (ClientList.Count >= 1)
        {
            string clientMsg = await clientData.streamReader.ReadLineAsync();

            if (clientMsg != null) {
                if (clientMsg.StartsWith("MSG:")) {
                    GD.Print($"{clientData.ID}: {clientMsg.Substring(4)}");

                    if (ClientList.Count > 1) {
                        foreach (ClientData otherClient in ClientList) {
                            if (otherClient.ID != clientData.ID) {
                                await otherClient.streamWriter.WriteLineAsync($"---  {clientData.ID}: {clientMsg.Substring(4)}  ---");
                                await otherClient.streamWriter.FlushAsync();
                            }
                        }
                    } else {
                        await clientData.streamWriter.WriteLineAsync("1");
                        await clientData.streamWriter.FlushAsync();
                    }
                } else {
                    await clientData.streamWriter.WriteLineAsync($"---  {clientData.ID} -- QUIT SUCCESFULLY  ---");
                    await clientData.streamWriter.FlushAsync();
                    break;
                }
            }
        }
        GD.Print($"CLIENT QUIT: {clientData.ID}");
    }


    //  TODO: Server Echo (Use as Testing)
    private static async Task EchoClientAsync(ClientData clientData) 
    {
        while (ClientList.Count >= 1)
        {
            string clientMsg = await clientData.streamReader.ReadLineAsync();

            if (clientMsg != null) {
                if (clientMsg.StartsWith("MSG:")) {
                    GD.Print($"{clientData.ID}: {clientMsg.Substring(4)}");  
                } else {
                    await clientData.streamWriter.WriteLineAsync($"---  {clientData.ID} -- QUIT SUCCESFULLY  ---");
                    await clientData.streamWriter.FlushAsync();
                    break;
                }
            }
        }
        GD.Print($"CLIENT QUIT: {clientData.ID}");
    }

    //  TODO: Overhaul this to clean data fully
    public void KillAll()
    {
        GD.Print("Shutting Down...");

        lock (_lock)
        {
            if (ClientList.Count > 0) {
                foreach (ClientData clientData in ClientList) {
                    clientData.Dispose();
                    ClientList.Remove(clientData);
                }
            }
        }
    }

	public override void _Ready()
	{
        Task serverStart = Task.Run(async () => await StartServer());
        if (serverStart.IsFaulted) {
            serverStart.Wait();
        }
	}

	public override void _Process(double delta)
	{

	}

    public override void _ExitTree()
    {
        KillAll();    
    }
}

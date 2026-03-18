using Godot;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;


public partial class Server : Node
{
    class ClientData : IDisposable
    {
        public string ID { get; private set; } 
        public TcpClient clientRef { get; private set; }
        public TextWriter streamWriter { get; private set; }
        public TextReader streamReader { get; private set; }

        public ClientData(TcpClient client)
        {
            clientRef = client;

            NetworkStream networkStream = clientRef.GetStream();

            streamReader = new StreamReader(networkStream);
            streamWriter = new StreamWriter(networkStream);
        }

        public async Task SaveID()
        {
            string incomingMsg;
            incomingMsg = await streamReader.ReadLineAsync();
            
            if (incomingMsg.StartsWith("CON:")) {
                GD.Print($"NEW CLIENT: " + incomingMsg.Substring(4));
                await streamWriter.WriteLineAsync($"---  {incomingMsg.Substring(4)}  -- CONNECTED SUCCESFULLY ---");
                await streamWriter.FlushAsync();

                ID = incomingMsg.Substring(4);
            }
            else
            {
                await streamWriter.WriteLineAsync("Please Send Confirmation ID");
                await streamWriter.FlushAsync();

                ID = "NONE";
            }
        }

        public void Dispose() 
        {
            Task closeClientData = Task.Run( async () => await Close());
        }

        public async Task Close() 
        {
            await streamWriter.WriteLineAsync($"Server Closing Connection to {ID}");
            await streamWriter.FlushAsync();

            streamWriter.Close();
            streamReader.Close();
            clientRef.Close();
            
            GD.Print($"Closing Client: {ID}");
        }
    }

    
    private static TcpListener server { get; set; }
    public static bool isServerRunning { get; set; }

    private static readonly int portNumber = 4001;
    private static IPAddress host = IPAddress.Parse("127.0.0.1");
    private static List<ClientData> clientList = [];

    private static readonly object _lock = new();

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
            clientList.Add(clientData); 
        }
        await clientData.SaveID();

        try {
            if (!string.IsNullOrEmpty(clientData.ID) && clientData.ID != "NONE") {
                await EchoClientAsync(clientData);
                // await LinkClientsAsync(clientData);
                // await PushtoAllClientsAsync();
            }
        }
        catch (Exception e) {
            GD.Print(e);
        }
        finally {
            lock (_lock) {
                clientList.Remove(clientData);
            }
            clientData.Dispose();
        }
    }


    private static async Task PushtoAllClientsAsync()
    {
        while (clientList.Count >= 1)
        {
            Console.WriteLine(" > ");
            string serverMsg = Console.ReadLine();

            if (serverMsg != null) {
                if (serverMsg != "q") {
                    for (int i = 0; i < clientList.Count; i++)
                    {
                        await clientList[i].streamWriter.WriteLineAsync($"---  Server: {serverMsg}  ---");
                        await clientList[i].streamWriter.FlushAsync();
                    }
                } else {
                    for (int i = 0; i < clientList.Count; i++)
                    {
                        await clientList[i].streamWriter.WriteLineAsync($"QUIT---  {clientList[i].ID} -- FORCE QUIT FROM SERVER  ---");
                        await clientList[i].streamWriter.FlushAsync();
                    }
                    break;
                }
            }
        }
    }

    private static async Task LinkClientsAsync(ClientData clientData)
    {
        while (clientList.Count >= 1)
        {
            string clientMsg = await clientData.streamReader.ReadLineAsync();

            if (clientMsg != null) {
                if (clientMsg.StartsWith("MSG:")) {
                    GD.Print($"{clientData.ID}: {clientMsg.Substring(4)}");

                    if (clientList.Count > 1) {
                        foreach (ClientData otherClient in clientList) {
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

    private static async Task EchoClientAsync(ClientData clientData) 
    {
        while (clientList.Count >= 1)
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


    public void KillAll()
    {
        GD.Print("Shutting Down...");

        lock (_lock)
        {
            if (clientList.Count > 0) {
                foreach (ClientData clientData in clientList) {
                    clientData.Dispose();
                    clientList.Remove(clientData);
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

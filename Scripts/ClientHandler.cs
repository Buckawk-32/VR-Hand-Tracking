using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using System.Threading.Tasks;
using Godot;

namespace ServerSystem.ClientHandler; 

//  NOTE: GOALS
//  1. Add MessageRef datatype
//      - Make sure to keep this as memory effecient as possible
//  2. Less Nested Classes the better

class ClientData : IDisposable 
{
    public class ClientSideMessageQueue
    {
        private readonly Queue<string> _queue = new Queue<string>();
        private int maxItem { get; set; } 

        public ClientSideMessageQueue(int maxItem) => this.maxItem = maxItem;

        public void Enqueue(string data)
        {
            _queue.Enqueue(data);
            if (_queue.Count == maxItem) {
                _queue.Dequeue();
            }
        }

        public string Dequeue() => _queue.Dequeue();

        public int Count() => _queue.Count;

        public void Dispose() 
        {
            _queue.Clear();
        }
    }


    public string ID { get; private set; } 
    public TcpClient clientRef { get; private set; }
    public TextWriter streamWriter { get; private set; }
    public TextReader streamReader { get; private set; }
    public ClientSideMessageQueue messageQueue { get; private set; }

    public ClientData(TcpClient client)
    {
        clientRef = client;

        NetworkStream networkStream = clientRef.GetStream();

        streamReader = new StreamReader(networkStream);
        streamWriter = new StreamWriter(networkStream);

        messageQueue = new ClientSideMessageQueue(3);
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
        messageQueue.Dispose();
        GD.Print($"Clearing Client {ID}'s Data..."); 

        await streamWriter.WriteLineAsync($"Server Closing Connection to {ID}");
        await streamWriter.FlushAsync();

        streamWriter.Close();
        streamReader.Close();
        clientRef.Close();

        GD.Print($"Closing Client: {ID}");
    }
}

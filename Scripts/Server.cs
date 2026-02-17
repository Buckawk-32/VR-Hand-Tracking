using Godot;
using System;
using System.Diagnostics;
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

        public async Task SaveDevId()
        {
        }
    }

    
    private static TcpListener server { get; set; }
    private static bool isServerRunning { get; set; }

    private static int portNumber = 4001;
    private static IPAddress host = IPAddress.Parse("1207.0.0.1")


	public override void _Ready()
	{
	}

	public override void _Process(double delta)
	{
	}
}

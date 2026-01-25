using Godot;
using System;
using System.Net.Sockets;
using System.Text;
using System.Collections.Generic;

public partial class Connection : Node
{
	private TcpClient _client;
	private NetworkStream _stream;
	private byte[] _buffer = new byte[4096];
	private StringBuilder _incoming = new StringBuilder();
	
	public override void _Ready()
	{
		try
		{
			_client = new TcpClient();
			_client.Connect("127.0.0.1", 12345);
			_stream = _client.GetStream();

			GD.Print("Conectado al servidor Zappy!");
			SendMessage("msz");
		}
		catch (Exception ex)
		{
			GD.PrintErr("Error al conectar: " + ex.Message);
		}
	}

	public void SendMessage(string msg)
	{
		if (_stream == null) return;

		byte[] data = Encoding.UTF8.GetBytes(msg + "\n");
		_stream.Write(data, 0, data.Length);
	}

	public override void _Process(double delta)
	{
		if (_stream == null || !_stream.DataAvailable) return;

		byte[] buffer = new byte[_client.Available];
		int bytesRead = _stream.Read(buffer, 0, buffer.Length);

		string msg = Encoding.UTF8.GetString(buffer, 0, bytesRead);
		HandleServerMessage(msg);
	}
	
	private void HandleServerMessage(string line)
	{
		var parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
		if (parts.Length == 0)
			return;

		switch (parts[0])
		{
			case "msz":
			{
				int width = int.Parse(parts[1]);
				int height = int.Parse(parts[2]);
				GD.Print($"Mapa de tamaño: {width} x {height}");
				break;
			}

			case "bct":
			{
				int x = int.Parse(parts[1]);
				int y = int.Parse(parts[2]);

				// Recursos son lo que viene a partir del índice 3
				var resources = new List<string>();
				for (int i = 3; i < parts.Length; i++)
					resources.Add(parts[i]);

				GD.Print($"Casilla {x},{y} tiene: {string.Join(", ", resources)}");
				break;
			}

			case "pnw":
				GD.Print("Nuevo jugador: " + string.Join(" ", parts));
				break;

			default:
				GD.Print("Mensaje desconocido: " + line);
				break;
		}
	}
}

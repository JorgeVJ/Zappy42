using Godot;
using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;

// Encapsula el transporte hacia el servidor Zappy: o bien un socket TCP real,
// o bien MockServer (modo mock). Hacia afuera es transparente: emite
// LineReceived(line) por cada línea completa del protocolo (reensamblada a
// partir del stream TCP, o generada por MockServer) y expone SendMessage()
// para enviar comandos (no-op en modo mock salvo para sgt/sst, ver SendMessage).
public partial class ServerTransport : Node
{
    private TcpClient _client;
    private NetworkStream _stream;

    // Acumula bytes recibidos entre frames: TCP es un stream y una línea del
    // protocolo puede llegar partida en varios paquetes (ver B1).
    private string _recvBuffer = "";

    // Destino de conexión real; sobrescrito por los flags -h/-p (ver ParseConnectionArgs).
    private string _host = "127.0.0.1";
    private int _port = 12345;

    private MockServer _mockServer;

    // Pon a true para usar el servidor simulado sin intentar conexión real.
    public bool UseMockServer = true;

    // Emitido por cada línea completa del protocolo, ya sea leída del socket
    // real (reensamblada por \n) o generada por MockServer.
    public event Action<string> LineReceived;

    // Emitido cuando se pierde la conexión real (fin de stream o error de socket).
    public event Action<string> Disconnected;

    public override void _Ready()
    {
        ParseConnectionArgs();

        if (UseMockServer)
        {
            _mockServer = new MockServer();
            Log.Debug("[ServerTransport] Modo mock activo — sin conexión TCP.");
            return;
        }

        try
        {
            _client = new TcpClient();
            _client.Connect(_host, _port);
            _stream = _client.GetStream();

            Log.Debug($"[ServerTransport] Conectado a {_host}:{_port}. Esperando WELCOME...");
            // El GRAPHIC se envía al recibir WELCOME (handshake Zappy), no al conectar.
        }
        catch (Exception ex)
        {
            Log.Error($"[ServerTransport] Error al conectar a {_host}:{_port}: {ex.Message}");
            Log.Error("[ServerTransport] Uso: zappy_gui -p <puerto> -h <host> [--mock]");
        }
    }

    // Lee los flags de línea de comandos (-p puerto, -h host, --mock) para soportar
    // el arranque exigido por el subject: zappy_gui -p <port> -h <host>.
    // Si se pasan -p/-h válidos se fuerza la conexión real; --mock siempre gana.
    private void ParseConnectionArgs()
    {
        var args = new List<string>();
        args.AddRange(OS.GetCmdlineUserArgs());
        args.AddRange(OS.GetCmdlineArgs());

        bool hasConnArgs = false;
        bool forceMock = false;

        for (int i = 0; i < args.Count; i++)
        {
            switch (args[i])
            {
                case "-p" when i + 1 < args.Count:
                    if (int.TryParse(args[i + 1], out int p))
                    {
                        _port = p;
                        hasConnArgs = true;
                    }
                    else
                    {
                        Log.Error($"[ServerTransport] Puerto inválido: '{args[i + 1]}'");
                    }
                    break;
                case "-h" when i + 1 < args.Count:
                    _host = args[i + 1];
                    hasConnArgs = true;
                    break;
                case "--mock":
                    forceMock = true;
                    break;
            }
        }

        if (forceMock)
            UseMockServer = true;
        else if (hasConnArgs)
            UseMockServer = false;

        Log.Debug($"[ServerTransport] Args de conexión: host={_host}, port={_port}, mock={UseMockServer}");
    }

    // Envía un comando al servidor real. En modo mock no hay socket: el ajuste
    // de velocidad (sst) se redirige a MockServer.SetSpeed vía SetMockSpeed(),
    // y el resto de comandos (GRAPHIC, sgt, mct, ...) son no-op.
    public void SendMessage(string msg)
    {
        if (_stream == null)
        {
            return;
        }

        Log.Debug($"Sending: {msg}");
        byte[] data = Encoding.UTF8.GetBytes(msg + "\n");
        _stream.Write(data, 0, data.Length);
    }

    // Ajusta la velocidad del MockServer (equivalente al "sst T" real). No-op si
    // no estamos en modo mock; en ese caso el llamador debe usar SendMessage("sst T").
    public void SetMockSpeed(int t)
    {
        _mockServer?.SetSpeed(t);
    }

    public override void _Process(double delta)
    {
        // Mockeo de mensajes para pruebas
        if (_mockServer != null)
        {
            string mockMsg = _mockServer.GetNextCommand(delta);
            if (!string.IsNullOrEmpty(mockMsg))
            {
                Log.Debug("[MOCK] " + mockMsg);
                LineReceived?.Invoke(mockMsg);
            }
            return;
        }

        if (_stream == null)
        {
            return;
        }

        try
        {
            if (!_stream.DataAvailable)
            {
                return;
            }

            byte[] buffer = new byte[_client.Available];
            int bytesRead = _stream.Read(buffer, 0, buffer.Length);

            if (bytesRead == 0)
            {
                // Lectura de 0 bytes con datos disponibles = cierre ordenado del servidor.
                HandleDisconnect("el servidor cerró la conexión");
                return;
            }

            string chunk;
            try
            {
                // Forzar excepción si hay bytes inválidos para UTF-8, así los detectamos y los registramos.
                chunk = new System.Text.UTF8Encoding(false, true).GetString(buffer, 0, bytesRead);
            }
            catch (System.Text.DecoderFallbackException)
            {
                // Loguear los bytes crudos en hex para depuración
                Log.Error($"Unicode parsing error: invalid UTF-8 bytes recibidos. Raw: {BitConverter.ToString(buffer, 0, bytesRead)}");

                // Intentar decodificar con el fallback permissivo para seguir procesando (reemplaza por caracter de sustitucion)
                chunk = Encoding.UTF8.GetString(buffer, 0, bytesRead);
            }

            // Acumular y procesar SOLO líneas completas (terminadas en \n);
            // conservar el residuo parcial para la siguiente iteración.
            _recvBuffer += chunk;

            int newline;
            while ((newline = _recvBuffer.IndexOf('\n')) >= 0)
            {
                string line = _recvBuffer.Substring(0, newline).TrimEnd('\r');
                _recvBuffer = _recvBuffer.Substring(newline + 1);

                if (line.Length == 0)
                {
                    continue;
                }

                Log.Debug("Processing line: " + line);
                LineReceived?.Invoke(line);
            }
        }
        catch (System.IO.IOException ex)
        {
            HandleDisconnect($"IOException: {ex.Message}");
        }
        catch (ObjectDisposedException ex)
        {
            HandleDisconnect($"socket cerrado: {ex.Message}");
        }
    }

    // Cierre limpio ante desconexión / fin de stream del servidor (B7).
    private void HandleDisconnect(string reason)
    {
        Log.Error($"[ServerTransport] Servidor desconectado ({reason}).");

        try { _stream?.Close(); } catch { /* ya cerrado */ }
        try { _client?.Close(); } catch { /* ya cerrado */ }

        _stream = null;
        _client = null;
        _recvBuffer = "";

        Disconnected?.Invoke(reason);
    }
}

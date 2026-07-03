using Godot;
using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;

/// <summary>
/// Encapsula el transporte hacia el servidor Zappy: o bien un socket TCP real,
/// o bien MockServer (modo mock).
/// </summary>
/// <remarks>
/// Hacia afuera es transparente: emite LineReceived(line) por cada línea
/// completa del protocolo (reensamblada a partir del stream TCP, o generada
/// por MockServer) y expone SendMessage() para enviar comandos (no-op en modo
/// mock salvo para sgt/sst, ver SendMessage).
/// </remarks>
public partial class ServerTransport : Node
{
    private TcpClient _client;
    private NetworkStream _stream;

    /// <summary>
    /// Acumula bytes recibidos entre frames: TCP es un stream y una línea del
    /// protocolo puede llegar partida en varios paquetes.
    /// </summary>
    private string _recvBuffer = "";

    /// <summary>
    /// Destino de conexión real; sobrescrito por los flags -h/-p (ver ParseConnectionArgs).
    /// </summary>
    private string _host = "127.0.0.1";
    private int _port = 12345;

    private MockServer _mockServer;

    /// <summary>
    /// Pon a true para usar el servidor simulado sin intentar conexión real.
    /// </summary>
    public bool UseMockServer = true;

    /// <summary>
    /// Emitido por cada línea completa del protocolo, ya sea leída del socket
    /// real (reensamblada por \n) o generada por MockServer.
    /// </summary>
    public event Action<string> LineReceived;

    /// <summary>
    /// Emitido cuando se pierde la conexión real (fin de stream o error de socket).
    /// </summary>
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

        ConnectReal();
    }

    /// <summary>
    /// El GRAPHIC se envía al recibir WELCOME (handshake Zappy), no al conectar.
    /// </summary>
    private void ConnectReal()
    {
        try
        {
            _client = new TcpClient();
            _client.Connect(_host, _port);
            _stream = _client.GetStream();

            Log.Debug($"[ServerTransport] Conectado a {_host}:{_port}. Esperando WELCOME...");
        }
        catch (Exception ex)
        {
            Log.Error($"[ServerTransport] Error al conectar a {_host}:{_port}: {ex.Message}");
            Log.Error("[ServerTransport] Uso: zappy_gui -p <puerto> -h <host> [--mock]");
        }
    }

    /// <summary>
    /// Lee los flags de línea de comandos (-p puerto, -h host, --mock) para soportar
    /// el arranque exigido por el subject: zappy_gui -p &lt;port&gt; -h &lt;host&gt;.
    /// Si se pasan -p/-h válidos se fuerza la conexión real; --mock siempre gana.
    /// </summary>
    private void ParseConnectionArgs()
    {
        List<string> args = new List<string>();
        args.AddRange(OS.GetCmdlineUserArgs());
        args.AddRange(OS.GetCmdlineArgs());

        bool hasConnArgs = false;
        bool forceMock = false;
        ParseConnectionArgsInto(args, ref hasConnArgs, ref forceMock);

        if (forceMock)
            UseMockServer = true;
        else if (hasConnArgs)
            UseMockServer = false;

        Log.Debug($"[ServerTransport] Args de conexión: host={_host}, port={_port}, mock={UseMockServer}");
    }

    private void ParseConnectionArgsInto(List<string> args, ref bool hasConnArgs, ref bool forceMock)
    {
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
    }

    /// <summary>
    /// Envía un comando al servidor real. En modo mock no hay socket: el ajuste
    /// de velocidad (sst) se redirige a MockServer.SetSpeed vía SetMockSpeed(),
    /// y el resto de comandos (GRAPHIC, sgt, mct, ...) son no-op.
    /// </summary>
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

    /// <summary>
    /// Ajusta la velocidad del MockServer (equivalente al "sst T" real). No-op si
    /// no estamos en modo mock; en ese caso el llamador debe usar SendMessage("sst T").
    /// </summary>
    public void SetMockSpeed(int t)
    {
        _mockServer?.SetSpeed(t);
    }

    public override void _Process(double delta)
    {
        if (_mockServer != null)
        {
            ProcessMockTick(delta);
            return;
        }

        if (_stream == null)
        {
            return;
        }

        ProcessRealSocket();
    }

    private void ProcessMockTick(double delta)
    {
        string mockMsg = _mockServer.GetNextCommand(delta);
        if (!string.IsNullOrEmpty(mockMsg))
        {
            Log.Debug("[MOCK] " + mockMsg);
            LineReceived?.Invoke(mockMsg);
        }
    }

    private void ProcessRealSocket()
    {
        try
        {
            ReadAndDispatch();
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

    /// <summary>
    /// Reads pending bytes from the socket and dispatches complete lines. Extracted from
    /// ProcessRealSocket to keep that method within the method-length convention.
    /// </summary>
    private void ReadAndDispatch()
    {
        if (!_stream.DataAvailable)
        {
            return;
        }

        byte[] buffer = new byte[_client.Available];
        int bytesRead = _stream.Read(buffer, 0, buffer.Length);

        if (bytesRead == 0)
        {
            HandleDisconnect("el servidor cerró la conexión");
            return;
        }

        string chunk = DecodeChunk(buffer, bytesRead);
        _recvBuffer += chunk;
        DispatchCompleteLines();
    }

    /// <summary>
    /// Fuerza excepción si hay bytes inválidos para UTF-8, así los detectamos y
    /// los registramos; en ese caso reintenta con el fallback permisivo (reemplaza
    /// por carácter de sustitución) para poder seguir procesando.
    /// </summary>
    private string DecodeChunk(byte[] buffer, int bytesRead)
    {
        try
        {
            return new UTF8Encoding(false, true).GetString(buffer, 0, bytesRead);
        }
        catch (DecoderFallbackException)
        {
            Log.Error($"Unicode parsing error: invalid UTF-8 bytes recibidos. Raw: {BitConverter.ToString(buffer, 0, bytesRead)}");
            return Encoding.UTF8.GetString(buffer, 0, bytesRead);
        }
    }

    /// <summary>
    /// Acumula y procesa solo líneas completas (terminadas en \n); conserva el
    /// residuo parcial en _recvBuffer para la siguiente iteración.
    /// </summary>
    private void DispatchCompleteLines()
    {
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

    private void HandleDisconnect(string reason)
    {
        Log.Error($"[ServerTransport] Servidor desconectado ({reason}).");

        try { _stream?.Close(); } catch { }
        try { _client?.Close(); } catch { }

        _stream = null;
        _client = null;
        _recvBuffer = "";

        Disconnected?.Invoke(reason);
    }
}

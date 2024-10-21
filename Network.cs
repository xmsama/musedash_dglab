using MelonLoader;
using MuseDash_DgLab;
using System;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

public class WebSocketClient
{
    private ClientWebSocket _webSocket;
    private Uri _serverUri;
    private CancellationTokenSource _cancellationTokenSource;
    private bool _isConnected;
    private const int ReconnectDelay = 5000; // 5 seconds

    public WebSocketClient(string serverUri)
    {
        _serverUri = new Uri(serverUri);
        _webSocket = new ClientWebSocket();
        _cancellationTokenSource = new CancellationTokenSource();
        _isConnected = false;
    }

    public async Task ConnectAsync()
    {
        if (_webSocket.State == WebSocketState.Open || _webSocket.State == WebSocketState.Connecting)
        {
            Melon<ModMain>.Logger.Msg("Is Connect!");
            return;
        }
            

        try
        {
            await _webSocket.ConnectAsync(_serverUri, _cancellationTokenSource.Token);
            _isConnected = true;
            MelonLogger.Msg("WebSocket Connect!");
            //Task.Run(() => ReceiveMessagesAsync());
        }
        catch (Exception ex)
        {
            MelonLogger.Error($"WebSocket Connect Failed: {ex.Message}");
            _isConnected = false;
            await Task.Delay(ReconnectDelay);
            await ConnectAsync(); // Attempt to reconnect
        }
    }

    public async Task SendMessageAsync(string message)
    {
       
        if (_webSocket.State != WebSocketState.Open)
        {
            MelonLogger.Error("WebSocket is not connected.");
            return;
        }

        var messageBuffer = Encoding.UTF8.GetBytes(message);
        var segment = new ArraySegment<byte>(messageBuffer);
       
        try
        {
            await _webSocket.SendAsync(segment, WebSocketMessageType.Text, true, _cancellationTokenSource.Token);
          
        }
        catch (Exception ex)
        {
            MelonLogger.Error($"Error sending message: {ex.Message}");
        }
    }

    private async Task ReceiveMessagesAsync()
    {
        var buffer = new byte[1024 * 4];

        try
        {
            while (_webSocket.State == WebSocketState.Open)
            {
                var result = await _webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), _cancellationTokenSource.Token);
                if (result.MessageType == WebSocketMessageType.Close)
                {
                    await _webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, string.Empty, _cancellationTokenSource.Token);
                    MelonLoader.MelonLogger.Msg("WebSocket closed by the server.");
                    _isConnected = false;
                    await Task.Delay(ReconnectDelay);
                    await ConnectAsync(); // Attempt to reconnect
                }
                else
                {
                    var message = Encoding.UTF8.GetString(buffer, 0, result.Count);
                    MelonLoader.MelonLogger.Msg($"Received message: {message}");
                }
            }
        }
        catch (Exception ex)
        {
            MelonLoader.MelonLogger.Error($"Error receiving messages: {ex.Message}");
            _isConnected = false;
            await Task.Delay(ReconnectDelay);
            await ConnectAsync(); // Attempt to reconnect
        }
    }

    public async Task DisconnectAsync()
    {
        if (_webSocket.State == WebSocketState.Open)
        {
            _cancellationTokenSource.Cancel();
            await _webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closed by client", CancellationToken.None);
            MelonLoader.MelonLogger.Msg("WebSocket disconnected.");
            _isConnected = false;
        }
    }

    public bool IsConnected => _isConnected;
}

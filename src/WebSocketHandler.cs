using EmotionUpdaterWS.src.Data;
using EmotionUpdaterWS.src.Models;
using MongoDB.Driver;
using Newtonsoft.Json;
using System.Net.WebSockets;
using System.Text;

namespace EmotionUpdaterWS.src;
public class WebSocketHandler
{
    private readonly EmotionContext _context;
    private static readonly HashSet<WebSocket> _connectedSockets = new();
    public WebSocketHandler(EmotionContext context)
    {
        _context = context;
    }
    public async Task HandleWebSocketAsync(HttpContext context)
    {
        if (!context.WebSockets.IsWebSocketRequest)
        {
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            return;
        }

        var webSocket = await context.WebSockets.AcceptWebSocketAsync();
        _connectedSockets.Add(webSocket);
        var buffer = new byte[1024 * 4];

        try
        {
            while (webSocket.State == WebSocketState.Open)
            {
                var result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
                var message = Encoding.UTF8.GetString(buffer, 0, result.Count);
                await ProcessMessageAsync(webSocket, message);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"WebSocket error: {ex.Message}");
        }
        finally
        {
            _connectedSockets.Remove(webSocket);
            webSocket.Dispose();
        }
    }

    private async Task ProcessMessageAsync(WebSocket webSocket, string message)
    {
        dynamic? command = JsonConvert.DeserializeObject(message);

        if (command?.package_command == "SERVICE" && command?.command == "AUTH")
        {
            await SendResponseAsync(webSocket, new
            {
                package_command  = "SERVICE",
                command = "AUTH",
                Data = new { Id = Guid.NewGuid().ToString() }
            });
        }
        else if (command?.package_command == "EMOTIONS" && command?.command == "FETCH")
        {
            var data = await _context.PersonEmotions.Find(FilterDefinition<PersonEmotion>.Empty).FirstOrDefaultAsync();
            await SendResponseAsync(webSocket, new
            {
                package_command = "EMOTIONS",
                command = "FETCH",
                Data = new { Emotions = data?.People }
            });
        }
    }
    
    private async Task SendResponseAsync(WebSocket webSocket, object response)
    {
        try
        {
            var responseMessage = JsonConvert.SerializeObject(response);
            var responseBuffer = Encoding.UTF8.GetBytes(responseMessage);

            if (webSocket.State == WebSocketState.Open)
            {
                await webSocket.SendAsync(
                    new ArraySegment<byte>(responseBuffer), 
                    WebSocketMessageType.Text, 
                    true, 
                    CancellationToken.None);
            }
            else
            {
                Console.WriteLine("WebSocket is not in an open state. Current state: " + webSocket.State);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error in SendResponseAsync: {ex.Message}");
        }
    }

    public static async Task NotifyClientsAsync(EmotionContext context)
    {
        var response = new
        {
            package_command = "EMOTIONS",
            command = "UPDATED"
        };
        var responseMessage = JsonConvert.SerializeObject(response);
        var responseBuffer = Encoding.UTF8.GetBytes(responseMessage);

        foreach (var client in _connectedSockets)
        {
            if (client.State == WebSocketState.Open)
            {
                await client.SendAsync(new ArraySegment<byte>(responseBuffer), WebSocketMessageType.Text, true, CancellationToken.None);
            }
        }
    }
}

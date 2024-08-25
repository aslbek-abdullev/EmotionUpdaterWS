using EmotionUpdaterWS.src;
using EmotionUpdaterWS.src.Data;
using EmotionUpdaterWS.src.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton(new EmotionContext("mongodb://localhost:27017/", "EmotionsDb"));
builder.Services.AddSingleton<DatabaseInitializer>();
builder.Services.AddSingleton<WebSocketHandler>();
builder.Services.AddHostedService<EmotionUpdateService>();

var app = builder.Build();

var databaseInitializer = app.Services.GetRequiredService<DatabaseInitializer>();
await databaseInitializer.InitializeDatabaseAsync();

app.UseWebSockets();

app.Use(async (context, next) =>
{
    if (context.WebSockets.IsWebSocketRequest)
    {
        var webSocketHandler = context.RequestServices.GetRequiredService<WebSocketHandler>();
        await webSocketHandler.HandleWebSocketAsync(context);
    }
    else
    {
        await next();
    }
});

app.Run();

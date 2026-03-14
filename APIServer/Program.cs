using APIServer.Services;
using APIServer.Services.SSE.Concrete;
using APIServer.Services.SSE.Contract;
using APIServer.Services.WS.Concrete;
using APIServer.Services.WS.Contract;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddHostedService<OrderEventEmitter>();
builder.Services.AddSingleton(typeof(IEventStore<>), typeof(InMemoryEventStore<>)); 
builder.Services.AddSingleton(typeof(IEventBroadcaster<>), typeof(EventBroacaster<>)); 
builder.Services.AddSingleton<IConnectionManager, ConnectionManager>();
builder.Services.AddSingleton<IChatHandler, ChatHandler>(); 
builder.Services.AddHostedService<TaskEventEmitter>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.UseWebSockets();

app.MapControllers();

app.Run();

using APIServer.Services;
using APIServer.Services.Concrete;
using APIServer.Services.Contract;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddHostedService<OrderEventEmitter>();
builder.Services.AddSingleton(typeof(IEventStore<>), typeof(InMemoryEventStore<>)); 
builder.Services.AddSingleton(typeof(IEventBroadcaster<>), typeof(EventBroacaster<>)); 
var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();

using CompraProg.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using CompraProg.Infrastructure.Messaging;
using CompraProg.Infrastructure.Services;


var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

//Logs claros e sem acentuacao: mensagens sem acento
builder.Logging.ClearProviders();
builder.Logging.AddConsole();

//EF Core + MySQL (Pomelo)
var conn = builder.Configuration.GetConnectionString("MySQL");

//Auto-detecta a versao do MySQL para configurar corretamente o provider.
builder.Services.AddDbContext<AppDbContext>(opt =>
{
    opt.UseMySql(conn, ServerVersion.AutoDetect(conn));
});

//Kafka
var kafkaBootstrap = builder.Configuration["Kafka:BootstrapServers"];
builder.Services.AddSingleton<IKafkaProducer>(sp =>
{
    var logger = sp.GetRequiredService<ILogger<KafkaProducer>>();
    return new KafkaProducer(kafkaBootstrap!, logger);
});

builder.Services.AddScoped<CustodiaService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
    app.UseSwagger();
    app.UseSwaggerUI();


app.MapControllers();

app.Run();

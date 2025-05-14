var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();

var app = builder.Build();

// Configure the HTTP request pipeline.

app.Urls.Add("http://0.0.0.0:5125");

app.UseAuthorization();

app.MapControllers();

app.Run();

using Microsoft.EntityFrameworkCore;
using Products.API.Database;
using Products.API.Endpoints;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<ApplicationDbContext>(opt =>
    opt.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

//builder.Services.AddOpenApi();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();     // отдаёт /swagger/v1/swagger.json
    app.UseSwaggerUI();   // рисует UI на /swagger
}
app.MapProductEndpoints();
app.UseHttpsRedirection();

app.Run();

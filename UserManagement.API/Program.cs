using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using MongoDB.Driver;
using UserManagement.API.Settings;
using System;

var builder = WebApplication.CreateBuilder(args);

// Add MongoDB
builder.Services.AddSingleton<IMongoDatabase>(sp =>
{
    var client = new MongoClient("mongodb://localhost:27017");
    return client.GetDatabase("UserManagementDB");
});

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthorization();
app.MapControllers();

app.Urls.Add("http://0.0.0.0:8080");

app.Run();
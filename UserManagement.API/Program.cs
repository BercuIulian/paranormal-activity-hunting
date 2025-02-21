using MongoDB.Driver;

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

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();
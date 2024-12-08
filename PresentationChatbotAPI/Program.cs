using PresentationChatbotAPI;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddSingleton<PresentationService>();
builder.Services.AddSingleton<ChatbotService>();
builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAngularApp", policy =>
    {
        policy.WithOrigins("http://localhost:4200") // Angular aplikacija
              .AllowAnyMethod()                    // Dozvoljava sve HTTP metode (POST, GET, DELETE itd.)
              .AllowAnyHeader()                    // Dozvoljava sva HTTP zaglavlja
              .SetIsOriginAllowed(origin => true)  // Opcionalno: Dozvoljava sve origin-e ako je potrebno
              .AllowCredentials();                 // Ako koristite autentifikaciju
    });
});


var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

// Aktivacija CORS politike
app.UseCors("AllowAngularApp");

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();

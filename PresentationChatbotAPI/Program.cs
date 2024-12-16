using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using PresentationChatbotAPI;
using PresentationChatbotAPI;
using Microsoft.AspNetCore.Identity.UI;

var builder = WebApplication.CreateBuilder(args);
// Dodaj DbContext sa konekcijom
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Dodaj Identity
builder.Services.AddIdentity<IdentityUser, IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Presentation Chatbot API",
        Version = "v1"
    });
});

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
    // Omogući Swagger i Swagger UI
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "PresentationChatbotAPI v1");
    });
}

// Aktivacija CORS politike
app.UseCors("AllowAngularApp");

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();

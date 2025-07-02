// --- Dependencias necesarias para configurar la aplicación ---
using System.Threading.RateLimiting;
using HighRiskEntitySearcherApi.Models; // Para acceder a los modelos
using HighRiskEntitySearcherApi.Services; // Para acceder a ISearchService y SearchService
using HighRiskEntitySearcherApi.Services.Clients;
using Microsoft.AspNetCore.RateLimiting; // Para acceder a las interfaces y clases de los clientes

// solo se ejecutará el código de instalación y la aplicación se cerrará.
if (args.FirstOrDefault() == "install-playwright")
{
    Console.WriteLine("Iniciando la instalación de los navegadores de Playwright...");

    var exitCode = Microsoft.Playwright.Program.Main(new[] { "install" });

    if (exitCode != 0)
    {
        Console.WriteLine($"La instalación de Playwright falló con el código de salida: {exitCode}");
    }
    else
    {
        Console.WriteLine("¡Éxito! Los navegadores de Playwright se han instalado correctamente.");
    }

    return;
}


var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRateLimiter(options =>
{
    // Define tu política
    options.AddFixedWindowLimiter(policyName: "SearchApiMinuteLimit", opt =>
    {
        opt.PermitLimit = 20; // Permite 20 solicitudes
        opt.Window = TimeSpan.FromMinutes(1); // Cada 1 minuto
        opt.QueueProcessingOrder = QueueProcessingOrder.OldestFirst; // Las solicitudes en cola se procesan en orden de llegada
        opt.QueueLimit = 0; // No encolar solicitudes; rechazarlas directamente si se excede el límite
    });

    // Manejo de la respuesta cuando se excede el límite
    options.OnRejected = async (context, cancellationToken) =>
    {
        context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
        await context.HttpContext.Response.WriteAsync("Demasiadas solicitudes. Por favor, espera un momento.", cancellationToken);
    };
});

var allowedOrigins = builder.Configuration.GetSection("CorsSettings:AllowedOrigins").Get<string[]>();

if (allowedOrigins == null || allowedOrigins.Length == 0)
{
    throw new InvalidOperationException("La configuración para CORS (CorsSettings:AllowedOrigins) no está definida o está vacía en appsettings.json");
}

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowSpecificOrigin",
        policy =>
        {
            // Es necesario reemplazar la ruta de cors policy con el puerto del frontend
            policy.WithOrigins(allowedOrigins)
                  .AllowAnyHeader()
                  .AllowAnyMethod();
        });
});

// Servicios estándar de una API de .NET
builder.Services.AddControllers(); // Habilita el uso de controladores
builder.Services.AddEndpointsApiExplorer(); // Necesario para que Swagger descubra los endpoints
builder.Services.AddSwaggerGen(); // Habilita la generación de la documentación de la API

builder.Services.AddScoped<ISearchService, SearchService>();

builder.Services.AddScoped<IOffshoreLeaksClient, OffshoreLeaksClient>();

builder.Services.AddScoped<IWorldBankClient, WorldBankClient>();


builder.Services.AddScoped<IOFACClient, OFACClient>();



// Una vez que todos los servicios están registrados, construimos la aplicación.
var app = builder.Build();


// Aquí definimos el orden en que se procesará cada petición que llegue a la API.

// En el entorno de desarrollo, habilitamos Swagger para facilitar las pruebas manuales y la documentación.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Redirige automáticamente las peticiones HTTP a HTTPS para mayor seguridad.
app.UseHttpsRedirection();

//  Habilitar el middleware de Rate Limiting en el pipeline de solicitudes
app.UseRateLimiter();

app.UseCors("AllowSpecificOrigin");

app.MapControllers();

// Inicia el servidor web y se queda escuchando peticiones.
app.Run();


public partial class Program { }

using Microsoft.OpenApi.Models;
using System.Reflection;

var builder = WebApplication.CreateBuilder(args);

// Adiciona os serviços de controllers
builder.Services.AddControllers();

// Adiciona o Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Consulta de Temperaturas API",
        Version = "v1",
        Description = "API para consultar a viabilidade de plantação com base nas temperaturas.",
        Contact = new OpenApiContact
        {
            Name = "Matheus Maia",
            Email = "matheusmaia535@gmail.com"
        }
    });

    // Configura o Swagger para usar os comentários XML
    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    c.IncludeXmlComments(xmlPath);
});

var app = builder.Build();

// Habilita o Swagger em desenvolvimento
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Consulta de Temperaturas API v1");
    });
}

// Define a porta 5000
app.Urls.Add("http://localhost:5000");

app.UseRouting();
app.UseAuthorization();

app.MapControllers();

app.Run();
using Serilog;
using Serilog.Sinks.Grafana.Loki;
using Serilog.Formatting.Compact;

var builder = WebApplication.CreateBuilder(args);

// Set up Serilog with JSON formatting and Loki sink
builder.Host.UseSerilog((context, services, configuration) =>
{
    configuration
        .MinimumLevel.Debug()
        .WriteTo.GrafanaLoki(
            "http://localhost:3100",
            labels: new[]
            {
                new LokiLabel { Key = "app", Value = "Monitored Microservice Version 1" },
                new LokiLabel { Key = "machine", Value = Environment.MachineName }
            },
            textFormatter: new RenderedCompactJsonFormatter()  // 🔥 This is key!
        );
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

app.UseSerilogRequestLogging(); // logs HTTP request pipeline data

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

var summaries = new[]
{
    "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
};

app.MapGet("/weatherforecast", (bool? fail) =>
{
    try
    {
        if (fail == true)
        {
            // simulate something going horribly wrong
            throw new InvalidOperationException("Simulated failure for testing error logging.");
        }

        var now = DateTime.UtcNow;
        var forecast = Enumerable.Range(1, 5).Select(index =>
            new WeatherForecast(
                DateOnly.FromDateTime(now.AddDays(index)),
                Random.Shared.Next(-20, 55),
                summaries[Random.Shared.Next(summaries.Length)]
            )).ToArray();

        return Results.Ok(forecast);
    }
    catch (Exception ex)
    {
        // Explicitly log the exception with Serilog
        Serilog.Log.Error(ex, "Error generating weather forecast. fail={FailFlag}", fail);

        // Return an HTTP 500 so caller sees failure too
        return Results.Problem(detail: "Simulated failure occurred.", statusCode: 500);
    }
})
.WithName("GetWeatherForecast")
.WithOpenApi();


app.Run();

record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}

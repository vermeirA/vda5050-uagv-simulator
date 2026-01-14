using Serilog;
using VehicleSim.Application.Contracts;
using VehicleSim.Application.Services;
using VehicleSim.Application.Settings;
using VehicleSim.Core.Vehicle.Helpers;
using VehicleSim.Infrastructure.Mqtt;
using VehicleSim.UI;
using VehicleSim.WebHost.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowReactApp",
        policy =>
        {
            policy.SetIsOriginAllowed(origin => origin.StartsWith("http://localhost"))
                .AllowAnyHeader()
                .AllowAnyMethod()
                .AllowCredentials();
        });
});

builder.WebHost.ConfigureKestrel(options =>
{
    options.ListenAnyIP(8080);
});

builder.Services.AddAuthorization();
builder.Services.AddSignalR();

Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}")
    .WriteTo.Seq(builder.Configuration["ProgramSettings:SeqServerUrl"] ?? "http://seq:80")
    .MinimumLevel.Override("Microsoft.AspNetCore", Serilog.Events.LogEventLevel.Warning)
    .CreateLogger();

builder.Host.UseSerilog();

builder.Services.Configure<ProgramSettings>(builder.Configuration.GetSection("ProgramSettings"));
builder.Services.Configure<MqttSettings>(builder.Configuration.GetSection("MqttSettings"));

builder.Services.AddVehicleSimulator();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = $"Vehicle Simulator API", Version = "v1" });
});

var app = builder.Build();


app.UseStaticFiles();
app.UseCors("AllowReactApp");
app.MapHub<SignalRHub>("/signalRHub");
app.UseAuthorization();

var mqttBridge = app.Services.GetRequiredService<IMqttBridge>();
var notificationService = app.Services.GetRequiredService<INotificationService>();

await mqttBridge.InitializeAsync();
await notificationService.InitializeAsync();

var fleetManager = app.Services.GetRequiredService<IFleetManager>();
var fleetConfigs = builder.Configuration.GetSection("Vehicles").Get<List<VehicleRequestContract>>();

if (fleetConfigs?.Any() == true)
{
    foreach (var config in fleetConfigs)
    {
        fleetManager.AddVehicle(config);
    }
}
else
{
    var singleConfig = builder.Configuration.GetSection("Vehicle").Get<VehicleRequestContract>();
    if (singleConfig != null)
    {
        fleetManager.AddVehicle(singleConfig);
    }
}

app.UseSwagger();
app.UseSwaggerUI();

var apiPrefix = builder.Configuration["ProgramSettings:ApiPrefix"] ?? "simulator";
app.MapVehicleEndpoints(apiPrefix);

app.MapFallbackToFile("index.html");
app.Run();

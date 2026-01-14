using MQTTnet;
using VehicleSim.Application;
using VehicleSim.Application.Factories;
using VehicleSim.Application.Helpers;
using VehicleSim.Application.Services;
using VehicleSim.UI;

namespace VehicleSim.WebHost.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddVehicleSimulator(this IServiceCollection services)
    {
        services.AddSingleton<IMqttClient>(_ => new MqttClientFactory().CreateMqttClient());
        services.AddSingleton<IMqttAdapter, MqttAdapter>();
        services.AddSingleton<INotificationService, NotificationService>();

        services.AddSingleton<ITimeProvider, SimulationTime>();
        services.AddSingleton<IVehicleFactory, VehicleFactory>();
        services.AddSingleton<IFleetManager, FleetManager>();

        services.AddSingleton<IMqttBridge, MqttBridge>();
        services.AddSingleton<ApplicationGateway>();
        services.AddSingleton<ISimulationGateway>(sp => sp.GetRequiredService<ApplicationGateway>());
        services.AddSingleton<IVehicleService>(sp => sp.GetRequiredService<ApplicationGateway>());

        services.AddHostedService<SimulationEngine>();

        return services;
    }
}
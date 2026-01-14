using Microsoft.AspNetCore.Mvc;
using VehicleSim.Application;
using VehicleSim.Application.Contracts;
using VehicleSim.Core.VdaModels;

namespace VehicleSim.WebHost.Extensions;

public static class WebApplicationExtensions
{
    public static WebApplication MapVehicleEndpoints(this WebApplication app, string apiPrefix)
    {
        var vehicles = app.MapGroup($"{apiPrefix}/vehicles");
        var simulation = app.MapGroup($"{apiPrefix}/simulation");

        vehicles.MapGet("", (IVehicleService queryService) =>
        {
            try
            {
                return Results.Ok(queryService.GetAllVehicles());
            }
            catch (Exception ex)
            {
                return Results.BadRequest($"Failed to get vehicles: {ex.Message}");
            }
        })
        .WithName("GetAllVehicles")
        .WithDescription("Gets all vehicles in the simulation.");

        vehicles.MapGet("{serialNumber}", (string serialNumber, IVehicleService queryService) =>
        {
            try
            {
                var vehicle = queryService.GetVehicle(serialNumber);
                return vehicle is not null
                    ? Results.Ok(vehicle)
                    : Results.NotFound($"Vehicle {serialNumber} not found.");
            }
            catch (Exception ex)
            {
                return Results.BadRequest($"Failed to get vehicle {serialNumber}: {ex.Message}");
            }
        })
        .WithName("GetVehicle")
        .WithDescription("Gets a vehicle by serial number.");

        vehicles.MapPost("", ([FromBody] VehicleRequestContract config, IVehicleService gateway) =>
        {
            try
            {
                gateway.AddVehicle(config);
                return Results.Created($"{apiPrefix}/vehicles/{config.SerialNumber}", $"Vehicle {config.SerialNumber} added.");
            }
            catch (Exception ex)
            {
                return Results.BadRequest($"Failed to add vehicle {config.SerialNumber}: {ex.Message}");
            }
        })
        .WithName("AddVehicle")
        .WithDescription("Add a new vehicle to the simulation.");

        vehicles.MapDelete("{serialNumber}", (string serialNumber, IVehicleService gateway) =>
        {
            try
            {
                gateway.RemoveVehicle(serialNumber);
                return Results.Ok($"Vehicle {serialNumber} removed.");
            }
            catch (Exception ex)
            {
                return Results.BadRequest($"Failed to remove vehicle {serialNumber}: {ex.Message}");
            }
        })
        .WithName("RemoveVehicle")
        .WithDescription("Remove a vehicle from the simulation.");

        vehicles.MapPost("{serialNumber}/inject-error", (string serialNumber, [FromBody] VdaError error, IVehicleService gateway) =>
        {
            try
            {
                gateway.InjectError(serialNumber, error);
                return Results.Ok($"Error {error.ErrorType} injected into {serialNumber}.");
            }
            catch (Exception ex)
            {
                return Results.BadRequest($"Failed to inject error: {ex.Message}");
            }
        })
        .WithName("InjectError")
        .WithDescription("Inject an error into a specific vehicle.");

        vehicles.MapPost("{serialNumber}/pair", (string serialNumber, IVehicleService gateway) =>
        {
            try
            {
                gateway.PairVehicle(serialNumber);
                return Results.Ok($"Vehicle {serialNumber} paired to MQTT.");
            }
            catch (Exception ex)
            {
                return Results.BadRequest($"Failed to pair vehicle: {ex.Message}");
            }
        })
        .WithName("PairVehicle")
        .WithDescription("Pair an existing vehicle to MQTT/SignalR without adding it to the fleet.");

        vehicles.MapPost("{serialNumber}/unpair", (string serialNumber, IVehicleService gateway) =>
        {
            try
            {
                gateway.UnpairVehicle(serialNumber);
                return Results.Ok($"Vehicle {serialNumber} unpaired from MQTT.");
            }
            catch (Exception ex)
            {
                return Results.BadRequest($"Failed to unpair vehicle: {ex.Message}");
            }
        })
        .WithName("UnpairVehicle")
        .WithDescription("Unpair a vehicle from MQTT/SignalR without removing it from the fleet.");

        vehicles.MapPost("{serialNumber}/reset", (string serialNumber, IVehicleService gateway) =>
        {
            try
            {
                gateway.ResetVehicle(serialNumber);
                return Results.Ok($"Vehicle {serialNumber} reset.");
            }
            catch (Exception ex)
            {
                return Results.BadRequest($"Failed to reset vehicle {serialNumber}: {ex.Message}");
            }
        })
        .WithName("ResetVehicle")
        .WithDescription("Reset a vehicle's errors and state.");

        simulation.MapPost("reset", (ISimulationGateway gateway) =>
        {
            try
            {
                gateway.ResetSimulation();
                return Results.Ok("Simulation reset.");
            }
            catch (Exception ex)
            {
                return Results.BadRequest($"Failed to reset simulation: {ex.Message}");
            }
        })
        .WithName("ResetSimulation")
        .WithDescription("Reset the entire simulation.");

        simulation.MapPut("time-scale/{scale}", (double scale, ISimulationGateway gateway) =>
        {
            try
            {
                gateway.AdjustTimeScale(scale);
                return Results.Ok($"Time scale adjusted to {scale}.");
            }
            catch (Exception ex)
            {
                return Results.BadRequest($"Failed to adjust time scale: {ex.Message}");
            }
        })
        .WithName("AdjustTimeScale")
        .WithDescription("Adjust the simulation time scale.");

        return app;
    }
}
import { useState, useEffect } from "react";
import { ChevronDown, ChevronUp, Plus } from "lucide-react";
import Axios from "axios";
import type {
  Position,
  PositionUpdate,
  VdaError,
  Vehicle,
  VehicleConfig,
} from "../types/types";
import { getConnection } from "../signalR/SignalRConnection.ts";

const VehicleGrid = () => {
  const [vehicles, setVehicles] = useState<Vehicle[]>([]);
  const [loading, setLoading] = useState(true);
  const [expandedRow, setExpandedRow] = useState<string | null>(null);
  const [showAddPopup, setShowAddPopup] = useState(false);
  const [newVehicleX, setNewVehicleX] = useState<string>("");
  const [newVehicleY, setNewVehicleY] = useState<string>("");

  useEffect(() => {
    fetchVehicles();
    const cleanup = registerHandlers();
    return cleanup;
  }, []);

  const fetchVehicles = async () => {
    Axios.get("api/vehicle-simulator/v1/vehicles")
      .then((response) => {
        const data = response.data;
        setVehicles(
          (data as Vehicle[]).sort((a, b) =>
            a.serialNumber.localeCompare(b.serialNumber, undefined, {
              numeric: true,
            })
          )
        );
      })
      .catch((error) => {
        console.error("Error fetching vehicles:", error);
      })
      .finally(() => {
        setLoading(false);
      });
  };

  const registerHandlers = () => {
    const connection = getConnection();

    connection.on("VehicleAdded", (newVehicle: Vehicle) => {
      setVehicles((prevVehicles) => [...prevVehicles, newVehicle]);
    });

    connection.on("VehicleRemoved", (serialNumber: string) => {
      setVehicles((prevVehicles) =>
        prevVehicles.filter((v) => v.serialNumber !== serialNumber)
      );
      console.log(`Vehicle ${serialNumber} removed`);
    });

    connection.on("VehicleStateChanged", (updatedVehicle: Vehicle) => {
      setVehicles((prevVehicles) =>
        prevVehicles.map((v) =>
          v.serialNumber === updatedVehicle.serialNumber ? updatedVehicle : v
        )
      );
    });

    connection.on(
      "VehiclePositionChanged",
      (updatedPosition: PositionUpdate) => {
        setVehicles((prevVehicles) =>
          prevVehicles.map((v) =>
            v.serialNumber === updatedPosition.serialNumber
              ? { ...v, position: updatedPosition }
              : v
          )
        );
      }
    );

    return () => {
      connection.off("VehicleAdded");
      connection.off("VehicleRemoved");
      connection.off("VehicleStateChanged");
      connection.off("VehiclePositionChanged");
    };
  };

  const handleAddVehicle = async () => {
    const x = parseFloat(newVehicleX) || 0;
    const y = parseFloat(newVehicleY) || 0;

    if (isNaN(x) || isNaN(y)) {
      console.error("Invalid position");
      return;
    }
    const vehicleConfig = createVehicleConfig({ x, y });

    Axios.post("api/vehicle-simulator/v1/vehicles/", vehicleConfig)
      .then(() => {
        setShowAddPopup(false);
        setNewVehicleX("");
        setNewVehicleY("");
      })
      .catch((error) => {
        console.error("Error adding vehicle:", error);
      });
  };

  const isValidDecimal = (value: string) => /^\d*\.?\d*$/.test(value);

  const handleRemoveVehicle = async (serialNumber: string) => {
    Axios.delete(`api/vehicle-simulator/v1/vehicles/${serialNumber}`).catch(
      (error) => {
        console.error("Error removing vehicle:", error);
      }
    );
  };

  const handleResetVehicle = async (serialNumber: string) => {
    Axios.post(`api/vehicle-simulator/v1/vehicles/${serialNumber}/reset`).catch(
      (error) => {
        console.error("Error resetting vehicle:", error);
      }
    );
  };

  const handlePairVehicle = async (serialNumber: string) => {
    Axios.post(`api/vehicle-simulator/v1/vehicles/${serialNumber}/pair`).catch(
      (error) => {
        console.error("Error pairing vehicle:", error);
      }
    );
  };

  const handleUnpairVehicle = async (serialNumber: string) => {
    Axios.post(
      `api/vehicle-simulator/v1/vehicles/${serialNumber}/unpair`
    ).catch((error) => {
      console.error("Error unpairing vehicle:", error);
    });
  };

  // For MVP purposes, we inject a predefined error
  const handleInjectError = async (
    serialNumber: string,
    severity: "WARNING" | "FATAL"
  ) => {
    Axios.post(
      `api/vehicle-simulator/v1/vehicles/${serialNumber}/inject-error`,
      severity === "WARNING" ? warningError : fatalError
    ).catch((error) => {
      console.error("Error injecting vehicle error:", error);
    });
  };

  const warningError: VdaError = {
    errorType: "sensorBlocked",
    errorDescription: "Vehicle sensor is blocked.",
    errorLevel: "WARNING",
  };

  const fatalError: VdaError = {
    errorType: "emergencyStop",
    errorDescription: "Vehicle emergency stop engaged.",
    errorLevel: "FATAL",
  };

  // For MVP purposes, we generate a simple vehicle request with incremental serial numbers
  const createVehicleConfig = (position: Position): VehicleConfig => {
    // Find the highest existing serial number and increment it
    const maxSerialNumber = vehicles.reduce((max, vehicle) => {
      const num = parseInt(vehicle.serialNumber, 10);
      return isNaN(num) ? max : Math.max(max, num);
    }, 0);

    const nextSerialNumber = (maxSerialNumber + 1).toString().padStart(3, "0");

    return {
      serialNumber: nextSerialNumber,
      manufacturer: "default",
      mapId: "Warehouse_A",
      startX: position.x,
      startY: position.y,
    };
  };

  const toggleRow = (id: string) => {
    setExpandedRow(expandedRow === id ? null : id);
  };

  const getStatusColor = (status: string) => {
    switch (status.toUpperCase()) {
      case "READY":
        return "text-[#00EA5E]";
      case "EXECUTING":
        return "text-yellow-400";
      case "WARNING":
        return "text-orange-400";
      case "FATAL":
        return "text-red-400";
      case "DISCONNECTED":
        return "text-gray-600";
      default:
        return "text-gray-400";
    }
  };

  if (loading) {
    return (
      <div className="text-white text-center p-8">Loading vehicles...</div>
    );
  }

  return (
    <div className="flex w-full flex-col h-full">
      <div className="flex justify-end items-center mb-4">
        <button
          onClick={() => setShowAddPopup(true)}
          className="flex items-center justify-center w-12 h-12 
            bg-[#00EA5E]/10 hover:bg-[#00EA5E]/20 
            border-2 border-[#00EA5E]/30 hover:border-[#00EA5E]/50
            rounded-xl transition-all duration-200
            active:scale-95"
          aria-label="Add vehicle"
        >
          <Plus className="text-[#00EA5E]" size={26} strokeWidth={2.5} />
        </button>
      </div>

      {showAddPopup && (
        <div className="fixed inset-0 bg-black/50 flex items-center justify-center z-50">
          <div className="bg-[#1a2824] border border-[#455C56] rounded-lg p-6 w-96">
            <h2 className="text-[#00EA5E] text-xl font-medium mb-4">
              Add New Vehicle
            </h2>
            <div className="space-y-4">
              <div>
                <label className="block text-white mb-2">X Position</label>
                <input
                  type="text"
                  value={newVehicleX}
                  onChange={(e) => {
                    const value = e.target.value;
                    if (value === "" || isValidDecimal(value)) {
                      setNewVehicleX(value);
                    }
                  }}
                  className="w-full bg-[#1B2724] text-white border border-[#455C56] rounded px-3 py-2 focus:outline-none focus:border-[#00EA5E]"
                />
              </div>
              <div>
                <label className="block text-white mb-2">Y Position</label>
                <input
                  type="text"
                  value={newVehicleY}
                  onChange={(e) => {
                    const value = e.target.value;
                    if (value === "" || isValidDecimal(value)) {
                      setNewVehicleY(value);
                    }
                  }}
                  className="w-full bg-[#1B2724] text-white border border-[#455C56] rounded px-3 py-2 focus:outline-none focus:border-[#00EA5E]"
                />
              </div>
              <div className="flex gap-3 mt-6">
                <button
                  onClick={() => handleAddVehicle()}
                  className="flex-1 bg-[#00EA5E] text-black font-medium px-4 py-2 rounded-lg hover:bg-[#00c950] transition-colors"
                >
                  Add
                </button>
                <button
                  onClick={() => {
                    setShowAddPopup(false);
                    setNewVehicleX("");
                    setNewVehicleY("");
                  }}
                  className="flex-1 bg-[#1B2724] text-white border border-[#455C56] px-4 py-2 rounded-lg hover:bg-[#2B764F]/20 transition-colors"
                >
                  Cancel
                </button>
              </div>
            </div>
          </div>
        </div>
      )}

      <div className="bg-[#1B2724] rounded-xl">
        <div className="grid grid-cols-5 gap-20 p-6 py-4 bg-[#1a2824] border-b border-[#455C56]/30 text-[#00EA5E] font-medium rounded-t-xl">
          <div>Serial Number</div>
          <div>Mode</div>
          <div>Status</div>
          <div>Position</div>
          <div></div>
        </div>

        <div className="divide-y divide-[#455C56]/30">
          {vehicles.length === 0 ? (
            <div className="px-6 py-4 text-center text-xl text-[#455C56]/90">
              No active vehicles
            </div>
          ) : (
            vehicles.map((vehicle) => (
              <div key={vehicle.serialNumber}>
                <div
                  className="grid grid-cols-5 gap-20 px-6 py-4 text-white hover:bg-[#1a2824]/50 transition-colors"
                  onClick={() => toggleRow(vehicle.serialNumber)}
                >
                  <div className="font-medium text-gray-300">
                    {vehicle.serialNumber}
                  </div>
                  <div className="text-gray-300">{vehicle.operatingMode}</div>
                  <div className={getStatusColor(vehicle.status)}>
                    {vehicle.status}
                  </div>
                  <div className="text-gray-300">{`(${vehicle.position.x}, ${vehicle.position.y})`}</div>
                  <div className="flex justify-end">
                    <button className="text-[#00EA5E] hover:text-[#00c950] transition-colors">
                      {expandedRow === vehicle.serialNumber ? (
                        <ChevronUp size={20} />
                      ) : (
                        <ChevronDown size={20} />
                      )}
                    </button>
                  </div>
                </div>

                {expandedRow === vehicle.serialNumber && (
                  <div className="bg-[#2B764F]/20 px-6 py-4">
                    <div className="flex justify-between">
                      <button
                        className="text-red-400 hover:text-red-300 transition-colors"
                        onClick={() =>
                          handleRemoveVehicle(vehicle.serialNumber)
                        }
                      >
                        Remove
                      </button>
                      <button
                        className="text-[#00EA5E] hover:text-[#00c950] transition-colors"
                        onClick={() => handleResetVehicle(vehicle.serialNumber)}
                      >
                        Reset
                      </button>
                      <button
                        className="text-[#00EA5E] hover:text-[#00c950] transition-colors"
                        onClick={() => handlePairVehicle(vehicle.serialNumber)}
                      >
                        Pair
                      </button>
                      <button
                        className="text-[#00EA5E] hover:text-[#00c950] transition-colors"
                        onClick={() =>
                          handleUnpairVehicle(vehicle.serialNumber)
                        }
                      >
                        Unpair
                      </button>
                      <button
                        className="text-orange-400 hover:text-orange-300 transition-colors"
                        onClick={() =>
                          handleInjectError(vehicle.serialNumber, "WARNING")
                        }
                      >
                        Inject Warning Error
                      </button>
                      <button
                        className="text-red-400 hover:text-red-300 transition-colors"
                        onClick={() =>
                          handleInjectError(vehicle.serialNumber, "FATAL")
                        }
                      >
                        Inject Fatal Error
                      </button>
                    </div>
                  </div>
                )}
              </div>
            ))
          )}
        </div>
      </div>
    </div>
  );
};

export default VehicleGrid;

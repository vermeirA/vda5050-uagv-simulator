import SettingsCard from "../components/SettingsCard";

export const SettingsPage = () => {
  return (
    <div className="flex flex-col gap-4 pt-14">
      <SettingsCard
        title="Timescale"
        description="Sets the simulation's time scale factor. A larger value accelerates all time-dependent processes, causing vehicles to move faster."
        value={1.0}
        icon="check"
        actionUrl="api/vehicle-simulator/v1/simulation/time-scale"
        actionType="PUT"
        storageKey="timescale"
      />
      <SettingsCard
        title="Reset simulation"
        description="Restores all simulation entities to their original starting values and conditions, clearing any progress or state changes."
        icon="reset"
        actionUrl="api/vehicle-simulator/v1/simulation/reset"
        actionType="POST"
      />
    </div>
  );
};

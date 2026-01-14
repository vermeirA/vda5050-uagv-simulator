import "./App.css";
import { Routes, Route } from "react-router";
import Sidebar from "./components/Sidebar";
import { Outlet } from "react-router-dom";
import VehicleGrid from "./components/VehicleGrid";
import { SettingsPage } from "./pages/SettingsPage";
import DocsPage from "./pages/DocsPage";

const PageLayout = ({ title }: { title: string }) => (
  <>
    <div className="h-full w-full flex flex-col mt-6 ml-6 mr-12 overflow-hidden">
      <header className="pl-6 pt-6">
        <h1 className="text-2xl text-[#00EA5E] font-semibold">{title}</h1>
      </header>
      <section className="flex-1 pl-12 pr-4 overflow-hidden mb-12">
        <Outlet />
      </section>
    </div>
  </>
);

function App() {
  return (
    <div className="flex h-screen">
      <Sidebar />
      <main className="flex-1 flex bg-linear-to-br from-[#243430] via-[#121A18] to-black">
        <Routes>
          <Route element={<PageLayout title="Vehicles" />}>
            <Route path="/" element={<VehicleGrid />} />
          </Route>

          <Route element={<PageLayout title="Documentation" />}>
            <Route path="/docs" element={<DocsPage />} />
          </Route>

          <Route element={<PageLayout title="Settings" />}>
            <Route path="/settings" element={<SettingsPage />} />
          </Route>
        </Routes>
      </main>
    </div>
  );
}

export default App;

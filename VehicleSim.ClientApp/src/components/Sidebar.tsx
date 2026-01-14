import { FaRobot } from "react-icons/fa6";
import { IoDocumentText } from "react-icons/io5";
import { HiOutlineExternalLink } from "react-icons/hi";
import { FaCog } from "react-icons/fa";
import { NavLink } from "react-router";

const Sidebar = () => {
  const navLinkClass = ({ isActive }: { isActive: boolean }) =>
    `flex items-center gap-3 px-8 py-4 transition-colors ${
      isActive
        ? "bg-[#2B764F] text-white"
        : "hover:bg-[#2B764F] hover:text-white"
    }`;

  return (
    <div className="flex flex-col h-screen w-40 bg-[#1B2724] text-white drop-shadow-xl">
      <div className="flex-1">
        <div className="flex flex-row items-center justify-center p-6 pt-12 pb-8">
          <FaRobot size={44} color="#00EA5E" className="pb-1" />
          <p className="pl-1 font-bold text-4xl text-white">SIM</p>
        </div>
        <div className="border-b-2 border-[#455C56] pt-2 opacity-50 ml-4 mr-4"></div>

        <nav className="flex flex-col mt-6">
          <NavLink to="/" className={navLinkClass}>
            <FaRobot size={35} />
            <span className="pt-1 font-medium text-base">Vehicles</span>
          </NavLink>

          <NavLink to="/docs" className={navLinkClass}>
            <IoDocumentText size={22} />
            <span className="pt-1 font-medium">Docs</span>
          </NavLink>

          <a
            href="http://localhost:5341"
            target="_blank"
            rel="noopener noreferrer"
            className="flex items-center gap-3 px-8 py-4 pl-8.5 hover:bg-[#2B764F] hover:text-white transition-colors"
          >
            <HiOutlineExternalLink size={22} />
            <span className="pt-1 font-medium">Logs</span>
          </a>
        </nav>
      </div>

      <div>
        <div className="border-t-2 border-[#455C56] mx-4 opacity-50"></div>
        <NavLink to="/settings" className={navLinkClass}>
          <FaCog size={18} />
          <span className=" font-medium">Settings</span>
        </NavLink>
      </div>
    </div>
  );
};

export default Sidebar;

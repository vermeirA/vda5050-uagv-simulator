const DocsPage = () => {
  return (
    <div className="h-full w-full mt-14 text-gray-300 bg-[#1B2724] overflow-y-auto pr-4 p-8 rounded-lg custom-scrollbar">
      <h1 className="text-4xl font-bold text-white mb-4">
        VDA5050 Vehicle Simulator
      </h1>

      <p className="text-lg mb-6">
        A <span className="font-semibold text-white">VDA 5050 compliant</span>{" "}
        simulation engine for testing and validating AGV/AMR fleet management
        systems.
      </p>

      <div className="flex gap-2 mb-8">
        <Badge color="purple" label=".NET" value="10.0" />
        <Badge color="cyan" label="Node.js" value="20+" />
        <Badge color="cyan" label="React" value="19" />
        <Badge color="violet" label="MQTT" value="5.0" />
        <Badge color="green" label="License" value="MIT" />
      </div>

      <Section title="Overview">
        <p>
          This simulator provides a complete testing environment for{" "}
          <span className="text-white font-semibold">VDA 5050</span>-compliant
          vehicle control systems. It simulates Unmanned Autonomous Ground
          Vehicles (uAGVs) communicating via MQTT, allowing developers to test
          fleet management software without requiring physical hardware or
          complex emulation setups.
        </p>
        <p className="mt-4">
          The backend simulation engine is built in .NET (C#), while the
          frontend is developed with React (TypeScript). Communication between
          these is handled through SignalR, a minimal API, and MQTT messaging
          for vehicle interactions.
        </p>
      </Section>

      <Section title="Key Features">
        <Table
          headers={["Feature", "Description"]}
          rows={[
            [
              "VDA 5050 Compliance",
              "Full protocol support for orders, state, connection & visualization",
            ],
            ["Real-time Updates", "SignalR-powered live UI updates"],
            ["Time Scaling", "Adjustable simulation speed (1x - 100x)"],
            ["Multi-Vehicle", "Simulate entire fleets simultaneously"],
            [
              "Error Injection",
              "Test error handling with WARNING/FATAL states",
            ],
            [
              "Pairing/Unpairing",
              "Simulate uAGV's that connect/disconnect via MQTT",
            ],
            ["Dynamic Fleet", "Add/remove vehicles at runtime"],
            [
              "Order/Stitching",
              "Place vda5050 compliant orders (stitching possible)",
            ],
          ]}
        />
      </Section>

      <Section title="Getting Started">
        <h3 className="text-xl font-semibold text-white mb-3">Prerequisites</h3>
        <ul className="list-disc list-inside mb-6 space-y-1">
          <li>
            <Link href="https://docs.docker.com/engine/install/">
              Docker Engine
            </Link>
          </li>
          <li>
            <Link href="https://docs.docker.com/compose/install/">
              Docker Compose
            </Link>
          </li>
        </ul>
        <Note>
          To develop or modify this project, install{" "}
          <Link href="https://dotnet.microsoft.com/en-us/download/dotnet/10.0">
            .NET 10
          </Link>{" "}
          and <Link href="https://nodejs.org/">Node.js 20+</Link>.
        </Note>

        <h3 className="text-xl font-semibold text-white mb-3 mt-6">
          1. Start the Simulator Stack
        </h3>
        <p className="mb-2">From the project root, run:</p>
        <CodeBlock language="bash">docker compose up --build</CodeBlock>
        <p className="mb-4">This will start:</p>
        <ul className="list-disc list-inside mb-6 space-y-1">
          <li>The MQTT broker (Mosquitto)</li>
          <li>The log server (Seq)</li>
          <li>The .NET backend</li>
          <li>The React frontend (as wwwroot)</li>
        </ul>
        <p>
          All services will be available as defined in your{" "}
          <span className="font-mono text-[#00EA5E]">docker-compose.yml</span>.
        </p>

        <h3 className="text-xl font-semibold text-white mb-3 mt-6">
          2. Access the Application
        </h3>
        <ul className="list-disc list-inside mb-6 space-y-1">
          <li>
            <span className="font-semibold text-white">Frontend:</span>{" "}
            <Link href="http://localhost:8080">http://localhost:8080</Link> (or
            the port specified in your compose file)
          </li>
          <li>
            <span className="font-semibold text-white">Backend API:</span>{" "}
            <Link href="http://localhost:8080">http://localhost:8080</Link>{" "}
            (same port, frontend is hosted in wwwroot)
          </li>
          <li>
            <span className="font-semibold text-white">MQTT Broker:</span>{" "}
            <span className="font-mono text-[#00EA5E]">localhost:1883</span>
          </li>
          <li>
            <span className="font-semibold text-white">Seq server:</span>{" "}
            <span className="font-mono text-[#00EA5E]">localhost:5341</span>
          </li>
        </ul>
        <Note>
          Swagger is enabled at{" "}
          <Link href="http://localhost:8080/Swagger/index.html">
            http://localhost:8080/Swagger/index.html
          </Link>
        </Note>

        <h3 className="text-xl font-semibold text-white mb-3 mt-6">
          3. Stopping the Stack
        </h3>
        <p className="mb-2">
          To stop all services, press <kbd>Ctrl+C</kbd> in the terminal, then
          run:
        </p>
        <CodeBlock language="bash">docker compose down</CodeBlock>

        <h4 className="text-lg font-medium text-white mt-8 mb-2">
          MQTT Topics (per uAGV)
        </h4>
        <Table
          headers={["Topic", "Description"]}
          rows={[
            ["uagv/v2/fleet/{SerialNumber}/order", "Incoming orders"],
            ["uagv/v2/fleet/{SerialNumber}/state", "State updates"],
            ["uagv/v2/fleet/{SerialNumber}/connection", "Connection status"],
            ["uagv/v2/fleet/{SerialNumber}/visualization", "Position updates"],
          ]}
        />
      </Section>

      <Section title="Project Structure">
        <CodeBlock language="text">
          {`VehicleSimulator/
├── VehicleSim.Core/            # 0 dependencies - VDA models and Vehicle class
├── VehicleSim.Core.Tests/      # Unit tests for Vehicle class
├── VehicleSim.Application/     # Simulation engine & fleet manager
├── VehicleSim.Infrastructure/  # MQTT contact/entrypoint
├── VehicleSim.UI/              # SignalR notification service
├── VehicleSim.WebHost/         # Program entrypoint
├── VehicleSim.ClientApp/       # React UI project
├── VdaOrders.txt               # VDA5050 compliant test orders
└── appsettings.json            # Settings and initial vehicles config`}
        </CodeBlock>
        <Note>
          Since there's no database, vehicles are instantiated via{" "}
          <code className="bg-[#2a3f39] px-1.5 py-0.5 rounded text-[#00EA5E]">
            appsettings.json
          </code>{" "}
          or dynamically through the UI at runtime.
        </Note>
      </Section>

      <Section title="Features">
        <h3 className="text-xl font-semibold text-white mb-3">Simulation</h3>
        <FeatureList
          items={[
            "VDA5050 compliant vehicle driving simulation",
            "Send orders on /order topic",
            "Receive state updates on /state topic",
            "Receive connection updates on /connection topic",
            "Receive frequent position updates on /visualization",
          ]}
        />

        <h3 className="text-xl font-semibold text-white mb-3 mt-4">
          Fleet Management
        </h3>
        <FeatureList
          items={[
            "Initialize multiple vehicles via appsettings.json (pre-runtime)",
            "Add/remove vehicles via UI (at-runtime)",
            "Set MQTT connection to offline when removing vehicles",
            "Simulate pairing/unpairing vehicles by sending connection messages.",
          ]}
        />

        <h3 className="text-xl font-semibold text-white mb-3 mt-4">
          Testing & Control
        </h3>
        <FeatureList
          items={[
            "Inject Fatal and Warning errors",
            "Publish orders (stitching possible)",
            "Soft reset a vehicle (reset errors, continue path)",
            "Hard reset simulation (reset all vehicles to starting values)",
            "Adjustable simulation timescale for faster/slower driving",
            "Basic order validation",
          ]}
        />
      </Section>

      <Section title="External Tools">
        <h3 className="text-xl font-semibold text-white mb-2">MQTT Explorer</h3>
        <p className="mb-2">
          For visualizing MQTT data flow and publishing orders to a uagv's
          /order topic.
        </p>
        <p className="mb-6">
          🔗 <Link href="https://mqtt-explorer.com/">MQTT_Explorer</Link>
        </p>

        <h3 className="text-xl font-semibold text-white mb-2">
          VDA5050 Visualizer
        </h3>
        <p className="mb-2">
          If the controller you intend to test with this simulator doesn't have
          its own visualization set up, you can use an external visualizer that
          listens on MQTT. Use this to visually validate uagv's behaviour:
        </p>
        <p>
          🔗{" "}
          <Link href="https://github.com/bekirbostanci/vda5050_visualizer">
            vda5050_visualizer
          </Link>
        </p>
        <Note>
          Use the electron variant, websockets for MQTT are not set up.
        </Note>
      </Section>
    </div>
  );
};

const Section = ({
  title,
  children,
}: {
  title: string;
  children: React.ReactNode;
}) => (
  <section className="mb-8">
    <h2 className="text-2xl font-bold text-white mb-4 pb-2 border-b border-[#455C56]">
      {title}
    </h2>
    {children}
  </section>
);

const Badge = ({
  color,
  label,
  value,
}: {
  color: string;
  label: string;
  value: string;
}) => {
  const colors: Record<string, string> = {
    purple: "bg-purple-600",
    cyan: "bg-cyan-600",
    violet: "bg-violet-800",
    green: "bg-green-600",
  };

  return (
    <span
      className={`${colors[color]} text-white text-xs font-medium px-2.5 py-1 rounded`}
    >
      {label} {value}
    </span>
  );
};

const Link = ({
  href,
  children,
}: {
  href: string;
  children: React.ReactNode;
}) => (
  <a
    href={href}
    target="_blank"
    rel="noopener noreferrer"
    className="text-[#00EA5E] hover:underline"
  >
    {children}
  </a>
);

const CodeBlock = ({
  children,
  language,
}: {
  children: string;
  language: string;
}) => (
  <div className="relative mb-4">
    <span className="absolute top-2 right-3 text-xs text-gray-500">
      {language}
    </span>
    <pre className="bg-[#0f1a17] border border-[#2a3f39] rounded-lg p-4 overflow-x-auto">
      <code className="text-sm text-gray-300">{children}</code>
    </pre>
  </div>
);

const Table = ({ headers, rows }: { headers: string[]; rows: string[][] }) => (
  <div className="overflow-x-auto mb-4">
    <table className="w-full border-collapse">
      <thead>
        <tr className="border-b border-[#455C56]">
          {headers.map((header, i) => (
            <th
              key={i}
              className="text-left py-3 px-4 text-white font-semibold"
            >
              {header}
            </th>
          ))}
        </tr>
      </thead>
      <tbody>
        {rows.map((row, i) => (
          <tr key={i} className="border-b border-[#2a3f39]">
            {row.map((cell, j) => (
              <td key={j} className="py-3 px-4">
                {j === 0 ? (
                  <span className="text-white font-semibold">{cell}</span>
                ) : (
                  cell
                )}
              </td>
            ))}
          </tr>
        ))}
      </tbody>
    </table>
  </div>
);

const FeatureList = ({ items }: { items: string[] }) => (
  <ul className="space-y-2 mb-4">
    {items.map((item, i) => (
      <li key={i} className="flex items-start gap-2">
        <span className="text-[#00EA5E] mt-1">✓</span>
        <span>{item}</span>
      </li>
    ))}
  </ul>
);

const Note = ({ children }: { children: React.ReactNode }) => (
  <div className="bg-[#1B2724] border-l-4 border-[#00EA5E] p-4 rounded-r-lg mt-4">
    <p className="text-sm">
      <span className="text-white font-semibold">Note:</span> {children}
    </p>
  </div>
);

export default DocsPage;

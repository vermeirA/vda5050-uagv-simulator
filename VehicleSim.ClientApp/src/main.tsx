import { StrictMode } from 'react'
import { createRoot } from 'react-dom/client'
import './index.css'
import App from './App.tsx'
import { BrowserRouter } from 'react-router';
import { getConnection } from "./signalR/SignalRConnection.ts";

const conn = getConnection();
conn.on("Reload", () => window.location.reload());

createRoot(document.getElementById('root')!).render(
  <StrictMode>
     <BrowserRouter>
      <App />
    </BrowserRouter>
  </StrictMode>,
)

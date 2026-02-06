// pages/Layout.tsx
import { Outlet } from "react-router-dom";
import Navbar from "../components/Navbar";
import Footer from "../components/Footer";

// pages/Layout.tsx
export default function Layout() {
  return (
    <div className="min-vh-100 d-flex flex-column">       {/* added min viewport for all pages */}

      <Navbar />
      <main className="flex-grow-1">       {/* Added flex for body */}
        <Outlet />
      </main>
      <Footer />
    </div>
  );
}

// pages/Layout.tsx
import { Outlet } from "react-router-dom";
import Navbar from "../components/Navbar";
import Footer from "../components/Footer";
import { useEffect, useState } from "react";
import type User from "../interfaces/Users";
import AiChatWidget from "./AiChatWidget";

// pages/Layout.tsx
export default function Layout() {

  const [user, setUser] = useState<User | null>(null);

  useEffect(() => {
    fetch('/api/login', { credentials: 'include' })
      .then(res => res.json())
      .then(data => {
        if (!data.error) {
          setUser(data);
        }
      });
  }, []);

  return (
    <div className="min-vh-100 d-flex flex-column">       {/* added min viewport for all pages */}

      <Navbar user={user} setUser={setUser} />
      <main className="flex-grow-1">       {/* Added flex for body */}
        <Outlet context={{ setUser }} />
      </main>
      <AiChatWidget />

      <Footer />
    </div>
  );
}

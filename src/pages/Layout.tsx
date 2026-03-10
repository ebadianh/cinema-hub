// pages/Layout.tsx
import { Outlet } from "react-router-dom";
import Navbar from "../components/Navbar";
import Footer from "../components/Footer";
import { useEffect, useState } from "react";
import type User from "../interfaces/Users";
import AiChatWidget from "./AiChatWidget";
import CookieConsentBanner from "../components/CookieConsentBanner";
import ScrollToTop from "../components/ScrollToTop";

// pages/Layout.tsx
export default function Layout() {
  const [user, setUser] = useState<User | null>(null);

  useEffect(() => {
    fetch("/api/login", { credentials: "include" })
      .then((res) => res.json())
      .then((data) => {
        if (!data.error) {
          setUser(data);
        }
      });
  }, []);

  return (
    <div className="min-vh-100 d-flex flex-column">
    <div className="min-vh-100 d-flex flex-column">       {/* added min viewport for all pages */}

      <ScrollToTop />

      <Navbar user={user} setUser={setUser} />
      <main className="flex-grow-1">
        <Outlet context={{ user, setUser }} />
      </main>
      <AiChatWidget />
      <CookieConsentBanner />
      <Footer />
    </div>
  );
}

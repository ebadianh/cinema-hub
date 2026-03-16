import { useState } from "react";
import AdminFilms from "../../pages/AdminFilms";
import ContactList from "../../components/Admin/ContactList";

type Tab = "films" | "messages";

export default function AdminPanel() {
  const [activeTab, setActiveTab] = useState<Tab>("films");

  return (
    <div className="container-fluid py-4">
      <div className="row mb-4">
        <div className="col-12">
          <h2 className="text-light mb-3">Admin Panel</h2>

          {/* Navigation Tabs */}
          <ul className="nav nav-tabs">
            <li className="nav-item">
              <button
                className={`nav-link ${activeTab === "films" ? "active" : ""}`}
                onClick={() => setActiveTab("films")}
                style={{
                  backgroundColor: activeTab === "films" ? "var(--primary-color)" : "transparent",
                  color: activeTab === "films" ? "#fff" : "#aaa",
                  border: "none",
                }}
              >
                Filmer
              </button>
            </li>
            <li className="nav-item">
              <button
                className={`nav-link ${activeTab === "messages" ? "active" : ""}`}
                onClick={() => setActiveTab("messages")}
                style={{
                  backgroundColor: activeTab === "messages" ? "var(--primary-color)" : "transparent",
                  color: activeTab === "messages" ? "#fff" : "#aaa",
                  border: "none",
                }}
              >
                Meddelanden
              </button>
            </li>
          </ul>
        </div>
      </div>

      {/* Content */}
      <div className="row">
        <div className="col-12">
          {activeTab === "films" && <AdminFilms />}
          {activeTab === "messages" && <ContactList />}
        </div>
      </div>
    </div>
  );
}
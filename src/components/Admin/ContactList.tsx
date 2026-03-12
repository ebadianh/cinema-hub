import { useEffect, useState } from "react";

type Contact = {
  id: number;
  name: string;
  email: string;
  subject: string;
  message: string;
  status: "unread" | "read";
  created_at: string;
};

export default function ContactList() {
  const [contacts, setContacts] = useState<Contact[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [searchTerm, setSearchTerm] = useState("");
  const [filterStatus, setFilterStatus] = useState<"all" | "unread" | "read">("all");
  const [expandedId, setExpandedId] = useState<number | null>(null);

  // Hämta alla meddelande

  useEffect(() => {
    const fetchContacts = async () => {
      try {
        setLoading(true);
        setError(null);

        const res = await fetch("/api/contacts");
        if (!res.ok) throw new Error("Kunde inte hämta meddelande");

        const data = await res.json();
        const contactList: Contact[] = Array.isArray(data) ? data : data.contacts ?? [];

        // Sorterar nyaste meddelande först
        contactList.sort((a, b) =>
          new Date(b.created_at).getTime() - new Date(a.created_at).getTime()
        );
        setContacts(contactList);
      } catch (err) {
        setError(err instanceof Error ? err.message : "Ett fel uppstod");
      } finally {
        setLoading(false);
      }
    };

    fetchContacts();
  }, []);

  // Markera som läst/oläst
  const toggleStatus = async (id: number, currentStatus: string) => {
    try {
      const newStatus = currentStatus === "read" ? "unread" : "read";

      const res = await fetch(`/api/contacts/${id}`, {
        method: "PUT",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ status: newStatus }),
      });

      if (!res.ok) throw new Error("Kunde inte uppdatera status");

      // Uppdatera i state
      setContacts(prev =>
        prev.map(c => (c.id === id ? { ...c, status: newStatus as "read" | "unread" } : c))
      );
    } catch (err) {
      alert("Kunde inte uppdatera status");
    }
  };

  // Filtrera meddelande
  const filteredContacts = contacts.filter(contact => {
    const matchesSearch =
      contact.name.toLowerCase().includes(searchTerm.toLowerCase()) ||
      contact.email.toLowerCase().includes(searchTerm.toLowerCase()) ||
      contact.subject.toLowerCase().includes(searchTerm.toLowerCase());

    const matchesStatus =
      filterStatus === "all" || contact.status === filterStatus;

    return matchesSearch && matchesStatus;
  });

  // Formatera datum
  const formatDate = (dateString: string) => {
    const date = new Date(dateString);
    return date.toLocaleDateString("sv-SE", {
      year: "numeric",
      month: "short",
      day: "numeric",
      hour: "2-digit",
      minute: "2-digit",
    });
  };

  if (loading) return <div className="text-center py-5">Laddar meddelande...</div>;
  if (error) return <div className="alert alert-danger">{error}</div>;

  return (
    <div className="container-fluid py-4">
      <div className="card bg-dark text-light border-0 shadow">
        <div className="card-body">
          <h3 className="mb-4">Kontaktmeddelande</h3>

          {/* Sök & Filter */}
          <div className="row mb-4 g-3">
            <div className="col-md-6">
              <input
                type="text"
                className="form-control"
                placeholder="Sök namn, email eller ämne..."
                value={searchTerm}
                onChange={(e) => setSearchTerm(e.target.value)}
              />
            </div>
            <div className="col-md-6">
              <div className="btn-group w-100" role="group">
                <button
                  type="button"
                  className={`btn ${filterStatus === "all" ? "btn-primary" : "btn-outline-secondary"}`}
                  onClick={() => setFilterStatus("all")}
                >
                  Alla ({contacts.length})
                </button>
                <button
                  type="button"
                  className={`btn ${filterStatus === "unread" ? "btn-primary" : "btn-outline-secondary"}`}
                  onClick={() => setFilterStatus("unread")}
                >
                  Olästa ({contacts.filter(c => c.status === "unread").length})
                </button>
                <button
                  type="button"
                  className={`btn ${filterStatus === "read" ? "btn-primary" : "btn-outline-secondary"}`}
                  onClick={() => setFilterStatus("read")}
                >
                  Lästa ({contacts.filter(c => c.status === "read").length})
                </button>
              </div>
            </div>
          </div>

          {/* Tabell */}
          {filteredContacts.length === 0 ? (
            <p className="text-muted text-center py-5">Inga meddelanden hittades</p>
          ) : (
            <div className="table-responsive">
              <table className="table table-dark table-hover">
                <thead>
                  <tr>
                    <th>Status</th>
                    <th>Namn</th>
                    <th>Email</th>
                    <th>Ämne</th>
                    <th>Datum</th>
                    <th>Åtgärder</th>
                  </tr>
                </thead>
                <tbody>
                  {filteredContacts.map((contact) => (
                    <>
                      <tr
                        key={contact.id}
                        onClick={() => setExpandedId(expandedId === contact.id ? null : contact.id)}
                        style={{ cursor: "pointer" }}
                      >
                        <td>
                          <span
                            className={`badge ${contact.status === "unread" ? "bg-danger" : "bg-success"
                              }`}
                          >
                            {contact.status === "unread" ? "Oläst" : "Läst"}
                          </span>
                        </td>
                        <td>{contact.name}</td>
                        <td>{contact.email}</td>
                        <td>{contact.subject}</td>
                        <td>{formatDate(contact.created_at)}</td>
                        <td>
                          <button
                            className="btn btn-sm btn-outline-light me-2"
                            onClick={(e) => {
                              e.stopPropagation();
                              toggleStatus(contact.id, contact.status);
                            }}
                          >
                            {contact.status === "unread" ? "Markera läst" : "Markera oläst"}
                          </button>

                          <a
                            href={`mailto:${contact.email}?subject=Re: ${contact.subject}`}
                            className="btn btn-sm btn-primary"
                            onClick={(e) => e.stopPropagation()}
                            target="_blank"
                            rel="noopener noreferrer"
                          >
                            Svara
                          </a>
                        </td>
                      </tr>

                      {expandedId === contact.id && (
                        <tr>
                          <td colSpan={6} className="bg-secondary">
                            <div className="p-3">
                              <h5>Meddelande:</h5>
                              <p className="mb-0" style={{ whiteSpace: "pre-wrap" }}>
                                {contact.message}
                              </p>
                            </div>
                          </td>
                        </tr>
                      )}
                    </>
                  ))}
                </tbody>
              </table>
            </div>
          )}
        </div>
      </div>
    </div >
  );
}
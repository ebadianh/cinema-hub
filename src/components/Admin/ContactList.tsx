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

});
}
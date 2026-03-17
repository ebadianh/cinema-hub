import { useEffect, useState } from "react";
import { Button, Offcanvas } from "react-bootstrap";
import AiChat from "../components/AiChat";

export interface Message {
  role: "user" | "assistant";
  content: string;
}

const STORAGE_KEY = "cinemahub_ai_chat_v1";
const MAX_MESSAGES_TO_STORE = 200;

function isValidMessages(data: unknown): data is Message[] {
  return (
    Array.isArray(data) &&
    data.every(
      (chatMessage: any) =>
        chatMessage &&
        (chatMessage.role === "user" || chatMessage.role === "assistant") &&
        typeof chatMessage.content === "string",
    )
  );
}

export default function AiChatWidget() {
  const [open, setOpen] = useState(false);

  // ✅ Widgeten äger messages (persistar även när AiChat unmountas)
  const [messages, setMessages] = useState<Message[]>([]);
  const [hasLoaded, setHasLoaded] = useState(false);

  // 1) Load from storage när appen startar
  useEffect(() => {
    try {
      const raw = localStorage.getItem(STORAGE_KEY);
      if (raw) {
        const parsed = JSON.parse(raw);
        if (isValidMessages(parsed)) setMessages(parsed);
      }
    } catch {
      // ignore
    } finally {
      setHasLoaded(true);
    }
  }, []);

  // 2) Save to storage när messages ändras (efter load)
  useEffect(() => {
    if (!hasLoaded) return;
    try {
      const trimmed = messages.slice(-MAX_MESSAGES_TO_STORE);
      localStorage.setItem(STORAGE_KEY, JSON.stringify(trimmed));
    } catch {
      // ignore
    }
  }, [messages, hasLoaded]);

  // 3) För att trigga öppning med ny knapp
  useEffect(() => {
    const openChat = () => setOpen(true);

    window.addEventListener("open-ai-chat", openChat);

    return () => {
      window.removeEventListener("open-ai-chat", openChat);
    };
  }, []);

  const clearChat = () => {
    setMessages([]);
    try {
      localStorage.removeItem(STORAGE_KEY);
    } catch {}
  };

  return (
    <>
      <Button
        variant="dark"
        className="ai-chat__launcher"
        onClick={() => setOpen(true)}
        style={{
          position: "fixed",
          right: 18,
          bottom: 18,
          zIndex: 1050,
          borderRadius: 999,
          padding: "10px 14px",
        }}
      >
        Chat
      </Button>

      <Offcanvas
        show={open}
        onHide={() => setOpen(false)}
        placement="end"
        style={{ width: 420 }}
      >
        <Offcanvas.Header closeButton>
          <Offcanvas.Title>AI Assistent</Offcanvas.Title>
        </Offcanvas.Header>

        <Offcanvas.Body>
          {/* AiChat kan unmountas – historiken ligger ändå kvar i widgetens state + localStorage */}
          {open ? (
            <AiChat
              messages={messages}
              setMessages={setMessages}
              onClear={clearChat}
            />
          ) : null}
        </Offcanvas.Body>
      </Offcanvas>
    </>
  );
}

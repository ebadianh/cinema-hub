import { useEffect, useLayoutEffect, useRef, useState } from 'react';
import { Card, Form, Button, Spinner } from 'react-bootstrap';

interface Message {
  role: 'user' | 'assistant' | 'system';
  content: string;
}

interface ChatResponse {
  choices: Array<{
    message: { content: string; role: string };
  }>;
}

export default function AiChat() {
  const [messages, setMessages] = useState<Message[]>([]);
  const [input, setInput] = useState('');
  const [isLoading, setIsLoading] = useState(false);

  const bodyRef = useRef<HTMLDivElement>(null);
  const textareaRef = useRef<HTMLTextAreaElement>(null);

  // Auto-resize textarea
  useEffect(() => {
    const el = textareaRef.current;
    if (!el) return;
    el.style.height = 'auto';
    el.style.height = Math.min(el.scrollHeight, 160) + 'px';
  }, [input]);

  // Scroll inside the chat body (NOT the page)
  useLayoutEffect(() => {
    const el = bodyRef.current;
    if (!el) return;
    // schedule after render to avoid “almost bottom”
    requestAnimationFrame(() => {
      el.scrollTop = el.scrollHeight;
    });
  }, [messages, isLoading]);

  const sendMessage = async () => {
    const text = input.trim();
    if (!text || isLoading) return;

    const userMessage: Message = { role: 'user', content: text };

    // build next list immediately (avoid stale closure)
    const nextMessages = [...messages, userMessage];

    setMessages(nextMessages);
    setInput('');
    setIsLoading(true);

    try {
      const response = await fetch('/api/chat', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ messages: nextMessages })
      });

      if (!response.ok) {
        const err = await response.json().catch(() => ({}));
        throw new Error(err.error || 'Request failed');
      }

      const data: ChatResponse = await response.json();
      const assistantMessage: Message = {
        role: 'assistant',
        content: data.choices?.[0]?.message?.content ?? '(No response)'
      };

      setMessages(prev => [...prev, assistantMessage]);
    } catch (error) {
      setMessages(prev => [
        ...prev,
        {
          role: 'assistant',
          content: `Error: ${error instanceof Error ? error.message : 'Unknown error'}`
        }
      ]);
    } finally {
      setIsLoading(false);
    }
  };

  const handleKeyDown = (e: React.KeyboardEvent<HTMLTextAreaElement>) => {
    if (e.key === 'Enter' && !e.shiftKey) {
      e.preventDefault();
      sendMessage();
    }
  };

  return (
    <Card className="ai-chat shadow-sm">
      <Card.Header className="ai-chat__header">
        <div className="d-flex align-items-center justify-content-between">
          <div className="fw-semibold">AI Chat</div>
          {isLoading && (
            <div className="ai-chat__status">
              <Spinner animation="border" size="sm" className="me-2" />
              Thinking…
            </div>
          )}
        </div>
      </Card.Header>

      <div ref={bodyRef} className="ai-chat__body">
        {messages.length === 0 ? (
          <div className="ai-chat__empty">
            Ask about movies, recommendations, plot questions, etc.
          </div>
        ) : (
          messages.map((m, i) => (
            <div
              key={i}
              className={`ai-chat__bubble ${m.role === 'user' ? 'is-user' : 'is-assistant'}`}
            >
              {m.content}
            </div>
          ))
        )}
      </div>

      <Card.Footer className="ai-chat__footer" style={{backgroundColor:'#060645'}}>
        <div className="ai-chat__inputRow">
          <Form.Control
            as="textarea"
            ref={textareaRef}
            value={input}
            onChange={(e) => setInput(e.target.value)}
            onKeyDown={handleKeyDown}
            placeholder="Type your message…"
            rows={1}
            disabled={isLoading}
            className="ai-chat__textarea"
          />
          <Button
            variant="primary"
            onClick={sendMessage}
            disabled={!input.trim() || isLoading}
            className="ai-chat__send"
          >
            Send
          </Button>
        </div>
        <div className="ai-chat__hint" style={{color:'white'}}>Enter to send • Shift+Enter for new line</div>
      </Card.Footer>
    </Card>
  );
}

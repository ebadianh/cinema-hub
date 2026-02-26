import "../ai-chat.css";

import { useEffect, useLayoutEffect, useRef, useState } from 'react';
import { Card, Form, Button, Spinner } from 'react-bootstrap';
import ReactMarkdown from 'react-markdown';
import remarkGfm from 'remark-gfm';
import rehypeSanitize from 'rehype-sanitize';
import type { PluggableList } from 'unified';
import type { Message } from '../pages/AiChatWidget';

interface ChatResponse {
  choices: Array<{
    message: { content: string; role: string };
  }>;
}

type Props = {
  messages: Message[];
  setMessages: React.Dispatch<React.SetStateAction<Message[]>>;
  onClear: () => void;
};

const remarkPlugins: PluggableList = [remarkGfm];
const rehypePlugins: PluggableList = [rehypeSanitize];

export default function AiChat({ messages, setMessages, onClear }: Props) {
  const [input, setInput] = useState('');
  const [isLoading, setIsLoading] = useState(false);

  const bodyRef = useRef<HTMLDivElement>(null);
  const textareaRef = useRef<HTMLTextAreaElement>(null);

  // Scroll behavior
  const stickToBottomRef = useRef(true);

  // Auto-resize textarea
  useEffect(() => {
    const el = textareaRef.current;
    if (!el) return;
    el.style.height = 'auto';
    el.style.height = Math.min(el.scrollHeight, 160) + 'px';
  }, [input]);

  const onBodyScroll = () => {
    const el = bodyRef.current;
    if (!el) return;
    const distanceFromBottom = el.scrollHeight - el.scrollTop - el.clientHeight;
    stickToBottomRef.current = distanceFromBottom < 120;
  };

  useLayoutEffect(() => {
    const el = bodyRef.current;
    if (!el) return;
    if (!stickToBottomRef.current) return;

    requestAnimationFrame(() => {
      el.scrollTop = el.scrollHeight;
    });
  }, [messages, isLoading]);

  const sendMessage = async () => {
    const text = input.trim();
    if (!text || isLoading) return;

    const userMessage: Message = { role: 'user', content: text };
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
          content: `Fel: ${error instanceof Error ? error.message : 'Okänt fel'}`
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
          <div className="fw-semibold">Cinema-Bot</div>

          <div className="d-flex align-items-center gap-2">
            {isLoading && (
              <div className="ai-chat__status">
                <Spinner animation="border" size="sm" className="me-2" />
                Tänker…
              </div>
            )}

            <Button variant="outline-light" size="sm" onClick={onClear}>
              Rensa
            </Button>
          </div>
        </div>
      </Card.Header>

      <div ref={bodyRef} className="ai-chat__body" onScroll={onBodyScroll}>
        {messages.length === 0 ? (
          <div className="ai-chat__empty">
            Fråga om öppettider, priser, snacks, bokning eller vilka filmer som går.
          </div>
        ) : (
          messages.map((m, i) => (
            <div
              key={i}
              className={`ai-chat__bubble ${m.role === 'user' ? 'is-user' : 'is-assistant'}`}
            >
              {m.role === 'assistant' ? (
                <ReactMarkdown
                  remarkPlugins={remarkPlugins}
                  rehypePlugins={rehypePlugins}
                  components={{
                    a: ({ node, ...props }) => (
                      <a {...props} target="_blank" rel="noreferrer noopener" />
                    )
                  }}
                >
                  {m.content}
                </ReactMarkdown>
              ) : (
                m.content
              )}
            </div>
          ))
        )}
      </div>

      <Card.Footer className="ai-chat__footer">
        <div className="ai-chat__inputRow">
          <Form.Control
            as="textarea"
            ref={textareaRef}
            value={input}
            onChange={(e) => setInput(e.target.value)}
            onKeyDown={handleKeyDown}
            placeholder="Skriv ditt meddelande…"
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
            Skicka
          </Button>
        </div>

        <div className="ai-chat__hint" style={{color: 'white'}}>
          Enter för att skicka • Shift+Enter för ny rad
        </div>
      </Card.Footer>
    </Card>
  );
}
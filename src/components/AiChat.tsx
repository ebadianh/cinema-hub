import { useState, useEffect, useRef } from 'react';
import { Card, Form, Button, Spinner, Container, Row, Col } from 'react-bootstrap';

interface Message {
  role: 'user' | 'assistant' | 'system';
  content: string;
}

interface ChatResponse {
  choices: Array<{
    message: {
      content: string;
      role: string;
    };
  }>;
}

export default function AiChat() {
  const [messages, setMessages] = useState<Message[]>([]);
  const [input, setInput] = useState('');
  const [isLoading, setIsLoading] = useState(false);
  const messagesEndRef = useRef<HTMLDivElement>(null);
  const textareaRef = useRef<HTMLTextAreaElement>(null);

  // Auto-scroll to bottom when messages change
  useEffect(() => {
    messagesEndRef.current?.scrollIntoView({ behavior: 'smooth' });
  }, [messages]);

  // Auto-resize textarea
  useEffect(() => {
    if (textareaRef.current) {
      textareaRef.current.style.height = 'auto';
      textareaRef.current.style.height = Math.min(textareaRef.current.scrollHeight, 200) + 'px';
    }
  }, [input]);

  const sendMessage = async () => {
    const text = input.trim();
    if (!text || isLoading) return;

    // Add user message to the UI
    const userMessage: Message = { role: 'user', content: text };
    setMessages(prev => [...prev, userMessage]);
    setInput('');
    setIsLoading(true);

    try {
      const response = await fetch('/api/chat', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ messages: [...messages, userMessage] })
      });

      if (!response.ok) {
        const err = await response.json();
        throw new Error(err.error || 'Request failed');
      }

      const data: ChatResponse = await response.json();
      const assistantMessage: Message = {
        role: 'assistant',
        content: data.choices[0].message.content
      };

      setMessages(prev => [...prev, assistantMessage]);
    } catch (error) {
      const errorMessage: Message = {
        role: 'assistant',
        content: `Error: ${error instanceof Error ? error.message : 'Unknown error'}`
      };
      setMessages(prev => [...prev, errorMessage]);
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
    <Card className="shadow-sm ai-chat">
      <Card.Header className="bg-primary text-white">
        <h5 className="mb-0">AI Chat</h5>
      </Card.Header>

      <Card.Body className="ai-chat-body">
        <Container fluid>
          {messages.map((message, index) => (
            <Row key={index} className="mb-3">
              <Col xs={12}>
                <div
                  className={`p-3 rounded ai-chat-message ${
                    message.role === 'user'
                      ? 'bg-primary text-white user'
                      : 'bg-light text-dark assistant'
                  }`}
                >
                  {message.content}
                </div>
              </Col>
            </Row>
          ))}
          <div ref={messagesEndRef} />
        </Container>
      </Card.Body>

      <Card.Footer className="bg-white ai-chat-footer">
        {isLoading && (
          <div className="text-muted mb-2 small ai-chat-status">
            <Spinner animation="border" size="sm" className="me-2" />
            Thinking...
          </div>
        )}
        <div className="ai-chat-input-area">
          <Form.Control
            as="textarea"
            ref={textareaRef}
            value={input}
            onChange={(e) => setInput(e.target.value)}
            onKeyDown={handleKeyDown}
            placeholder="Type your message..."
            rows={1}
          />
          <Button
            variant="primary"
            onClick={sendMessage}
            disabled={!input.trim() || isLoading}
          >
            Send
          </Button>
        </div>
      </Card.Footer>
    </Card>
  );
}


import { useState } from 'react';
import { Button, Offcanvas } from 'react-bootstrap';
import AiChat from './AiChatPage';

export default function AiChatWidget() {
  const [open, setOpen] = useState(false);

  return (
    <>
      <Button
        variant="primary"
        onClick={() => setOpen(true)}
        style={{
          position: 'fixed',
          right: 18,
          bottom: 18,
          zIndex: 1050,
          borderRadius: 999,
          padding: '10px 14px'
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
          <Offcanvas.Title>AI Assistant</Offcanvas.Title>
        </Offcanvas.Header>
        <Offcanvas.Body>
          <AiChat />
        </Offcanvas.Body>
      </Offcanvas>
    </>
  );
}

import { Container, Row, Col, Form, Button, Card } from 'react-bootstrap';

export default function SignIn() {

    return <>
        <Container className="d-flex align-items-center justify-content-center" style={{ minHeight: '100vh' }}>
            <Row className="w-100">
                <Col md={{ span: 6, offset: 3 }} lg={{ span: 6, offset: 3 }}>
                    <Card className="p-4 shadow">
                        <Card.Body>
                            <h1>Logga in</h1>
                            <Form>
                                <Form.Group className="mb-3" controlId="formBasicEmail">
                                    <Form.Label>E-post adress</Form.Label>
                                    <Form.Control type="email" placeholder="Skriv in din e-post adress" />
                                </Form.Group>
                                <Form.Group className="mb-3" controlId="formBasicPassword">
                                    <Form.Label>Lösenord</Form.Label>
                                    <Form.Control type="password" placeholder="Lösenord" />
                                </Form.Group>
                                <div className="d-grid gap-2">
                                    <Button variant="primary" size="lg" type="submit">Logga in</Button>
                                </div>
                            </Form>
                        </Card.Body>
                    </Card>
                </Col>
            </Row>
        </Container>
    </>
};


import { useState } from 'react';
import { useNavigate, Link } from 'react-router-dom';
import { Container, Row, Col, Form, Button, Card, Alert } from 'react-bootstrap';

export default function Register() {
    const [firstName, setFirstName] = useState('');
    const [lastName, setLastName] = useState('');
    const [email, setEmail] = useState('');
    const [password, setPassword] = useState('');
    const [confirmPassword, setConfirmPassword] = useState('');
    const [error, setError] = useState<string | null>(null);
    const [loading, setLoading] = useState(false);

    const navigate = useNavigate();

    const handleRegister = async (e: React.FormEvent) => {
        e.preventDefault();
        setError(null);

        if (password !== confirmPassword) {
            setError('Lösenorden matchar inte');
            return;
        }

        if (password.length < 6) {
            setError('Lösenordet måste vara minst 6 tecken');
            return;
        }

        setLoading(true);

        try {
            const res = await fetch('/api/users', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                },
                body: JSON.stringify({
                    email,
                    password,
                    firstName,
                    lastName
                }),
                credentials: 'include',
            });

            if (!res.ok) {
                if (res.status === 400) {
                    throw new Error('Ogiltiga uppgifter');
                }
                throw new Error(`Registrering misslyckades (${res.status})`);
            }

            navigate('/signin');
        } catch (err: any) {
            setError(err.message ?? 'Ett fel uppstod vid registrering');
        } finally {
            setLoading(false);
        }
    };

    return (
        <Container className="d-flex align-items-center justify-content-center" style={{ minHeight: '50vh' }}>
            <Row className="w-100">
                <Col md={{ span: 6, offset: 3 }} lg={{ span: 6, offset: 3 }}>
                    <Card className="p-4 shadow">
                        <Card.Body>
                            <h1>Skapa konto</h1>

                            {error && (
                                <Alert variant="danger" onClose={() => setError(null)} dismissible>
                                    {error}
                                </Alert>
                            )}

                            <Form onSubmit={handleRegister}>
                                <Row>
                                    <Col>
                                        <Form.Group className="mb-3" controlId="formFirstName">
                                            <Form.Label>Förnamn</Form.Label>
                                            <Form.Control
                                                type="text"
                                                placeholder="Förnamn"
                                                value={firstName}
                                                onChange={(e) => setFirstName(e.target.value)}
                                                required
                                                disabled={loading}
                                            />
                                        </Form.Group>
                                    </Col>
                                    <Col>
                                        <Form.Group className="mb-3" controlId="formLastName">
                                            <Form.Label>Efternamn</Form.Label>
                                            <Form.Control
                                                type="text"
                                                placeholder="Efternamn"
                                                value={lastName}
                                                onChange={(e) => setLastName(e.target.value)}
                                                required
                                                disabled={loading}
                                            />
                                        </Form.Group>
                                    </Col>
                                </Row>

                                <Form.Group className="mb-3" controlId="formEmail">
                                    <Form.Label>E-post adress</Form.Label>
                                    <Form.Control
                                        type="email"
                                        placeholder="Skriv in din e-post adress"
                                        value={email}
                                        onChange={(e) => setEmail(e.target.value)}
                                        required
                                        disabled={loading}
                                    />
                                </Form.Group>

                                <Form.Group className="mb-3" controlId="formPassword">
                                    <Form.Label>Lösenord</Form.Label>
                                    <Form.Control
                                        type="password"
                                        placeholder="Välj ett lösenord"
                                        value={password}
                                        onChange={(e) => setPassword(e.target.value)}
                                        required
                                        disabled={loading}
                                    />
                                </Form.Group>

                                <Form.Group className="mb-3" controlId="formConfirmPassword">
                                    <Form.Label>Bekräfta lösenord</Form.Label>
                                    <Form.Control
                                        type="password"
                                        placeholder="Skriv lösenordet igen"
                                        value={confirmPassword}
                                        onChange={(e) => setConfirmPassword(e.target.value)}
                                        required
                                        disabled={loading}
                                    />
                                </Form.Group>

                                <div className="d-grid gap-2">
                                    <Button
                                        variant="primary"
                                        size="lg"
                                        type="submit"
                                        disabled={loading}
                                    >
                                        {loading ? 'Skapar konto...' : 'Registrera'}
                                    </Button>
                                </div>
                            </Form>

                            <div className="text-center mt-3">
                                <span className="text-muted">Har du redan ett konto? </span>
                                <Link to="/signin">Logga in</Link>
                            </div>
                        </Card.Body>
                    </Card>
                </Col>
            </Row>
        </Container>
    );
}

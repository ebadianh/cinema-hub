import { useState, useEffect } from 'react';

import { useParams, useOutletContext } from 'react-router-dom';

import type User from '../interfaces/Users';

import { Container, Row, Col, Card, Alert } from 'react-bootstrap';
 
interface OutletContextType {

    setUser: (user: User | null) => void;

}
 
export default function Profile() {

    const { id } = useParams<{ id: string }>();

    const { setUser } = useOutletContext<OutletContextType>();

    const [profile, setProfile] = useState<User | null>(null);

    const [loggedInUser, setLoggedInUser] = useState<User | null>(null);
 
    useEffect(() => {

        fetch('/api/login', { credentials: 'include' })

            .then(res => res.json())

            .then(data => {

                if (!data.error) setLoggedInUser(data);

            });

    }, []);
 
    useEffect(() => {

        if (!id) return;

        (async () => {

            const res = await fetch(`/api/users/${id}`);

            const data = await res.json();

            setProfile(data);

        })();

    }, [id]);
 
    const isOwnProfile = loggedInUser?.id === parseInt(id || '0');

    const isAdmin = loggedInUser?.role === 'admin';

    const canEdit = isOwnProfile || isAdmin;
 
    if (!profile) return null;
 
    return (
<Container className="mt-5">
<Row>
<Col md={{ span: 8, offset: 2 }}>
<Card className="shadow">
<Card.Body>
<div className="d-flex justify-content-between align-items-start mb-4">
<div>
<h2>{profile.firstName} {profile.lastName}</h2>
<p className="text-muted mb-0">{profile.email}</p>
</div>

                                {profile.role === 'admin' && (
<span className="badge bg-danger">Admin</span>

                                )}
</div>
 
                            {isOwnProfile && (
<Alert variant="info">Detta är din profil</Alert>

                            )}
 
                            {isAdmin && !isOwnProfile && (
<Card className="mt-4 border-danger">
<Card.Header className="bg-danger text-white">Admin-verktyg</Card.Header>
<Card.Body>
<p className="text-muted">Här kan admin-funktioner läggas till i framtiden.</p>
</Card.Body>
</Card>

                            )}
 
                            {canEdit && (
<Card className="mt-4">
<Card.Header>Redigera profil</Card.Header>
<Card.Body>
<p className="text-muted">Redigeringsfunktionalitet kan läggas till här.</p>
</Card.Body>
</Card>

                            )}
</Card.Body>
</Card>
</Col>
</Row>
</Container>

    );

}
 
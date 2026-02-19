import { useState, useEffect } from 'react';
import { useParams, useOutletContext, useNavigate } from 'react-router-dom';
import type User from '../interfaces/Users';
import { Container, Row, Col, Card, Alert, Badge, Button } from 'react-bootstrap';

interface OutletContextType {
    user: User | null;
    setUser: (user: User | null) => void;
}

type Booking = {
    id: number;
    email: string;
    showing_id: number;
    booked_at: string;
};

type Showing = {
    id: number;
    film_id: number;
    start_time: string;
    salong_id: number;
};

type Film = {
    id: number;
    title: string;
};

type BookingWithDetails = Booking & {
    showing?: Showing;
    film?: Film;
};

export default function Profile() {
    const { id } = useParams<{ id: string }>();
    const { user: loggedInUser, setUser } = useOutletContext<OutletContextType>();
    const navigate = useNavigate();

    const [profile, setProfile] = useState<User | null>(null);
    const [bookings, setBookings] = useState<BookingWithDetails[]>([]);
    const [loadingBookings, setLoadingBookings] = useState(true);

    useEffect(() => {
        if (!loggedInUser) {
            navigate('/login');
            return;
        }
        if (loggedInUser.id !== parseInt(id || '0') && loggedInUser.role !== 'admin') {
            navigate(`/profile/${loggedInUser.id}`);
            return;
        }
    }, [loggedInUser, id, navigate]);

    useEffect(() => {
        if (!id) return;
        const controller = new AbortController();

        (async () => {
            try {
                const res = await fetch(`/api/users/${id}`, { signal: controller.signal });
                if (!res.ok) throw new Error(`${res.status} ${res.statusText}`);
                const data = await res.json();
                setProfile(data);
            } catch (e: any) {
                if (e.name !== "AbortError") {
                    console.error('Failed to load profile:', e);
                }
            }
        })();

        return () => controller.abort();
    }, [id]);

    useEffect(() => {
        if (!profile?.email) return;
        const controller = new AbortController();

        (async () => {
            setLoadingBookings(true);
            try {
                const res = await fetch(`/api/bookings?email=${profile.email}`, { signal: controller.signal });
                if (!res.ok) throw new Error(`${res.status} ${res.statusText}`);
                const data = await res.json();
                const bookingList: Booking[] = Array.isArray(data) ? data : data.bookings ?? [];

                const bookingsWithDetails = await Promise.all(
                    bookingList.map(async (booking) => {
                        const showingRes = await fetch(`/api/showings/${booking.showing_id}`, { signal: controller.signal });
                        const showing = await showingRes.json();

                        const filmRes = await fetch(`/api/films/${showing.film_id}`, { signal: controller.signal });
                        const film = await filmRes.json();

                        return { ...booking, showing, film };
                    })
                );

                setBookings(bookingsWithDetails);
            } catch (e: any) {
                if (e.name !== "AbortError") {
                    console.error('Failed to load bookings:', e);
                }
            } finally {
                setLoadingBookings(false);
            }
        })();

        return () => controller.abort();
    }, [profile]);

    const handleCancel = async (bookingId: number, startTime: string) => {
        const isUpcoming = new Date(startTime) > new Date();
        if (!isUpcoming) return;

        const confirmed = window.confirm('Är du säker på att du vill avboka?');
        if (!confirmed) return;

        await fetch(`/api/bookings/${bookingId}`, {
            method: 'DELETE',
            credentials: 'include'
        });

        setBookings(prev => prev.filter(b => b.id !== bookingId));
    };

    const isOwnProfile = loggedInUser?.id === parseInt(id || '0');
    const isAdmin = loggedInUser?.role === 'admin';

    const now = new Date();
    const upcomingBookings = bookings.filter(b => b.showing && new Date(b.showing.start_time) > now);
    const pastBookings = bookings.filter(b => b.showing && new Date(b.showing.start_time) <= now);

    if (!profile) return null;

    return (
        <Container className="mt-5 mb-5">
            <Row>
                <Col md={{ span: 8, offset: 2 }}>

                    <Card className="shadow mb-4">
                        <Card.Body>
                            <div className="d-flex justify-content-between align-items-start">
                                <div>
                                    <h2>{profile.firstName} {profile.lastName}</h2>
                                    <p className="text-muted mb-0">{profile.email}</p>
                                </div>
                                {profile.role === 'admin' && (
                                    <Badge bg="danger">Admin</Badge>
                                )}
                            </div>

                            {isOwnProfile && (
                                <Alert variant="info" className="mt-3 mb-0">
                                    Detta är din profil
                                </Alert>
                            )}
                        </Card.Body>
                    </Card>

                    {isAdmin && !isOwnProfile && (
                        <Card className="mb-4 border-danger">
                            <Card.Header className="bg-danger text-white">
                                Admin-verktyg
                            </Card.Header>
                            <Card.Body>
                                <p className="text-muted">Här kan admin-funktioner läggas till i framtiden.</p>
                            </Card.Body>
                        </Card>
                    )}

                    <Card className="shadow mb-4">
                        <Card.Header>
                            <h5 className="mb-0">Kommande bokningar</h5>
                        </Card.Header>
                        <Card.Body>
                            {loadingBookings ? (
                                <p className="text-muted">Laddar bokningar...</p>
                            ) : upcomingBookings.length === 0 ? (
                                <p className="text-muted">Inga kommande bokningar.</p>
                            ) : (
                                upcomingBookings.map(booking => (
                                    <div key={booking.id} className="d-flex justify-content-between align-items-center border-bottom py-3">
                                        <div>
                                            <strong>{booking.film?.title ?? 'Okänd film'}</strong>
                                            <p className="text-muted mb-0 small">
                                                {booking.showing
                                                    ? new Date(booking.showing.start_time).toLocaleString('sv-SE')
                                                    : 'Okänt datum'}
                                            </p>
                                            <p className="text-muted mb-0 small">
                                                Bokningsnr: #{booking.id}
                                            </p>
                                        </div>
                                        <Button
                                            variant="outline-danger"
                                            size="sm"
                                            onClick={() => handleCancel(booking.id, booking.showing!.start_time)}
                                        >
                                            Avboka
                                        </Button>
                                    </div>
                                ))
                            )}
                        </Card.Body>
                    </Card>

                    <Card className="shadow">
                        <Card.Header>
                            <h5 className="mb-0">Bokningshistorik</h5>
                        </Card.Header>
                        <Card.Body>
                            {loadingBookings ? (
                                <p className="text-muted">Laddar historik...</p>
                            ) : pastBookings.length === 0 ? (
                                <p className="text-muted">Ingen bokningshistorik.</p>
                            ) : (
                                pastBookings.map(booking => (
                                    <div key={booking.id} className="d-flex justify-content-between align-items-center border-bottom py-3">
                                        <div>
                                            <strong>{booking.film?.title ?? 'Okänd film'}</strong>
                                            <p className="text-muted mb-0 small">
                                                {booking.showing
                                                    ? new Date(booking.showing.start_time).toLocaleString('sv-SE')
                                                    : 'Okänt datum'}
                                            </p>
                                            <p className="text-muted mb-0 small">
                                                Bokningsnr: #{booking.id}
                                            </p>
                                        </div>
                                        <Badge bg="secondary">Genomförd</Badge>
                                    </div>
                                ))
                            )}
                        </Card.Body>
                    </Card>

                </Col>
            </Row>
        </Container>
    );
}
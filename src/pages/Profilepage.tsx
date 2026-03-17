import { useState, useEffect } from 'react';
import { useParams, useOutletContext, useNavigate } from 'react-router-dom';
import type User from '../interfaces/Users';
import { formatShowtime } from '../utils/bookingUtils';

interface OutletContextType {
    user: User | null;
}

type Booking = {
    id: number;
    booking_reference: string;
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
    const { user: loggedInUser } = useOutletContext<OutletContextType>();
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

    const handleCancel = async (bookingRef: string, startTime: string) => {
        const isUpcoming = new Date(startTime) > new Date();
        if (!isUpcoming) return;

        const confirmed = window.confirm('Är du säker på att du vill avboka?');
        if (!confirmed) return;

        await fetch(`/api/bookings/${bookingRef}`, {
            method: 'DELETE',
            credentials: 'include'
        });

        setBookings(prev => prev.filter(b => b.booking_reference !== bookingRef));
    };

    const isOwnProfile = loggedInUser?.id === parseInt(id || '0');
    const isAdmin = loggedInUser?.role === 'admin';

    const now = new Date();
    const upcomingBookings = bookings.filter(b => b.showing && new Date(b.showing.start_time) > now);
    const pastBookings = bookings.filter(b => b.showing && new Date(b.showing.start_time) <= now);

    if (!profile) return null;

    return (
        <div className="container mt-5 mb-5">
            <div className="row">
                <div className="col-12 col-md-8 offset-md-2">

                    <div className="card shadow mb-4">
                        <div className="card-body">
                            <div className="d-flex justify-content-between align-items-start">
                                <div>
                                    <h2>{profile.firstName} {profile.lastName}</h2>
                                    <p className="text-muted mb-0">{profile.email}</p>
                                </div>
                                {profile.role === 'admin' && (
                                    <span className="badge bg-danger">Admin</span>
                                )}
                            </div>

                            {isOwnProfile && (
                                <div className="alert alert-info mt-3 mb-0" role="alert">
                                    Detta är din profil
                                </div>
                            )}
                        </div>
                    </div>

                    {isAdmin && !isOwnProfile && (
                        <div className="card mb-4 border-danger">
                            <div className="card-header bg-danger text-white">
                                Admin-verktyg
                            </div>
                            <div className="card-body">
                                <p className="text-muted">Här kan admin-funktioner läggas till i framtiden.</p>
                            </div>
                        </div>
                    )}

                    <div className="card shadow mb-4">
                        <div className="card-header">
                            <h5 className="mb-0">Kommande bokningar</h5>
                        </div>
                        <div className="card-body">
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
                                                    ? formatShowtime(booking.showing.start_time)
                                                    : 'Okänt datum'}
                                            </p>
                                            <p className="text-muted mb-0 small">
                                                Bokningsref: {booking.booking_reference}
                                            </p>
                                        </div>
                                        <button
                                            className="btn btn-outline-danger btn-sm"
                                            onClick={() => handleCancel(booking.booking_reference, booking.showing!.start_time)}
                                        >
                                            Avboka
                                        </button>
                                    </div>
                                ))
                            )}
                        </div>
                    </div>

                    <div className="card shadow">
                        <div className="card-header">
                            <h5 className="mb-0">Bokningshistorik</h5>
                        </div>
                        <div className="card-body">
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
                                                    ? formatShowtime(booking.showing.start_time)
                                                    : 'Okänt datum'}
                                            </p>
                                            <p className="text-muted mb-0 small">
                                                Bokningsref: {booking.booking_reference}
                                            </p>
                                        </div>
                                        <span className="badge bg-secondary">Genomförd</span>
                                    </div>
                                ))
                            )}
                        </div>
                    </div>

                </div>
            </div>
        </div>
    );
}
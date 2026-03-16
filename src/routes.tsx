// src/routes.tsx
import { useState, useEffect } from 'react';
import { Navigate } from 'react-router-dom';
import Layout from "./pages/Layout";
import Main from "./pages/Main";
import LogIn from "./pages/LogIn";
import Profile from "./pages/Profilepage";
import Register from "./pages/Register";
import FilmDetails from "./pages/FilmDetails";
import About from "./pages/About";
import AiChatPage from "./pages/AiChatPage";
import Booking from "./pages/Booking";
import BookingConfirmation from "./pages/BookingConfirmation";
import Contact from "./pages/Contact";
import PrivacyPolicy from "./pages/PrivacyPolicy";
import TermsOfUse from "./pages/TermsOfUse";
import HelpCenter from "./pages/HelpCenter";


function ProfileRedirect() {
  const [userId, setUserId] = useState<number | null>(null);

  useEffect(() => {
    fetch('/api/login', { credentials: 'include' })
      .then(res => res.json())
      .then(data => {
        if (!data.error) setUserId(data.id);
      });
  }, []);

  if (userId === null) return null; // or a loading spinner
  return <Navigate to={`/profile/${userId}`} replace />;
}

const routes = [
  {
    path: "/",
    element: <Layout />,
    children: [
      { path: "/", element: <Main /> },
      { path: "/login", element: <LogIn /> },
      { path: "/profile", element: <ProfileRedirect /> },
      { path: "/profile/:id", element: <Profile /> },
      { path: "/register", element: <Register /> },
      { path: "/films/:id", element: <FilmDetails /> },
      { path: "/about", element: <About /> },
      { path: '/chat', element: <AiChatPage /> },
      { path: '/booking/:showingId', element: <Booking /> },
      { path: '/booking/confirmation/:reference', element: <BookingConfirmation /> },
      { path: "/contact", element: <Contact /> },
      { path: "/integritetspolicy", element: <PrivacyPolicy /> },
      { path: "/anvandarvillkor", element: <TermsOfUse /> },
      { path: "/hjalpcenter", element: <HelpCenter /> },
      { path: "*", element: <h1>Page not found</h1> },
    ],
  },
];

export default routes;
// src/routes.tsx
import Layout from "./pages/Layout";
import Main from "./pages/Main";
import LogIn from "./pages/LogIn";
import Profile from "./pages/Profilepage";
import Register from "./pages/Register";
import FilmDetails from "./pages/FilmDetails";
import About from "./pages/About";
import AiChatPage from "./pages/AiChatPage";

const routes = [
  {
    path: "/",
    element: <Layout />,
    children: [
      { path: "/", element: <Main /> },            // "/"
      { path: "/login", element: <LogIn /> },     // "/signin"
      { path: "/profile/:id", element: <Profile /> }, // Profile Page
      { path: "/register", element: <Register /> },
      { path: "/films/:id", element: <FilmDetails /> }, // "/films/:id"
      { path: "/about", element: <About /> },            // "/"
      { path: '/chat', element: <AiChatPage/> },
      { path: "*", element: <h1>Page not found</h1> },
    ],
  },
];

export default routes;

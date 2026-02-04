import type Routes from "./interfaces/Routes";
import StartPage from "./pages/Start.tsx";
import FilmDetails from "./pages/FilmDetails.tsx";
/*import Films from "./pages/Films.tsx";
import Bookings from "./pages/Bookings.tsx";
import BookingDetails from "./pages/BookingDetails.tsx";
*/
const routes: Routes[] = [
    { 
        element: <StartPage />, 
        path: "/"
    },
    { 
        element: <FilmDetails />, 
        path: "/films/:id"
    },
    /*{ 
        element: <Films />, 
        path: "/films"
    },
    { 
        element: <Bookings />, 
        path: "/bookings"
    },
    { 
        element: <BookingDetails />, 
        path: "/bookings/:id"
    },*/
    { 
        element: <h1>Page not found</h1>, 
        path: "*"
    }
]

export default routes;
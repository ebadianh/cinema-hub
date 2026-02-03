import type Routes from "./interfaces/Routes";
import CardInfo from "./pages/CardInfo.tsx";
import Home from "./pages/Home.tsx";

const routes: Routes[] = [
    { 
        element: <Home />, 
        path: "/"
    },
    { 
        element: <CardInfo />, 
        path: "/card/:id"
    },
    { 
        element: <h1>Page not found</h1>, 
        path: "*"
    }
]

export default routes;
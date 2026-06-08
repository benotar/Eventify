import type {FC} from "react";
import {Route, Routes} from "react-router-dom";
import EventsPage from "./pages/EventsPage.tsx";
import MyTicketsPage from "./pages/MyTicketsPage.tsx";
import CallbackPage from "./pages/CallbackPage.tsx";
import Navbar from "./components/Navbar.tsx";

const App: FC = () => {
    return (
        <div>
            <Navbar/>

            <Routes>
                <Route path="/" element={<EventsPage/>}/>
                <Route path="/callback" element={<CallbackPage/>}/>
                <Route path="/my-tickets" element={<MyTicketsPage/>}/>
            </Routes>
        </div>
    );
};

export default App;
import {StrictMode} from "react";
import {createRoot} from "react-dom/client";
import App from "./App.tsx";
import AuthProvider from "./auth/AuthProvider.tsx";
import {BrowserRouter} from "react-router-dom";
import "./i18n.ts";
import "./index.css";
import AnimatedBackground from "./components/AnimatedBackground.tsx";

createRoot(document.getElementById("root")!).render(
    <StrictMode>
        <BrowserRouter>
            <AuthProvider>
                <AnimatedBackground />
                <App/>
            </AuthProvider>
        </BrowserRouter>
    </StrictMode>
);

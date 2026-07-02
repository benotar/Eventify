import type {FC} from "react";
import {Link} from "react-router-dom";
import {useTranslation} from "react-i18next";
import {useTheme} from "../theme/useTheme.ts";
import {Moon, Sun} from "lucide-react";
import LanguageSwitcher from "./LanguageSwitcher.tsx";
import {useAuth} from "../auth/useAuth.ts";

const Navbar: FC = () => {
    const {isAuthenticated, isLoading, login, logout} = useAuth();
    const {theme, toggleTheme} = useTheme();
    const {t} = useTranslation();

    return (
        <nav className="sticky top-0 z-50 bg-surface backdrop-blur-xl border-b border-border">
            <div className="mx-auto flex h-16 max-w-7xl items-center justify-between px-6">

                <div className="flex items-center gap-8">
                    <Link
                        to="/"
                        className="font-mono bg-gradient-to-r from-brand to-brand-2 bg-clip-text text-xl font-bold text-transparent"
                    >
                        Eventify
                    </Link>
                    <Link
                        to="/"
                        className="text-sm font-medium text-fg-muted transition-colors hover:text-fg"
                    >
                        {t("nav.events")}
                    </Link>
                    {isAuthenticated && (
                        <Link
                            to="/my-tickets"
                            className="text-sm font-medium text-fg-muted transition-colors hover:text-fg"
                        >
                            {t("nav.myTickets")}
                        </Link>
                    )}
                </div>

                <div className="flex items-center gap-2">

                    <div className="flex items-center">
                        <LanguageSwitcher/>
                    </div>

                    <button
                        onClick={toggleTheme}
                        aria-label="Toggle theme"
                        className="cursor-pointer rounded-lg p-2 text-fg-muted transition-colors hover:bg-surface-2 hover:text-fg
  focus:outline-none focus:ring-2 focus:ring-brand"
                    >
                        {theme === "dark"
                            ? <Sun size={18}/>
                            : <Moon size={18}/>}
                    </button>

                    {!isLoading && (
                        isAuthenticated ? (
                            <button
                                onClick={logout}
                                className="cursor-pointer rounded-lg bg-surface-2 px-4 py-1.5 text-sm font-medium text-fg transition-opacity hover:opacity-80 focus:outline-none focus:ring-2 focus:ring-brand"
                            >
                                {t("nav.signOut")}
                            </button>
                        ) : (
                            <button
                                onClick={login}
                                className="cursor-pointer rounded-lg bg-brand px-4 py-1.5 text-sm font-medium text-white transition-opacity hover:opacity-90 focus:outline-none focus:ring-2 focus:ring-brand focus:ring-offset-2"
                            >
                                {t("nav.signIn")}
                            </button>
                        )
                    )}
                </div>
            </div>
        </nav>
    );
};

export default Navbar;
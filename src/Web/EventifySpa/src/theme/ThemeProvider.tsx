import {type FC, type PropsWithChildren, useCallback, useEffect, useMemo, useState} from "react";
import {Theme, ThemeContext, type ThemeContextValue, type ThemeMode} from "./useTheme.ts";

const ThemeKey = "theme";
const getInitialTheme = (): ThemeMode => {
    const stored = localStorage.getItem(ThemeKey);

    if (stored === Theme.Light || stored === Theme.Dark)
        return stored;

    return Theme.Dark;
}

const ThemeProvider: FC<PropsWithChildren> = ({children}) => {
    const [theme, setTheme] = useState<ThemeMode>(getInitialTheme);

    useEffect(() => {
        document.documentElement.classList.toggle(Theme.Dark, theme === Theme.Dark);
        localStorage.setItem(ThemeKey, theme);
    }, [theme]);

    const toggleTheme = useCallback(() => setTheme(t =>
            t === Theme.Dark
                ? Theme.Light
                : Theme.Dark),
        []);

    const value = useMemo<ThemeContextValue>(() => ({
        theme,
        toggleTheme
    }), [theme, toggleTheme]);

    return (
        <ThemeContext.Provider value={value}>
            {children}
        </ThemeContext.Provider>
    );
};

export default ThemeProvider;
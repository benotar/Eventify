import {useEffect, useState} from "react";

type Theme = "light" | "dark";

const useTheme = () => {
    const [theme, setTheme] = useState<Theme>(() => {
        const storedTheme = localStorage.getItem("theme");

        if (storedTheme === "light" || storedTheme === "dark")
            return storedTheme;

        const systemPrefersDark = window.matchMedia("(prefers-color-scheme: dark)").matches;

        return systemPrefersDark
            ? "dark"
            : "light";
    });

    useEffect(() => {
        document.documentElement.classList.toggle("dark", theme === "dark");
        localStorage.setItem("theme", theme);
    }, [theme]);

    return {
        theme,
        toggleTheme: () => setTheme(t => (t === "dark" ? "light" : "dark"))
    };
};

export default useTheme;

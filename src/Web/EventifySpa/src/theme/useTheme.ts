import {createContext, useContext} from "react";

export const Theme = {
    Light : "light",
    Dark : "dark",
} as const;

export type ThemeMode = typeof Theme[keyof typeof Theme];

export interface ThemeContextValue {
    theme: ThemeMode;
    toggleTheme: () => void;
}

export const ThemeContext = createContext<ThemeContextValue | undefined>(undefined);

export const useTheme = (): ThemeContextValue => {
    const context = useContext(ThemeContext);

    if (context === undefined) {
        throw new Error("useTheme must be used within ThemeProvider");
    }

    return context;
};
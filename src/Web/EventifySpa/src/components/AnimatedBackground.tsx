import type {FC} from "react";

const AnimatedBackground: FC = () => {
    return (
        <>
            <div className="bg-animated"/>
            <div
                className="orb"
                style={{
                    width: "700px",
                    height: "700px",
                    top: "-200px",
                    right: "-200px",
                    background: "radial-gradient(circle, rgba(99,102,241,0.22), transparent 70%)",
                }}
            />
            <div
                className="orb"
                style={{
                    width: "600px",
                    height: "600px",
                    bottom: "-150px",
                    left: "-150px",
                    background: "radial-gradient(circle, rgba(139,92,246,0.18), transparent 70%)",
                    animationDelay: "-6s",
                }}
            />
            <div
                className="orb"
                style={{
                    width: "400px",
                    height: "400px",
                    top: "40%",
                    left: "35%",
                    background: "radial-gradient(circle, rgba(59,130,246,0.10), transparent 70%)",
                    animationDelay: "-3s",
                }}
            />
        </>
    );
};

export default AnimatedBackground;
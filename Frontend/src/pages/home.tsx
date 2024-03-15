import { DateTime } from "luxon";
import { useEffect, useState } from "react";
import getString from "../components/text";

export default function Home() {
    const [time, setTime] = useState<DateTime>(DateTime.local());

    useEffect(() => {
        const interval = setInterval(() => {
            setTime(DateTime.local());
        }, 1000);

        return () => clearInterval(interval);
    }, []);

    return (
        <div className="container mt-4">
            <div className="row">
                <h1>Home</h1>
            </div>
            {getString("home", time).map((str, index) => (
                <div key={index} className="row">
                    <p>{str}</p>
                </div>
            ))}
            <div className="row">
                <a className="btn btn-primary" href="https://discord.gg/Yf6eDSuQ4f" target="_blank" rel="noreferrer"><i className="bi bi-discord me-2"></i>{getString("join", time)[0]}</a>
            </div>
        </div>
    );
}
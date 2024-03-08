import { DateTime } from "luxon";
import { useEffect, useState } from "react";

const strs = [
    `Welcome one and all to Dynamis Extreme Raiding Parties, aka DERP!`,
    `This is a high end focused Dynamis server with the goal to ensure a safe learning environment for those wanting to get into harder content. Here you will find parties hosted by both our team and other players within Ultimates, Savage, Extremes, Unreals, Criterion, BLU and more!`,
    `We wish to become a hub for Dynamis players to find others to raid with, regardless of if they are newer to the game or veterans.`
];

const owod = [
    `HIIII! Wewcome one and aww to Dynamis Extweme Waiding Pawties, aka DEWP!`,
    `This is a high end focused Dynamis sewvew with da goaw to ensuwe a safe weawning enviwonment fow those wanting to get into hawdew content. Hewe uu wiww find pawties hosted by both ouw team and othew pwayews within Uwtimates, Savage, Extwemes, Unweaws, Cwitewion, BWU and mowe!`,
    `We wish to become a hub fow Dynamis pwayews to find othews to waid with, wegawdwess of if they awe newew to da game ow vetewans.`
];

export default function Home() {
    const [html, setHtml] = useState<string[]>(strs);
    const [time, setTime] = useState<DateTime>(DateTime.local());

    useEffect(() => {
        if (time.month === 4 && time.day === 1) {
            setHtml(owod);
        }

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
            {html.map((str, index) => (
                <div key={index} className="row">
                    <p>{str}</p>
                </div>
            ))}
        </div>
    );
}
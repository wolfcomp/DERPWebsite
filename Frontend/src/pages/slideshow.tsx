import { useEffect, useRef, useState } from "react";
import { useSlideshow } from "../components/slideshow";
import { Tooltip } from "bootstrap";
import { DateTime } from "luxon";
import getString from "../components/text";

export default function Slideshow() {
    const { expansion, setAutoShift, setBlured, setExpansion, setNavContent, autoShift, nextImage, prevImage, navContent } = useSlideshow();
    const [prevBlur, setPrevBlur] = useState<"default" | "blured" | "unblured">("default");
    const [time, setTime] = useState<DateTime>(DateTime.local());
    const listRef = useRef<HTMLUListElement>(null);

    useEffect(() => {
        setExpansion("main");
        setBlured(blured => {
            setPrevBlur(blured);
            return "unblured";
        });
        localStorage.removeItem("unblured");
        return () => {
            setAutoShift(true);
            setBlured(prevBlur);
            setExpansion("slideshow_main");
            setNavContent(null);
        }
    }, []);

    useEffect(() => {
        const interval = setInterval(() => {
            setTime(DateTime.local());
        }, 1000);

        return () => clearInterval(interval);
    }, [setTime]);

    useEffect(() => {
        if (!listRef.current) return;
        var tooltips = Array.from(listRef.current.children).map((t) => {
            const tooltip = new Tooltip(t, {
                placement: "bottom",
                trigger: "hover"
            });
            return tooltip;
        });
        return () => {
            tooltips.forEach((t) => t.dispose());
        }
    }, [navContent]);

    useEffect(() => {
        setNavContent(<>
            <ul className="navbar-nav" ref={listRef}>
                <li className="navbar-item me-2" data-bs-title={getString("slideshow", time)[0]}><button className={"btn " + (expansion === "main" ? "btn-primary" : "btn-secondary")} onClick={() => setExpansion("main")}>{getString("slideshow", time)[0]}</button></li>
                <li className="navbar-item me-2" data-bs-title={getString("slideshow", time)[1]}><button className={"btn " + (expansion === "ex1" ? "btn-primary" : "btn-secondary")} onClick={() => setExpansion("ex1")}>{getString("slideshow", time)[1]}</button></li>
                <li className="navbar-item me-2" data-bs-title={getString("slideshow", time)[2]}><button className={"btn " + (expansion === "ex2" ? "btn-primary" : "btn-secondary")} onClick={() => setExpansion("ex2")}>{getString("slideshow", time)[2]}</button></li>
                <li className="navbar-item me-2" data-bs-title={getString("slideshow", time)[3]}><button className={"btn " + (expansion === "ex3" ? "btn-primary" : "btn-secondary")} onClick={() => setExpansion("ex3")}>{getString("slideshow", time)[3]}</button></li>
                <li className="navbar-item me-2" data-bs-title={getString("slideshow", time)[4]}><button className={"btn " + (expansion === "ex4" ? "btn-primary" : "btn-secondary")} onClick={() => setExpansion("ex4")}>{getString("slideshow", time)[4]}</button></li>
                <li className="navbar-item me-2" data-bs-title={getString("slideshow", time)[5]}><button className={"btn " + (expansion === "ex5" ? "btn-primary" : "btn-secondary")} onClick={() => setExpansion("ex5")}>{getString("slideshow", time)[5]}</button></li>
            </ul>
            <ul className="navbar-nav">
                <li className="navbar-item"><button className={"btn btn-info me-2"} onClick={() => setAutoShift(!autoShift)}>{getString("slideshowToggle", time)[autoShift ? 1 : 0]}</button></li>
            </ul>
        </>
        );
    }, [expansion, autoShift]);

    return (!autoShift && (<div style={{ position: "absolute", top: 0, left: 0, height: "100vh", width: "100vw", overflow: "hidden" }}>
        <i className="rounded-5 text-center hover-glow bi bi-arrow-left-short" style={{ position: "absolute", top: "calc(50vh - 23px)", left: 14, fontSize: 30, width: 46, height: 46, background: "#333333", cursor: "pointer" }} onClick={() => prevImage()}></i>
        <i className="rounded-5 text-center hover-glow bi bi-arrow-right-short" style={{ position: "absolute", top: "calc(50vh - 23px)", right: 14, fontSize: 30, width: 46, height: 46, background: "#333333", cursor: "pointer" }} onClick={() => nextImage()}></i>
    </div>
    )) || (<></>);
}
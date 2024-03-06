import hljs from "highlight.js";
import { Carousel, Collapse, Dropdown, Modal, Offcanvas, Popover, ScrollSpy, Tab, Toast, Tooltip } from "bootstrap";
import { useEffect, useRef } from "react";
import { Resource } from "../structs/resource";
import "highlight.js/styles/monokai-sublime.css";

export default function ResourceRender({ resource }: { resource: Resource }) {
    const divRef = useRef<HTMLDivElement>(null);
    const scrollRefs = useRef<Record<string, HTMLElement>>({});
    useEffect(() => {
        if (divRef.current) {
            scrollRefs.current = {};
            divRef.current.querySelectorAll("h1").forEach((el) => {
                const el2 = el as HTMLElement;
                if (el2.id)
                    scrollRefs.current[el2.id] = el2;
            });
            divRef.current.querySelectorAll("h2").forEach((el) => {
                const el2 = el as HTMLElement;
                if (el2.id)
                    scrollRefs.current[el2.id] = el2;
            });
            divRef.current.querySelectorAll("h3").forEach((el) => {
                const el2 = el as HTMLElement;
                if (el2.id)
                    scrollRefs.current[el2.id] = el2;
            });
            divRef.current.querySelectorAll("h4").forEach((el) => {
                const el2 = el as HTMLElement;
                if (el2.id)
                    scrollRefs.current[el2.id] = el2;
            });
            divRef.current.querySelectorAll("[data-navigate]").forEach((el) => {
                el.addEventListener("click", (e) => {
                    e.preventDefault();
                    const target = e.target as HTMLElement;
                    const href = target.dataset.navigate;
                    if (href) {
                        const el = scrollRefs.current[href];
                        if (el) {
                            el.scrollIntoView({ behavior: "smooth" });
                        }
                    }
                });
            });
            divRef.current.querySelectorAll("pre code").forEach((el) => {
                const el2 = el as HTMLElement;
                hljs.highlightElement(el2);
            });
            divRef.current.querySelectorAll(".carousel").forEach((el) => {
                const el2 = el as HTMLElement;
                new Carousel(el2);
            });
            divRef.current.querySelectorAll(".collapse").forEach((el) => {
                const el2 = el as HTMLElement;
                new Collapse(el2);
            });
            divRef.current.querySelectorAll(".dropdown-toggle").forEach((el) => {
                const el2 = el as HTMLElement;
                new Dropdown(el2);
            });
            divRef.current.querySelectorAll(".modal").forEach((el) => {
                const el2 = el as HTMLElement;
                new Modal(el2);
            });
            divRef.current.querySelectorAll(".offcanvas").forEach((el) => {
                const el2 = el as HTMLElement;
                new Offcanvas(el2);
            });
            divRef.current.querySelectorAll(".popover").forEach((el) => {
                const el2 = el as HTMLElement;
                new Popover(el2);
            });
            divRef.current.querySelectorAll(".scrollspy").forEach((el) => {
                const el2 = el as HTMLElement;
                new ScrollSpy(el2);
            });
            divRef.current.querySelectorAll(".tab-pane").forEach((el) => {
                const el2 = el as HTMLElement;
                new Tab(el2);
            });
            divRef.current.querySelectorAll(".toast").forEach((el) => {
                const el2 = el as HTMLElement;
                new Toast(el2);
            });
            divRef.current.querySelectorAll(".tooltip").forEach((el) => {
                const el2 = el as HTMLElement;
                new Tooltip(el2);
            });
        }
    }, [resource]);

    return (<div className="p-2 bg-body container" ref={divRef} dangerouslySetInnerHTML={{ __html: resource.htmlContent }} />)
}
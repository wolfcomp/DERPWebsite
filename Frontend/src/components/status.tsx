import { useEffect, useRef, useState } from "react";
import { useToast } from "./toast";
import { chunk } from "./linq";
import { Tooltip } from "bootstrap";
import "./status.scss";

const filePath = 'https://pdp.wildwolf.dev/files/game_icons/{0}/{1}_hr1.png';

export function Status(props: { status: Status, scale: number }) {
    const { status, scale } = props;
    const toast = useToast().toast;
    const ref = useRef<HTMLDivElement>(null);
    const tooltipRef = useRef<HTMLDivElement>(null);
    const tooltipNameRef = useRef<HTMLSpanElement>(null);

    function copyHTML(e: React.MouseEvent<HTMLButtonElement, MouseEvent>) {
        e.preventDefault();

        const html = ref.current.outerHTML;

        navigator.clipboard.writeText(html);

        toast("Copied HTML to clipboard", "Editor", "success");
    }

    useEffect(() => {
        if (!tooltipRef.current)
            return;
        const tooltip = new Tooltip(tooltipRef.current, { title: status.description, placement: "right" });
        return () => tooltip.dispose();
    }, [tooltipRef, status]);

    useEffect(() => {
        if (!tooltipNameRef.current)
            return;
        const tooltip = new Tooltip(tooltipNameRef.current, { title: status.name, placement: "bottom" });
        return () => tooltip.dispose();
    }, [tooltipNameRef, status]);

    return (
        <div className="mt-1 mb-1 d-flex">
            <div ref={ref} className="col-9 d-flex align-items-center">
                <span className="me-1 text-truncate d-inline-block" style={{ maxWidth: "85%" }} ref={tooltipNameRef}>{status.name}</span>
                <div style={{ display: "inline-block", position: "relative" }} ref={tooltipRef}>
                    {status.canDispel && <img src={filePath.replace("{0}", "").replace("{1}", "dispel")} alt="Dispel line" style={{ position: "absolute", top: 0, right: -8 * scale, width: 64 * scale }} />}
                    <img src={filePath.replace("{0}", status.iconGroup).replace("{1}", status.icon)} alt={status.name} style={{ marginTop: 8 * scale, height: 64 * scale }} onError={(e) => e.currentTarget.parentElement.style.display = "none"} onLoad={(e) => e.currentTarget.parentElement.style.display = "inline-block"} />
                </div>
            </div>
            <button className="btn btn-sm btn-primary col-3" onClick={copyHTML}>Copy HTML</button>
        </div>
    );
}

export function Statuses(props: { statuses: Status[], scale: number }) {
    const pageLength = 30;
    const { statuses, scale } = props;
    const [search, setSearch] = useState<string>("");
    const [pageInfo, setPageInfo] = useState<{ page: number, total: number }>({ page: 0, total: 0 });
    const [statusesShown, setStatusesShown] = useState<Status[]>(statuses.slice(0, pageLength));

    function filter(search: string, page: number) {
        setSearch(search);
        if (search === "") {
            setStatusesShown(statuses.slice(page * pageLength, (page + 1) * pageLength));
            setPageInfo({ page: page, total: Math.ceil(statuses.length / pageLength) });
            return;
        }
        const filtered = statuses.filter((status) => status.name.toLowerCase().includes(search.toLowerCase()));
        setPageInfo({ page: page, total: Math.ceil(filtered.length / pageLength) });
        setStatusesShown(filtered.slice(page * pageLength, (page + 1) * pageLength));
    }

    useEffect(() => {
        filter(search, 0);
    }, [statuses]);

    return (
        <div className="row">
            <div className="container-fluid bg-body mt-3 pt-2" id="status-search-list">
                <input type="text" className="form-control" placeholder="Search" value={search} onChange={(e) => {
                    e.preventDefault();
                    filter(e.target.value, 0);
                }} />
                <div className="row pt-2 pb-2 border-bottom border-3">
                    <div className="d-flex justify-content-between">
                        <span>{statusesShown.length} statuses shown</span>
                        <button className="btn btn-sm btn-primary" onClick={() => filter("", 0)}>Reset search filter</button>
                    </div>
                </div>
                <div className="col-12">
                    {[...chunk(statusesShown, 3)].map((subStatuses, i) => (
                        <div className={`row ${i > 0 ? "border-top border-2" : ""}`} key={`status_${i}`}>
                            {subStatuses.map((status, j) => <div className="col-4" key={`status_${i}_${j}`}><Status status={status} scale={scale} /></div>)}
                        </div>))}
                </div>
                <div className="row pt-2 pb-2 border-top border-3">
                    <div className="d-flex justify-content-between">
                        <button className="btn btn-sm btn-primary" disabled={pageInfo.page === 0} onClick={() => filter(search, pageInfo.page - 1)}>Previous page</button>
                        <span
                            onClick={(e) => {
                                e.preventDefault();
                                var pageInput = document.querySelector("#pageInput") as HTMLInputElement;
                                pageInput.classList.add("d-inline-block");
                                pageInput.classList.remove("d-none");
                                var pageInfo = document.querySelector("#pageInfo");
                                pageInfo.classList.add("d-none");
                                pageInfo.classList.remove("d-inline-block");
                                pageInput.focus();
                            }}
                        >
                            Page&nbsp;
                            <span id="pageInfo">{pageInfo.page + 1}</span>
                            <input type="number" className="form-control d-none w-auto" id="pageInput" min={1} max={pageInfo.total} value={pageInfo.page + 1} onChange={(e) => {
                                e.preventDefault();
                                const page = parseInt(e.target.value);
                                filter(search, page - 1);
                            }}
                                onBlur={(e) => {
                                    e.preventDefault();
                                    var pageInfo = document.querySelector("#pageInfo");
                                    pageInfo.classList.add("d-inline-block");
                                    pageInfo.classList.remove("d-none");
                                    var pageInput = document.querySelector("#pageInput")
                                    pageInput.classList.add("d-none");
                                    pageInput.classList.remove("d-inline-block");
                                }}
                            />
                            &nbsp;of {pageInfo.total}</span>
                        <button className="btn btn-sm btn-primary" disabled={pageInfo.page === pageInfo.total - 1} onClick={() => filter(search, pageInfo.page + 1)}>Next page</button>
                    </div>
                </div>
            </div>
        </div>
    );
}
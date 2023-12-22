import { useRef } from "react";
import { useToast } from "../components/toast";

export default function Files() {
    const ifrRef = useRef<HTMLIFrameElement>(null);
    const toast = useToast().toast;

    function copyUrl() {
        window.navigator.clipboard.writeText(ifrRef.current?.contentWindow?.location.href ?? "");
        toast("Copied url to clipboard", "Files");
    }

    return (
        <div className="container mt-4">
            <div className="d-flex">
                <h1>Files</h1>
                <div className="ms-auto align-items-center d-flex">
                    <button className="btn btn-primary" onClick={copyUrl}>Copy url</button>
                </div>
            </div>
            <iframe ref={ifrRef} src="https://pdp.wildwolf.dev/files" style={{ width: "100%", height: 900, background: "transparent" }} sandbox="allow-same-origin allow-scripts" allowTransparency={true} />
        </div>
    )
}
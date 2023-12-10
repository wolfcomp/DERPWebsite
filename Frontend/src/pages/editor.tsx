import MarkdownEditor from "@uiw/react-markdown-editor";
import DOMPurify from "dompurify";
import { parse } from "marked";

import { useEffect, useRef, useState } from "react";
import { useRequest } from "../components/request";
import { Status, Statuses } from "../components/status";
import { useModal } from "../components/modal";
import { useToast } from "../components/toast";
import { Category, Expansion } from "../structs/resource";

export default function Editor() {
    const [markdown, setMarkdown] = useState<string>('');
    const [prevMarkdown, setPrevMarkdown] = useState<string>('');
    const [statuses, setStatuses] = useState<Status[]>([]);
    const [display, setDisplay] = useState<"editor" | "preview">("editor");
    const divRef = useRef<HTMLDivElement>(null);
    const scrollRefs = useRef<Record<string, HTMLElement>>({});
    const request = useRequest().request;
    const modal = useModal();

    useEffect(() => {
        try {
            const html = parse(markdown);
            const clean = DOMPurify.sanitize(html);

            setPrevMarkdown(clean);
        }
        catch (err) {
            console.log(err);
        }
    }, [markdown]);

    useEffect(() => {
        if (divRef.current) {
            scrollRefs.current = {};
            divRef.current.innerHTML = prevMarkdown;
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
        }
    }, [prevMarkdown]);

    useEffect(() => {
        getStatuses();
    }, [setStatuses])

    async function getStatuses() {
        const res = await request("/api/status");
        if (!res.ok)
            return;
        const statuses = await res.json() as Status[];
        setStatuses(statuses);
    }

    return (
        <div className="container">
            <Statuses statuses={statuses} scale={0.5} />
            <div className="d-flex justify-content-between align-items-center mt-4">
                <button className="btn btn-primary" onClick={(e) => {
                    e.preventDefault();
                    setDisplay(display === "editor" ? "preview" : "editor");
                }}>{display === "editor" ? "Change to preview" : "Change to editor"}</button>
                <button className="btn btn-primary" onClick={(e) => {
                    e.preventDefault();
                    modal(<EditorSaveModal markdown={markdown} />);
                }}>Save</button>
            </div>
            <div className="mt-1 d-flex flex-grow-1">
                <div className="col-12" style={{ display: display === "editor" ? "block" : "none" }}>
                    <MarkdownEditor
                        value={markdown}
                        onChange={(value, viewUpdate) => {
                            setMarkdown(value);
                        }}
                        enablePreview={false}
                    />
                </div>
                <div className="col-12" style={{ display: display === "preview" ? "block" : "none" }}>
                    <div className="p-2 bg-body" ref={divRef} />
                </div>
            </div>
        </div>
    );
}

function EditorSaveModal(props: { pageName?: string, markdown: string, category?: string, expansion?: string }) {
    const modal = useModal();
    const request = useRequest().request;
    const toast = useToast().toast;
    const { pageName, markdown, category, expansion } = props;
    const [pageNameInternal, setPageNameInternal] = useState<string>(pageName || "");
    const [categoryInternal, setCategoryInternal] = useState<string>(category || "");
    const [expansionInternal, setExpansionInternal] = useState<string>(expansion || "");
    const [categories, setCategories] = useState<string[]>([]);
    const [expansions, setExpansions] = useState<string[]>([]);
    const [validation, setValidation] = useState<Record<string, string>>({});
    const allowedTitleChars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-_";
    const charLimit = 32;

    function validate() {
        var validationInternal: Record<string, string> = {};
        if (!pageNameInternal) {
            validationInternal["pageName"] = "Page name cannot be empty";
        }
        if (pageNameInternal.length > charLimit) {
            validationInternal["pageName"] = `Page name cannot be longer than ${charLimit} characters`;
        }
        if (pageNameInternal.length < 3) {
            validationInternal["pageName"] = "Page name cannot be shorter than 3 characters";
        }
        for (const char of pageNameInternal) {
            if (!allowedTitleChars.includes(char)) {
                validationInternal["pageName"] = `Page name cannot contain '${char}'`;
            }
        }
        if (validationInternal["pageName"] === undefined) {
            validationInternal["pageName"] = "";
        }
        if (!categoryInternal) {
            validationInternal["category"] = "Category cannot be empty";
        }
        if (!categories.includes(categoryInternal)) {
            validationInternal["category"] = `Category '${categoryInternal}' does not exist`;
        }
        if (validationInternal["category"] === undefined) {
            validationInternal["category"] = "";
        }
        if (!expansionInternal) {
            validationInternal["expansion"] = "Expansion cannot be empty";
        }
        if (!expansions.includes(expansionInternal)) {
            validationInternal["expansion"] = `Expansion '${expansionInternal}' does not exist`;
        }
        if (validationInternal["expansion"] === undefined) {
            validationInternal["expansion"] = "";
        }
        setValidation(validationInternal);
        for (const key in validationInternal) {
            if (validationInternal[key].length > 0) {
                throw new Error(validationInternal[key]);
            }
        }
    }

    function save() {
        try {
            validate();
        }
        catch (e) {
            if (e instanceof Error) {
                toast(e.message, "EditorSaveModal", "error");
            }
            return;
        }
    }

    async function getCategories() {
        const res = await request("/api/resources/categories");
        if (!res.ok)
            return;
        const categories = await res.json() as Category[];
        setCategories(categories.map((category) => category.name));
    }

    useEffect(() => {
        getCategories();
    }, [setCategories]);

    async function getExpansions() {
        const res = await request("/api/resources/expansions");
        if (!res.ok)
            return;
        const expansions = await res.json() as Expansion[];
        setExpansions(expansions.map((expansion) => expansion.name));
    }

    useEffect(() => {
        getExpansions();
    }, [setExpansions]);

    function getValidation(key: string) {
        var val = validation[key];
        if (val)
            return val.length > 0 ? " is-invalid" : " is-valid";
        return "";
    }

    return (
        <div className="modal-dialog">
            <div className="modal-content">
                <div className="modal-header">
                    <h5>Save changes?</h5>
                </div>
                <div className="modal-body">
                    <div className="form-group">
                        <label htmlFor="pageName">Page name</label>
                        <input type="text" className={"form-control" + (getValidation("pageName"))} id="pageName" value={pageNameInternal} onChange={(e) => {
                            setPageNameInternal(e.target.value);
                        }} />
                        {validation["pageName"] && <div className="invalid-feedback">{validation["pageName"]}</div>}
                    </div>
                    <div className="form-group">
                        <label htmlFor="category">Category</label>
                        <select className={"form-control" + (getValidation("category"))} id="category" value={categoryInternal} onChange={(e) => {
                            setCategoryInternal(e.target.value);
                        }}>
                            <option selected={categoryInternal === ""}>Select a category</option>
                            {categories.map((category) => {
                                return <option key={category} value={category} selected={category === categoryInternal}>{category}</option>
                            })}
                        </select>
                        {validation["category"] && <div className="invalid-feedback">{validation["category"]}</div>}
                    </div>
                    <div className="form-group">
                        <label htmlFor="expansion">Expansion</label>
                        <select className={"form-control" + (getValidation("expansion"))} id="expansion" value={expansionInternal} onChange={(e) => {
                            setExpansionInternal(e.target.value);
                        }}>
                            <option selected={expansionInternal === ""}>Select an expansion</option>
                            {expansions.map((expansion) => {
                                return <option key={expansion} value={expansion} selected={expansion === expansionInternal}>{expansion}</option>
                            })}
                        </select>
                        {validation["expansion"] && <div className="invalid-feedback">{validation["expansion"]}</div>}
                    </div>
                </div>
                <div className="modal-footer">
                    <button className="btn btn-primary" onClick={() => save()}>Save</button>
                    <button className="btn btn-secondary" onClick={() => modal(null)}>Cancel</button>
                </div>
            </div>
        </div>
    );
}
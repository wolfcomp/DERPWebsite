import MarkdownEditor from "@uiw/react-markdown-editor";
import hljs from "highlight.js";
import DOMPurify from "dompurify";
import { parse } from "marked";
import { rehype } from "rehype";
import rehypeMinifyAttributeWhitespace from "rehype-minify-attribute-whitespace";
import rehypeMinifyEnumeratedAttribute from "rehype-minify-enumerated-attribute";
import rehypeMinifyLanguage from "rehype-minify-language";
import rehypeMinifyMetaContent from "rehype-minify-meta-content";
import rehypeMinifyWhitespace from "rehype-minify-whitespace";
import rehypeNormalizeAttributeValueCase from "rehype-normalize-attribute-value-case";
import rehypeRemoveComments from "rehype-remove-comments";
import rehypeRemoveDuplicateAttributeValues from "rehype-remove-duplicate-attribute-values";
import rehypeRemoveEmptyAttribute from "rehype-remove-empty-attribute";
import rehypeRemoveMetaHttpEquiv from "rehype-remove-meta-http-equiv";
import rehypeRemoveStyleTypeCss from "rehype-remove-style-type-css";
import rehypeSortAttributeValues from "rehype-sort-attribute-values";
import rehypeSortAttributes from "rehype-sort-attributes";

import { useEffect, useRef, useState } from "react";
import { useRequest } from "../components/request";
import { Statuses } from "../components/status";
import { useModal } from "../components/modal";
import { useToast } from "../components/toast";
import { Category, Tier, Resource, ResourceFile } from "../structs/resource";
import "./editor.scss";
import "highlight.js/styles/monokai-sublime.css";
import { useNavigate, useParams } from "react-router-dom";
import { Carousel, Collapse, Dropdown, Modal, Offcanvas, Popover, ScrollSpy, Tab, Toast, Tooltip } from "bootstrap";

const sanitize = (source: string | Node) => rehype()
    .use(rehypeMinifyAttributeWhitespace)
    .use(rehypeMinifyEnumeratedAttribute)
    .use(rehypeMinifyLanguage)
    .use(rehypeMinifyMetaContent)
    .use(rehypeMinifyWhitespace)
    .use(rehypeNormalizeAttributeValueCase)
    .use(rehypeRemoveComments)
    .use(rehypeRemoveDuplicateAttributeValues)
    .use(rehypeRemoveEmptyAttribute)
    .use(rehypeRemoveMetaHttpEquiv)
    .use(rehypeRemoveStyleTypeCss)
    .use(rehypeSortAttributeValues)
    .use(rehypeSortAttributes)
    .processSync(DOMPurify.sanitize(source, { ADD_TAGS: ["iframe"] })).value.toString();

export default function Editor() {
    const [markdown, setMarkdown] = useState<string>('');
    const [prevMarkdown, setPrevMarkdown] = useState<string>('');
    const [statuses, setStatuses] = useState<Status[]>([]);
    const [display, setDisplay] = useState<"editor" | "preview">("editor");
    const [resource, setResource] = useState<Resource | null>(null);
    const [resourceFiles, setResourceFiles] = useState<ResourceFile[]>([]);
    const { id } = useParams();
    const divRef = useRef<HTMLDivElement>(null);
    const scrollRefs = useRef<Record<string, HTMLElement>>({});
    const request = useRequest().request;
    const modal = useModal();

    useEffect(() => {
        try {
            const html = parse(markdown);
            const clean = sanitize(html);

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
    }, [prevMarkdown]);

    useEffect(() => {
        if (resource)
            getFiles();
    }, [resource]);

    useEffect(() => {
        getStatuses();
    }, [setStatuses])

    useEffect(() => {
        if (id)
            getResource();
    }, [id]);

    async function getFiles() {
        const res = await request(`/api/resources/${resource?.id}/files`);
        const files = await res.json() as ResourceFile[];
        setResourceFiles(files);
    }

    async function getResource() {
        const res = await request(`/api/resources/${id}`);
        const data = await res.json() as Resource;
        setResource(data);
        setMarkdown(data.markdownContent);
    }

    async function getStatuses() {
        const res = await request("/api/status");
        if (!res.ok)
            return;
        const statuses = await res.json() as Status[];
        setStatuses(statuses);
    }

    return (
        <div className="container">
            <div className={`editor-extra ${display === "editor" ? "" : "hidden"}`}>
                <Statuses statuses={statuses} scale={0.5} />
                <div className="row mt-2 bg-body">
                    <div className="d-flex justify-content-between pt-2 pb-2 align-items-center border-bottom">
                        <span className="fw-bold">Images</span>
                        <div>
                            {!resource && <em>Must save before image upload is available.</em>}
                            <button className="btn btn-primary ms-2" disabled={!resource} onClick={(e) => {
                                e.preventDefault();
                                modal(<UploadFileModal id={resource?.id || null} onUpload={() => getFiles()} />);
                            }}>Upload</button>
                        </div>
                    </div>
                    <FileList files={resourceFiles} onDelete={() => getFiles()} />
                </div>
            </div>
            <div className="d-flex justify-content-between align-items-center mt-4">
                <button className="btn btn-primary" onClick={(e) => {
                    e.preventDefault();
                    setDisplay(display === "editor" ? "preview" : "editor");
                }}>{display === "editor" ? "Change to preview" : "Change to editor"}</button>
                <button className="btn btn-primary" onClick={(e) => {
                    e.preventDefault();
                    modal(<EditorSaveModal markdown={markdown} tier={resource?.category.hasTiers ? resource?.tier?.id || null : null} category={resource?.category.id || null} id={resource?.id || null} pageName={resource?.pageName || null} onSave={getResource} />);
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

function TimelineEditor({ timeline }: { timeline: { name: string }[] }) {

}

function EditorSaveModal({ pageName, markdown, category, tier, id, onSave }: { pageName?: string, markdown: string, category?: string, tier?: string, id?: string, onSave: () => void }) {
    const modal = useModal();
    const request = useRequest().request;
    const toast = useToast().toast;
    const navigate = useNavigate();
    const [pageNameInternal, setPageNameInternal] = useState<string>(pageName || "");
    const [categoryInternal, setCategoryInternal] = useState<string>(category || "");
    const [tierInternal, setTierInternal] = useState<string>(tier || "");
    const [categories, setCategories] = useState<Category[]>([]);
    const [tiers, setTiers] = useState<Tier[]>([]);
    const [validation, setValidation] = useState<Record<string, string>>({});
    const allowedTitleChars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-_ ()!@#$%^&*+=[]{}|;:,./<>?~`";
    const charLimit = 50;

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
        if (!categories.map(t => t.id).includes(categoryInternal)) {
            validationInternal["category"] = `Selected category does not exist`;
        }
        if (validationInternal["category"] === undefined) {
            validationInternal["category"] = "";
        }
        if (categoryInternal !== "" && categories.find(c => c.id === categoryInternal).hasTiers) {
            if (!tierInternal) {
                validationInternal["tier"] = "Tier cannot be empty";
            }
            if (!tiers.map(t => t.id).includes(tierInternal)) {
                validationInternal["tier"] = `Selected tier does not exist`;
            }
            if (validationInternal["tier"] === undefined) {
                validationInternal["tier"] = "";
            }
        }
        setValidation(validationInternal);
        for (const key in validationInternal) {
            if (validationInternal[key].length > 0) {
                throw new Error(validationInternal[key]);
            }
        }
    }

    async function save() {
        try {
            validate();
            const html = parse(markdown);
            const clean = sanitize(html);
            var res: Resource =
            {
                categoryId: categoryInternal,
                htmlContent: clean,
                markdownContent: markdown,
                pageName: pageNameInternal
            }
            if (categoryInternal !== "" && categories.find(c => c.id === categoryInternal).hasTiers)
                res.tierId = tierInternal;
            if (id)
                res.id = id;
            const req = await request("/api/resources", {
                method: "POST",
                headers: {
                    "Content-Type": "application/json"
                },
                body: JSON.stringify(res)
            });
            res = await req.json() as Resource;
            modal(null);
            navigate("/editor/" + res.id, { replace: true });
            onSave();
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
        setCategories(categories);
    }

    useEffect(() => {
        getCategories();
    }, [setCategories]);

    async function getTiers() {
        const res = await request("/api/resources/tiers");
        if (!res.ok)
            return;
        const tiers = await res.json() as Tier[];
        setTiers(tiers);
    }

    useEffect(() => {
        getTiers();
    }, [setTiers]);

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
                                return <option key={category.id} value={category.id} selected={category.id === categoryInternal}>{category.name}</option>
                            })}
                        </select>
                        {validation["category"] && <div className="invalid-feedback">{validation["category"]}</div>}
                    </div>
                    {categoryInternal !== "" && categories.find(c => c.id === categoryInternal) && categories.find(c => c.id === categoryInternal).hasTiers &&
                        <div className="form-group">
                            <label htmlFor="tier">Tier</label>
                            <select className={"form-control" + (getValidation("tier"))} id="tier" value={tierInternal} onChange={(e) => {
                                setTierInternal(e.target.value);
                            }}>
                                <option selected={tierInternal === ""}>Select a tier</option>
                                {tiers.filter(t => t.category.id === categoryInternal).map((tier) => {
                                    return <option key={tier.id} value={tier.id} selected={tier.id === tierInternal}>{tier.name}</option>
                                })}
                            </select>
                            {validation["tier"] && <div className="invalid-feedback">{validation["tier"]}</div>}
                        </div>}
                </div>
                <div className="modal-footer">
                    <button className="btn btn-primary" onClick={() => save()}>Save</button>
                    <button className="btn btn-secondary" onClick={() => modal(null)}>Cancel</button>
                </div>
            </div>
        </div>
    );
}

function UploadFileModal({ id, onUpload }: { id: string | null, onUpload: () => void }) {
    const request = useRequest().request;
    const toast = useToast().toast;
    const modal = useModal();
    const [file, setFile] = useState<File | null>(null);

    async function upload() {
        if (!id) {
            toast("No resource id provided", "UploadFileModal", "error");
            return;
        }
        if (!file) {
            toast("No files provided", "UploadFileModal", "error");
            return;
        }
        const formData = new FormData();
        formData.append("file", file);
        formData.append("id", id);
        const res = await request(`/api/resources/upload`, {
            method: "POST",
            body: formData
        });
        if (!res.ok) {
            toast("Failed to upload files", "UploadFileModal", "error");
            return;
        }
        onUpload();
    }

    return (
        <div className="modal-dialog">
            <div className="modal-content">
                <div className="modal-header">
                    <h5>Upload files</h5>
                </div>
                <div className="modal-body">
                    <div className="form-group">
                        <label htmlFor="files">Files</label>
                        <input type="file" className="form-control" id="files" onChange={(e) => {
                            setFile(e.currentTarget.files[0]);
                        }} />
                    </div>
                </div>
                <div className="modal-footer">
                    <button className="btn btn-primary" onClick={() => upload()}>Upload</button>
                    <button className="btn btn-secondary" onClick={() => modal(null)}>Close</button>
                </div>
            </div>
        </div>
    );

}

function FileList({ files, onDelete }: { files: ResourceFile[], onDelete: () => void }) {
    const size = 10;
    const [page, setPage] = useState<number>(0);
    const [filesShown, setFilesShown] = useState<ResourceFile[]>(files.slice(0, size));

    useEffect(() => {
        setFilesShown(files.slice(page * size, (page + 1) * size));
    }, [page, files]);

    return (
        <div className="mt-2 mb-2">
            <div className="d-flex flex-wrap">
                {filesShown.map((file) => <FileCard key={file.id} file={file} onDelete={onDelete} />)}
            </div>
            <div className="d-flex justify-content-between mt-2">
                <button className="btn btn-primary" disabled={page === 0} onClick={() => setPage(page - 1)}>Previous page</button>
                <button className="btn btn-primary" disabled={(page + 1) * size >= files.length} onClick={() => setPage(page + 1)}>Next page</button>
            </div>
        </div>
    );
}

function FileCard({ file, onDelete }: { file: ResourceFile, onDelete: () => void }) {
    const request = useRequest().request;
    const toast = useToast().toast;

    async function deleteFile() {
        const res = await request(`/api/resources/files/${file.id}`, {
            method: "DELETE"
        });
        if (!res.ok) {
            toast("Failed to delete file", "FileCard", "error");
            return;
        }
        onDelete();
    }

    return (
        <div className="card me-2 mb-2">
            <div className="card-body">
                <img src={`https://pdp.wildwolf.dev/files/guides/${file.path}`} alt={file.name} style={{ maxWidth: "200px", maxHeight: "125px" }} />
                <div className="d-flex justify-content-between mt-2">
                    <button className="btn btn-primary" onClick={(e) => {
                        e.preventDefault();
                        var html = `<img class="d-inline-block" src="https://pdp.wildwolf.dev/files/guides/${file.path}" alt="${file.name}" style="max-width: 600px; max-height: 300px;" />`
                        navigator.clipboard.writeText(html);
                        toast("Copied HTML to clipboard", "FileCard", "success");
                    }}>Copy HTML</button>
                    <button className="btn btn-danger" onClick={(e) => {
                        e.preventDefault();
                        deleteFile();
                    }}>Delete</button>
                </div>
            </div>
        </div>
    );
}
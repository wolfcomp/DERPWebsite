import { MouseEventHandler, useEffect, useRef, useState } from "react";
import { Category, Expansion, Resource } from "../structs/resource";
import { useRequest } from "../components/request";
import { useAuth } from "../components/auth";
import { useNavigate, useParams, useSearchParams } from "react-router-dom";
import hljs from "highlight.js";
import "highlight.js/styles/monokai-sublime.css";
import { useToast } from "../components/toast";
import { Carousel, Collapse, Dropdown, Modal, Offcanvas, Popover, ScrollSpy, Tab, Toast, Tooltip } from "bootstrap";

export default function Resources() {
    const [resources, setResources] = useState<Resource[]>([]);
    const [expansions, setExpansions] = useState<Expansion[]>([]);
    const [categories, setCategories] = useState<Category[]>([]);
    const [selectedExpansion, setSelectedExpansion] = useState<Expansion | null>(null);
    const [selectedCategory, setSelectedCategory] = useState<Category | null>(null);
    const [selectedResource, setSelectedResource] = useState<Resource | null>(null);
    const { categoryId, expansionId, resourceId } = useParams();
    const request = useRequest().request;
    const toast = useToast().toast;
    const navigate = useNavigate();
    const auth = useAuth();

    async function getResources() {
        var res = await request("/api/resources");
        var data = await res.json() as Resource[];
        setResources(data);
    }

    async function deleteResource(id: string) {
        var res = await request(`/api/resources/${id}`, {
            method: "DELETE"
        });
        if (res.ok) {
            getResources();
        }
    }

    async function publishResource(id: string) {
        var resource = resources.find(resource => resource.id === id);
        resource.publish = true;
        var res = await request(`/api/resources`, {
            method: "POST",
            headers: {
                "Content-Type": "application/json"
            },
            body: JSON.stringify(resource)
        });
        if (res.ok) {
            getResources();
        }
    }

    async function getExpansions() {
        var res = await request("/api/resources/expansions");
        var data = await res.json() as Expansion[];
        setExpansions(data);
        if (expansionId) {
            var expansion = data.find(t => t.id === expansionId);
            if (expansion) {
                setSelectedExpansion(expansion);
            }
        }
    }

    async function getCategories() {
        var res = await request("/api/resources/categories");
        var data = await res.json() as Category[];
        setCategories(data);
        if (categoryId) {
            var category = data.find(t => t.id === categoryId);
            if (category) {
                setSelectedCategory(category);
            }
        }
    }

    async function getFullResource(id: string) {
        var res = await request(`/api/resources/${id}`);
        var data = await res.json() as Resource;
        setSelectedResource(data);
    }

    useEffect(() => {
        getResources();
    }, [setResources]);

    useEffect(() => {
        getExpansions();
    }, [setExpansions]);

    useEffect(() => {
        getCategories();
    }, [setCategories]);

    useEffect(() => {
        var sParams = "";
        if (selectedExpansion !== null)
            sParams += `${selectedExpansion.id}`;
        if (selectedCategory !== null) {
            if (sParams !== "")
                sParams += "/";

            sParams += `${selectedCategory.id}`;
        }
        if (selectedResource !== null) {
            if (sParams !== "")
                sParams += "/";

            sParams += `${selectedResource.id}`;
        }
        navigate(`/resources/${sParams}`, { replace: true });
    }, [selectedExpansion, selectedCategory, selectedResource]);

    useEffect(() => {
        if (resourceId) {
            getFullResource(resourceId);
        }
    }, [resourceId]);

    function getFilteredResources(withExpansion: boolean = true, withCategory: boolean = true) {
        if (!withExpansion) {
            if (auth.user)
                return resources;
            return resources.filter(resource => resource.published);
        }
        if (!withCategory) {
            if (auth.user)
                return resources.filter(resource => resource.expansion.id === selectedExpansion.id);
            return resources.filter(resource => resource.expansion.id === selectedExpansion.id && resource.published);
        }
        if (auth.user)
            return resources.filter(resource => resource.expansion.id === selectedExpansion.id && resource.category.id === selectedCategory.id);
        return resources.filter(resource => resource.expansion.id === selectedExpansion.id && resource.category.id === selectedCategory.id && resource.published);
    }

    return (
        <div className="container mt-4">
            <div className="d-flex">
                {selectedResource && <>
                    <div className="me-auto d-flex align-content-center flex-wrap" style={{ cursor: "pointer" }} onClick={() => {
                        setSelectedResource(null);
                    }}>
                        <i className="bi bi-arrow-90deg-up me-2"></i>
                        <h5>Back</h5>
                    </div>
                    <h1 className="me-2">{selectedResource.pageName}</h1>
                    <div className="d-flex align-content-center flex-wrap">
                        {auth.user && <button className="btn btn-primary" onClick={() => {
                            navigate(`/editor/${selectedResource.id}`);
                        }}>Edit</button>}
                        <button className="btn btn-success ms-2" onClick={(e) => {
                            e.preventDefault();
                            var loc = `https://pdp.wildwolf.dev/resources/${selectedExpansion.id}/${selectedCategory.id}/${selectedResource.id}`;
                            navigator.clipboard.writeText(loc);
                            toast("Copied link to clipboard", "Resources", "success");
                        }}>Share</button>
                    </div>
                </>}
                {!selectedResource && <>
                    <h1 className="me-auto">Resources</h1>
                    <div className="d-flex align-items-center">
                        <button className="btn btn-primary" onClick={() => {
                            navigate("/editor");
                        }}>New Resource</button>
                    </div>
                </>}
            </div>
            {selectedResource && <ResourceRender resource={selectedResource} />}
            {!selectedResource &&
                <div className="row d-flex justify-content-center">
                    {!selectedExpansion && expansions.map((expansion, i) => <ExpansionCard key={`expansion-${i}`} expansion={expansion} resources={getFilteredResources(false)} onClick={(e) => {
                        e.preventDefault();
                        setSelectedExpansion(expansion);
                    }} />)}
                    {selectedExpansion && !selectedCategory &&
                        <>
                            <div className="row">
                                <div className="d-flex justify-content-between align-items-center">
                                    <div className="me-auto d-flex align-content-center flex-wrap" style={{ cursor: "pointer" }} onClick={() => {
                                        setSelectedExpansion(null);
                                    }}>
                                        <i className="bi bi-arrow-90deg-up me-2"></i>
                                        <h5>Back</h5>
                                    </div>
                                    <h2>{selectedExpansion.name}</h2>
                                </div>
                            </div>
                            {categories.filter(t => getFilteredResources(true, false).flatMap(t => t.category.id).includes(t.id) || auth.user).map((category, i) => <CategoryCard key={`category-${i}`} category={category} resources={getFilteredResources(true, false)} onClick={(e) => {
                                e.preventDefault();
                                setSelectedCategory(category);
                            }} />)}
                        </>
                    }
                    {selectedCategory &&
                        <>
                            <div className="row">
                                <div className="d-flex justify-content-between align-items-center">
                                    <div className="me-auto d-flex align-content-center flex-wrap" style={{ cursor: "pointer" }} onClick={() => {
                                        setSelectedCategory(null);
                                    }}>
                                        <i className="bi bi-arrow-90deg-up me-2"></i>
                                        <h5>Back</h5>
                                    </div>
                                    <h2>{selectedExpansion.name} &gt; {selectedCategory.name}</h2>
                                </div>
                            </div>
                            <div className="row">
                                <ul className="list-group">
                                    {getFilteredResources().map((resource, i) => <ResourceItem key={`resource-${i}`} resource={resource} onDelete={(e) => {
                                        e.preventDefault();
                                        deleteResource(resource.id);
                                    }} onPublish={(e) => {
                                        e.preventDefault();
                                        publishResource(resource.id)
                                    }} onView={(e) => {
                                        e.preventDefault();
                                        getFullResource(resource.id);
                                    }} />)}
                                </ul>
                            </div>
                        </>
                    }
                </div>
            }
        </div>
    )
}

function ExpansionCard({ expansion, onClick, resources }: { expansion: Expansion, onClick: React.MouseEventHandler<HTMLDivElement>, resources: Resource[] }) {
    return (
        <div className="card col-3 m-2 pt-2" onClick={onClick}>
            <img src={expansion.iconUrl} className="card-img-top" alt={expansion.name} />
            <div className="card-body">
                <p className="card-text">{expansion.description}</p>
                <sub className="card-text">{resources.filter(t => t.expansion.id === expansion.id).length} resources</sub>
            </div>
        </div>
    );
}

function CategoryCard({ category, onClick, resources }: { category: Category, onClick: React.MouseEventHandler<HTMLDivElement>, resources: Resource[] }) {
    return (
        <div className="card col-3 m-2 pt-2" onClick={onClick}>
            <div className="card-body">
                <h5 className="card-title"><img src={category.iconUrl} alt={category.name} className="me-2" style={{ width: 48 }} />{category.name}</h5>
                <p className="card-text">{category.description}</p>
                <sub className="card-text">{resources.filter(t => t.category.id === category.id).length} resources</sub>
            </div>
        </div>
    );
}

function ResourceItem({ resource, onDelete, onPublish, onView }: { resource: Resource, onDelete: MouseEventHandler<HTMLButtonElement>, onPublish: MouseEventHandler<HTMLButtonElement>, onView: MouseEventHandler<HTMLHeadingElement> }) {
    const auth = useAuth();
    const navigate = useNavigate();
    return (
        <li className="list-group-item d-flex justify-content-between align-items-center">
            {resource.published && <h4 className="m-0 mt-auto mb-auto" onClick={onView} style={{ cursor: "pointer", flexGrow: 1 }}>{resource.pageName}</h4>}
            {!resource.published && <h4 className="d-flex align-items-center m-0 mt-auto mb-auto" onClick={onView} style={{ cursor: "pointer", flexGrow: 1 }}>{resource.pageName} <span className="ms-2" style={{ fontSize: "0.75rem", fontStyle: "italic" }}>Not published</span></h4>}
            <div>
                {auth.user && <button className="btn btn-primary me-2" onClick={() => {
                    navigate(`/editor/${resource.id}`);
                }}>Edit</button>}
                {auth.user && <button className="btn btn-danger me-2" onClick={onDelete}>Delete</button>}
                {auth.user && !resource.published && <button className="btn btn-primary" onClick={onPublish}>Publish</button>}
            </div>
        </li>
    )
}

function ResourceRender({ resource }: { resource: Resource }) {
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
import { MouseEventHandler, lazy, useEffect, useState, Dispatch, SetStateAction } from "react";
import { Category, Tier, Resource } from "../structs/resource";
import { useRequest } from "../components/request";
import { useAuth } from "../components/auth";
import { useNavigate, useParams } from "react-router-dom";
import "highlight.js/styles/monokai-sublime.css";
import { useToast } from "../components/toast";
import { useModal } from "../components/modal";
const ResourceRender = lazy(() => import("../components/resourceViewer"));

export default function Resources() {
    const [resources, setResources] = useState<Resource[]>([]);
    const [categories, setCategories] = useState<Category[]>([]);
    const [tiers, setTiers] = useState<Tier[]>([]);
    const [selectedCategory, setSelectedCategory] = useState<Category | null>(null);
    const [selectedResource, setSelectedResource] = useState<Resource | null>(null);
    const [selectedTier, setSelectedTier] = useState<Tier | null>(null);
    const [hasNavigated, setHasNavigated] = useState(false);
    const { categoryId, tierId, resourceId } = useParams();
    const request = useRequest().request;
    const toast = useToast().toast;
    const modal = useModal();
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

    async function getTiers() {
        var res = await request("/api/resources/tiers");
        var data = await res.json() as Tier[];
        setTiers(data);
        if (tierId) {
            var tier = data.find(t => t.id === tierId);
            if (tier) {
                setSelectedTier(tier);
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
        if (resourceId) {
            getFullResource(resourceId);
        }
        if (selectedCategory && !selectedCategory.hasTiers && tierId) {
            getFullResource(tierId);
        }
    }, [resourceId, selectedCategory]);

    useEffect(() => {
        getResources();
    }, [setResources]);

    useEffect(() => {
        getTiers();
    }, [setTiers]);

    useEffect(() => {
        getCategories();
    }, [setCategories]);

    useEffect(() => {
        var sParams = "";
        if (selectedResource !== null) {
            sParams += `${selectedCategory.id}`
            if (selectedResource.category.hasTiers)
                sParams += `/${selectedResource.tier.id}`;

            sParams += `/${selectedResource.id}`;
        }
        else {
            if (selectedCategory !== null)
                sParams += `${selectedCategory.id}`;
            else if (categoryId && !hasNavigated)
                sParams += `${categoryId}`;

            if (selectedTier !== null) {
                sParams += `/${selectedTier.id}`;
            }
            else if (tierId && !hasNavigated)
                sParams += `/${tierId}`;
        }
        navigate(`/resources/${sParams}`, { replace: true });
    }, [selectedTier, selectedCategory, selectedResource]);

    function getFilteredResources(withCategory: boolean = true, withTier: boolean = true, skipTierCheck: boolean = false) {
        var filterPredicate = (resource: Resource) => true;
        if (!withCategory) {
            filterPredicate = (resource: Resource) => (auth.user) ? true : resource.published;
        }
        else if (!withTier) {
            filterPredicate = (resource: Resource) => resource.category.id === selectedCategory.id && (auth.user) ? true : resource.published;
        }
        else {
            filterPredicate = (resource: Resource) => resource.category.id === selectedCategory.id && (skipTierCheck || (resource.category.hasTiers && resource.tier != null && resource.tier.id === selectedTier.id)) && (auth.user) ? true : resource.published;
        }
        return resources.filter(filterPredicate);
    }

    function getTopView(func: Dispatch<SetStateAction<any>>) {
        var title = "";
        if (selectedCategory)
            title = selectedCategory.name;
        if (selectedCategory.hasTiers && selectedTier)
            title = title.concat(` > ${selectedTier.name}`);

        return (
            <div className="row">
                <div className="d-flex justify-content-between align-items-center">
                    <div className="me-auto d-flex align-content-center flex-wrap" style={{ cursor: "pointer" }} onClick={() => {
                        func(null);
                        setHasNavigated(true);
                    }}>
                        <i className="bi bi-arrow-90deg-up me-2"></i>
                        <h5>Back</h5>
                    </div>
                    <h2>{title}</h2>
                </div>
            </div>
        )
    }

    return (
        <div className="container mt-4">
            <div className="d-flex">
                {selectedResource && <>
                    <div className="me-auto d-flex align-content-center flex-wrap" style={{ cursor: "pointer" }} onClick={() => {
                        setSelectedResource(null);
                        setHasNavigated(true);
                    }}>
                        <i className="bi bi-arrow-90deg-up me-2"></i>
                        <h5>Back</h5>
                    </div>
                    <h1 className="me-2">{selectedResource.pageName}</h1>
                    <div className="d-flex align-content-center flex-wrap">
                        {auth.user &&
                            <button className="btn btn-primary" onClick={() => {
                                navigate(`/editor/${selectedResource.id}`);
                            }}>Edit</button>
                        }
                        <button className="btn btn-success ms-2" onClick={(e) => {
                            e.preventDefault();
                            var loc = `https://pdp.wildwolf.dev/resources/${selectedCategory.id}/${selectedTier.id}/${selectedResource.id}`;
                            navigator.clipboard.writeText(loc);
                            toast("Copied link to clipboard", "Resources", "success");
                        }}>Share</button>
                    </div>
                </>}
                {!selectedResource && <>
                    <h1 className="me-auto">Resources</h1>
                    <div className="d-flex align-items-center">
                        {auth.user && <>
                            {selectedCategory && selectedCategory.hasTiers && <button className="btn btn-primary" onClick={() => {
                                modal(<AddTierModal category={selectedCategory} onSubmit={getTiers} />);
                            }}>New Tier</button>}
                            <button className="btn btn-primary ms-3" onClick={() => {
                                navigate("/editor");
                            }}>New Resource</button>
                        </>}
                    </div>
                </>}
            </div>
            {selectedResource && <ResourceRender resource={selectedResource} />}
            {!selectedResource &&
                <div className="row d-flex justify-content-center">
                    {!selectedCategory &&
                        categories.map((category, i) => <CategoryCard key={`category-${i}`} category={category} resources={getFilteredResources(false, false)} onClick={(e) => {
                            e.preventDefault();
                            setSelectedCategory(category);
                        }} />)
                    }
                    {selectedCategory && selectedCategory.hasTiers && !selectedTier &&
                        <>
                            {getTopView(setSelectedCategory)}
                            {tiers && <TierView tiers={tiers} onClick={setSelectedTier} category={selectedCategory} resources={getFilteredResources(true, false)} />}
                        </>
                    }
                    {selectedCategory && (!selectedCategory.hasTiers || selectedTier) &&
                        <>
                            {getTopView(selectedCategory.hasTiers ? setSelectedTier : setSelectedCategory)}
                            <ResourceView resources={getFilteredResources(true, true, !selectedCategory.hasTiers)} onDelete={deleteResource} onPublish={publishResource} onView={getFullResource} />
                        </>
                    }
                </div>
            }
        </div>
    )
}

function TierView({ tiers, onClick, category, resources }: { tiers: Tier[], onClick: Dispatch<SetStateAction<Tier>>, category: Category, resources: Resource[] }) {
    return (
        <ul className="list-group">
            {tiers.filter(t => t.category.id === category.id).map((tier, i) => <TierItem key={`tier-${i}`} tier={tier} onClick={(e => {
                e.preventDefault();
                onClick(tier);
            })} resources={resources} />)}
        </ul>
    );
}

function TierItem({ tier, onClick, resources }: { tier: Tier, onClick: React.MouseEventHandler<HTMLLIElement>, resources: Resource[] }) {
    return (
        <li className="list-group-item d-flex align-items-center" onClick={onClick}>
            <img src={tier.iconUrl} style={{ maxHeight: 68 }} />
            <h5 className="me-auto mb-0 ms-2">{tier.name}</h5>
            <span>{resources.filter(t => t.tier.id === tier.id).length} resources</span>
        </li>
    )

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

function ResourceView({ resources, onDelete, onPublish, onView }: { resources: Resource[], onDelete: (e: string) => void, onPublish: (e: string) => void, onView: (e: string) => void }) {
    return (
        <div className="row">
            <ul className="list-group">
                {resources.map((resource, i) => <ResourceItem key={`resource-${i}`} resource={resource} onDelete={(e) => {
                    e.preventDefault();
                    onDelete(resource.id);
                }} onPublish={(e) => {
                    e.preventDefault();
                    onPublish(resource.id)
                }} onView={(e) => {
                    e.preventDefault();
                    onView(resource.id);
                }} />)}
            </ul>
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

function AddTierModal({ category, onSubmit }: { category: Category, onSubmit: () => void }) {
    const [name, setName] = useState("");
    const [iconUrl, setIconUrl] = useState("");
    const [path, setPath] = useState("");
    const [validation, setValidation] = useState<Record<string, string>>({});
    const request = useRequest().request;
    const modal = useModal();
    const toast = useToast().toast;
    const allowedTitleChars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-_ ()!@#$%^&*+=[]{}|;:,./<>?~`";
    const allowedPathChars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-_";
    const charLimit = 50;

    function validate() {
        var validationInternal: Record<string, string> = {};
        if (!name) {
            validationInternal["name"] = "Name cannot be empty";
        }
        if (name.length > charLimit) {
            validationInternal["name"] = `Name cannot be longer than ${charLimit} characters`;
        }
        if (name.length < 3) {
            validationInternal["name"] = "Name cannot be shorter than 3 characters";
        }
        for (const char of name) {
            if (!allowedTitleChars.includes(char)) {
                validationInternal["name"] = `Name cannot contain '${char}'`;
            }
        }
        if (validationInternal["name"] === undefined) {
            validationInternal["name"] = "";
        }
        if (!iconUrl) {
            validationInternal["iconUrl"] = "Icon URL cannot be empty";
        }
        if (validationInternal["iconUrl"] === undefined) {
            validationInternal["iconUrl"] = "";
        }
        if (!path) {
            validationInternal["path"] = "Path cannot be empty";
        }
        for (const char of path) {
            if (!allowedPathChars.includes(char)) {
                validationInternal["path"] = `Path cannot contain '${char}'`;
            }
        }
        if (validationInternal["path"] === undefined) {
            validationInternal["path"] = "";
        }
        setValidation(validationInternal);
        for (const key in validationInternal) {
            if (validationInternal[key].length > 0) {
                throw new Error(validationInternal[key]);
            }
        }
    }

    function createTier() {
        try {
            validate();
            var tier = {
                name: name,
                iconUrl: iconUrl,
                categoryId: category.id,
                path: path
            }
            request("/api/resources/tiers", {
                method: "POST",
                headers: {
                    "Content-Type": "application/json"
                },
                body: JSON.stringify(tier)
            }).then(res => {
                if (res.ok) {
                    modal(null);
                    onSubmit();
                }
            });
        }
        catch (e) {
            if (e instanceof Error) {
                toast(e.message, "EditorSaveModal", "error");
            }
            return;
        }
    }

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
                    <h5>Create Tier for <i>{category.name}</i></h5>
                </div>
                <div className="modal-body">
                    <div className="form-group">
                        <label htmlFor="name">Name</label>
                        <input type="text" className={"form-control" + (getValidation("name"))} id="name" onChange={(e) => {
                            e.preventDefault();
                            setName(e.target.value);
                        }} />
                        {validation["name"] && <div className="invalid-feedback">{validation["name"]}</div>}
                    </div>
                    <div className="form-group mt-2">
                        <label htmlFor="files">Icon URL<br /><sub>Use the file view and look either in game_icons/114000 or game_icons/112000</sub></label>
                        <input type="url" className={"form-control" + (getValidation("iconUrl"))} id="files" onChange={(e) => {
                            e.preventDefault();
                            setIconUrl(e.target.value);
                        }} />
                        {validation["iconUrl"] && <div className="invalid-feedback">{validation["iconUrl"]}</div>}
                    </div>
                    <div className="form-group mt-2">
                        <label htmlFor="path">Path<br /><sub>This is for where files are stored on the server</sub></label>
                        <input type="text" className={"form-control" + (getValidation("path"))} id="path" onChange={(e) => {
                            e.preventDefault();
                            setPath(e.target.value);
                        }} />
                        {validation["path"] && <div className="invalid-feedback">{validation["path"]}</div>}
                    </div>
                </div>
                <div className="modal-footer">
                    <button className="btn btn-primary" onClick={() => createTier()}>Upload</button>
                    <button className="btn btn-secondary" onClick={() => modal(null)}>Close</button>
                </div>
            </div>
        </div>
    )
}
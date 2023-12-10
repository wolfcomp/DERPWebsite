export interface Resource {
    id: string;
    pageName: string;
    category: Category;
    expansion: Expansion;
    hostId: string;
}

export interface Category {
    id: string;
    name: string;
    desccription: string;
    iconUrl: string;
}

export interface Expansion {
    id: string;
    name: string;
    desccription: string;
    iconUrl: string;
}
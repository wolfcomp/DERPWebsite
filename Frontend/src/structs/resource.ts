export interface Resource {
    publish?: boolean;
    published?: boolean;
    id?: string;
    pageName?: string;
    htmlContent?: string;
    markdownContent?: string;
    categoryId?: string;
    category?: Category;
    expansionId?: string;
    expansion?: Expansion;
    hostId?: string;
}

export interface Category {
    id: string;
    name: string;
    description: string;
    iconUrl: string;
}

export interface Expansion {
    id: string;
    name: string;
    description: string;
    iconUrl: string;
}

export interface ResourceFile {
    id: string;
    name: string;
    path: string;
}
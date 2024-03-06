export interface Resource {
    publish?: boolean;
    published?: boolean;
    id?: string;
    pageName?: string;
    htmlContent?: string;
    markdownContent?: string;
    categoryId?: string;
    category?: Category;
    tierId?: string;
    tier?: Tier;
    hostId?: string;
}

export interface Category {
    id: string;
    name: string;
    description: string;
    iconUrl: string;
    hasTiers: boolean;
    path: string;
}

export interface Tier {
    id: string;
    category: Category;
    categoryId: string;
    name: string;
    iconUrl: string;
    path: string;
}

export interface ResourceFile {
    id: string;
    name: string;
    path: string;
}
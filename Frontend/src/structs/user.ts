export interface User {
    id: string;
    description: string;
    roleName: string;
    roleColor: string;
    roles: Role[];
    avatar: string;
    originalName: string;
    visualName: string | null;
}

interface Role {
    id: string;
    name: string;
    color: string;
}
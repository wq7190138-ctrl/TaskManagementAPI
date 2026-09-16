export type ProjectStatus = 0 | 1 | 2 | 3

// 响应体
export interface Project {
    id: number;
    name: string;
    createdAt: string;
    status: ProjectStatus;
    statusUpdateAt?: string | null;
}

// 创建项目请求体
export interface CreateProjectDto {
    name: string;
}

// 更新项目请求体
export interface UpdateProjectDto {
    name: string;
}

// 变更项目请求体
export interface UpdateProjectStatusDto {
    status: ProjectStatus
}
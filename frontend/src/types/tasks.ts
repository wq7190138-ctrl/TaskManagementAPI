export type TaskStatus = 0 | 1 | 2;
export type TaskPriority = 0 | 1;

// 响应体
export interface Task {
    id: number;
    title: string;
    description?: string | null;
    status: TaskStatus;
    priority: TaskPriority;
    createdAt: string;
    updatedAt?: string | null;
    dueDate?: string | null;
    projectId: number;
}

// 创建任务请求体
export interface CreateTaskDto {
    title: string;
    description?: string | null;
    priority: TaskPriority;
    dueDate?: string | null;
}

// 更新任务请求体
export interface UpdateTaskDto {
    title: string;
    description?: string | null;
    priority: TaskPriority;
    dueDate?: string | null;
}

// 变更任务请求体
export interface UpdateTaskStatusDto {
    status: TaskStatus;
}

// ===== 常量映射 =====
export const STATUS_MAP: Record<TaskStatus, string> = {
    0: '待办',
    1: '进行中',
    2: '已完成',
}

export const PRIORITY_MAP: Record<TaskPriority, string> = {
    0: '普通',
    1: '紧急',
}
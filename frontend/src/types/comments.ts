// 响应体
export interface Comment {
    id: number;
    content: string;
    createdAt: string;
    createdBy: string;
    taskId: number;
}

export interface CreateCommentDto {
    content: string
    createdBy: string
}

// src/api/projectsApi.ts
import { baseApi } from './baseApi'
import type { Comment, CreateCommentDto } from '../types'

export const commentsApi = baseApi.injectEndpoints({
    endpoints: (builder) => ({
        // 获取评论列表
        getComments: builder.query<Comment[], number>({
            query: (taskId) => `/tasks/${taskId}/comments`,
            providesTags: (result) =>
                result
                    ? [
                        ...result.map(({ id }) => ({ type: 'Comment' as const, id })),
                        { type: 'Comment', id: 'LIST' },
                    ]
                    : [{ type: 'Comment', id: 'LIST' }],
        }),

        // 创建评论
        createComment: builder.mutation<Comment, { taskId: number, body: CreateCommentDto }>({
            query: ({ taskId, body }) => ({
                url: `/tasks/${taskId}/comments`,
                method: 'POST',
                body,
            }),
            // ✅ 乐观更新：立即添加到列表缓存
            async onQueryStarted({ taskId, body }, { dispatch, queryFulfilled }) {
                // 乐观更新：立即把新任务添加到列表
                const patchResult = dispatch(
                    commentsApi.util.updateQueryData('getComments', taskId, (draft) => {
                        // 生成临时 ID（后端会返回真实 ID）
                        const tempId = Date.now()
                        draft.push({
                            id: tempId,
                            content: body.content,
                            createdAt: new Date().toISOString(),
                            createdBy: body.createdBy,
                            taskId: taskId,
                        })
                    })
                )

                try {
                    // 等待请求完成
                    const { data } = await queryFulfilled
                    // 用后端返回的真实数据替换临时数据
                    dispatch(
                        commentsApi.util.updateQueryData('getComments', taskId, (draft) => {
                            const index = draft.findIndex(t => t.id === data.id)
                            if (index !== -1) {
                                draft[index] = data
                            } else {
                                draft.push(data)
                            }
                        })
                    )
                } catch {
                    // 请求失败，回滚
                    patchResult.undo()
                }
            },
            invalidatesTags: [{ type: 'Comment', id: 'LIST' }],
        }),

        // 删除评论
        deleteComment: builder.mutation<void, { id: number; taskId: number }>({
            query: ({ id }) => ({
                url: `/comments/${id}`,
                method: 'DELETE',
            }),
            // ✅ 乐观更新：直接从列表中移除
            async onQueryStarted({ id, taskId }, { dispatch, queryFulfilled }) {
                const patchResult = dispatch(
                    commentsApi.util.updateQueryData('getComments', taskId, (draft) => {
                        const index = draft.findIndex(task => task.id === id)
                        if (index !== -1) {
                            draft.splice(index, 1)
                        }
                    })
                )

                try {
                    await queryFulfilled
                } catch {
                    patchResult.undo()
                }
            },
            invalidatesTags: (_result, _error, { id }) => [
                { type: 'Comment', id },
                { type: 'Comment', id: 'LIST' },
            ],
        }),
    }),
})

// 自动生成的 hooks
export const {
    useGetCommentsQuery,
    useCreateCommentMutation,
    useDeleteCommentMutation,
} = commentsApi
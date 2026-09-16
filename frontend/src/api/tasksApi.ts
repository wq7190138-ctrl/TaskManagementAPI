// providesTags 声明缓存数据带什么标签
// invalidatesTags 告诉 RTK Query 哪些缓存失效了

import { baseApi } from './baseApi'
import type {
    Task,
    CreateTaskDto,
    UpdateTaskDto,
    UpdateTaskStatusDto
} from '../types'

export const tasksApi = baseApi.injectEndpoints({
    endpoints: (builder) => ({
        // 获取任务列表
        getTasks: builder.query<Task[], number>({
            query: (projectId) => `/projects/${projectId}/tasks`,

            // 设置缓存，一个LIST整体列表缓存，一个给每个任务建立的单独缓存，
            // LIST缓存在增删改执行后100%失效并重新请求
            // 任务单独缓存与getTasksById的任务单独缓存保持同步
            // 当增删改导致任务单独缓存失效时，用于乐观更新和局部更新！
            // 乐观更新，用户体验极佳，增删改会立刻更新界面，而不需要等待请求完成后更新
            // onQueryStarted，当api执行开始时触发，传入该api的所有参数及{ dispatch, queryFulfilled }RTK Query 提供的工具
            // dispatch乐观更新，tasksApi.util.updateQueryData('getTasks', projectId, (draft) => {draft.push/filter})
            // 更新getTasks查询对应的缓存，需要传入getTasks相同的参数，并进行Immer操作更新
            providesTags: (_result) =>
                _result
                    ? [
                        ..._result.map(({ id }) => ({ type: 'Task' as const, id })),
                        { type: 'Task', id: 'LIST' },
                    ]
                    : [{ type: 'Task', id: 'LIST' }],
        }),

        // id查询
        getTasksById: builder.query<Task, number>({
            query: (id) => `/tasks/${encodeURIComponent(id)}`,
            providesTags: (__result, __error, id) => [{ type: 'Task', id }],
        }),

        // 创建任务
        createTask: builder.mutation<Task, { projectId: number, body: CreateTaskDto }>({
            query: ({ projectId, body }) => ({
                url: `/projects/${projectId}/tasks`,
                method: 'POST',
                body,
            }),
            // ✅ 乐观更新：立即添加到列表缓存
            async onQueryStarted({ projectId, body }, { dispatch, queryFulfilled }) {
                // 乐观更新：立即把新任务添加到列表
                const patch_result = dispatch(
                    tasksApi.util.updateQueryData('getTasks', projectId, (draft) => {
                        // 生成临时 ID（后端会返回真实 ID）
                        const tempId = Date.now()
                        draft.push({
                            id: tempId,
                            title: body.title,
                            description: body.description || null,
                            status: 0,
                            priority: body.priority || 0,
                            createdAt: new Date().toISOString(),
                            updatedAt: null,
                            dueDate: body.dueDate || null,
                            projectId: projectId,
                        })
                    })
                )

                try {
                    // 等待请求完成
                    const { data } = await queryFulfilled
                    // 用后端返回的真实数据替换临时数据
                    dispatch(
                        tasksApi.util.updateQueryData('getTasks', projectId, (draft) => {
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
                    patch_result.undo()
                }
            },
            invalidatesTags: [{ type: 'Task', id: 'LIST' }],
        }),

        // 修改任务
        updateTask: builder.mutation<Task, { id: number; projectId: number; body: UpdateTaskDto }>({
            query: ({ id, body }) => ({
                url: `/tasks/${id}`,
                method: 'PUT',
                body,
            }),
            // ✅ 乐观更新：立即更新列表和详情缓存
            async onQueryStarted({ id, projectId, body }, { dispatch, queryFulfilled }) {
                // 1. 更新列表缓存
                const patchList = dispatch(
                    tasksApi.util.updateQueryData('getTasks', projectId, (draft) => {
                        const task = draft.find(t => t.id === id)
                        if (task) {
                            Object.assign(task, body)
                        }
                    })
                )

                // 2. 更新详情缓存
                const patchDetail = dispatch(
                    tasksApi.util.updateQueryData('getTasksById', id, (draft) => {
                        Object.assign(draft, body)
                    })
                )

                try {
                    const { data } = await queryFulfilled
                    // 用服务器返回的真实数据更新详情缓存
                    dispatch(
                        tasksApi.util.updateQueryData('getTasksById', id, (draft) => {
                            Object.assign(draft, data)
                        })
                    )
                } catch {
                    // 请求失败，回滚
                    patchList.undo()
                    patchDetail.undo()
                }
            },
            invalidatesTags: (_result, _error, { id }) => [
                { type: 'Task', id },
                { type: 'Task', id: 'LIST' },
            ],
        }),

        // 变更任务状态
        updateTaskStatus: builder.mutation<Task, { id: number, body: UpdateTaskStatusDto }>({
            query: ({ id, body }) => ({
                url: `/tasks/${id}/status`,
                method: 'PATCH',
                body,
            }),
            // ✅ 乐观更新：立即更新状态
            async onQueryStarted({ id, body }, { dispatch, queryFulfilled }) {
                // 更新详情缓存
                const patchDetail = dispatch(
                    tasksApi.util.updateQueryData('getTasksById', id, (draft) => {
                        draft.status = body.status
                    })
                )

                try {
                    const { data } = await queryFulfilled
                    // 用服务器返回的真实数据更新
                    dispatch(
                        tasksApi.util.updateQueryData('getTasksById', id, (draft) => {
                            Object.assign(draft, data)
                        })
                    )
                } catch {
                    patchDetail.undo()
                }
            },
            invalidatesTags: (_result, _error, { id }) => [
                { type: 'Task', id },
                { type: 'Task', id: 'LIST' },
            ],
        }),

        // 删除任务
        deleteTask: builder.mutation<void, { id: number; projectId: number }>({
            query: ({ id }) => ({
                url: `/tasks/${id}`,
                method: 'DELETE',
            }),
            // ✅ 乐观更新：直接从列表中移除
            async onQueryStarted({ id, projectId }, { dispatch, queryFulfilled }) {
                const patch_result = dispatch(
                    tasksApi.util.updateQueryData('getTasks', projectId, (draft) => {
                        const index = draft.findIndex(task => task.id === id)
                        if (index !== -1) {
                            draft.splice(index, 1)
                        }
                    })
                )

                try {
                    await queryFulfilled
                } catch {
                    patch_result.undo()
                }
            },
            invalidatesTags: (_result, _error, { id }) => [
                { type: 'Task', id },
                { type: 'Task', id: 'LIST' },
            ],
        }),
    }),
})

export const {
    useGetTasksQuery,
    useGetTasksByIdQuery,
    useCreateTaskMutation,
    useUpdateTaskMutation,
    useUpdateTaskStatusMutation,
    useDeleteTaskMutation,
} = tasksApi
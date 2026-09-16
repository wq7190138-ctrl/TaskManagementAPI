// providesTags 声明缓存数据带什么标签
// invalidatesTags 告诉 RTK Query 哪些缓存失效了

import { baseApi } from './baseApi'
import type {
    Project,
    CreateProjectDto,
    UpdateProjectDto,
    UpdateProjectStatusDto
} from '../types'

export const projectsApi = baseApi.injectEndpoints({
    endpoints: (builder) => ({
        // 获取项目列表
        getProjects: builder.query<Project[], string | void>({
            query: (keyword) => {
                if (keyword && keyword.trim() !== '') {
                    return `/projects?projectName=${encodeURIComponent(keyword)}`
                }
                return '/projects'
            },
            providesTags: ['Project'],
        }),

        // id查询
        getProjectById: builder.query<Project, number>({
            query: (id) => `/projects/${id}`,
            providesTags: (_result, _error, id) => [{ type: 'Project', id }],
        }),

        // 创建项目
        createProject: builder.mutation<Project, CreateProjectDto>({
            query: (body) => ({
                url: '/projects',
                method: 'POST',
                body,
            }),
            invalidatesTags: ['Project'],
        }),

        // 修改项目
        updateProject: builder.mutation<Project, { id: number, body: UpdateProjectDto }>({
            query: ({ id, body }) => ({
                url: `/projects/${id}`,
                method: 'PUT',
                body,
            }),
            invalidatesTags: ['Project'],
        }),

        // 变更项目状态
        updateProjectStatus: builder.mutation<Project, { id: number, body: UpdateProjectStatusDto }>({
            query: ({ id, body }) => ({
                url: `/projects/${id}/status`,
                method: 'PATCH',
                body,
            }),
            invalidatesTags: ['Project'],
        }),

        // 删除项目
        deleteProject: builder.mutation<void, number>({
            query: (id) => ({
                url: `/projects/${id}`,
                method: 'DELETE',
            }),
            invalidatesTags: ['Project'],
        }),
    }),
})

export const {
    useGetProjectsQuery,
    useGetProjectByIdQuery,
    useCreateProjectMutation,
    useUpdateProjectMutation,
    useUpdateProjectStatusMutation,
    useDeleteProjectMutation,
} = projectsApi
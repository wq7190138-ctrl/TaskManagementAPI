// src/api/baseApi.ts
import { createApi, fetchBaseQuery } from '@reduxjs/toolkit/query/react'
import type { BaseQueryFn, FetchArgs, FetchBaseQueryError } from '@reduxjs/toolkit/query'

const API_BASE = import.meta.env.VITE_API_BASE_URL

const baseQuery = fetchBaseQuery({
    baseUrl: `${API_BASE}/api`,
    credentials: 'include',
    prepareHeaders: (headers) => {
        headers.set('Content-Type', 'application/json')
        return headers
    },
})

const wrappedBaseQuery: BaseQueryFn<string | FetchArgs, unknown, FetchBaseQueryError> = async (args, api, extraOptions) => {
    const result = await baseQuery(args, api, extraOptions)

    if (result.error) {
        const data = result.error.data as Record<string, unknown> | undefined
        const detail = data?.detail || data?.message || '请求失败，请重试'
        // ✅ 直接修改 error.data，保持结构不变
        result.error.data = { detail, ...data }
    }

    return result
}

export const baseApi = createApi({
    reducerPath: 'api',
    baseQuery: wrappedBaseQuery,
    tagTypes: ['Project', 'Task', 'Comment'],
    endpoints: () => ({}),
})
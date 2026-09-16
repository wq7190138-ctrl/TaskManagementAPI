// src/store/slices/toastSlice.ts
import { createSlice } from '@reduxjs/toolkit'
import type { PayloadAction } from '@reduxjs/toolkit'

export type ToastType = 'success' | 'error' | 'info' | 'warning'

interface ToastState {
    message: string
    type: ToastType
    duration: number
    isOpen: boolean
}

const initialState: ToastState = {
    message: '',
    type: 'success',
    duration: 3000,
    isOpen: false,
}

const toastSlice = createSlice({
    name: 'toast',
    initialState,
    reducers: {
        showToast: (
            state,
            action: PayloadAction<{
                message: string
                type?: ToastType
                duration?: number
            }>
        ) => {
            state.message = action.payload.message
            state.type = action.payload.type || 'success'
            state.duration = action.payload.duration || 3000
            state.isOpen = true
        },
        hideToast: (state) => {
            state.isOpen = false
        },
    },
})

export const { showToast, hideToast } = toastSlice.actions
export default toastSlice.reducer
import { configureStore } from '@reduxjs/toolkit';
import { baseApi } from '../api/baseApi';
import toastReducer from './slices/toastSlice'

export const store = configureStore({
  reducer: {
    toast: toastReducer,
    [baseApi.reducerPath]: baseApi.reducer,
  },

  middleware: (getDefaultMiddleware) =>
    getDefaultMiddleware().concat(
      baseApi.middleware,
    ),
})

export type RootState = ReturnType<typeof store.getState>
export type AppDispatch = typeof store.dispatch
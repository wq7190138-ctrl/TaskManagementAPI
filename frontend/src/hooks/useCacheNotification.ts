// hooks/useCacheNotification.ts
import { useEffect, useRef } from 'react';
import * as signalR from '@microsoft/signalr';
import { useAppDispatch } from '../store/hooks';
import { tasksApi } from '../api/tasksApi';

export function useCacheNotification() {
    console.log('🔵 useCacheNotification 被调用了');
    const dispatch = useAppDispatch();
    const connectionRef = useRef<signalR.HubConnection | null>(null);

    useEffect(() => {
        // 建立 SignalR 连接
        const API_BASE = import.meta.env.VITE_API_BASE_URL
        const connection = new signalR.HubConnectionBuilder()
            .withUrl(`${API_BASE}/api/cacheHub`)
            .withAutomaticReconnect() // 自动重连
            .build();

        connectionRef.current = connection;

        // 监听“缓存失效”消息
        connection.on('CacheInvalidated', (cacheKey: string) => {
            console.log('📢 收到缓存失效通知:', cacheKey);

            // 根据收到的 key，作废对应的 RTK Query L1 缓存
            if (cacheKey.includes('tasks:all')) {
                // 作废项目任务列表缓存
                dispatch(tasksApi.util.invalidateTags([{ type: 'Task', id: 'LIST' }]));
            }

            if (cacheKey.includes('task:')) {
                const taskId = cacheKey.split(':')[1];
                dispatch(tasksApi.util.invalidateTags([{ type: 'Task', id: taskId }]));
            }
        });

        connection.start()
            .then(() => console.log('✅ SignalR 连接已建立'))
            .catch(err => console.error('❌ SignalR 连接失败:', err));

        // 清理：组件卸载时断开连接
        return () => {
            connection.stop();
        };
    }, [dispatch]);

    return null; // 这个 Hook 只负责建立连接和监听事件，不渲染任何内容
}
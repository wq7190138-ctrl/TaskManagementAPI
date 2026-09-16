// components/Toast.jsx
import { useEffect } from 'react';
import { useAppSelector, useAppDispatch } from '../../store/hooks'
import { hideToast } from '../../store/slices/toastSlice'

export default function Toast() {
    const dispatch = useAppDispatch()
    const { message, type, duration, isOpen } = useAppSelector((state) => state.toast)

    useEffect(() => {
        if (isOpen) {
            const timer = setTimeout(() => {
                dispatch(hideToast())
            }, duration);
            return () => clearTimeout(timer);
        }
    }, [duration, isOpen, dispatch]);

    if (!isOpen) return null;

    const bgColorMap = {
        success: '#52c41a',
        error: '#ff4d4f',
        info: '#1890ff',
        warning: '#faad14',
    }

    return (
        <div style={{
            position: 'fixed',
            top: '20px',
            left: '50%',
            transform: 'translateX(-50%)',
            zIndex: 9999,
            padding: '12px 24px',
            borderRadius: '8px',
            color: '#fff',
            backgroundColor: bgColorMap[type] || '#52c41a',
            boxShadow: '0 4px 12px rgba(0,0,0,0.15)',
            animation: 'slideDown 0.3s ease-out',
        }}>
            {message}
        </div>
    );
}
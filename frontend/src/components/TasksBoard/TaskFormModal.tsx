import { useState } from 'react'
import type { Task, UpdateTaskDto, TaskPriority } from '../../types'
import styles from './TaskFormModal.module.css'
import { formatDateForInput } from '../../utils/timeUtils'

interface TaskFormModalProps {
    isOpen: boolean
    initialData?: Task | null
    onClose: () => void
    onSave: (data: UpdateTaskDto) => Promise<void>
}

export default function TaskFormModal({
    isOpen,
    initialData,
    onClose,
    onSave,
}: TaskFormModalProps) {
    const [title, setTitle] = useState(initialData?.title || '');
    const [description, setDescription] = useState(initialData?.description || '');
    const [priority, setPriority] = useState(initialData?.priority || 0);
    const [dueDate, setDueDate] = useState(formatDateForInput(initialData?.dueDate) || '');
    const [loading, setLoading] = useState(false);


    const isEdit = !!initialData

    if (!isOpen) return null

    const handleSubmit = async (e: React.FormEvent) => {
        e.preventDefault()
        const trimmed = title.trim()
        if (!trimmed) return

        setLoading(true)
        try {
            await onSave({
                title: trimmed,
                description,
                priority: priority as TaskPriority,
                dueDate: dueDate ? new Date(dueDate).toISOString() : null,
            })
            onClose()
        } catch {
            // 错误由父组件处理
        } finally {
            setLoading(false)
            setTitle('')
            setDescription('')
            setPriority(0)
            setDueDate('')
        }
    }

    return (
        <div className={styles.modalOverlay} >
            <div className={styles.modal} onClick={(e) => e.stopPropagation()}>
                <h2>{isEdit ? '编辑任务' : '新建任务'}</h2>
                <form onSubmit={handleSubmit}>
                    <div className={styles.formGroup}>
                        <label>标题 *</label>
                        <input
                            type="text"
                            value={title}
                            onChange={(e) => setTitle(e.target.value)}
                            placeholder="请输入任务标题"
                            required
                        />
                    </div>
                    <div className={styles.formGroup}>
                        <label>描述</label>
                        <textarea
                            value={description}
                            onChange={(e) => setDescription(e.target.value)}
                            placeholder="请输入任务描述"
                            rows={3}
                        />
                    </div>
                    <div className={styles.formGroup}>
                        <label>优先级</label>
                        <select
                            value={priority}
                            onChange={(e) => setPriority(Number(e.target.value))}
                        >
                            <option value={0}>普通</option>
                            <option value={1}>紧急</option>
                        </select>
                    </div>
                    <div className={styles.formGroup}>
                        <label>截止日期</label>
                        <input
                            type="date"
                            value={dueDate}
                            onChange={(e) => setDueDate(e.target.value)}
                        />
                    </div>
                    <div className={styles.modalActions}>
                        <button type="button" onClick={onClose} disabled={loading}>
                            取消
                        </button>
                        <button type="submit" disabled={loading}>
                            {loading ? '保存中...' : '保存'}
                        </button>
                    </div>
                </form>

            </div>
        </div>
    )
}
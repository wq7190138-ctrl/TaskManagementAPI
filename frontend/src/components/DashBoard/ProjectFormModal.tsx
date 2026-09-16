import { useState } from 'react'
import type { Project } from '../../types'
import styles from './TaskFormModal.module.css'

interface ProjectFormModalProps {
    isOpen: boolean
    initialData?: Project | null
    onClose: () => void
    onSave: (data: { name: string }) => Promise<void>
}

export default function ProjectFormModal({
    isOpen,
    initialData,
    onClose,
    onSave,
}: ProjectFormModalProps) {
    const [name, setName] = useState(initialData?.name || '')
    const [loading, setLoading] = useState(false)

    const isEdit = !!initialData

    if (!isOpen) return null

    const handleSubmit = async (e: React.FormEvent) => {
        e.preventDefault()
        const trimmed = name.trim()
        if (!trimmed) return

        setLoading(true)
        try {
            await onSave({ name: trimmed })
            onClose()
        } catch {
            // 错误由父组件处理
        } finally {
            setLoading(false)
        }
    }

    return (
        <div className={styles.modalOverlay} >
            <div className={styles.modal} onClick={(e) => e.stopPropagation()}>
                <h2>{isEdit ? '编辑项目' : '新建项目'}</h2>
                <form onSubmit={handleSubmit}>
                    <div className={styles.formGroup}>
                        <label>项目名称 *</label>
                        <input
                            type="text"
                            value={name}
                            onChange={(e) => setName(e.target.value)}
                            placeholder="请输入项目名称"
                            required
                            autoFocus
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
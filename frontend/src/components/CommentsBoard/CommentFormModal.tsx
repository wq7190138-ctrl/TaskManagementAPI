import { useState } from 'react'
import type { CreateCommentDto } from '../../types'
import styles from '../TasksBoard/TaskFormModal.module.css'

interface CommentFormModalProps {
    isOpen: boolean
    onClose: () => void
    onSave: (data: CreateCommentDto) => Promise<void>
}

export default function CommentFormModal({
    isOpen,
    onClose,
    onSave,
}: CommentFormModalProps) {
    const [content, setContent] = useState('');
    const [createdBy, setCreatedBy] = useState('');
    const [loading, setLoading] = useState(false);

    if (!isOpen) return null

    const handleSubmit = async (e: React.FormEvent) => {
        e.preventDefault()
        const trimmed = content.trim()
        if (!trimmed) return

        setLoading(true)
        try {
            await onSave({ content: trimmed, createdBy })
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
                <h2>添加评论</h2>
                <form onSubmit={handleSubmit}>
                    <div className={styles.formGroup}>
                        <label>内容 *</label>
                        <input
                            type="text"
                            value={content}
                            onChange={(e) => setContent(e.target.value)}
                            placeholder="请输入评论内容"
                            required
                        />
                    </div>
                    <div className={styles.formGroup}>
                        <label>评论人</label>
                        <textarea
                            value={createdBy}
                            onChange={(e) => setCreatedBy(e.target.value)}
                            placeholder="请输入评论人"
                            rows={3}
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
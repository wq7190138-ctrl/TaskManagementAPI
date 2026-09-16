import { useState, useCallback } from 'react'
import { useNavigate, useParams } from 'react-router-dom'
import Toolbar from '../Common/Toolbar'
import styles from '../Common/Toolbar.module.css'
import {
    useGetCommentsQuery,
    useCreateCommentMutation,
    useDeleteCommentMutation,
} from '../../api/commentsApi'
import type { CreateCommentDto } from '../../types'
import CommentFormModal from '../CommentsBoard/CommentFormModal'
import { useAppDispatch } from '../../store/hooks'
import { showToast } from '../../store/slices/toastSlice'


export default function CommentsList() {
    const navigate = useNavigate()
    const { taskId } = useParams()
    const taskIdNum = Number(taskId)

    // ===== 弹窗状态 =====
    const [isModalOpen, setIsModalOpen] = useState(false)

    // ===== Toast 弹窗 =====
    const dispatch = useAppDispatch();

    // ===== Api =====
    const { data: comments = [], isLoading, refetch } = useGetCommentsQuery(taskIdNum)
    const [createComment] = useCreateCommentMutation()
    const [deleteComment] = useDeleteCommentMutation()

    // ===== 返回 =====
    const handleBack = () => {
        navigate(-1)
    }

    // ===== 新增 =====
    const handleAdd = () => {
        setIsModalOpen(true)
    }

    // ===== 删除 =====
    const handleDelete = async (id: number) => {
        if (!confirm('确定删除该评论吗？')) return

        try {
            await deleteComment({ id: id, taskId: taskIdNum }).unwrap()
            dispatch(showToast({ message: '✅ 删除成功', type: 'success' }))
        } catch {
            dispatch(showToast({ message: '❌ 删除失败', type: 'error' }))
        }
    }

    // ===== 刷新 =====
    const handleRefresh = () => {
        refetch()
    }

    // ===== 保存弹窗 =====
    const handleSave = useCallback(async (data: CreateCommentDto) => {
        await createComment({ taskId: taskIdNum, body: data }).unwrap()
        dispatch(showToast({ message: '✅ 创建成功', type: 'success' }))
        setIsModalOpen(false)
    }, [dispatch, createComment, taskIdNum])

    return (
        <div>
            <Toolbar>
                <button className={styles.btn} onClick={handleBack}>↩︎ 返回</button>
                <button className={styles.btn} onClick={handleAdd}>➕ 添加评论</button>
                <button className={styles.btn} onClick={handleRefresh}>🔄 刷新</button>
            </Toolbar>

            {
                isLoading ? (<div className="loading">⏳ 加载中...</div>)
                    : comments.length === 0 ? (<div className="empty">暂无评论</div>)
                        :
                        comments.map(comment => {
                            return (
                                <div key={comment.id}>
                                    <div>{new Date(comment.createdAt).toLocaleString()}</div>
                                    <div>{comment.createdBy}</div>
                                    <div>{comment.content}</div>
                                    <button className={`${styles.btn} ${styles.btnDanger}`} onClick={() => handleDelete(comment.id)}>🗑 删除</button>
                                    <hr />
                                </div>
                            );
                        })
            }

            <CommentFormModal
                isOpen={isModalOpen}
                onClose={() => {
                    setIsModalOpen(false)
                }}
                onSave={handleSave}
            />
        </div>
    );
}
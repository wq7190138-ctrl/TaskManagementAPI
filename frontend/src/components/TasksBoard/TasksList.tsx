import { useState, useCallback } from 'react'
import { useParams, useNavigate } from 'react-router-dom'
import {
    useGetTasksQuery,
    useCreateTaskMutation,
    useUpdateTaskMutation,
    useUpdateTaskStatusMutation,
    useDeleteTaskMutation,
} from '../../api/tasksApi'
import type { Task, TaskStatus, ProjectStatus, UpdateTaskDto } from '../../types'
import Toolbar from '../Common/Toolbar'
import TaskTable from './TaskTable'
import TaskFormModal from './TaskFormModal'
import styles from '../Common/Toolbar.module.css'
import { useAppDispatch } from '../../store/hooks'
import { showToast } from '../../store/slices/toastSlice'
// import { getErrorMessage } from '../../utils/errorHandler'

export default function TasksList() {
    const navigate = useNavigate()
    const { projectId } = useParams()

    // ===== 选择状态 =====
    const [selectedId, setSelectedId] = useState<number | null>(null)

    // ===== 弹窗状态 =====
    const [isModalOpen, setIsModalOpen] = useState(false)
    const [editingTask, setEditingTask] = useState<Task | null>(null)

    // ===== Toast 弹窗 =====
    const dispatch = useAppDispatch();

    // ===== API =====
    const projectIdNum = Number(projectId)
    const { data: tasks = [], isLoading, refetch } = useGetTasksQuery(projectIdNum)
    const [createProject] = useCreateTaskMutation()
    const [updateProject] = useUpdateTaskMutation()
    const [updateStatus] = useUpdateTaskStatusMutation()
    const [deleteTask] = useDeleteTaskMutation()

    // ==== 返回 ====
    const handleBack = () => {
        navigate(-1)
    }

    // ===== 刷新 =====
    const handleRefresh = () => {
        refetch()
    }

    // ===== 添加 =====
    const handleAdd = () => {
        setEditingTask(null)
        setIsModalOpen(true)
    }

    // ===== 编辑 =====
    const handleEdit = (task: Task) => {
        setEditingTask(task)
        setIsModalOpen(true)
    }

    // ===== 详情跳转 =====
    const handleDetail = (task: Task) => {
        navigate(`/projects/${projectId}/tasks/${task.id}`)
    }

    // ===== 删除 =====
    const handleDelete = async () => {
        if (selectedId === null) return
        if (!confirm('确定删除该项目吗？')) return

        try {
            await deleteTask({ id: selectedId, projectId: projectIdNum }).unwrap()
            setSelectedId(null)
            dispatch(showToast({ message: '✅ 删除成功', type: 'success' }))
        } catch (error: unknown) {
            const err = error as { data?: { detail?: string } }
            const message = err?.data?.detail || '操作失败'
            dispatch(showToast({ message: `❌ ${message}`, type: 'error' }))
        }
    }

    // ===== 状态变更 =====
    const handleStatusChange = async (status: TaskStatus | ProjectStatus) => {
        if (selectedId === null) return

        try {
            await updateStatus({ id: selectedId, body: { status: status as TaskStatus } }).unwrap()
            dispatch(showToast({ message: '✅ 状态更新成功', type: 'success' }))
        } catch (error: unknown) {
            const err = error as { data?: { detail?: string } }
            const message = err?.data?.detail || '操作失败'
            dispatch(showToast({ message: `❌ ${message}`, type: 'error' }))
        }
    }

    // ===== 选择行 =====
    const handleSelect = useCallback((id: number) => {
        setSelectedId((prev) => (prev === id ? null : id))
    }, [])

    // ===== 保存弹窗 =====
    const handleSave = useCallback(async (data: UpdateTaskDto) => {
        try {
            if (editingTask) {
                await updateProject({ id: editingTask.id, projectId: projectIdNum, body: data }).unwrap()
                dispatch(showToast({ message: '✅ 更新成功', type: 'success' }))
            } else {
                console.log('📤 发送的数据:', JSON.stringify(data, null, 2))
                await createProject({ projectId: projectIdNum, body: data }).unwrap()
                dispatch(showToast({ message: '✅ 创建成功', type: 'success' }))
            }
            setIsModalOpen(false)
            setEditingTask(null)
        } catch (error: unknown) {
            const err = error as { data?: { detail?: string } }
            const message = err?.data?.detail || '操作失败'
            dispatch(showToast({ message: `❌ ${message}`, type: 'error' }))
        }
    }, [editingTask, projectIdNum, createProject, updateProject, dispatch])

    return (
        <div>
            <Toolbar selectedId={selectedId}>
                <button className={styles.btn} onClick={handleBack}>↩︎ 返回</button>
                <button className={styles.btn} onClick={handleAdd}>➕ 添加任务</button>
                <button className={`${styles.btn} ${styles.btnDanger}`} onClick={handleDelete} disabled={!selectedId}>🗑 删除</button>
                <button className={styles.btn} onClick={() => handleStatusChange(1)} disabled={!selectedId}>⏳ 进行中</button>
                <button className={styles.btn} onClick={() => handleStatusChange(2)} disabled={!selectedId}>✅ 已完成</button>
                <button className={styles.btn} onClick={handleRefresh}>🔄 刷新</button>
            </Toolbar>

            <TaskTable
                tasks={tasks}
                selectedId={selectedId}
                onSelect={handleSelect}
                onEdit={handleEdit}
                onDetail={handleDetail}
                loading={isLoading}
            />

            <TaskFormModal
                key={editingTask?.id}
                isOpen={isModalOpen}
                initialData={editingTask}
                onClose={() => {
                    setIsModalOpen(false)
                    setEditingTask(null)
                }}
                onSave={handleSave}
            />
        </div>
    )
}
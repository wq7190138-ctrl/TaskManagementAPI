import { useState, useCallback } from 'react'
import { useNavigate } from 'react-router-dom'
import {
    useGetProjectsQuery,
    useCreateProjectMutation,
    useUpdateProjectMutation,
    useUpdateProjectStatusMutation,
    useDeleteProjectMutation,
} from '../../api/projectsApi'
import type { Project, ProjectStatus } from '../../types'
import SearchBar from '../Common/SearchBar'
import Toolbar from '../Common/Toolbar'
import ProjectTable from './ProjectTable'
import ProjectFormModal from './ProjectFormModal'
import styles from '../Common/Toolbar.module.css'
import { useAppDispatch, useAppSelector } from '../../store/hooks'
import { showToast } from '../../store/slices/toastSlice'

export default function ProjectsList() {
    const navigate = useNavigate()

    // ===== 搜索状态 =====
    const [keyword, setKeyword] = useState('')
    const [searchTerm, setSearchTerm] = useState('')

    // ===== 选择状态 =====
    const [selectedId, setSelectedId] = useState<number | null>(null)

    // ===== 弹窗状态 =====
    const [isModalOpen, setIsModalOpen] = useState(false)
    const [editingProject, setEditingProject] = useState<Project | null>(null)

    // ===== Toast 弹窗 =====
    const dispatch = useAppDispatch()
    const message = useAppSelector((state) => state.toast.message)

    // ===== API =====
    const { data: projects = [], isLoading, refetch } = useGetProjectsQuery(searchTerm || undefined)
    const [createProject] = useCreateProjectMutation()
    const [updateProject] = useUpdateProjectMutation()
    const [updateStatus] = useUpdateProjectStatusMutation()
    const [deleteProject] = useDeleteProjectMutation()

    // ===== 搜索 =====
    const handleSearch = useCallback(() => {
        setSearchTerm(keyword)
    }, [keyword])

    // ===== 刷新 =====
    const handleRefresh = useCallback(() => {
        refetch()
    }, [refetch])

    // ===== 添加 =====
    const handleAdd = useCallback(() => {
        setEditingProject(null)
        setIsModalOpen(true)
    }, [])

    // ===== 编辑 =====
    const handleEdit = useCallback((project: Project) => {
        setEditingProject(project)
        setIsModalOpen(true)
    }, [])

    // ===== 详情跳转 =====
    const handleDetail = useCallback((project: Project) => {
        if (project.status === 3) {
            alert('项目已终止！')
            return
        }
        navigate(`/projects/${project.id}`)
    }, [navigate])

    // ===== 删除 =====
    const handleDelete = useCallback(async () => {
        if (selectedId === null) return
        if (!confirm('确定删除该项目吗？')) return

        try {
            await deleteProject(selectedId).unwrap()
            setSelectedId(null)
            dispatch(showToast({ message: '✅ 删除成功', type: 'success' }))
        } catch (error: unknown) {
            const err = error as { data?: { detail?: string } }
            const message = err?.data?.detail || '操作失败'
            dispatch(showToast({ message: `❌ ${message}`, type: 'error' }))
        }
    }, [selectedId, deleteProject, dispatch])

    // ===== 状态变更 =====
    const handleStatusChange = useCallback(async (status: ProjectStatus) => {
        if (selectedId === null) return

        try {
            await updateStatus({ id: selectedId, body: { status } }).unwrap()
            dispatch(showToast({ message: '✅ 状态更新成功', type: 'success' }))
        } catch (error: unknown) {
            const err = error as { data?: { detail?: string } }
            const message = err?.data?.detail || '操作失败'
            dispatch(showToast({ message: `❌ ${message}`, type: 'error' }))
        }
    }, [selectedId, updateStatus, dispatch])

    // ===== 选择行 =====
    const handleSelect = useCallback((id: number) => {
        setSelectedId((prev) => (prev === id ? null : id))
    }, [])

    // ===== 保存弹窗 =====
    const handleSave = useCallback(async (data: { name: string }) => {
        try {
            if (editingProject) {
                await updateProject({ id: editingProject.id, body: data }).unwrap()
                dispatch(showToast({ message: '✅ 更新成功', type: 'success' }))
            } else {
                await createProject(data).unwrap()
                dispatch(showToast({ message: '✅ 创建成功', type: 'success' }))
            }
            setIsModalOpen(false)
            setEditingProject(null)
        } catch (error: unknown) {
            const err = error as { data?: { detail?: string } }
            const message = err?.data?.detail || '操作失败'
            dispatch(showToast({ message: `❌ ${message}`, type: 'error' }))
        }
    }, [editingProject, createProject, updateProject, dispatch])

    return (
        <div>
            <Toolbar selectedId={selectedId}>
                <SearchBar value={keyword} onChange={setKeyword} onSearch={handleSearch} />
                <button className={styles.btn} onClick={handleAdd}>➕ 添加项目</button>
                <button className={`${styles.btn} ${styles.btnDanger}`} onClick={handleDelete} disabled={!selectedId}>🗑 删除</button>
                <button className={styles.btn} onClick={() => handleStatusChange(1)} disabled={!selectedId}>⏳ 进行中</button>
                <button className={styles.btn} onClick={() => handleStatusChange(2)} disabled={!selectedId}>✅ 已完成</button>
                <button className={styles.btn} onClick={() => handleStatusChange(3)} disabled={!selectedId}>🛑 已终止</button>
                <button className={styles.btn} onClick={handleRefresh}>🔄 刷新</button>
                <div>{message}</div>
            </Toolbar>
            <ProjectTable
                projects={projects}
                selectedId={selectedId}
                onSelect={handleSelect}
                onEdit={handleEdit}
                onDetail={handleDetail}
                loading={isLoading}
            />

            <ProjectFormModal
                key={editingProject?.id}
                isOpen={isModalOpen}
                initialData={editingProject}
                onClose={() => {
                    setIsModalOpen(false)
                    setEditingProject(null)
                }}
                onSave={handleSave}
            />
        </div>
    )
}
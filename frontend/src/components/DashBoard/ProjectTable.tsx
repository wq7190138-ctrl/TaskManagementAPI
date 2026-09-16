import type { Project } from '../../types'
import styles from './ProjectsList.module.css';

const STATUS_MAP: Record<number, string> = {
    0: '待办',
    1: '进行中',
    2: '已完成',
    3: '已终止',
}

interface ProjectTableProps {
    projects: Project[]
    selectedId: number | null
    onSelect: (id: number) => void
    onEdit: (project: Project) => void
    onDetail: (project: Project) => void
    loading?: boolean
}

export default function ProjectTable({
    projects,
    selectedId,
    onSelect,
    onEdit,
    onDetail,
    loading = false,
}: ProjectTableProps) {
    if (loading) {
        return <div className="loading">⏳ 加载中...</div>
    }

    if (projects.length === 0) {
        return <div className="empty">暂无项目</div>
    }

    return (
        <table className={styles.table}>
            <thead>
                <tr>
                    <th className={styles.colName}>项目名称</th>
                    <th className={styles.colDate}>创建日期</th>
                    <th className={styles.colStatus}>项目状态</th>
                    <th className={styles.colStatusDate}>状态日期</th>
                    <th className={styles.colAction}>操作</th>
                    <th className={styles.colAction}>查看详情</th>
                </tr>
            </thead>
            <tbody>
                {projects.map((project) => {
                    const isSelected = selectedId === project.id

                    return (
                        <tr
                            key={project.id}
                            className={isSelected ? styles.rowSelected : styles.row}
                            onClick={() => onSelect(project.id)}
                        >
                            <td className={styles.colName}>{project.name}</td>
                            <td className={styles.colDate}>{new Date(project.createdAt).toLocaleDateString()}</td>
                            <td className={styles.colStatus}>{STATUS_MAP[project.status] ?? project.status}</td>
                            <td className={styles.colStatusDate}>
                                {project.statusUpdateAt
                                    ? new Date(project.statusUpdateAt).toLocaleDateString()
                                    : '-'}
                            </td>
                            <td className={styles.colAction}>
                                <button onClick={(e) => { e.stopPropagation(); onEdit(project) }}>
                                    ✏️编辑
                                </button>
                            </td>
                            <td className={styles.colAction}>
                                <button onClick={(e) => { e.stopPropagation(); onDetail(project) }}>
                                    🔍️详情
                                </button>
                            </td>
                        </tr>
                    )
                })}
            </tbody>
        </table>
    )
}
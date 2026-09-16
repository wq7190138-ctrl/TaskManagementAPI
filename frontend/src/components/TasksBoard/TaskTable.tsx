import type { Task } from '../../types'
import { STATUS_MAP, PRIORITY_MAP } from '../../types'
import styles from '../DashBoard/ProjectsList.module.css'

interface TaskTableProps {
    tasks: Task[]
    selectedId: number | null
    onSelect: (id: number) => void
    onEdit: (task: Task) => void
    onDetail: (task: Task) => void
    loading?: boolean
}

export default function TaskTable({
    tasks,
    selectedId,
    onSelect,
    onEdit,
    onDetail,
    loading = false,
}: TaskTableProps) {

    if (loading) {
        return <div className="loading">⏳ 加载中...</div>
    }

    if (tasks.length === 0) {
        return <div className="empty">暂无任务</div>
    }

    return (
        <table className={styles.table}>
            <thead>
                <tr>
                    <th className={styles.colName}>任务</th>
                    <th className={styles.colName}>描述</th>
                    <th className={styles.colStatus}>状态</th>
                    <th className={styles.colStatus}>优先级</th>
                    <th className={styles.colDate}>创建日期</th>
                    <th className={styles.colDate}>修改日期</th>
                    <th className={styles.colDate}>截止日期</th>
                    <th className={styles.colAction}>操作</th>
                    <th className={styles.colAction}>查看详情</th>
                </tr>
            </thead>
            <tbody>
                {tasks.map((task) => {
                    const isSelected = selectedId === task.id

                    return (
                        <tr
                            key={task.id}
                            className={isSelected ? styles.rowSelected : styles.row}
                            onClick={() => onSelect(task.id)}
                        >
                            <td className={styles.colName}>{task.title}</td>
                            <td className={styles.colName}>{task.description || '-'}</td>
                            <td className={styles.colStatus}>{STATUS_MAP[task.status] ?? task.status}</td>
                            <td className={styles.colStatus}>{PRIORITY_MAP[task.priority] ?? task.priority}</td>
                            <td className={styles.colDate}>{new Date(task.createdAt).toLocaleDateString()}</td>
                            <td className={styles.colDate}>{task.updatedAt ? new Date(task.updatedAt).toLocaleString() : '-'}</td>
                            <td className={styles.colDate}>{task.dueDate ? new Date(task.dueDate).toLocaleDateString() : '-'}</td>
                            <td className={styles.colAction}>
                                <button onClick={(e) => { e.stopPropagation(); onEdit(task) }}>
                                    ✏️编辑
                                </button>
                            </td>
                            <td className={styles.colAction}>
                                <button onClick={(e) => { e.stopPropagation(); onDetail(task) }}>
                                    🔍️详情
                                </button>
                            </td>
                        </tr>

                    )
                })}
            </tbody>
        </table>


    );
}
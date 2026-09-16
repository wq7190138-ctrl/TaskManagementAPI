    import { useGetProjectsQuery } from '../../api/projectsApi'
import styles from './StatsBoard.module.css'

interface StatsCard {
    key: string
    label: string
    value: number
    icon: string
    className: string
}

export default function StatsBoard() {
    const { data: projects = [], isLoading } = useGetProjectsQuery()

    if (isLoading) {
        return (
            <div className={styles.container}>
                {[1, 2, 3, 4, 5].map((i) => (
                    <div key={i} className={`${styles.statItem} ${styles.skeleton}`}>
                        <span className={styles.value}>--</span>
                        <span className={styles.label}>加载中...</span>
                    </div>
                ))}
            </div>
        )
    }

    const total = projects.length
    const todo = projects.filter((p) => p.status === 0).length
    const inProgress = projects.filter((p) => p.status === 1).length
    const completed = projects.filter((p) => p.status === 2).length
    const terminated = projects.filter((p) => p.status === 3).length

    const stats: StatsCard[] = [
        { key: 'total', icon: '📊', label: '全部项目', value: total, className: styles.total },
        { key: 'todo', icon: '📋', label: '待办', value: todo, className: styles.todo },
        { key: 'inProgress', icon: '⏳', label: '进行中', value: inProgress, className: styles.inProgress },
        { key: 'completed', icon: '✅', label: '已完成', value: completed, className: styles.completed },
        { key: 'terminated', icon: '🛑', label: '已终止', value: terminated, className: styles.terminated },
    ]

    return (
        <div className={styles.container}>
            {stats.map((stat) => (
                <div key={stat.key} className={`${styles.statItem} ${stat.className}`}>
                    <span className={styles.icon}>{stat.icon}</span>
                    <span className={styles.value}>{stat.value}</span>
                    <span className={styles.label}>{stat.label}</span>
                </div>
            ))}
        </div>
    )
}
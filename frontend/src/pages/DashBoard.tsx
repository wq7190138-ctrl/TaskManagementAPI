import StatsBoard from '../components/DashBoard/StatsBoard'
import ProjectsList from '../components/DashBoard/ProjectsList'
import styles from './DashBoard.module.css';

export default function DashBoard() {
    return (
        <div className={styles.container}>
            <div className={styles.header}>
                <h1 className={styles.title}>
                    <span className={styles.titleIcon}>📋</span>项目看板1
                </h1>
            </div>
            <StatsBoard />
            <h2>📋 项目列表</h2>
            <ProjectsList />
        </div>
    )
}
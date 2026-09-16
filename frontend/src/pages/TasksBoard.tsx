import TasksList from '../components/TasksBoard/TasksList';
import styles from './DashBoard.module.css';

export default function TasksBoard() {
    return (

        <div className={styles.container}>
            <div className={styles.header}>
                <h1 className={styles.title}>
                    <span className={styles.titleIcon}>📋</span>任务看板
                </h1>
            </div>
            <h2>📋 任务列表</h2>
            <TasksList />
        </div>
    );
}
import styles from './Toolbar.module.css';

interface ActionButtonsProps {
    onAdd: () => void
    onDel: () => void
    onStatusChange: (status: number) => void
    onRefetch: () => void
    selectedProjectId: number | null
}

export default function ActionButtons({ onAdd, onDel, onStatusChange, onRefetch, selectedProjectId }: ActionButtonsProps) {
    return (
        <div style={{ display: 'flex', gap: 8, alignItems: 'center' }}>
            <button
                className={`${styles.btn} ${styles.btnPrimary}`}
                onClick={onAdd}
            >
                ➕️ 新增
            </button>
            <button
                className={`${styles.btn} ${styles.btnDanger}`}
                onClick={onDel}
                disabled={!selectedProjectId}
            >
                🗑 删除
            </button>
            <button
                className={styles.btn}
                onClick={() => onStatusChange(1)}
                disabled={!selectedProjectId}
            >
                ⏳ 进行中
            </button>
            <button
                className={styles.btn}
                onClick={() => onStatusChange(2)}
                disabled={!selectedProjectId}
            >
                ✅ 已完成
            </button>
            <button
                className={styles.btn}
                onClick={() => onStatusChange(3)}
                disabled={!selectedProjectId}
            >
                🛑 终止
            </button>
            <button
                className={styles.btn}
                onClick={onRefetch}
            >
                🔄 刷新
            </button>
        </div>
    )
}
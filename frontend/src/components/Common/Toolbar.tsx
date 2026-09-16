import styles from './Toolbar.module.css'

interface ToolbarProps {
    children: React.ReactNode
    selectedId?: number | null
}

export default function Toolbar({
    children,
    selectedId,
}: ToolbarProps) {
    const hasSelection = selectedId !== null

    return (
        <div className={styles.toolbar}>
            {children}
            <span className={styles.selectedInfo}>
                {hasSelection ? `已选择项目 ID: ${selectedId}` : '请选择一行'}
            </span>
        </div>
    )
}
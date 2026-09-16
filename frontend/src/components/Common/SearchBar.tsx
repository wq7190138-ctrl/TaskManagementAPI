import styles from './Toolbar.module.css';

interface SearchBarProps {
    value: string
    onChange: (value: string) => void
    onSearch: () => void
    placeholder?: string
}

export default function SearchBar({
    value,
    onChange,
    onSearch,
    placeholder = '按项目名称搜索...',
}: SearchBarProps) {
    const handleKeyDown = (e: React.KeyboardEvent<HTMLInputElement>) => {
        if (e.key === 'Enter') {
            onSearch()
        }
    }

    return (
        <div style={{ display: 'flex', gap: 8, alignItems: 'center' }}>
            <input
                type="text"
                className={styles.searchInput}
                value={value}
                onChange={(e) => onChange(e.target.value)}
                onKeyDown={handleKeyDown}
                placeholder={placeholder}
            />
            <button className={styles.btn} onClick={onSearch}>
                🔍 查询
            </button>
        </div>
    )
}
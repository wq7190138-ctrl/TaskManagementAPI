import { format } from 'date-fns';

export function formatDateForInput(dateString: string | null | undefined) {
    if (!dateString) return '';
    const date = new Date(dateString);
    return format(date, 'yyyy-MM-dd');
};
function getEndOfWeek(startDateStr: string): string {
    // Parse the date string from DD-MM-YYYY format
    const [year, month, day] = startDateStr.split('-').map(Number);

    // Create a date object (JS months are 0-indexed)
    const startDate = new Date(year, month - 1, day);

    // Add 6 days to get the end of the week
    const endDate = new Date(startDate);
    endDate.setDate(endDate.getDate() + 6);

    // Use formatDate function for consistent formatting
    return formatDate(`${endDate.getFullYear()}-${endDate.getMonth()+1}-${endDate.getDate()}`);
}

export function getWeekRange(startDateStr: string): string {
    // Get the end of the week based on the start date
    const endDateStr = getEndOfWeek(startDateStr);

    // Return the formatted week range
    return `${formatDate(startDateStr)} - ${endDateStr}`;
}

function getEndOfMonth(startDateStr: string): string {
    const [year, month, ] = startDateStr.split('-').map(Number);
    
    // Create a date for the first day of the next month
    const nextMonth = new Date(year, month, 1);
    
    // Subtract one day to get the last day of the current month
    const endDate = new Date(nextMonth);
    endDate.setDate(endDate.getDate() - 1);
    
    return formatDate(`${endDate.getFullYear()}-${endDate.getMonth()+1}-${endDate.getDate()}`);
}

export function getMonthRange(startDateStr: string): string {
    const endDateStr = getEndOfMonth(startDateStr);
    return `${formatDate(startDateStr)} - ${endDateStr}`;
}

function getEndOfQuarter(startDateStr: string): string {
    const [year, month, ] = startDateStr.split('-').map(Number);
    
    // Calculate the last month of the quarter
    const quarterEndMonth = Math.ceil(month / 3) * 3;
    
    // Create a date for the first day of the month after the quarter
    const nextMonth = new Date(year, quarterEndMonth, 1);
    
    // Subtract one day to get the last day of the quarter
    const endDate = new Date(nextMonth);
    endDate.setDate(endDate.getDate() - 1);
    
    return formatDate(`${endDate.getFullYear()}-${endDate.getMonth()+1}-${endDate.getDate()}`);
}

export function getQuarterRange(startDateStr: string): string {
    const endDateStr = getEndOfQuarter(startDateStr);
    return `${formatDate(startDateStr)} - ${endDateStr}`;
}

function getEndOfYear(startDateStr: string): string {
    const [year, ] = startDateStr.split('-').map(Number);
    
    // Create a date for December 31st of the same year
    const endDate = new Date(year, 11, 31);
    
    return formatDate(`${endDate.getFullYear()}-${endDate.getMonth()+1}-${endDate.getDate()}`);
}

export function getYearRange(startDateStr: string): string {
    const endDateStr = getEndOfYear(startDateStr);
    return `${formatDate(startDateStr)} - ${endDateStr}`;
}

function formatDate(dateString: string): string {
    return new Date(dateString).toLocaleDateString('ru-RU', {
        year: 'numeric',
        month: '2-digit',
        day: '2-digit'
    });
}
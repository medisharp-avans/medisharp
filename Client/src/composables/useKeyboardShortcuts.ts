const useKeyboardShortcuts = (event: KeyboardEvent) => {
    if (event.altKey) {
        if (event.key.toLowerCase() === 'n') {
            event.preventDefault();
            const navElement = document.getElementById('main-navigation')?.querySelector('a, button');
            (navElement as HTMLElement)?.focus();
        } 
        else if (event.key.toLowerCase() === 'p') {
            event.preventDefault();
            const profileElement = document.getElementById('profile-button');
            (profileElement as HTMLElement)?.focus();
        } 
        else if (event.key.toLowerCase() === 'm') {
            event.preventDefault();
            const mainElement = document.getElementById('main-content');
            (mainElement as HTMLElement)?.focus();
        }
    }
};

export default useKeyboardShortcuts;
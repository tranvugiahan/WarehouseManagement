// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

document.querySelectorAll('.password-toggle').forEach(button => {
    button.addEventListener('click', () => {
        const input = document.getElementById(button.dataset.target);
        if (!input) return;

        const visible = input.type === 'password';
        input.type = visible ? 'text' : 'password';
        button.setAttribute('aria-pressed', String(visible));
        const label = `${visible ? 'Ẩn' : 'Hiện'} ${button.dataset.label}`;
        button.setAttribute('aria-label', label);
        button.title = label;
        button.querySelector('i').classList.toggle('fa-eye', !visible);
        button.querySelector('i').classList.toggle('fa-eye-slash', visible);
    });
});

// Tự động gắn class active cho menu Sidebar
const currentPath = window.location.pathname;
document.querySelectorAll('.sidebar-menu a').forEach(link => {
    link.classList.remove('active');
    const href = link.getAttribute('href');
    if (href === currentPath || (currentPath === '/' && href === '/Home/Index')) {
        link.classList.add('active');
    }
});

// Đóng/mở menu tài khoản ở Header
const accountMenu = document.getElementById('accountMenu');
if (accountMenu) {
    document.addEventListener('click', event => {
        if (!accountMenu.contains(event.target)) accountMenu.open = false;
    });
    accountMenu.addEventListener('keydown', event => {
        if (event.key === 'Escape') {
            accountMenu.open = false;
            accountMenu.querySelector('summary').focus();
        }
    });
}
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

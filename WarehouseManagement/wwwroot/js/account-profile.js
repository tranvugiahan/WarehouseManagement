const form = document.getElementById('changePasswordForm');

form?.addEventListener('submit', async event => {
    event.preventDefault();

    const currentPassword = document.getElementById('currentPassword').value;
    const newPassword = document.getElementById('newPassword').value;
    const confirmPassword = document.getElementById('confirmPassword').value;

    if (!currentPassword || !newPassword || !confirmPassword) {
        alert('Vui lòng nhập đủ các ô mật khẩu.');
        return;
    }

    if (newPassword !== confirmPassword) {
        alert('Mật khẩu xác nhận không khớp.');
        return;
    }

    const token = form.querySelector(
        'input[name="__RequestVerificationToken"]'
    )?.value;

    try {
        const response = await fetch(form.dataset.url, {
            method: 'POST',
            credentials: 'same-origin',
            headers: {
                'Content-Type': 'application/json',
                'RequestVerificationToken': token
            },
            body: JSON.stringify({ currentPassword, newPassword })
        });

        if (response.ok) {
            alert('Đổi mật khẩu thành công.');
            form.reset();
            return;
        }

        const contentType = response.headers.get('content-type') || '';
        const result = contentType.includes('application/json')
            ? await response.json()
            : null;

        alert(result?.error || 'Không thể đổi mật khẩu. Vui lòng thử lại.');
    } catch {
        alert('Không thể kết nối đến máy chủ.');
    }
});
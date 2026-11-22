let targetUserRow = null;
let editingUserRow = null;
let isSavingUser = false;

function openModal(id) { document.getElementById(id).classList.add('show'); }
function closeModal(id) { document.getElementById(id).classList.remove('show'); }

function setFieldError(id, message) {
    const input = document.getElementById(id);
    const error = document.getElementById('err-' + id);
    if (!input || !error) return;
    input.classList.toggle('is-invalid', !!message);
    error.textContent = message;
    error.classList.toggle('show', !!message);
}
function clearFormValidation(ids) { ids.forEach(id => setFieldError(id, '')); }

function validateCreateField(id) {
    const value = document.getElementById(id).value.trim();
    let message = '';
    if (id === 'createUsername' && !/^[^@\s]{1,50}$/.test(value))
        message = 'Tên đăng nhập bắt buộc, tối đa 50 ký tự, không chứa khoảng trắng hoặc @.';
    if (id === 'createPassword' && (value.length < 6 || value.length > 128))
        message = 'Mật khẩu phải có từ 6 đến 128 ký tự.';
    if (id === 'createFullName' && (!value || value.length > 100))
        message = 'Họ và tên phải có tối đa 100 ký tự.';
    if (id === 'createEmail' && (!/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(value) || value.length > 150))
        message = 'Email không hợp lệ hoặc quá 150 ký tự.';
    if (id === 'createRole' && !value) message = 'Vui lòng chọn vai trò.';
    setFieldError(id, message);
    return !message;
}
function validateEditField(id) {
    const value = document.getElementById(id).value.trim();
    let message = '';
    if (id === 'editFullName' && (!value || value.length > 100))
        message = 'Họ và tên phải có tối đa 100 ký tự.';
    if (id === 'editEmail' && (!/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(value) || value.length > 150))
        message = 'Email không hợp lệ hoặc quá 150 ký tự.';
    if (id === 'editRole' && !value) message = 'Vui lòng chọn vai trò.';
    setFieldError(id, message);
    return !message;
}

async function sendUserRequest(url, body) {
    const token = document.querySelector('input[name="__RequestVerificationToken"]').value;
    const options = { method: 'POST', credentials: 'same-origin',
        headers: { RequestVerificationToken: token } };
    if (body !== undefined) {
        options.headers['Content-Type'] = 'application/json';
        options.body = JSON.stringify(body);
    }
    const response = await fetch(url, options);
    if (response.redirected || response.status === 401)
        throw new Error('Phiên đăng nhập đã hết hạn. Hãy đăng nhập lại.');
    if (response.status === 403)
        throw new Error('Bạn không có quyền quản lý tài khoản.');
    if (!response.ok) {
        const payload = (response.headers.get('content-type') || '').includes('application/json')
            ? await response.json() : null;
        throw new Error(payload && payload.error ? payload.error : 'Không thể lưu thay đổi. Vui lòng thử lại.');
    }
}
async function saveUser(url, body, message) {
    if (isSavingUser) return;
    isSavingUser = true;
    try {
        await sendUserRequest(url, body);
        alert(message);
        window.location.reload();
    } catch (error) {
        alert(error.message || 'Không thể kết nối đến máy chủ.');
    } finally {
        isSavingUser = false;
    }
}

function openCreateUserModal() {
    document.getElementById('createUserForm').reset();
    clearFormValidation(['createUsername', 'createPassword', 'createFullName', 'createEmail', 'createRole']);
    openModal('createUserModal');
}
function handleCreateUser(event) {
    event.preventDefault();
    const ids = ['createUsername', 'createPassword', 'createFullName', 'createEmail', 'createRole'];
    if (!ids.map(validateCreateField).every(Boolean)) return;
    const request = {
        username: document.getElementById('createUsername').value.trim(),
        password: document.getElementById('createPassword').value,
        fullName: document.getElementById('createFullName').value.trim(),
        email: document.getElementById('createEmail').value.trim(),
        role: document.getElementById('createRole').value
    };
    saveUser(document.getElementById('userTableBody').dataset.createUrl, request, 'Tạo tài khoản thành công.');
}

function openEditUserModal(button) {
    editingUserRow = button.closest('tr');
    document.getElementById('editUserForm').reset();
    clearFormValidation(['editFullName', 'editEmail', 'editRole']);
    document.getElementById('editUsername').value = editingUserRow.dataset.username;
    document.getElementById('editFullName').value = editingUserRow.dataset.fullname;
    document.getElementById('editEmail').value = editingUserRow.dataset.email;
    document.getElementById('editRole').value = editingUserRow.dataset.role;
    const isSelf = editingUserRow.dataset.id === document.getElementById('userTableBody').dataset.currentUserId;
    document.getElementById('editPasswordSection').hidden = isSelf;
    ['editNewPassword', 'editConfirmPassword'].forEach(id => {
        const input = document.getElementById(id);
        input.value = '';
        input.type = 'password';
        input.disabled = isSelf;
        const toggle = document.querySelector('.password-toggle[data-target="' + id + '"]');
        toggle.setAttribute('aria-pressed', 'false');
        toggle.querySelector('i').classList.replace('fa-eye-slash', 'fa-eye');
    });
    openModal('editUserModal');
}
function handleUpdateUser(event) {
    event.preventDefault();
    if (!editingUserRow) return;
    const ids = ['editFullName', 'editEmail', 'editRole'];
    if (!ids.map(validateEditField).every(Boolean)) return;
    const request = {
        username: editingUserRow.dataset.username,
        fullName: document.getElementById('editFullName').value.trim(),
        email: document.getElementById('editEmail').value.trim(),
        role: document.getElementById('editRole').value
    };
    const password = document.getElementById('editNewPassword').value;
    const confirmation = document.getElementById('editConfirmPassword').value;
    if (password || confirmation) {
        if (!password.trim() || password.length < 6 || password.length > 128) {
            alert('Mật khẩu mới phải có từ 6 đến 128 ký tự.');
            return;
        }
        if (password !== confirmation) {
            alert('Xác nhận mật khẩu mới không khớp.');
            document.getElementById('editConfirmPassword').focus();
            return;
        }
        request.newPassword = password;
    }
    const url = document.getElementById('userTableBody').dataset.updateUrl + '/' + editingUserRow.dataset.id;
    saveUser(url, request, 'Cập nhật tài khoản thành công.');
}

function openAssignRoleModal(button) {
    const id = button.closest('tr').dataset.id;
    const select = document.getElementById('assignAccountSelect');
    select.replaceChildren();
    document.querySelectorAll('#userTableBody tr').forEach(row => {
        const option = document.createElement('option');
        option.value = row.dataset.id;
        option.textContent = row.dataset.fullname + ' (' + row.dataset.username + ')';
        option.selected = row.dataset.id === id;
        select.appendChild(option);
    });
    onAssignAccountChange();
    openModal('assignRoleModal');
}
function onAssignAccountChange() {
    const id = document.getElementById('assignAccountSelect').value;
    const row = Array.from(document.querySelectorAll('#userTableBody tr')).find(item => item.dataset.id === id);
    if (row) selectRoleOption(row.dataset.role);
}
function selectRoleOption(role) {
    ['ADMIN', 'MANAGER', 'EMPLOYEE'].forEach(value => {
        const card = document.getElementById('roleCard-' + value);
        const selected = value === role;
        card.classList.toggle('selected', selected);
        card.querySelector('input[type="radio"]').checked = selected;
    });
}
function handleAssignRole(event) {
    event.preventDefault();
    const id = document.getElementById('assignAccountSelect').value;
    const row = Array.from(document.querySelectorAll('#userTableBody tr')).find(item => item.dataset.id === id);
    const radio = document.querySelector('input[name="assignRoleRadio"]:checked');
    if (!row || !radio) return;
    const request = { username: row.dataset.username, fullName: row.dataset.fullname,
        email: row.dataset.email, role: radio.value };
    const url = document.getElementById('userTableBody').dataset.updateUrl + '/' + id;
    saveUser(url, request, 'Gán vai trò thành công.');
}

function openDeactivateModal(button) {
    targetUserRow = button.closest('tr');
    document.getElementById('deactivateUsername').textContent = targetUserRow.dataset.username;
    document.getElementById('deactivateFullName').textContent = targetUserRow.dataset.fullname;
    openModal('deactivateUserModal');
}
function confirmDeactivateUser() {
    if (!targetUserRow) return;
    const url = document.getElementById('userTableBody').dataset.deactivateUrl + '/' + targetUserRow.dataset.id;
    saveUser(url, undefined, 'Đã vô hiệu hóa tài khoản.');
}

function activateUser(button) {
    const row = button.closest('tr');
    if (!confirm('Kích hoạt lại tài khoản "' + row.dataset.username + '"?')) return;
    const url = document.getElementById('userTableBody').dataset.activateUrl + '/' + row.dataset.id;
    saveUser(url, undefined, 'Đã kích hoạt lại tài khoản.');
}

function filterUsers() {
    const keyword = document.getElementById('searchUserInput').value.trim().toLowerCase();
    const role = document.getElementById('userRoleFilter').value;
    const status = document.getElementById('userStatusFilter').value;
    const rows = document.querySelectorAll('#userTableBody tr');
    let count = 0;
    rows.forEach(row => {
        const text = (row.dataset.username + ' ' + row.dataset.fullname + ' ' + row.dataset.email).toLowerCase();
        const show = text.includes(keyword) && (!role || row.dataset.role === role) &&
            (!status || row.dataset.status === status);
        row.style.display = show ? '' : 'none';
        if (show) count++;
    });
    document.getElementById('paginationInfo').textContent =
        'Hiển thị ' + count + ' trên ' + rows.length + ' tài khoản';
}

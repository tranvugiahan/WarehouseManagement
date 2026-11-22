let targetUserRow = null;
let editingUserRow = null;

function openModal(id) { document.getElementById(id).classList.add('show'); }
function closeModal(id) { document.getElementById(id).classList.remove('show'); }

function setFieldError(inputId, message) {
    const input = document.getElementById(inputId);
    const errEl = document.getElementById('err-' + inputId);
    if (!input || !errEl) return;

    if (message) {
        input.classList.add('is-invalid');
        errEl.textContent = message;
        errEl.classList.add('show');
    } else {
        input.classList.remove('is-invalid');
        errEl.textContent = '';
        errEl.classList.remove('show');
    }
}

function clearFormValidation(fieldIds) {
    fieldIds.forEach(id => setFieldError(id, ''));
}

function getRoleBadgeHtml(role) {
    if (role === 'ADMIN') return '<span class="badge" style="background:#fee2e2; color:#b91c1c; font-weight:600;">Quản trị viên</span>';
    if (role === 'MANAGER') return '<span class="badge" style="background:#fef3c7; color:#b45309; font-weight:600;">Quản lý kho</span>';
    return '<span class="badge" style="background:#e0f2fe; color:#0369a1; font-weight:600;">Nhân viên kho</span>';
}

// --- TẠO TÀI KHOẢN MỚI (FR2.1) ---
function openCreateUserModal() {
    document.getElementById('createUserForm').reset();
    clearFormValidation(['createUsername', 'createPassword', 'createFullName', 'createEmail', 'createRole']);
    openModal('createUserModal');
}

function validateCreateField(fieldId) {
    const val = document.getElementById(fieldId).value.trim();
    const emailRegex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
    const usernameRegex = /^[a-zA-Z0-9_.-]{3,30}$/;

    if (fieldId === 'createUsername') {
        if (!val) { setFieldError(fieldId, 'Vui lòng nhập tên đăng nhập.'); return false; }
        if (!usernameRegex.test(val)) { setFieldError(fieldId, 'Tên đăng nhập từ 3-30 ký tự, không chứa khoảng trắng hoặc ký tự đặc biệt.'); return false; }
        const exists = Array.from(document.querySelectorAll('#userTableBody tr')).some(r => r.getAttribute('data-username').toLowerCase() === val.toLowerCase());
        if (exists) { setFieldError(fieldId, 'Tên đăng nhập này đã tồn tại trong hệ thống.'); return false; }
    }
    if (fieldId === 'createPassword') {
        if (!val) { setFieldError(fieldId, 'Vui lòng nhập mật khẩu ban đầu.'); return false; }
        if (val.length < 6) { setFieldError(fieldId, 'Mật khẩu phải có ít nhất 6 ký tự.'); return false; }
    }
    if (fieldId === 'createFullName') {
        if (!val) { setFieldError(fieldId, 'Vui lòng nhập họ và tên.'); return false; }
    }
    if (fieldId === 'createEmail') {
        if (!val) { setFieldError(fieldId, 'Vui lòng nhập địa chỉ email.'); return false; }
        if (!emailRegex.test(val)) { setFieldError(fieldId, 'Định dạng email không hợp lệ (VD: ten@wms.com).'); return false; }
    }
    if (fieldId === 'createRole') {
        if (!val) { setFieldError(fieldId, 'Vui lòng chọn vai trò phân quyền.'); return false; }
    }

    setFieldError(fieldId, '');
    return true;
}

function handleCreateUser(e) {
    e.preventDefault();
    const fields = ['createUsername', 'createPassword', 'createFullName', 'createEmail', 'createRole'];
    let isValid = true;
    fields.forEach(f => {
        if (!validateCreateField(f)) isValid = false;
    });
    if (!isValid) return;

    const username = document.getElementById('createUsername').value.trim();
    const fullName = document.getElementById('createFullName').value.trim();
    const email = document.getElementById('createEmail').value.trim();
    const role = document.getElementById('createRole').value;
    const status = document.getElementById('createStatus').value;

    const today = new Date();
    const dateStr = String(today.getDate()).padStart(2, '0') + '/' + String(today.getMonth() + 1).padStart(2, '0') + '/' + today.getFullYear();

    const statusBadge = status === 'active'
        ? '<span class="status-badge status-active">Hoạt động</span>'
        : '<span class="status-badge status-inactive">Đã khóa</span>';

    const actionButtons = status === 'active'
        ? `<button class="btn-action edit" title="Chỉnh sửa" onclick="openEditUserModal(this)"><i class="fa-solid fa-pen-to-square"></i></button>
           <button class="btn-action" style="color:#2563eb;" title="Gán vai trò" onclick="openAssignRoleModal(this)"><i class="fa-solid fa-user-shield"></i></button>
           <button class="btn-action" title="Đổi mật khẩu"><i class="fa-solid fa-key"></i></button>
           <button class="btn-action delete" style="color:#dc2626;" title="Vô hiệu hóa tài khoản" onclick="openDeactivateModal(this)"><i class="fa-solid fa-user-slash"></i></button>`
        : `<button class="btn-action edit" title="Chỉnh sửa" onclick="openEditUserModal(this)"><i class="fa-solid fa-pen-to-square"></i></button>
           <button class="btn-action" style="color:#2563eb;" title="Gán vai trò" onclick="openAssignRoleModal(this)"><i class="fa-solid fa-user-shield"></i></button>
           <button class="btn-action" title="Mở khóa"><i class="fa-solid fa-lock-open"></i></button>`;

    const tr = document.createElement('tr');
    tr.setAttribute('data-username', username);
    tr.setAttribute('data-fullname', fullName);
    tr.setAttribute('data-email', email);
    tr.setAttribute('data-role', role);
    tr.setAttribute('data-status', status);
    tr.innerHTML = `
        <td class="font-mono"><strong>${username}</strong></td>
        <td class="font-medium col-fullname">${fullName}</td>
        <td class="col-email">${email}</td>
        <td class="col-role">${getRoleBadgeHtml(role)}</td>
        <td class="status-cell">${statusBadge}</td>
        <td style="color:#64748b;">${dateStr}</td>
        <td class="text-right action-cell">${actionButtons}</td>
    `;

    document.getElementById('userTableBody').appendChild(tr);
    closeModal('createUserModal');
    filterUsers();
    alert(`Tạo tài khoản "${username}" thành công!`);
}

// --- CẬP NHẬT TÀI KHOẢN (FR2.2) ---
function openEditUserModal(button) {
    editingUserRow = button.closest('tr');
    clearFormValidation(['editFullName', 'editEmail', 'editRole']);

    document.getElementById('editUsername').value = editingUserRow.getAttribute('data-username');
    document.getElementById('editFullName').value = editingUserRow.getAttribute('data-fullname');
    document.getElementById('editEmail').value = editingUserRow.getAttribute('data-email');
    document.getElementById('editRole').value = editingUserRow.getAttribute('data-role');

    openModal('editUserModal');
}

function validateEditField(fieldId) {
    const val = document.getElementById(fieldId).value.trim();
    const emailRegex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;

    if (fieldId === 'editFullName') {
        if (!val) { setFieldError(fieldId, 'Họ và tên không được để trống.'); return false; }
    }
    if (fieldId === 'editEmail') {
        if (!val) { setFieldError(fieldId, 'Email không được để trống.'); return false; }
        if (!emailRegex.test(val)) { setFieldError(fieldId, 'Định dạng email không hợp lệ (VD: ten@wms.com).'); return false; }
    }
    if (fieldId === 'editRole') {
        if (!val) { setFieldError(fieldId, 'Vui lòng chọn vai trò.'); return false; }
    }

    setFieldError(fieldId, '');
    return true;
}

function handleUpdateUser(e) {
    e.preventDefault();
    if (!editingUserRow) return;

    const fields = ['editFullName', 'editEmail', 'editRole'];
    let isValid = true;
    fields.forEach(f => {
        if (!validateEditField(f)) isValid = false;
    });
    if (!isValid) return;

    const fullName = document.getElementById('editFullName').value.trim();
    const email = document.getElementById('editEmail').value.trim();
    const role = document.getElementById('editRole').value;

    editingUserRow.setAttribute('data-fullname', fullName);
    editingUserRow.setAttribute('data-email', email);
    editingUserRow.setAttribute('data-role', role);

    editingUserRow.querySelector('.col-fullname').textContent = fullName;
    editingUserRow.querySelector('.col-email').textContent = email;
    editingUserRow.querySelector('.col-role').innerHTML = getRoleBadgeHtml(role);

    const username = editingUserRow.getAttribute('data-username');
    closeModal('editUserModal');
    editingUserRow = null;
    filterUsers();
    alert(`Cập nhật thông tin tài khoản "${username}" thành công!`);
}

// --- GÁN VAI TRÒ CHO TÀI KHOẢN (FR2.4) ---
function openAssignRoleModal(button) {
    const row = button.closest('tr');
    const selectedUsername = row.getAttribute('data-username');
    const selectEl = document.getElementById('assignAccountSelect');

    selectEl.innerHTML = '';
    document.querySelectorAll('#userTableBody tr').forEach(r => {
        const u = r.getAttribute('data-username');
        const fn = r.getAttribute('data-fullname');
        const opt = document.createElement('option');
        opt.value = u;
        opt.textContent = `${fn} (${u})`;
        if (u === selectedUsername) opt.selected = true;
        selectEl.appendChild(opt);
    });

    selectRoleOption(row.getAttribute('data-role'));
    openModal('assignRoleModal');
}

function onAssignAccountChange() {
    const selectedUsername = document.getElementById('assignAccountSelect').value;
    const row = Array.from(document.querySelectorAll('#userTableBody tr')).find(r => r.getAttribute('data-username') === selectedUsername);
    if (row) {
        selectRoleOption(row.getAttribute('data-role'));
    }
}

function selectRoleOption(role) {
    ['ADMIN', 'MANAGER', 'EMPLOYEE'].forEach(r => {
        const card = document.getElementById('roleCard-' + r);
        const radio = card ? card.querySelector('input[type="radio"]') : null;
        if (r === role) {
            card.classList.add('selected');
            if (radio) radio.checked = true;
        } else {
            card.classList.remove('selected');
            if (radio) radio.checked = false;
        }
    });
}

function handleAssignRole(e) {
    e.preventDefault();
    const username = document.getElementById('assignAccountSelect').value;
    const checkedRadio = document.querySelector('input[name="assignRoleRadio"]:checked');
    if (!username || !checkedRadio) return;

    const newRole = checkedRadio.value;
    const row = Array.from(document.querySelectorAll('#userTableBody tr')).find(r => r.getAttribute('data-username') === username);
    if (row) {
        row.setAttribute('data-role', newRole);
        row.querySelector('.col-role').innerHTML = getRoleBadgeHtml(newRole);
    }

    closeModal('assignRoleModal');
    filterUsers();
    alert(`Đã gán vai trò "${newRole}" cho tài khoản "${username}" thành công!`);
}

// --- VÔ HIỆU HÓA TÀI KHOẢN (FR2.3) ---
function openDeactivateModal(button) {
    targetUserRow = button.closest('tr');
    const username = targetUserRow.getAttribute('data-username');
    const fullName = targetUserRow.getAttribute('data-fullname');

    document.getElementById('deactivateUsername').textContent = username;
    document.getElementById('deactivateFullName').textContent = fullName;
    openModal('deactivateUserModal');
}

function confirmDeactivateUser() {
    if (!targetUserRow) return;

    targetUserRow.setAttribute('data-status', 'inactive');

    const statusCell = targetUserRow.querySelector('.status-cell');
    if (statusCell) {
        statusCell.innerHTML = '<span class="status-badge status-inactive">Đã khóa</span>';
    }

    const actionCell = targetUserRow.querySelector('.action-cell');
    if (actionCell) {
        actionCell.innerHTML = `
            <button class="btn-action edit" title="Chỉnh sửa" onclick="openEditUserModal(this)"><i class="fa-solid fa-pen-to-square"></i></button>
            <button class="btn-action" style="color:#2563eb;" title="Gán vai trò" onclick="openAssignRoleModal(this)"><i class="fa-solid fa-user-shield"></i></button>
            <button class="btn-action" title="Mở khóa"><i class="fa-solid fa-lock-open"></i></button>
        `;
    }

    const username = targetUserRow.getAttribute('data-username');
    closeModal('deactivateUserModal');
    targetUserRow = null;
    filterUsers();
    alert(`Đã vô hiệu hóa tài khoản "${username}" thành công.`);
}

// --- TÌM KIẾM & LỌC ---
function filterUsers() {
    const keyword = document.getElementById('searchUserInput').value.toLowerCase();
    const role = document.getElementById('userRoleFilter').value;
    const status = document.getElementById('userStatusFilter').value;
    const rows = document.querySelectorAll('#userTableBody tr');
    let visibleCount = 0;

    rows.forEach(row => {
        const text = row.innerText.toLowerCase();
        const rowRole = row.getAttribute('data-role');
        const rowStatus = row.getAttribute('data-status');

        const matchText = text.includes(keyword);
        const matchRole = !role || rowRole === role;
        const matchStatus = !status || rowStatus === status;

        const show = (matchText && matchRole && matchStatus);
        row.style.display = show ? '' : 'none';
        if (show) visibleCount++;
    });

    const info = document.getElementById('paginationInfo');
    if (info) {
        info.textContent = `Hiển thị ${visibleCount} trên ${rows.length} tài khoản`;
    }
}

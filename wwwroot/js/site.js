function fetchSlots(doctorId, date, containerId) {
    const container = document.getElementById(containerId);
    if (!container) return;
    container.innerHTML = '<p style="color: var(--text-muted);">Loading slots...</p>';

    fetch(`/api/AppointmentApi/slots?doctorId=${doctorId}&date=${date}`)
        .then(r => r.json())
        .then(data => {
            container.innerHTML = '';
            if (data.message) {
                container.innerHTML = `<p style="color: var(--warning);">${data.message}</p>`;
                return;
            }
            if (data.slots.length === 0) {
                container.innerHTML = '<p style="color: var(--text-muted);">No slots available for this date.</p>';
                return;
            }
            data.slots.forEach(slot => {
                const div = document.createElement('div');
                div.className = 'time-slot';
                div.textContent = slot;
                div.onclick = () => selectSlot(div, slot);
                container.appendChild(div);
            });
        })
        .catch(() => {
            container.innerHTML = '<p style="color: var(--danger);">Error loading slots.</p>';
        });
}

function selectSlot(el, slot) {
    document.querySelectorAll('.time-slot').forEach(s => s.classList.remove('selected'));
    el.classList.add('selected');
    const input = document.getElementById('TimeSlot');
    if (input) input.value = slot;
}

function showToast(message, type) {
    let container = document.querySelector('.toast-container');
    if (!container) {
        container = document.createElement('div');
        container.className = 'toast-container';
        document.body.appendChild(container);
    }
    const toast = document.createElement('div');
    toast.className = 'toast';
    const icon = type === 'success' ? '✓' : type === 'error' ? '✕' : 'ℹ';
    toast.innerHTML = `<span>${icon}</span> ${message}`;
    container.appendChild(toast);
    setTimeout(() => toast.remove(), 3500);
}

document.addEventListener('DOMContentLoaded', function () {
    const successMsg = document.querySelector('[data-success-message]');
    if (successMsg && successMsg.dataset.successMessage) {
        showToast(successMsg.dataset.successMessage, 'success');
    }

    const dateInput = document.getElementById('AppointmentDate');
    if (dateInput) {
        const doctorId = document.getElementById('DoctorId');
        if (doctorId) {
            dateInput.addEventListener('change', function () {
                fetchSlots(doctorId.value, this.value, 'slotsContainer');
            });
            if (dateInput.value) {
                fetchSlots(doctorId.value, dateInput.value, 'slotsContainer');
            }
        }
    }
});

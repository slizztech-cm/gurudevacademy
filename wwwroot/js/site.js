// Gurudev Defence Academy — shared client JS

// Password show/hide toggle (data-pw-toggle="targetId")
document.addEventListener('click', function (e) {
    const t = e.target.closest('[data-pw-toggle]');
    if (!t) return;
    const inp = document.getElementById(t.getAttribute('data-pw-toggle'));
    if (!inp) return;
    const show = inp.type === 'password';
    inp.type = show ? 'text' : 'password';
    t.textContent = show ? '🙈' : '👁';
});

// Toast helper
function showToast(msg) {
    let el = document.getElementById('toast');
    if (!el) {
        el = document.createElement('div');
        el.id = 'toast';
        el.className = 'toast';
        document.body.appendChild(el);
    }
    el.textContent = msg;
    el.classList.add('show');
    setTimeout(() => el.classList.remove('show'), 3000);
}

// Confirm-before-action (data-confirm="message")
document.addEventListener('submit', function (e) {
    const f = e.target;
    const msg = f.getAttribute && f.getAttribute('data-confirm');
    if (msg && !confirm(msg)) e.preventDefault();
});

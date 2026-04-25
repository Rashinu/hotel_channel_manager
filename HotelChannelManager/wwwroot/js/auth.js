function getToken() {
  return localStorage.getItem('token');
}

function requireAuth() {
  if (!getToken()) {
    window.location.href = '/index.html';
  }
}

function logout() {
  localStorage.removeItem('token');
  window.location.href = '/index.html';
}

async function apiFetch(url, options = {}) {
  const token = getToken();
  const res = await fetch(url, {
    ...options,
    headers: {
      'Content-Type': 'application/json',
      ...(token ? { Authorization: `Bearer ${token}` } : {}),
      ...(options.headers || {})
    }
  });

  if (res.status === 401) {
    logout();
    return null;
  }

  return res;
}

function formatDateTime(val) {
  if (!val) return '—';
  const d = new Date(val);
  if (isNaN(d)) return '—';
  const pad = n => String(n).padStart(2, '0');
  return `${d.getDate()}.${pad(d.getMonth() + 1)}.${d.getFullYear()} ${pad(d.getHours())}:${pad(d.getMinutes())}`;
}

function formatDate(val) {
  if (!val) return '—';
  const d = new Date(val);
  if (isNaN(d)) return '—';
  const pad = n => String(n).padStart(2, '0');
  return `${d.getDate()}.${pad(d.getMonth() + 1)}.${d.getFullYear()}`;
}

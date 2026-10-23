'use strict';

const $ = (sel) => document.querySelector(sel);

const MIN_GAP = 1; // duración mínima del recorte, en segundos

const state = {
  info: null,
  kind: 'video',
  start: 0,
  end: 0,
  player: null,
  playerCreated: false,
  playerReady: false,
  wantedVideo: null,
  previewing: false,
  jobs: new Map(),
};

// ---------------------------------------------------------------------------
// Formato de datos
// ---------------------------------------------------------------------------

const nf1 = new Intl.NumberFormat('es', { maximumFractionDigits: 1 });
const nfCompact = new Intl.NumberFormat('es', { notation: 'compact', maximumFractionDigits: 1 });

function fmtTime(sec, withTenths = true) {
  sec = Math.max(0, sec || 0);
  let whole = Math.floor(sec);
  let tenths = Math.round((sec - whole) * 10);
  if (tenths === 10) { whole += 1; tenths = 0; }
  const h = Math.floor(whole / 3600);
  const m = Math.floor((whole % 3600) / 60);
  const s = String(whole % 60).padStart(2, '0');
  let out = h ? `${h}:${String(m).padStart(2, '0')}:${s}` : `${m}:${s}`;
  if (withTenths && tenths) out += `.${tenths}`;
  return out;
}

function parseTime(text) {
  const t = String(text).trim().replace(',', '.');
  if (!/^\d+(\.\d+)?$|^\d+:\d{1,2}(\.\d+)?$|^\d+:\d{1,2}:\d{1,2}(\.\d+)?$/.test(t)) return null;
  return t.split(':').reduce((acc, part) => acc * 60 + parseFloat(part), 0);
}

function fmtBytes(n) {
  if (!n) return '';
  const units = ['B', 'KB', 'MB', 'GB'];
  let i = 0;
  while (n >= 1024 && i < units.length - 1) { n /= 1024; i += 1; }
  return `${nf1.format(n)} ${units[i]}`;
}

function fmtDate(yyyymmdd) {
  if (!yyyymmdd || yyyymmdd.length !== 8) return '';
  const d = new Date(`${yyyymmdd.slice(0, 4)}-${yyyymmdd.slice(4, 6)}-${yyyymmdd.slice(6)}T12:00:00`);
  return d.toLocaleDateString('es', { day: 'numeric', month: 'short', year: 'numeric' });
}

const clamp = (v, lo, hi) => Math.min(Math.max(v, lo), hi);
const round1 = (v) => Math.round(v * 10) / 10;

// ---------------------------------------------------------------------------
// API
// ---------------------------------------------------------------------------

// Dentro de la app de Android la misma API la atiende el código nativo a través
// de este puente; en el navegador se usa HTTP contra el servidor de Python.
const native = window.AndroidApp || null;
const nativeCalls = new Map();
let nativeSeq = 0;

if (native) {
  native.onmessage = (event) => {
    const { id, status, data } = JSON.parse(event.data);
    const resolve = nativeCalls.get(id);
    nativeCalls.delete(id);
    if (resolve) resolve({ status, data });
  };
}

function nativeRequest(path, body) {
  return new Promise((resolve) => {
    const id = String(++nativeSeq);
    nativeCalls.set(id, resolve);
    native.postMessage(JSON.stringify({ action: 'request', id, path, body: body ? JSON.stringify(body) : '' }));
  });
}

const nativeAction = (action, job) => native.postMessage(JSON.stringify({ action, job }));

async function api(path, body) {
  let status;
  let data = null;
  if (native) {
    ({ status, data } = await nativeRequest(path, body));
  } else {
    const options = body
      ? { method: 'POST', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify(body) }
      : undefined;
    const res = await fetch(path, options);
    status = res.status;
    try { data = await res.json(); } catch { /* respuesta sin JSON */ }
  }
  if (status >= 400) {
    const err = new Error(data?.error || `Error del servidor (${status})`);
    err.status = status;
    throw err;
  }
  return data;
}

function setLoading(btn, loading) {
  btn.classList.toggle('loading', loading);
  btn.disabled = loading;
}

function showError(el, message) {
  el.textContent = message || '';
  el.hidden = !message;
}

// ---------------------------------------------------------------------------
// Búsqueda
// ---------------------------------------------------------------------------

async function search(url) {
  url = url.trim();
  if (!url) return;
  const btn = $('#search-btn');
  setLoading(btn, true);
  showError($('#search-error'), '');
  try {
    loadVideo(await api('/api/info', { url }));
  } catch (err) {
    showError($('#search-error'), err.message);
  } finally {
    setLoading(btn, false);
  }
}

$('#search').addEventListener('submit', (e) => {
  e.preventDefault();
  search($('#url').value);
});

$('#url').addEventListener('paste', () => {
  // Buscar automáticamente al pegar un enlace.
  setTimeout(() => {
    const value = $('#url').value.trim();
    if (/youtu\.?be/i.test(value)) search(value);
  }, 0);
});

function loadVideo(info) {
  state.info = info;
  state.start = 0;
  state.end = info.duration || 0;

  $('#v-title').textContent = info.title;
  const views = info.view_count ? `${nfCompact.format(info.view_count)} visualizaciones` : '';
  $('#v-sub').textContent = [info.channel, fmtTime(info.duration, false), views, fmtDate(info.upload_date)]
    .filter(Boolean).join(' · ');
  $('#thumb').src = info.thumbnail || '';
  $('.player-wrap').style.backgroundImage = info.thumbnail ? `url(${JSON.stringify(info.thumbnail)})` : '';

  $('#trim-card').hidden = !(info.duration >= MIN_GAP * 2);
  setupRange();
  renderQualities();

  $('#video').hidden = false;
  $('#options').hidden = false;
  showError($('#download-error'), '');
  loadPlayer(info.id);
  updateSummary();
  $('#video').scrollIntoView({ behavior: 'smooth', block: 'start' });
}

// ---------------------------------------------------------------------------
// Reproductor de YouTube
// ---------------------------------------------------------------------------

let ytApi = null;

function loadYouTubeApi() {
  if (window.YT?.Player) return Promise.resolve(window.YT);
  if (!ytApi) {
    ytApi = new Promise((resolve, reject) => {
      const fail = () => { ytApi = null; reject(new Error('No se pudo cargar el reproductor')); };
      const timer = setTimeout(fail, 15000);
      window.onYouTubeIframeAPIReady = () => { clearTimeout(timer); resolve(window.YT); };
      const script = document.createElement('script');
      script.src = 'https://www.youtube.com/iframe_api';
      script.onerror = () => { clearTimeout(timer); fail(); };
      document.head.appendChild(script);
    });
  }
  return ytApi;
}

function setPlayerAvailable(available) {
  state.playerReady = available;
  $('#player-fallback').hidden = available;
  document.querySelectorAll('.player-btn').forEach((b) => { b.disabled = !available; });
  if (!available) $('#playhead').hidden = true;
}

async function loadPlayer(videoId) {
  state.wantedVideo = videoId;
  setPlayerAvailable(false);
  $('#player-fallback').hidden = true; // mientras carga se ve la miniatura de fondo
  try {
    const YT = await loadYouTubeApi();
    if (state.player) {
      // Si el reproductor aún se está creando, onReady cargará el video pedido.
      if (state.playerCreated) {
        state.player.cueVideoById(videoId);
        setPlayerAvailable(true);
      }
      return;
    }
    state.player = new YT.Player('player', {
      videoId,
      width: '100%',
      height: '100%',
      playerVars: { rel: 0, playsinline: 1, origin: location.origin },
      events: {
        onReady: () => {
          state.playerCreated = true;
          if (state.wantedVideo !== videoId) state.player.cueVideoById(state.wantedVideo);
          setPlayerAvailable(true);
        },
        onError: () => setPlayerAvailable(false),
      },
    });
  } catch {
    if (state.wantedVideo === videoId) setPlayerAvailable(false);
  }
}

function currentTime() {
  try { return state.player.getCurrentTime() || 0; } catch { return 0; }
}

function seek(seconds) {
  if (!state.playerReady) return;
  state.player.seekTo(seconds, true);
}

setInterval(() => {
  if (!state.playerReady || !state.info?.duration) return;
  const t = currentTime();
  const playhead = $('#playhead');
  playhead.hidden = false;
  playhead.style.left = `${(t / state.info.duration) * 100}%`;
  if (state.previewing && t >= state.end) {
    state.previewing = false;
    state.player.pauseVideo();
  }
}, 200);

// ---------------------------------------------------------------------------
// Recorte
// ---------------------------------------------------------------------------

const rangeStart = $('#range-start');
const rangeEnd = $('#range-end');

function setupRange() {
  const d = state.info.duration || 0;
  for (const r of [rangeStart, rangeEnd]) {
    r.min = 0;
    r.max = d;
    r.step = 0.1;
  }
  $('#scale-end').textContent = fmtTime(d, false);
  syncTrim();
}

function isTrimmed() {
  const d = state.info?.duration;
  return Boolean(d) && (state.start > 0.05 || state.end < d - 0.05);
}

function setStart(value) {
  state.start = round1(clamp(value, 0, state.end - MIN_GAP));
  syncTrim();
}

function setEnd(value) {
  state.end = round1(clamp(value, state.start + MIN_GAP, state.info.duration));
  syncTrim();
}

function syncTrim() {
  const d = state.info.duration || 1;
  rangeStart.value = state.start;
  rangeEnd.value = state.end;
  $('#start').value = fmtTime(state.start);
  $('#end').value = fmtTime(state.end);
  $('#start').classList.remove('invalid');
  $('#end').classList.remove('invalid');

  const fill = $('#range-fill');
  fill.style.left = `${(state.start / d) * 100}%`;
  fill.style.width = `${((state.end - state.start) / d) * 100}%`;

  const trimmed = isTrimmed();
  const pill = $('#trim-length');
  pill.textContent = trimmed ? `Recorte de ${fmtTime(state.end - state.start)}` : 'Video completo';
  pill.classList.toggle('active', trimmed);
  updateSummary();
}

rangeStart.addEventListener('input', () => setStart(parseFloat(rangeStart.value)));
rangeEnd.addEventListener('input', () => setEnd(parseFloat(rangeEnd.value)));
rangeStart.addEventListener('change', () => seek(state.start));
rangeEnd.addEventListener('change', () => seek(state.end));

// Cuando los dos tiradores están cerca, poner encima el más próximo al puntero.
$('#range').addEventListener('pointermove', (e) => {
  if (e.buttons) return;
  const rect = $('#range-track').getBoundingClientRect();
  const t = clamp((e.clientX - rect.left) / rect.width, 0, 1) * (state.info?.duration || 0);
  const startCloser = Math.abs(t - state.start) <= Math.abs(t - state.end);
  rangeStart.style.zIndex = startCloser ? 3 : 2;
  rangeEnd.style.zIndex = startCloser ? 2 : 3;
});

$('#range-track').addEventListener('click', (e) => {
  const rect = e.currentTarget.getBoundingClientRect();
  seek(clamp((e.clientX - rect.left) / rect.width, 0, 1) * state.info.duration);
});

for (const [id, setter] of [['#start', setStart], ['#end', setEnd]]) {
  const input = $(id);
  input.addEventListener('change', () => {
    const value = parseTime(input.value);
    if (value === null) {
      input.classList.remove('invalid');
      void input.offsetWidth; // reiniciar la animación
      input.classList.add('invalid');
      return;
    }
    setter(value);
  });
  input.addEventListener('keydown', (e) => {
    if (e.key === 'Enter') input.dispatchEvent(new Event('change'));
  });
}

$('#set-start').addEventListener('click', () => setStart(currentTime()));
$('#set-end').addEventListener('click', () => setEnd(currentTime()));

$('#preview-btn').addEventListener('click', () => {
  if (!state.playerReady) return;
  state.player.seekTo(state.start, true);
  state.player.playVideo();
  state.previewing = true;
});

$('#reset-trim').addEventListener('click', () => {
  state.start = 0;
  state.end = state.info.duration;
  syncTrim();
});

// ---------------------------------------------------------------------------
// Formato y calidad
// ---------------------------------------------------------------------------

const radioValue = (name) => document.querySelector(`input[name="${name}"]:checked`)?.value;

function renderQualities() {
  const box = $('#quality-chips');
  box.textContent = '';
  const qualities = state.info.qualities || [];
  if (!qualities.length) {
    box.innerHTML = '<p class="muted">No se encontraron calidades; se descargará la mejor disponible.</p>';
    return;
  }
  const preferred = qualities.find((q) => q.res <= 1080) || qualities[qualities.length - 1];
  for (const q of qualities) {
    const label = document.createElement('label');
    label.className = 'chip';
    label.innerHTML = '<input type="radio" name="quality"><span><b></b><small></small></span>';
    const input = label.querySelector('input');
    input.value = q.res;
    input.checked = q === preferred;
    label.querySelector('b').textContent = q.label;
    if (q.badge) {
      const badge = document.createElement('em');
      badge.textContent = q.badge;
      label.querySelector('b').append(badge);
    }
    label.querySelector('small').dataset.size = q.size || '';
    box.append(label);
  }
}

document.querySelectorAll('.segmented button').forEach((btn) => {
  btn.addEventListener('click', () => {
    state.kind = btn.dataset.kind;
    document.querySelectorAll('.segmented button').forEach((b) => {
      b.classList.toggle('active', b === btn);
      b.setAttribute('aria-checked', String(b === btn));
    });
    $('#video-opts').hidden = state.kind !== 'video';
    $('#audio-opts').hidden = state.kind !== 'audio';
    updateSummary();
  });
});

$('#options').addEventListener('change', (e) => {
  if (e.target.matches('input[type="radio"], #precise')) updateSummary();
});

function estimateBytes() {
  const info = state.info;
  const seconds = state.end - state.start;
  if (state.kind === 'video') {
    const q = info.qualities.find((x) => String(x.res) === radioValue('quality'));
    if (!q?.size || !info.duration) return null;
    return q.size * (seconds / info.duration);
  }
  const format = radioValue('audio_format');
  if (format === 'wav') return 192000 * seconds; // PCM 16 bits, 48 kHz, estéreo
  if (format === 'flac') return 110000 * seconds;
  return Number(radioValue('audio_quality')) * 125 * seconds;
}

function updateSummary() {
  const info = state.info;
  if (!info) return;
  const trimmed = isTrimmed();
  const fraction = info.duration ? (state.end - state.start) / info.duration : 1;

  // Tamaño aproximado en cada chip de calidad.
  document.querySelectorAll('#quality-chips small').forEach((small) => {
    const size = Number(small.dataset.size);
    small.textContent = size ? `≈ ${fmtBytes(size * fraction)}` : '';
  });

  const lossless = ['wav', 'flac'].includes(radioValue('audio_format'));
  document.querySelectorAll('input[name="audio_quality"]').forEach((input) => { input.disabled = lossless; });

  const quality = Number(radioValue('quality'));
  $('#codec-hint').hidden = !(radioValue('container') === 'mp4' && quality > 1080);
  $('#precise-row').hidden = !(trimmed && state.kind === 'video');

  let summary;
  if (state.kind === 'video') {
    const q = info.qualities.find((x) => x.res === quality);
    summary = `Video ${q ? q.label : ''} · ${radioValue('container').toUpperCase()}`;
  } else {
    const format = radioValue('audio_format');
    summary = `Audio ${format.toUpperCase()}${lossless ? '' : ` · ${radioValue('audio_quality')} kbps`}`;
  }
  summary += trimmed ? ` · ${fmtTime(state.start)} → ${fmtTime(state.end)}` : ' · video completo';
  $('#summary').textContent = summary;

  const bytes = estimateBytes();
  $('#estimate').textContent = bytes ? `Tamaño aproximado: ${fmtBytes(bytes)}` : '';
}

// ---------------------------------------------------------------------------
// Descargas
// ---------------------------------------------------------------------------

$('#download-btn').addEventListener('click', async () => {
  const info = state.info;
  const trimmed = isTrimmed();
  const body = {
    id: info.id,
    title: info.title,
    label: $('#summary').textContent,
    kind: state.kind,
    start: trimmed && state.start > 0.05 ? state.start : null,
    end: trimmed && state.end < info.duration - 0.05 ? state.end : null,
  };
  if (state.kind === 'video') {
    body.quality = radioValue('quality') || 'best';
    body.container = radioValue('container');
    body.precise = $('#precise').checked;
  } else {
    body.audio_format = radioValue('audio_format');
    body.audio_quality = radioValue('audio_quality');
  }

  const btn = $('#download-btn');
  setLoading(btn, true);
  showError($('#download-error'), '');
  try {
    const job = await api('/api/download', body);
    addJob(job, info.title, $('#summary').textContent);
  } catch (err) {
    showError($('#download-error'), err.message);
  } finally {
    setLoading(btn, false);
  }
});

const FINISHED = ['done', 'error', 'cancelled'];

function addJob(job, title, meta) {
  const el = $('#job-template').content.firstElementChild.cloneNode(true);
  el.querySelector('.job-title').textContent = title;
  el.querySelector('.job-meta').textContent = meta;
  // Las descargas ya terminadas que se recuperan al abrir la app no se vuelven a guardar.
  const entry = { ...job, el, startedAt: Date.now(), saved: FINISHED.includes(job.status) };
  state.jobs.set(job.id, entry);
  $('#job-list').prepend(el);
  $('#downloads').hidden = false;

  el.querySelector('.job-cancel').addEventListener('click', async () => {
    try { Object.assign(entry, await api(`/api/jobs/${entry.id}/cancel`, {})); } catch { /* ya terminó */ }
    renderJob(entry);
  });
  el.querySelector('.job-open').addEventListener('click', () => nativeAction('open', entry.id));
  el.querySelector('.job-share').addEventListener('click', () => nativeAction('share', entry.id));

  renderJob(entry);
  ensurePolling();
}

function renderJob(job) {
  const { el } = job;
  const active = !FINISHED.includes(job.status);
  el.classList.toggle('done', job.status === 'done');
  el.classList.toggle('error', job.status === 'error' || job.status === 'cancelled');

  const bar = el.querySelector('.bar');
  const fill = bar.querySelector('span');
  bar.classList.toggle('indeterminate', active && job.progress == null);
  fill.style.width = `${job.status === 'done' ? 100 : job.progress || 0}%`;

  let text;
  if (job.status === 'done') {
    text = [job.location ? `Guardado en ${job.location}` : 'Listo', fmtBytes(job.size)].filter(Boolean).join(' · ');
  } else if (job.status === 'error') {
    text = job.error || 'Error';
  } else if (job.status === 'cancelled') {
    text = 'Cancelada';
  } else {
    const parts = [job.stage];
    if (job.progress != null) parts.push(`${nf1.format(job.progress)} %`);
    if (job.speed) parts.push(`${fmtBytes(job.speed)}/s`);
    if (job.eta) parts.push(`quedan ${fmtTime(job.eta, false)}`);
    if (job.progress == null) parts.push(fmtTime((Date.now() - job.startedAt) / 1000, false));
    text = parts.filter(Boolean).join(' · ');
  }
  el.querySelector('.job-stage').textContent = text;

  const done = job.status === 'done';
  const save = el.querySelector('.job-save');
  save.hidden = native || !done;
  if (done && !native) {
    save.href = `/api/jobs/${job.id}/file`;
    save.setAttribute('download', job.filename || '');
  }
  // En la app el archivo ya queda en Descargas: se ofrece abrirlo, compartirlo o cancelar.
  el.querySelector('.job-open').hidden = !(native && done);
  el.querySelector('.job-share').hidden = !(native && done);
  el.querySelector('.job-cancel').hidden = !(native && active);
}

function autoSave(job) {
  if (job.saved || native) return;
  job.saved = true;
  job.el.querySelector('.job-save').click();
}

let pollTimer = null;
let polling = false;

function ensurePolling() {
  if (!pollTimer) pollTimer = setInterval(pollJobs, 1000);
}

async function pollJobs() {
  if (polling) return;
  const active = [...state.jobs.values()].filter((j) => !FINISHED.includes(j.status));
  if (!active.length) {
    clearInterval(pollTimer);
    pollTimer = null;
    return;
  }
  polling = true;
  await Promise.all(active.map(async (job) => {
    try {
      Object.assign(job, await api(`/api/jobs/${job.id}`));
    } catch (err) {
      if (err.status === 404) {
        job.status = 'error';
        job.error = 'La descarga ya no existe en el servidor.';
      }
    }
    renderJob(job);
    if (job.status === 'done') autoSave(job);
  }));
  polling = false;
}

// ---------------------------------------------------------------------------
// Inicio
// ---------------------------------------------------------------------------

function showVersion(version) {
  $('#version').textContent = version ? `yt-dlp ${version}` : '';
}

$('#update-btn').addEventListener('click', async () => {
  const btn = $('#update-btn');
  btn.disabled = true;
  btn.textContent = 'Buscando actualización…';
  try {
    const result = await api('/api/update', {});
    showVersion(result.yt_dlp);
    btn.textContent = result.updated ? 'yt-dlp actualizado' : 'Ya tienes la última versión';
  } catch (err) {
    btn.textContent = err.message;
  } finally {
    btn.disabled = false;
  }
});

// La app de Android llama a esto cuando se comparte un enlace desde YouTube.
window.loadSharedUrl = (url) => {
  $('#url').value = url;
  search(url);
};

(async () => {
  $('#update-btn').hidden = !native;
  const banner = $('#banner');
  // La primera vez, la app de Android tarda en preparar Python y ffmpeg.
  const preparing = native && setTimeout(() => {
    banner.textContent = 'Preparando la app… la primera vez puede tardar hasta un minuto.';
    banner.hidden = false;
  }, 1500);
  try {
    const status = await api('/api/status');
    showVersion(status.yt_dlp);
    banner.hidden = true;
    if (!status.ffmpeg) {
      banner.textContent = 'ffmpeg no está instalado en el servidor: no se podrá unir video y audio, '
        + 'recortar ni convertir. Instálalo y reinicia la aplicación (ver README).';
      banner.hidden = false;
    }
  } catch (err) {
    if (native) {
      banner.textContent = err.message;
      banner.hidden = false;
    }
  } finally {
    clearTimeout(preparing);
  }

  if (native) {
    // Recuperar las descargas que siguen en marcha si la app se cerró y se volvió a abrir.
    try {
      const { jobs } = await api('/api/jobs');
      for (const job of jobs) addJob(job, job.title, job.label);
    } catch { /* sin descargas previas */ }
  }

  // Permite abrir la página con ?url=… (por ejemplo, desde un marcador).
  const shared = new URLSearchParams(location.search).get('url');
  if (shared) {
    $('#url').value = shared;
    search(shared);
  } else if (!native) {
    $('#url').focus();
  }
})();

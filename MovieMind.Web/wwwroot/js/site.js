/* MovieMind — database-backed film catalog.
 * Vanilla JavaScript, no packages. Hash routing: #/  #/library  #/watchlist  #/studio  #/film/<id>
 * Film metadata is saved through the MovieMind API; watchlist and theme stay in this browser.
 * Sign in and registration use the API with a JWT stored for this browser session.
 */
(() => {
  'use strict';

  /* ------------------------------------------------------------------ config */
  const CFG = window.MOVIEMIND_CONFIG || {};
  const KEYS = { films: 'moviemind.v3.films', watch: 'moviemind.v3.watchlist', seen: 'moviemind.v3.seen', view: 'moviemind.v3.view', theme: 'moviemind.theme' };
  const LEGACY = { films: 'moviemind.custom.movies.v2', watch: 'moviemind.custom.saved.v2' };
  const GENRES = ['Action', 'Adventure', 'Animation', 'Biography', 'Comedy', 'Crime', 'Documentary', 'Drama', 'Family', 'Fantasy', 'History', 'Horror', 'Musical', 'Mystery', 'Romance', 'Sci-Fi', 'Thriller', 'War', 'Western'];
  const AGE_RATINGS = ['G', 'PG', 'PG-13', 'R', 'NC-17', 'AG', 'AP-12', 'N-15', 'IM-18', 'Not rated'];
  const PALETTES = [
    { name: 'Midnight', a: '#22385c', b: '#0a1220', ink: '#f4ead2', acc: '#f2b544' },
    { name: 'Desert', a: '#e59a52', b: '#6e3014', ink: '#2a1206', acc: '#fde3b0' },
    { name: 'Lagoon', a: '#155a5c', b: '#05201f', ink: '#e7f3ea', acc: '#7fd8bd' },
    { name: 'Oxblood', a: '#62121a', b: '#160406', ink: '#f6e4d4', acc: '#ee5a3f' },
    { name: 'Dusk', a: '#41307a', b: '#120c2b', ink: '#f2e8ff', acc: '#ff9fc6' },
    { name: 'Paper', a: '#efe5cf', b: '#c2b08d', ink: '#1c160f', acc: '#b8341f' },
    { name: 'Glacier', a: '#c9dfee', b: '#58809f', ink: '#0d1a26', acc: '#ffffff' },
    { name: 'Hazard', a: '#2c2c2a', b: '#0d0d0c', ink: '#f6d84a', acc: '#f6d84a' },
    { name: 'Blush', a: '#f4c6b8', b: '#cf6c62', ink: '#2c0f12', acc: '#fff4ee' },
    { name: 'Forest', a: '#2e4a2b', b: '#0d180c', ink: '#eff0d8', acc: '#dcc56b' },
  ];
  const MOTIFS = { rings: 'Rings', orb: 'Orbit', dunes: 'Dunes', sun: 'Horizon', stairs: 'Stairs', rain: 'Rain', bands: 'Bands', grid: 'Multiverse', window: 'Window', stars: 'Stars', spot: 'Spotlight' };

  /* --------------------------------------------------------------- helpers */
  const $ = (sel, root = document) => root.querySelector(sel);
  const $$ = (sel, root = document) => [...root.querySelectorAll(sel)];
  const esc = v => String(v ?? '').replace(/[&<>"']/g, c => ({ '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;', "'": '&#39;' })[c]);
  const clamp = (n, lo, hi) => Math.max(lo, Math.min(hi, n));
  const str = (v, max = 300) => String(v ?? '').trim().slice(0, max);
  const list = (v, max = 40) => (Array.isArray(v) ? v : String(v || '').split(',')).map(x => str(x, 120)).filter(Boolean).slice(0, max);
  const uid = () => {
    if (globalThis.crypto?.randomUUID) return globalThis.crypto.randomUUID();
    const bytes = new Uint8Array(16);
    if (globalThis.crypto?.getRandomValues) globalThis.crypto.getRandomValues(bytes);
    else bytes.forEach((_, i) => { bytes[i] = Math.floor(Math.random() * 256); });
    bytes[6] = (bytes[6] & 15) | 64;
    bytes[8] = (bytes[8] & 63) | 128;
    const hex = [...bytes].map(b => b.toString(16).padStart(2, '0')).join('');
    return `${hex.slice(0, 8)}-${hex.slice(8, 12)}-${hex.slice(12, 16)}-${hex.slice(16, 20)}-${hex.slice(20)}`;
  };
  const plural = (n, one, many = `${one}s`) => `${n} ${n === 1 ? one : many}`;
  const lenClass = t => (t.length > 22 ? 'is-long' : t.length > 12 ? 'is-mid' : '');
  const runtime = m => (m ? (m >= 60 ? `${Math.floor(m / 60)}h ${String(m % 60).padStart(2, '0')}m` : `${m}m`) : '');
  const initials = name => String(name).split(/\s+/).filter(Boolean).slice(0, 2).map(p => p[0]).join('').toUpperCase();
  const hash = s => { let h = 2166136261; for (const ch of String(s)) { h ^= ch.charCodeAt(0); h = Math.imul(h, 16777619); } return h >>> 0; };
  const rng = seed => () => { seed |= 0; seed = (seed + 0x6D2B79F5) | 0; let t = Math.imul(seed ^ (seed >>> 15), 1 | seed); t = (t + Math.imul(t ^ (t >>> 7), 61 | t)) ^ t; return ((t ^ (t >>> 14)) >>> 0) / 4294967296; };
  const read = (key, fallback) => { try { const v = JSON.parse(localStorage.getItem(key) || 'null'); return v ?? fallback; } catch { return fallback; } };
  const readSession = key => { try { return JSON.parse(sessionStorage.getItem(key) || 'null'); } catch { return null; } };
  let authSession = readSession('moviemind.auth');
  const isAdmin = () => authSession?.user?.role === 'Admin';
  const apiImage = value => { const image = safeImage(value); return image.startsWith('/media/posters/') ? `${CFG.apiOrigin || ''}${image}` : image; };
  const trailerFor = f => f.trailerUrl || `https://www.youtube.com/results?search_query=${encodeURIComponent(`${f.title} ${f.year} official trailer`)}`;

  function safeImage(value) {
    const v = String(value || '').trim();
    if (/^\/media\/posters\/[a-f0-9]{32}\.(jpg|png|webp)$/.test(v)) return v;
    if (/^data:image\/(jpeg|png|webp);base64,[a-z0-9+/=]+$/i.test(v)) return v;
    try { const u = new URL(v); if (u.protocol === 'https:' || u.protocol === 'http:') return u.href; } catch { /* not a URL */ }
    return '';
  }
  function safeLink(value) {
    try { const u = new URL(String(value || '').trim()); return u.protocol === 'https:' || u.protocol === 'http:' ? u.href : ''; } catch { return ''; }
  }
  function parseCast(value) {
    const rows = Array.isArray(value) ? value : String(value || '').split(/\n/);
    return rows.map(row => {
      if (Array.isArray(row)) return { name: str(row[0], 80), role: str(row[1], 80) };
      if (row && typeof row === 'object') return { name: str(row.name, 80), role: str(row.role, 80) };
      const text = String(row || '').trim();
      const m = text.match(/^(.*?)\s+(?:as|—|–|-|\|)\s+(.*)$/i);
      return m ? { name: str(m[1], 80), role: str(m[2], 80) } : { name: str(text, 80), role: '' };
    }).filter(c => c.name).slice(0, 40);
  }

  function normalize(raw) {
    if (!raw || typeof raw !== 'object') return null;
    const title = str(raw.title, 140);
    if (!title) return null;
    const id = str(raw.id, 90) || uid();
    const h = hash(id + title);
    const art = raw.art && typeof raw.art === 'object' ? raw.art : {};
    // v2 stored actors as plain strings
    const cast = parseCast(raw.cast || (Array.isArray(raw.actors) ? raw.actors.map(a => [a, '']) : []));
    return {
      id, title,
      originalTitle: str(raw.originalTitle, 140),
      year: clamp(Math.round(Number(raw.year)) || new Date().getFullYear(), 1888, 2100),
      duration: clamp(Math.round(Number(raw.duration)) || 0, 0, 999),
      ageRating: str(raw.ageRating, 20),
      rating: Math.round(clamp(Number(raw.rating) || 0, 0, 10) * 10) / 10,
      genres: list(raw.genres, 8),
      director: str(raw.director, 140),
      writers: list(raw.writers, 10),
      composer: str(raw.composer, 120),
      cast,
      country: str(raw.country, 140),
      language: str(raw.language, 140),
      synopsis: str(raw.synopsis, 3000),
      awards: str(raw.awards, 800),
      trailerUrl: safeLink(raw.trailerUrl),
      poster: safeImage(raw.poster),
      art: {
        palette: Number.isInteger(art.palette) && PALETTES[art.palette] ? art.palette : h % PALETTES.length,
        motif: MOTIFS[art.motif] ? art.motif : Object.keys(MOTIFS)[h % Object.keys(MOTIFS).length],
      },
      createdAt: Number(raw.createdAt) || Date.now(),
      updatedAt: Number(raw.updatedAt) || Number(raw.createdAt) || Date.now(),
    };
  }

  /* ----------------------------------------------------------------- state */
  function loadFilms() {
    let films = read(KEYS.films, null);
    if (!Array.isArray(films)) {
      const legacy = read(LEGACY.films, []);
      films = Array.isArray(legacy) ? legacy : [];
    }
    return films.map(normalize).filter(Boolean);
  }
  const state = {
    films: [],
    loading: true,
    loadError: '',
    saving: false,
    imageBusy: false,
    collectionBusy: false,
    watch: new Set(read(KEYS.watch, null) ?? read(LEGACY.watch, [])),
    seen: new Set(read(KEYS.seen, [])),
    route: { name: 'home', id: null },
    q: '', genre: '', decade: '', sort: 'recent',
    view: read(KEYS.view, 'grid') === 'list' ? 'list' : 'grid',
    hero: 0,
    rating: { movieId: null, items: [], average: 0, count: 0, loading: false, loaded: false, error: '', submitting: false },
    preferences: { data: null, loading: false, loaded: false, error: '', saving: false },
    recommendations: { data: null, loading: false, loaded: false, error: '' },
    modal: null,
    draft: null,
  };
  function persist(films = state.films) {
    state.films = films;
    try {
      localStorage.setItem(KEYS.watch, JSON.stringify([...state.watch]));
      localStorage.setItem(KEYS.seen, JSON.stringify([...state.seen]));
    } catch { /* Film saves do not depend on browser storage. */ }
    return true;
  }
  const film = id => state.films.find(f => f.id === id);

  async function api(path, options = {}) {
    const controller = new AbortController();
    const timeout = setTimeout(() => controller.abort(), 30000);
    try {
      const headers = new Headers(options.headers || {});
      if (authSession?.accessToken) headers.set('Authorization', `Bearer ${authSession.accessToken}`);
      const response = await fetch(`${CFG.apiBaseUrl || '/api'}${path}`, { ...options, headers, signal: controller.signal });
      if (response.status === 401 && authSession?.accessToken && !path.startsWith('/auth/')) {
        try { sessionStorage.removeItem('moviemind.auth'); } catch { /* optional */ }
        authSession = null;
        queueMicrotask(() => render());
      }
      const body = response.status === 204 ? null : await response.json().catch(() => null);
      if (!response.ok) {
        const messages = body?.errors ? Object.values(body.errors).flat().join(' ') : '';
        throw new Error(messages || body?.detail || body?.message || `The server returned ${response.status}. Try again.`);
      }
      return body;
    } catch (error) {
      if (error.name === 'AbortError') throw new Error('The server took too long to respond. Your form is still open; try again.');
      if (error instanceof TypeError) throw new Error('Cannot reach the MovieMind server. Check the connection and try again.');
      throw error;
    } finally { clearTimeout(timeout); }
  }
  async function uploadPoster(blob) {
    const data = new FormData();
    data.append('file', blob, 'poster.jpg');
    return (await api('/posters', { method: 'POST', body: data })).url;
  }
  async function saveFilm(entry) {
    if (entry.poster.startsWith('data:')) {
      entry = { ...entry, poster: await uploadPoster(await (await fetch(entry.poster)).blob()) };
    }
    if (!/^[a-f0-9]{8}-[a-f0-9]{4}-[a-f0-9]{4}-[a-f0-9]{4}-[a-f0-9]{12}$/i.test(entry.id)) entry = { ...entry, id: uid() };
    return normalize(await api(`/films/${encodeURIComponent(entry.id)}`, {
      method: 'PUT', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify(entry),
    }));
  }
  async function loadCatalog() {
    state.loading = true;
    state.loadError = '';
    render();
    try {
      state.films = (await api('/films')).map(normalize).filter(Boolean);
    } catch (error) { state.loadError = error.message; }
    finally { state.loading = false; render(); }
  }
  async function loadFilmRatings(id, force = false) {
    if (!force && state.rating.movieId === id && (state.rating.loading || state.rating.loaded)) return;
    state.rating = { movieId: id, items: [], average: 0, count: 0, loading: true, loaded: false, error: '', submitting: false };
    if (state.route.name === 'film' && state.route.id === id) render();
    try {
      const encodedId = encodeURIComponent(id);
      const results = await Promise.all([
        api('/movies/' + encodedId + '/ratings'),
        api('/movies/' + encodedId),
      ]);
      if (state.route.name !== 'film' || state.route.id !== id) return;
      state.rating = {
        movieId: id,
        items: Array.isArray(results[0]) ? results[0] : [],
        average: Number(results[1]?.averageRating) || 0,
        count: Number(results[1]?.ratingCount) || 0,
        loading: false,
        loaded: true,
        error: '',
        submitting: false,
      };
      render();
    } catch (error) {
      if (state.route.name !== 'film' || state.route.id !== id) return;
      state.rating = { ...state.rating, loading: false, loaded: true, error: error.message, submitting: false };
      render();
    }
  }
  async function loadUserPreferences(force = false) {
    if (!authSession?.accessToken) {
      state.preferences = { data: null, loading: false, loaded: false, error: '', saving: false };
      if (state.route.name === 'preferences') render();
      return;
    }
    if (!force && (state.preferences.loading || state.preferences.loaded)) return;
    state.preferences = { ...state.preferences, loading: true, error: '' };
    if (state.route.name === 'preferences') render();
    try {
      state.preferences.data = await api('/users/me/preferences');
      state.preferences.loaded = true;
    } catch (error) {
      state.preferences.error = error.message;
      state.preferences.loaded = true;
    } finally {
      state.preferences.loading = false;
      if (state.route.name === 'preferences') render();
    }
  }
  async function loadRecommendations(force = false) {
    if (!authSession?.accessToken) {
      state.recommendations = { data: null, loading: false, loaded: false, error: '' };
      if (state.route.name === 'recommendations') render();
      return;
    }
    if (!force && (state.recommendations.loading || state.recommendations.loaded)) return;
    state.recommendations = { ...state.recommendations, loading: true, error: '' };
    if (state.route.name === 'recommendations') render();
    try {
      state.recommendations.data = await api('/recommendations?count=12');
      state.recommendations.loaded = true;
    } catch (error) {
      state.recommendations.error = error.message;
      state.recommendations.loaded = true;
    } finally {
      state.recommendations.loading = false;
      if (state.route.name === 'recommendations') render();
    }
  }
  async function saveCollection(entries) {
    if (state.collectionBusy) throw new Error('A collection is already being saved. Wait for it to finish.');
    state.collectionBusy = true;
    // Commit each confirmed result so a partial import can be retried without losing successful saves.
    try {
      for (const entry of entries) {
        const saved = await saveFilm(entry);
        persist([saved, ...state.films.filter(f => f.id !== saved.id)]);
      }
    } finally { state.collectionBusy = false; }
  }

  /* ----------------------------------------------------------------- icons */
  const ICONS = {
    search: '<circle cx="11" cy="11" r="6.5"/><path d="m16 16 4.5 4.5"/>',
    plus: '<path d="M12 5v14M5 12h14"/>',
    bookmark: '<path d="M6.5 3.5h11v17L12 16.5l-5.5 4z"/>',
    check: '<path d="m5 12.5 4.5 4.5L19 7.5"/>',
    play: '<path d="M8 5.5v13l10.5-6.5z"/>',
    edit: '<path d="M4 20h4L19 9l-4-4L4 16zM13.5 6.5l4 4"/>',
    trash: '<path d="M4.5 7h15M9.5 3.5h5M7 7l.9 13h8.2L17 7"/>',
    user: '<circle cx="12" cy="8.5" r="3.8"/><path d="M4.5 20.5c.9-3.9 3.9-6 7.5-6s6.6 2.1 7.5 6"/>',
    close: '<path d="M6 6l12 12M18 6 6 18"/>',
    left: '<path d="m14.5 6-6 6 6 6"/>',
    right: '<path d="m9.5 6 6 6-6 6"/>',
    grid: '<path d="M4 4h6.5v6.5H4zM13.5 4H20v6.5h-6.5zM4 13.5h6.5V20H4zM13.5 13.5H20V20h-6.5z"/>',
    rows: '<path d="M4 5.5h16M4 12h16M4 18.5h16"/>',
    upload: '<path d="M12 15.5V4m0 0L7.5 8.5M12 4l4.5 4.5M4.5 15v3.5a1.5 1.5 0 0 0 1.5 1.5h12a1.5 1.5 0 0 0 1.5-1.5V15"/>',
    download: '<path d="M12 4v11.5m0 0L7.5 11M12 15.5l4.5-4.5M4.5 15v3.5a1.5 1.5 0 0 0 1.5 1.5h12a1.5 1.5 0 0 0 1.5-1.5V15"/>',
    film: '<rect x="3.5" y="4.5" width="17" height="15" rx="1.5"/><path d="M7.5 4.5v15M16.5 4.5v15M3.5 9h4M3.5 15h4M16.5 9h4M16.5 15h4"/>',
    star: '<path d="m12 3.8 2.5 5.3 5.8.7-4.3 4 1.1 5.7L12 16.7l-5.1 2.8L8 13.8l-4.3-4 5.8-.7z"/>',
    sparkle: '<path d="M12 3.5c.6 4.4 2.1 5.9 6.5 6.5-4.4.6-5.9 2.1-6.5 6.5-.6-4.4-2.1-5.9-6.5-6.5 4.4-.6 5.9-2.1 6.5-6.5z"/>',
    link: '<path d="M10 14a4 4 0 0 0 5.7 0l3-3a4 4 0 0 0-5.7-5.7l-1 1M14 10a4 4 0 0 0-5.7 0l-3 3a4 4 0 0 0 5.7 5.7l1-1"/>',
    image: '<rect x="3.5" y="4.5" width="17" height="15" rx="1.5"/><circle cx="9" cy="10" r="1.8"/><path d="m4 18 5.5-5 4 3.5 2.5-2 4.5 4"/>',
    eye: '<path d="M2.8 12S6.2 5.5 12 5.5 21.2 12 21.2 12 17.8 18.5 12 18.5 2.8 12 2.8 12z"/><circle cx="12" cy="12" r="2.7"/>',
    arrowRight: '<path d="M5 12h14m-5.5-5.5L19 12l-5.5 5.5"/>',
    sun: '<circle cx="12" cy="12" r="3.8"/><path d="M12 2.8v2.4M12 18.8v2.4M2.8 12h2.4M18.8 12h2.4M5.5 5.5l1.7 1.7M16.8 16.8l1.7 1.7M5.5 18.5l1.7-1.7M16.8 7.2l1.7-1.7"/>',
    moon: '<path d="M19.5 14.6A7.8 7.8 0 0 1 9.4 4.5a7.8 7.8 0 1 0 10.1 10.1z"/>',
    command: '<path d="M9 6.5A2.5 2.5 0 1 0 6.5 9H9zm0 0v11m0-11h6m-6 11a2.5 2.5 0 1 1-2.5-2.5H9zm6-11a2.5 2.5 0 1 1 2.5 2.5H15zm0 0v11m0 0a2.5 2.5 0 1 0 2.5-2.5H15zm0 0H9"/>',
  };
  const icon = (name, size = 18) => `<svg class="i" width="${size}" height="${size}" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.6" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true">${ICONS[name] || ''}</svg>`;

  /* ----------------------------------------------------------------- theme */
  const currentTheme = () => (document.documentElement.dataset.theme === 'light' ? 'light' : 'dark');
  function applyTheme(theme, save = true) {
    document.documentElement.dataset.theme = theme;
    $('meta[name="theme-color"]')?.setAttribute('content', theme === 'light' ? '#eef0f3' : '#0e1522');
    if (save) { try { localStorage.setItem(KEYS.theme, theme); } catch { /* optional */ } }
    $$('.theme-switch').forEach(b => { b.setAttribute('aria-checked', String(theme === 'dark')); b.title = theme === 'dark' ? 'Switch to light theme' : 'Switch to dark theme'; });
  }
  function themeSwitch() {
    const dark = currentTheme() === 'dark';
    return `<button class="theme-switch" role="switch" data-act="theme" aria-checked="${dark}" aria-label="Dark theme" title="${dark ? 'Switch to light theme' : 'Switch to dark theme'}"><span class="knob" aria-hidden="true"></span><span class="i-sun">${icon('sun', 15)}</span><span class="i-moon">${icon('moon', 15)}</span></button>`;
  }
  if (!document.documentElement.dataset.theme) {
    let saved = null;
    try { saved = localStorage.getItem(KEYS.theme); } catch { /* optional */ }
    applyTheme(saved === 'light' || saved === 'dark' ? saved : (matchMedia('(prefers-color-scheme: light)').matches ? 'light' : 'dark'), false);
  }

  /* --------------------------------------------------------- poster artwork */
  let svgSeq = 0;
  function titleLines(title) {
    const words = String(title).toUpperCase().split(/\s+/).filter(Boolean);
    const lines = [];
    let cur = '';
    for (const w of words) {
      if (!cur) cur = w;
      else if ((cur + ' ' + w).length <= 11) cur += ' ' + w;
      else { lines.push(cur); cur = w; }
    }
    if (cur) lines.push(cur);
    if (lines.length > 4) return [...lines.slice(0, 3), lines.slice(3).join(' ')];
    return lines;
  }
  function motifSVG(kind, p, r, id) {
    let s = '';
    switch (kind) {
      case 'rings':
        for (let i = 0; i < 9; i++) {
          const rad = 22 + i * 21;
          s += `<circle cx="200" cy="215" r="${rad}" fill="none" stroke="${p.acc}" stroke-width="${i % 3 === 0 ? 2.4 : 1.1}" stroke-opacity="${(0.95 - i * 0.09).toFixed(2)}" stroke-dasharray="${i % 2 ? `${8 + i * 4} ${6 + i}` : 'none'}" transform="rotate(${Math.round(r() * 360)} 200 215)"/>`;
        }
        s += `<circle cx="200" cy="215" r="7" fill="${p.acc}"/>`;
        break;
      case 'orb':
        s += `<circle cx="200" cy="225" r="175" fill="url(#${id}r)"/>`;
        s += `<ellipse cx="200" cy="225" rx="190" ry="24" fill="none" stroke="${p.acc}" stroke-opacity=".75" stroke-width="2" transform="rotate(-14 200 225)"/>`;
        s += `<circle cx="200" cy="225" r="78" fill="${p.b}" stroke="${p.acc}" stroke-width="3"/>`;
        s += `<ellipse cx="200" cy="225" rx="190" ry="24" fill="none" stroke="${p.acc}" stroke-width="3" stroke-dasharray="0 300 300" transform="rotate(-14 200 225)"/>`;
        break;
      case 'dunes': {
        s += `<circle cx="${250 + Math.round(r() * 60)}" cy="150" r="58" fill="${p.acc}"/>`;
        for (let i = 0; i < 4; i++) {
          const y = 250 + i * 55, a = 30 + r() * 40;
          s += `<path d="M0 ${y} Q 110 ${y - a} 220 ${y - 8} T 400 ${y - 20} V600 H0Z" fill="${p.b}" fill-opacity="${(0.35 + i * 0.18).toFixed(2)}"/>`;
        }
        break;
      }
      case 'sun':
        s += `<circle cx="200" cy="300" r="135" fill="${p.acc}"/>`;
        for (let i = 0; i < 7; i++) s += `<rect x="0" y="${232 + i * 13 + i * i * 0.6}" width="400" height="${1.5 + i * 1.6}" fill="${p.a}"/>`;
        s += `<rect x="0" y="300" width="400" height="300" fill="${p.b}"/>`;
        for (let i = 0; i < 6; i++) s += `<rect x="${110 + i * 9}" y="${318 + i * 16}" width="${180 - i * 18}" height="3" fill="${p.acc}" fill-opacity="${(0.7 - i * 0.1).toFixed(2)}"/>`;
        break;
      case 'stairs': {
        let d = 'M0 470';
        let x = 0, y = 470;
        while (x < 400) { x += 46; d += ` H${x}`; y -= 38; d += ` V${y}`; }
        s += `<path d="${d} V0 H0Z" fill="${p.b}" fill-opacity=".55"/>`;
        s += `<path d="${d}" fill="none" stroke="${p.acc}" stroke-width="2"/>`;
        s += `<rect x="288" y="96" width="34" height="48" fill="${p.acc}" fill-opacity=".9"/>`;
        break;
      }
      case 'rain':
        for (let i = 0; i < 90; i++) {
          const x = Math.round(r() * 400), y = Math.round(r() * 520), l = 30 + Math.round(r() * 110);
          s += `<line x1="${x}" y1="${y}" x2="${x - 6}" y2="${y + l}" stroke="${p.ink}" stroke-opacity="${(0.08 + r() * 0.22).toFixed(2)}" stroke-width="1"/>`;
        }
        s += `<path d="M168 150 l14 -26 l14 26 v40 h-28z" fill="${p.acc}" fill-opacity=".9"/>`;
        s += `<line x1="182" y1="190" x2="182" y2="600" stroke="${p.acc}" stroke-opacity=".25"/>`;
        break;
      case 'bands':
        for (let i = 0; i < 6; i++) {
          const o = -200 + i * 105;
          s += `<polygon points="${o},600 ${o + 60},600 ${o + 460},0 ${o + 400},0" fill="${i % 2 ? p.ink : p.acc}" fill-opacity="${i % 2 ? 0.08 : 0.85}"/>`;
        }
        break;
      case 'grid':
        for (let row = 0; row < 4; row++) for (let col = 0; col < 4; col++) {
          const cx = 64 + col * 91, cy = 78 + row * 84, t = r();
          s += t < 0.33 ? `<circle cx="${cx}" cy="${cy}" r="30" fill="${p.acc}"/>`
            : t < 0.66 ? `<circle cx="${cx}" cy="${cy}" r="30" fill="none" stroke="${p.ink}" stroke-opacity=".55" stroke-width="2"/>`
              : `<circle cx="${cx}" cy="${cy}" r="9" fill="${p.ink}" fill-opacity=".8"/>`;
        }
        break;
      case 'window':
        s += `<rect x="135" y="140" width="130" height="170" fill="${p.acc}" fill-opacity=".14"/>`;
        s += `<rect x="150" y="155" width="100" height="140" fill="${p.acc}"/>`;
        s += `<path d="M200 155 V295 M150 225 H250" stroke="${p.a}" stroke-width="5"/>`;
        s += `<path d="M150 295 L60 600 H340 L250 295Z" fill="${p.acc}" fill-opacity=".07"/>`;
        break;
      case 'stars':
        for (let i = 0; i < 140; i++) s += `<circle cx="${Math.round(r() * 400)}" cy="${Math.round(r() * 430)}" r="${(0.4 + r() * 1.6).toFixed(1)}" fill="${p.acc}" fill-opacity="${(0.3 + r() * 0.7).toFixed(2)}"/>`;
        s += `<circle cx="270" cy="150" r="56" fill="${p.acc}"/><circle cx="292" cy="136" r="50" fill="${p.a}"/>`;
        break;
      case 'spot':
      default:
        s += `<polygon points="200,-10 40,470 360,470" fill="url(#${id}r)"/>`;
        s += `<ellipse cx="200" cy="470" rx="160" ry="22" fill="${p.acc}" fill-opacity=".25"/>`;
        s += `<circle cx="200" cy="300" r="38" fill="${p.b}"/><path d="M150 470 C150 380 250 380 250 470Z" fill="${p.b}"/>`;
    }
    return s;
  }
  function posterSVG(f) {
    const p = PALETTES[f.art?.palette] || PALETTES[0];
    const r = rng(hash(f.id + f.title));
    const id = `mm${++svgSeq}`;
    const lines = titleLines(f.title || 'Untitled');
    const longest = Math.max(...lines.map(l => l.length));
    const fs = clamp(Math.floor(352 / (longest * 0.5)), 34, lines.length > 2 ? 78 : 104);
    const lh = fs * 0.9;
    const startY = 552 - (lines.length - 1) * lh;
    const text = lines.map((l, i) => `<text x="24" y="${(startY + i * lh).toFixed(1)}" font-size="${fs}">${esc(l)}</text>`).join('');
    return `<svg class="art" viewBox="0 0 400 600" preserveAspectRatio="xMidYMid slice" role="img" aria-label="${esc(f.title)} poster artwork" xmlns="http://www.w3.org/2000/svg">
      <defs>
        <linearGradient id="${id}g" x1="0" y1="0" x2=".35" y2="1"><stop offset="0" stop-color="${p.a}"/><stop offset="1" stop-color="${p.b}"/></linearGradient>
        <radialGradient id="${id}r"><stop offset="0" stop-color="${p.acc}" stop-opacity=".55"/><stop offset="1" stop-color="${p.acc}" stop-opacity="0"/></radialGradient>
        <linearGradient id="${id}v" x1="0" y1="0" x2="0" y2="1"><stop offset=".45" stop-color="${p.b}" stop-opacity="0"/><stop offset="1" stop-color="${p.b}" stop-opacity=".92"/></linearGradient>
      </defs>
      <rect width="400" height="600" fill="url(#${id}g)"/>
      ${motifSVG(f.art?.motif, p, r, id)}
      <rect width="400" height="600" fill="url(#${id}v)"/>
      <g class="art-top" fill="${p.ink}" font-size="13"><text x="24" y="40">${esc((f.director || '').split(',')[0])}</text><text x="376" y="40" text-anchor="end">${esc(f.year || '')}</text></g>
      <g class="art-title" fill="${p.ink}">${text}</g>
    </svg>`;
  }
  function poster(f, cls = '') {
    const img = apiImage(f.poster);
    return `<div class="poster ${cls}">${img ? `<img src="${esc(img)}" alt="${esc(f.title)} poster" loading="lazy" decoding="async">` : posterSVG(f)}</div>`;
  }

  /* -------------------------------------------------------------- toasts */
  function toast(message, tone = 'info', action) {
    let wrap = $('#toasts');
    if (!wrap) { wrap = document.createElement('div'); wrap.id = 'toasts'; wrap.className = 'toasts'; wrap.setAttribute('aria-live', 'polite'); document.body.append(wrap); }
    const el = document.createElement('div');
    el.className = `toast toast-${tone}`;
    el.innerHTML = `<span>${esc(message)}</span>${action ? `<a href="${esc(action.href)}">${esc(action.label)}</a>` : ''}`;
    wrap.append(el);
    while (wrap.children.length > 3) wrap.firstElementChild.remove();
    setTimeout(() => { el.classList.add('out'); setTimeout(() => el.remove(), 250); }, 3600);
  }

  /* -------------------------------------------------------------- layout */
  const NAV = [['home', '#/', 'Home'], ['library', '#/library', 'Library'], ['watchlist', '#/watchlist', 'Watchlist'], ['recommendations', '#/recommendations', 'For you'], ['preferences', '#/preferences', 'Preferences'], ['studio', '#/studio', 'Studio']];
  function topbar() {
    const active = state.route.name === 'film' ? 'library' : state.route.name;
    return `<header class="topbar"><div class="wrap topbar-inner">
      <a class="brand" href="#/" aria-label="MovieMind home"><span class="brand-mark" aria-hidden="true"></span>MovieMind</a>
      <nav class="nav" aria-label="Main">${NAV.filter(([name]) => (name !== 'studio' || isAdmin()) && (!['preferences', 'recommendations'].includes(name) || !!authSession?.accessToken)).map(([name, href, label]) => `<a href="${href}" class="${active === name ? 'is-active' : ''}" ${active === name ? 'aria-current="page"' : ''}>${label}${name === 'watchlist' && state.watch.size ? `<span class="count" data-watch-count>${state.watch.size}</span>` : ''}</a>`).join('')}</nav>
      <div class="top-actions">
        <button class="search-trigger" data-act="palette" aria-label="Search films (Ctrl+K)">${icon('search', 17)}<span>Search films</span><kbd>Ctrl K</kbd></button>
        ${isAdmin() ? `<button class="btn btn-primary btn-sm hide-xs" data-act="add">${icon('plus', 16)}Add film</button>` : ''}
        ${themeSwitch()}
        <button class="icon-btn" data-act="account" aria-label="Sign in or create an account">${icon('user', 19)}</button>
      </div>
    </div></header>`;
  }
  function footer() {
    return `<footer class="footer"><div class="wrap footer-inner"><span>MovieMind ${esc(CFG.version || '')}</span><span>${plural(state.films.length, 'film')} in the database</span></div></footer>`;
  }

  /* --------------------------------------------------------------- cards */
  function watchBtn(f, cls = '') {
    const on = state.watch.has(f.id);
    return `<button class="watch-btn ${cls} ${on ? 'is-on' : ''}" data-act="watch" data-id="${esc(f.id)}" aria-pressed="${on}" aria-label="${on ? 'Remove from' : 'Add to'} watchlist: ${esc(f.title)}" title="${on ? 'In your watchlist' : 'Add to watchlist'}">${icon(on ? 'check' : 'bookmark', 16)}</button>`;
  }
  function card(f) {
    return `<article class="card">
      <a href="#/film/${encodeURIComponent(f.id)}" class="card-link" aria-label="${esc(f.title)}, ${f.year}">${poster(f)}</a>
      ${watchBtn(f)}
      <div class="card-body">
        <a href="#/film/${encodeURIComponent(f.id)}" class="card-title">${esc(f.title)}</a>
        <div class="card-meta"><span>${f.year}</span>${f.rating ? `<span class="score">${icon('star', 12)}${f.rating.toFixed(1)}</span>` : ''}</div>
      </div>
    </article>`;
  }
  function rowItem(f) {
    return `<article class="list-row">
      <a href="#/film/${encodeURIComponent(f.id)}" class="list-poster" tabindex="-1" aria-hidden="true">${poster(f)}</a>
      <div class="list-main">
        <a href="#/film/${encodeURIComponent(f.id)}" class="list-title">${esc(f.title)}</a>
        <p>${esc(f.director || 'Director not set')}${f.cast.length ? `, with ${esc(f.cast.slice(0, 2).map(c => c.name).join(' and '))}` : ''}</p>
        <div class="tags">${f.genres.map(g => `<span class="tag">${esc(g)}</span>`).join('')}</div>
      </div>
      <div class="list-facts"><span>${f.year}</span><span>${runtime(f.duration) || '—'}</span><span class="score">${f.rating ? icon('star', 12) + f.rating.toFixed(1) : '—'}</span></div>
      ${watchBtn(f, 'static')}
    </article>`;
  }
  function shelf(title, films, href, note = '') {
    if (!films.length) return '';
    return `<section class="shelf">
      <div class="shelf-head"><div><h2>${esc(title)}</h2>${note ? `<p>${esc(note)}</p>` : ''}</div>
        <div class="shelf-tools">${href ? `<a class="text-link" href="${href}">See all</a>` : ''}
          <button class="icon-btn sm" data-act="shelf-left" aria-label="Scroll ${esc(title)} left">${icon('left', 18)}</button>
          <button class="icon-btn sm" data-act="shelf-right" aria-label="Scroll ${esc(title)} right">${icon('right', 18)}</button></div></div>
      <div class="shelf-track">${films.map(card).join('')}</div>
    </section>`;
  }

  /* ---------------------------------------------------------------- pages */
  function featuredFilms() { return [...state.films].sort((a, b) => b.rating - a.rating || b.createdAt - a.createdAt).slice(0, 5); }
  function heroHTML() {
    const picks = featuredFilms();
    const f = picks[((state.hero % picks.length) + picks.length) % picks.length];
    const img = apiImage(f.poster);
    return `<section class="hero" id="hero" aria-label="Featured film">
      <div class="hero-backdrop" aria-hidden="true">${img ? `<img src="${esc(img)}" alt="">` : posterSVG(f)}</div>
      <div class="wrap hero-inner">
        <div class="hero-copy">
          <p class="hero-kicker">Highest rated in your collection</p>
          <h1 class="hero-title ${lenClass(f.title)}">${esc(f.title)}</h1>
          <div class="facts">${[f.year, runtime(f.duration), f.ageRating].filter(Boolean).map(x => `<span>${esc(x)}</span>`).join('')}${f.rating ? `<span class="score">${icon('star', 14)}${f.rating.toFixed(1)}</span>` : ''}</div>
          <p class="hero-synopsis">${esc(f.synopsis || 'No synopsis yet. Open the film to add one.')}</p>
          <div class="btn-row">
            <a class="btn btn-primary" href="#/film/${encodeURIComponent(f.id)}">View details</a>
            <a class="btn btn-ghost" href="${esc(trailerFor(f))}" target="_blank" rel="noopener">${icon('play', 15)}Find trailer</a>
            ${watchBtn(f, 'lg')}
          </div>
        </div>
        <a class="hero-poster" href="#/film/${encodeURIComponent(f.id)}" aria-hidden="true" tabindex="-1">${poster(f)}</a>
      </div>
      ${picks.length > 1 ? `<div class="wrap hero-pager"><button class="icon-btn sm" data-act="hero-prev" aria-label="Previous featured film">${icon('left')}</button>
        <div class="pager-dots">${picks.map((p, i) => `<button data-act="hero-go" data-i="${i}" class="${p.id === f.id ? 'is-on' : ''}" aria-label="Show ${esc(p.title)}"></button>`).join('')}</div>
        <button class="icon-btn sm" data-act="hero-next" aria-label="Next featured film">${icon('right')}</button></div>` : ''}
    </section>`;
  }
  function home() {
    if (!state.films.length) {
      return `<section class="welcome wrap">
        <div class="welcome-art" aria-hidden="true">${['Inception', 'Parasite', 'La La Land'].map((t, i) => `<div class="poster ghost g${i}">${posterSVG({ id: t, title: t, year: '', director: '', art: { palette: [0, 2, 4][i], motif: ['rings', 'stairs', 'sun'][i] } })}</div>`).join('')}</div>
        <div class="welcome-copy">
          <h1 class="display">Your screening room is empty.</h1>
          <p>Add the films you love with their cast, crew, synopsis and poster — or start with a ready-made collection of 13 acclaimed films to see how everything looks.</p>
          <div class="btn-row"><button class="btn btn-primary" data-act="demo">${icon('sparkle', 16)}Load demo collection</button><button class="btn btn-ghost" data-act="add">${icon('plus', 16)}Add a film</button></div>
        </div>
      </section>`;
    }
    const recent = [...state.films].sort((a, b) => b.createdAt - a.createdAt).slice(0, 14);
    const top = [...state.films].filter(f => f.rating).sort((a, b) => b.rating - a.rating).slice(0, 14);
    const counts = {};
    state.films.forEach(f => f.genres.forEach(g => { counts[g] = (counts[g] || 0) + 1; }));
    const topGenres = Object.entries(counts).sort((a, b) => b[1] - a[1]).slice(0, 2).map(([g]) => g);
    const minutes = state.films.reduce((s, f) => s + f.duration, 0);
    const directors = new Set(state.films.map(f => f.director).filter(Boolean)).size;
    const watch = state.films.filter(f => state.watch.has(f.id));
    return `${heroHTML()}
      <div class="wrap">
        <p class="summary">${plural(state.films.length, 'film')}, ${Math.round(minutes / 60)} hours of cinema, ${plural(Object.keys(counts).length, 'genre')} and ${plural(directors, 'director')} in your collection.</p>
        ${watch.length ? shelf('Up next on your watchlist', watch, '#/watchlist') : ''}
        ${shelf('Recently added', recent, '#/library')}
        ${shelf('Highest rated', top, '#/library?sort=rating')}
        ${topGenres.map(g => shelf(g, state.films.filter(f => f.genres.includes(g)), `#/library?genre=${encodeURIComponent(g)}`)).join('')}
      </div>`;
  }

  function filtered(source) {
    const q = state.q.toLowerCase().trim();
    const out = source.filter(f => {
      if (state.genre && !f.genres.includes(state.genre)) return false;
      if (state.decade && Math.floor(f.year / 10) * 10 !== Number(state.decade)) return false;
      if (!q) return true;
      return [f.title, f.originalTitle, f.director, f.composer, f.country, f.language, f.writers.join(' '), f.genres.join(' '), f.cast.map(c => `${c.name} ${c.role}`).join(' ')].join(' ').toLowerCase().includes(q);
    });
    const by = {
      recent: (a, b) => b.createdAt - a.createdAt,
      rating: (a, b) => b.rating - a.rating,
      newest: (a, b) => b.year - a.year,
      oldest: (a, b) => a.year - b.year,
      title: (a, b) => a.title.localeCompare(b.title),
      runtime: (a, b) => b.duration - a.duration,
    };
    return out.sort(by[state.sort] || by.recent);
  }
  function resultsHTML(source, emptyMsg) {
    const items = filtered(source);
    if (!source.length) return emptyMsg;
    if (!items.length) return `<div class="empty small"><h3>No films match these filters.</h3><p>Try a different search or clear the filters.</p><button class="btn btn-ghost btn-sm" data-act="clear-filters">Clear filters</button></div>`;
    return state.view === 'list' ? `<div class="list">${items.map(rowItem).join('')}</div>` : `<div class="grid">${items.map(card).join('')}</div>`;
  }
  function catalogPage(kind) {
    const source = kind === 'watchlist' ? state.films.filter(f => state.watch.has(f.id)) : state.films;
    const genres = [...new Set(source.flatMap(f => f.genres))].sort();
    const decades = [...new Set(source.map(f => Math.floor(f.year / 10) * 10))].sort((a, b) => b - a);
    const empty = kind === 'watchlist'
      ? `<div class="empty"><h3>Nothing on your watchlist yet.</h3><p>Use the bookmark on any poster to save a film for later.</p><a class="btn btn-ghost btn-sm" href="#/library">Browse the library</a></div>`
      : `<div class="empty"><h3>Your library is empty.</h3><p>Add a film, or load the demo collection to see the library in action.</p><div class="btn-row center"><button class="btn btn-primary btn-sm" data-act="demo">Load demo collection</button><button class="btn btn-ghost btn-sm" data-act="add">Add a film</button></div></div>`;
    return `<div class="wrap page">
      <div class="page-head"><div><h1 class="display">${kind === 'watchlist' ? 'Watchlist' : 'Library'}</h1>
        <p class="lede">${kind === 'watchlist' ? 'Films you saved to watch later.' : 'Every film in your collection. Search by title, actor, character, director or country.'}</p></div>
        <span class="page-count" id="result-count">${plural(filtered(source).length, 'film')}</span></div>
      ${source.length ? `<div class="toolbar">
        <label class="field-search">${icon('search', 17)}<input id="q" type="search" value="${esc(state.q)}" placeholder="Search title, cast, director…" autocomplete="off" aria-label="Search films"></label>
        <select id="f-genre" class="select" aria-label="Filter by genre"><option value="">All genres</option>${genres.map(g => `<option ${state.genre === g ? 'selected' : ''}>${esc(g)}</option>`).join('')}</select>
        <select id="f-decade" class="select" aria-label="Filter by decade"><option value="">Any decade</option>${decades.map(d => `<option value="${d}" ${String(state.decade) === String(d) ? 'selected' : ''}>${d}s</option>`).join('')}</select>
        <select id="f-sort" class="select" aria-label="Sort films">${[['recent', 'Recently added'], ['rating', 'Highest rated'], ['newest', 'Newest first'], ['oldest', 'Oldest first'], ['title', 'Title A–Z'], ['runtime', 'Longest first']].map(([v, l]) => `<option value="${v}" ${state.sort === v ? 'selected' : ''}>${l}</option>`).join('')}</select>
        <div class="seg" role="group" aria-label="Layout"><button data-act="view" data-v="grid" aria-pressed="${state.view === 'grid'}" aria-label="Poster grid">${icon('grid', 17)}</button><button data-act="view" data-v="list" aria-pressed="${state.view === 'list'}" aria-label="Detailed list">${icon('rows', 17)}</button></div>
      </div>` : ''}
      <div id="results" data-kind="${kind}">${resultsHTML(source, empty)}</div>
    </div>`;
  }
  function refreshResults() {
    const box = $('#results');
    if (!box) return;
    const kind = box.dataset.kind;
    const source = kind === 'watchlist' ? state.films.filter(f => state.watch.has(f.id)) : state.films;
    box.innerHTML = resultsHTML(source, '');
    const c = $('#result-count');
    if (c) c.textContent = plural(filtered(source).length, 'film');
  }

  function updatePreferencePreview(row, useCustom = row?.querySelector('[data-pref-toggle]')?.checked) {
    if (!row) return;
    const range = row.querySelector('[data-pref-range]');
    const score = Number(range?.value ?? 5);
    const value = useCustom ? (score - 5) / 5 : Number(row.dataset.vectorValue || 0);
    const label = value > 0.02 ? `+${value.toFixed(2)}` : value.toFixed(2);
    const valueNode = row.querySelector('.preference-vector-value');
    const dot = row.querySelector('.preference-meter-dot');
    const source = row.querySelector('.preference-row-head small');
    if (valueNode) valueNode.textContent = label;
    if (dot) dot.style.left = `${clamp((value + 1) * 50, 0, 100)}%`;
    if (source) source.textContent = useCustom ? 'Your preference' : row.dataset.vectorSignal === 'RatingHistory' ? 'From your film ratings' : 'No rating history yet';
  }
  function preferencesPage() {
    if (!authSession?.accessToken) {
      return `<div class="wrap page"><div class="page-head"><div><h1 class="display">Your taste profile</h1><p class="lede">Sign in to set your genre preferences.</p></div></div><div class="empty"><h3>Sign in to continue</h3><p>Your preferences are saved to your MovieMind account and help build your recommendation profile.</p><button class="btn btn-primary" data-act="account">Sign in</button></div></div>`;
    }
    const data = state.preferences.data;
    if (state.preferences.loading && !data) return `<div class="wrap page"><div class="page-head"><div><h1 class="display">Your taste profile</h1><p class="lede">Loading your genre preferences…</p></div></div><div class="boot" role="status">Loading preferences…</div></div>`;
    if (state.preferences.error && !data) return `<div class="wrap page"><div class="page-head"><div><h1 class="display">Your taste profile</h1><p class="lede">We couldn't load your preferences.</p></div></div><div class="empty"><p role="alert">${esc(state.preferences.error)}</p><button class="btn btn-primary" data-act="reload-preferences">Try again</button></div></div>`;

    const explicit = new Map((data?.genrePreferences || []).map(item => [item.genreId, item]));
    const vector = data?.vector || [];
    const rows = vector.map(item => {
      const saved = explicit.get(item.genreId);
      const inferredLevel = Math.round(((Number(item.value) + 1) * 5) * 10) / 10;
      const level = saved ? Number(saved.score) : inferredLevel;
      const value = Number(item.value) || 0;
      const position = clamp((value + 1) * 50, 0, 100);
      const source = item.signal === 'ExplicitPreference' ? 'Your preference' : item.signal === 'RatingHistory' ? 'From your film ratings' : 'No rating history yet';
      const vectorLabel = value > 0.02 ? `+${value.toFixed(2)}` : value.toFixed(2);
      return `<article class="preference-row" data-vector-value="${value}" data-vector-signal="${esc(item.signal)}">
        <div class="preference-row-head"><div><h3>${esc(item.genreName)}</h3><small>${source}</small></div><strong class="preference-vector-value">${vectorLabel}</strong></div>
        <div class="preference-meter" role="img" aria-label="Affinity ${vectorLabel}, from avoid to prefer"><span class="preference-meter-dot" style="left:${position}%"></span></div>
        <label class="preference-enable"><input type="checkbox" data-pref-toggle value="${esc(item.genreId)}" ${saved ? 'checked' : ''}><span>Set my interest for this genre</span></label>
        <div class="preference-range"><span>Avoid</span><input type="range" min="0" max="10" step="0.5" value="${clamp(level, 0, 10)}" data-pref-range ${saved ? '' : 'disabled'} aria-label="Interest in ${esc(item.genreName)}"><span>Prefer</span><output data-pref-value>${clamp(level, 0, 10).toFixed(1).replace(/\.0$/, '')}</output></div>
      </article>`;
    }).join('');
    return `<div class="wrap page preference-page">
      <div class="page-head"><div><h1 class="display">Your taste profile</h1><p class="lede">Set how interested you are in each genre. This is your preference, not a rating of the genre.</p></div><span class="page-count">${plural(explicit.size, 'custom preference')}</span></div>
      <div class="preference-note"><strong>How the profile works</strong><span>5 is neutral, 0 means avoid, and 10 means strongly prefer. The vector below each genre is centered from −1 to +1. Where you have no custom preference, MovieMind estimates it from your film ratings.</span></div>
      <p class="form-error" id="preferences-error" role="alert">${esc(state.preferences.error)}</p>
      <form id="preferences-form" class="preference-form">
        <div class="preference-grid">${rows || '<div class="empty small"><h3>No genres yet</h3><p>Add genres to the movie catalog first.</p></div>'}</div>
        <div class="preference-actions"><span class="rating-message">Uncheck a genre to use your rating history again.</span><button class="btn btn-primary" type="submit" ${state.preferences.saving || !vector.length ? 'disabled' : ''}>${state.preferences.saving ? 'Saving…' : 'Save preferences'}</button></div>
      </form>
    </div>`;
  }
  function recommendationsPage() {
    if (!authSession?.accessToken) {
      return `<div class="wrap page"><div class="page-head"><div><h1 class="display">For you</h1><p class="lede">Sign in to get films selected for your taste.</p></div></div><div class="empty"><h3>Your next favourite film is waiting.</h3><p>Sign in to combine your genre preferences and rating history into personal recommendations.</p><button class="btn btn-primary" data-act="account">Sign in</button></div></div>`;
    }
    const { data, loading, error } = state.recommendations;
    if (loading && !data) return `<div class="wrap page"><div class="page-head"><div><h1 class="display">For you</h1><p class="lede">Finding films that fit your taste…</p></div></div><div class="boot" role="status">Loading recommendations…</div></div>`;
    if (error && !data) return `<div class="wrap page"><div class="page-head"><div><h1 class="display">For you</h1><p class="lede">We couldn't load your recommendations.</p></div></div><div class="empty"><p role="alert">${esc(error)}</p><button class="btn btn-primary" data-act="reload-recommendations">Try again</button></div></div>`;
    const items = data?.items || [];
    const strategyNames = { Popularity: 'Popular starting points', ContentBased: 'Based on your taste profile', Hybrid: 'Taste and community ratings' };
    const cards = items.map(item => {
      const f = normalize({ id: item.movieId, title: item.title, year: item.releaseYear, poster: item.posterUrl, genres: item.genres });
      if (!f) return '';
      return `<article class="card recommendation-card">
        <a href="#/film/${encodeURIComponent(f.id)}" class="card-link" aria-label="${esc(f.title)}, ${f.year}">${poster(f)}</a>
        ${watchBtn(f)}
        <div class="card-body"><a href="#/film/${encodeURIComponent(f.id)}" class="card-title">${esc(f.title)}</a>
          <div class="card-meta"><span>${f.year}</span><span class="recommendation-score">${Math.round(Number(item.score) * 100)} pts</span></div>
          <p class="recommendation-reason">${esc(item.reason)}</p>
          <div class="tags">${f.genres.map(genre => `<span class="tag">${esc(genre)}</span>`).join('')}</div>
        </div>
      </article>`;
    }).filter(Boolean).join('');
    return `<div class="wrap page recommendations-page">
      <div class="page-head"><div><h1 class="display">For you</h1><p class="lede">${esc(data?.message || 'A selection from the MovieMind catalog.')}</p></div>
        <button class="btn btn-ghost" data-act="reload-recommendations" ${loading ? 'disabled' : ''}>${loading ? 'Refreshing…' : 'Refresh picks'}</button></div>
      ${data ? `<div class="recommendation-context"><span>${esc(strategyNames[data.strategy] || 'Personalized selection')}</span><span>Updated ${esc(new Date(data.generatedAtUtc).toLocaleString())}</span><span>Scores rank the picks; they are not rating predictions.</span></div>` : ''}
      ${error ? `<p class="form-error" role="alert">${esc(error)}</p>` : ''}
      ${cards ? `<div class="grid recommendation-grid">${cards}</div>` : `<div class="empty"><h3>No recommendations yet.</h3><p>Rate a few films or set genre preferences to give MovieMind more signals to work with.</p><a class="btn btn-primary" href="#/preferences">Set genre preferences</a></div>`}
    </div>`;
  }
  function ratingPanel(f) {
    const data = state.rating.movieId === f.id ? state.rating : null;
    const items = data?.items || [];
    const mine = items.find(item => item.userId === authSession?.user?.id);
    const average = data && data.count > 0 ? data.average.toFixed(1) : '—';
    const summary = '<div class="community-rating-summary"><strong>' + average + '</strong><span>/ 10</span><span class="community-rating-count">' + (data ? plural(data.count, 'rating') : 'Loading ratings…') + '</span></div>';
    let form = '';
    if (!authSession?.accessToken) {
      form = '<p class="rating-message">Sign in to rate this film.</p><button class="btn btn-primary btn-sm" data-act="account">Sign in</button>';
    } else {
      const scoreValue = mine ? mine.score : '';
      const ratingInput = '<input id="rating-score" name="score" class="rating-input" type="number" min="0" max="10" step="0.1" value="' + scoreValue + '" required>';
      const disabled = !data || data.loading || data.submitting;
      form = '<form id="rating-form" class="rating-form" data-id="' + esc(f.id) + '">' +
        '<label for="rating-score">' + (mine ? 'Your rating (0–10)' : 'Rate this film (0–10)') + '</label>' +
        ratingInput +
        '<button class="btn btn-primary btn-sm" type="submit" ' + (disabled ? 'disabled' : '') + '>' + (mine ? 'Update rating' : 'Submit rating') + '</button>' +
        (mine ? '<span class="rating-message">You rated this film ' + mine.score + ' / 10.</span>' : '') +
        '</form>';
    }
    let history = '';
    if (data?.loading) history = '<p class="rating-message" role="status">Loading ratings…</p>';
    else if (data?.error) history = '<p class="form-error" role="alert">' + esc(data.error) + '</p><button class="btn btn-ghost btn-sm" data-act="reload-ratings" data-id="' + esc(f.id) + '">Try again</button>';
    else if (!items.length) history = '<p class="rating-message">No community ratings yet.</p>';
    else history = '<ul class="rating-list">' + items.map(item => {
      const changed = item.updatedAt ? 'Updated' : 'Rated';
      const timestamp = item.updatedAt || item.createdAt;
      const date = timestamp ? new Date(timestamp).toLocaleDateString() : '';
      return '<li><span><strong>' + esc(item.userName || 'MovieMind user') + '</strong><small>' + changed + (date ? ' · ' + esc(date) : '') + '</small></span><b>' + Number(item.score).toFixed(1) + ' / 10</b></li>';
    }).join('') + '</ul>';
    return '<section class="community-rating" aria-labelledby="community-rating-title"><div class="community-rating-head"><h2 id="community-rating-title">Community rating</h2>' + summary + '</div>' + form + '<div class="rating-history"><h3>Rating history</h3>' + history + '</div></section>';
  }
  function filmPage(id) {
    const f = film(id);
    if (!f) return `<div class="wrap page"><div class="empty"><h3>This film is not in your collection.</h3><p>It may have been deleted from the database.</p><a class="btn btn-ghost btn-sm" href="#/library">Go to library</a></div></div>`;
    const img = apiImage(f.poster);
    const seen = state.seen.has(f.id);
    const similar = state.films.filter(o => o.id !== f.id && o.genres.some(g => f.genres.includes(g)))
      .map(o => [o, o.genres.filter(g => f.genres.includes(g)).length + (o.director && o.director === f.director ? 2 : 0)])
      .sort((a, b) => b[1] - a[1] || b[0].rating - a[0].rating).slice(0, 10).map(x => x[0]);
    const facts = [
      ['Director', f.director], ['Written by', f.writers.join(', ')], ['Music', f.composer],
      ['Original title', f.originalTitle && f.originalTitle !== f.title ? f.originalTitle : ''],
      ['Country', f.country], ['Language', f.language], ['Runtime', f.duration ? `${f.duration} minutes` : ''], ['Age rating', f.ageRating],
    ].filter(([, v]) => v);
    return `<article class="film">
      <div class="film-backdrop" aria-hidden="true">${img ? `<img src="${esc(img)}" alt="">` : posterSVG(f)}</div>
      <div class="wrap film-top">
        <div class="film-poster">${poster(f)}</div>
        <div class="film-head">
          <a class="back" href="#/library">${icon('left', 16)}Library</a>
          <h1 class="film-title ${lenClass(f.title)}">${esc(f.title)}</h1>
          ${f.originalTitle && f.originalTitle !== f.title ? `<p class="original">${esc(f.originalTitle)}</p>` : ''}
          <div class="facts">${[f.year, runtime(f.duration), f.ageRating].filter(Boolean).map(x => `<span>${esc(x)}</span>`).join('')}</div>
          <div class="tags">${f.genres.map(g => `<a class="tag" href="#/library?genre=${encodeURIComponent(g)}">${esc(g)}</a>`).join('')}</div>
          ${f.rating ? `<div class="rating-big" aria-label="Rating ${f.rating} out of 10"><strong>${f.rating.toFixed(1)}</strong><span>/ 10</span><div class="meter"><i style="width:${f.rating * 10}%"></i></div></div>` : ''}
          <div class="btn-row">
            <button class="btn ${state.watch.has(f.id) ? 'btn-ghost' : 'btn-primary'}" data-act="watch" data-id="${esc(f.id)}" aria-pressed="${state.watch.has(f.id)}">${icon(state.watch.has(f.id) ? 'check' : 'bookmark', 16)}${state.watch.has(f.id) ? 'On your watchlist' : 'Add to watchlist'}</button>
            <button class="btn btn-ghost" data-act="seen" data-id="${esc(f.id)}" aria-pressed="${seen}">${icon(seen ? 'check' : 'eye', 16)}${seen ? 'Watched' : 'Mark as watched'}</button>
            <a class="btn btn-ghost" href="${esc(trailerFor(f))}" target="_blank" rel="noopener">${icon('play', 15)}${f.trailerUrl ? 'Watch trailer' : 'Find trailer'}</a>
          </div>
        </div>
      </div>
      <div class="wrap film-body">
        <div class="film-main">
          ${ratingPanel(f)}
          <section><h2>Story</h2><p class="synopsis">${esc(f.synopsis || 'No synopsis has been added yet.')}</p></section>
          ${f.cast.length ? `<section><h2>Cast</h2><ul class="cast">${f.cast.map(c => `<li><span class="avatar" style="--h:${hash(c.name) % 360}">${esc(initials(c.name))}</span><div><strong>${esc(c.name)}</strong>${c.role ? `<span>${esc(c.role)}</span>` : ''}</div></li>`).join('')}</ul></section>` : ''}
          ${f.awards ? `<section><h2>Awards</h2><p class="awards">${esc(f.awards)}</p></section>` : ''}
        </div>
        <aside class="film-side">
          <dl class="facts-list">${facts.map(([k, v]) => `<div><dt>${esc(k)}</dt><dd>${esc(v)}</dd></div>`).join('')}</dl>
          ${isAdmin() ? `<div class="side-actions"><button class="btn btn-ghost btn-sm" data-act="edit" data-id="${esc(f.id)}">${icon('edit', 15)}Edit film</button><button class="btn btn-danger btn-sm" data-act="delete" data-id="${esc(f.id)}">${icon('trash', 15)}Delete</button></div>` : ''}
        </aside>
      </div>
      <div class="wrap">${shelf('More like this', similar, '')}</div>
    </article>`;
  }

  function studio() {
    if (!isAdmin()) return `<div class="wrap page"><h1 class="display">Administrator access required</h1><p class="lede">Sign in with an administrator account to manage the shared catalog.</p><button class="btn btn-primary" data-act="account">Sign in</button></div>`;
    const rows = [...state.films].sort((a, b) => b.updatedAt - a.updatedAt);
    return `<div class="wrap page">
      <div class="page-head"><div><h1 class="display">Studio</h1><p class="lede">Add, edit and remove films. Import or export the whole collection as a JSON file to move it to another computer.</p></div></div>
      <div class="studio-actions">
        <button class="btn btn-primary" data-act="add">${icon('plus', 16)}Add film</button>
        <button class="btn btn-ghost" data-act="demo">${icon('sparkle', 16)}Load demo collection</button>
        <span class="spacer"></span>
        ${loadFilms().length ? '<button class="btn btn-ghost btn-sm" data-act="import-browser">Import browser films</button>' : ''}
        <button class="btn btn-ghost btn-sm" data-act="import">${icon('upload', 15)}Import</button>
        <button class="btn btn-ghost btn-sm" data-act="export" ${state.films.length ? '' : 'disabled'}>${icon('download', 15)}Export</button>
        <button class="btn btn-danger btn-sm" data-act="clear-all" ${state.films.length ? '' : 'disabled'}>${icon('trash', 15)}Delete all</button>
        <input type="file" id="import-file" accept="application/json,.json" hidden>
      </div>
      ${rows.length ? `<div class="table-wrap"><table class="table">
        <thead><tr><th scope="col">Film</th><th scope="col">Year</th><th scope="col" class="hide-sm">Director</th><th scope="col" class="hide-sm">Genres</th><th scope="col">Rating</th><th scope="col"><span class="sr">Actions</span></th></tr></thead>
        <tbody>${rows.map(f => `<tr>
          <td><a class="t-film" href="#/film/${encodeURIComponent(f.id)}">${poster(f, 'thumb')}<span><strong>${esc(f.title)}</strong><small>${plural(f.cast.length, 'cast member')}</small></span></a></td>
          <td>${f.year}</td><td class="hide-sm">${esc(f.director || '—')}</td><td class="hide-sm">${esc(f.genres.join(', ') || '—')}</td>
          <td>${f.rating ? f.rating.toFixed(1) : '—'}</td>
          <td class="t-actions"><button class="btn btn-ghost btn-xs" data-act="edit" data-id="${esc(f.id)}">Edit</button><button class="btn btn-danger btn-xs" data-act="delete" data-id="${esc(f.id)}">Delete</button></td>
        </tr>`).join('')}</tbody></table></div>`
        : `<div class="empty"><h3>No films yet.</h3><p>Start with the demo collection or add your first film.</p></div>`}
      <div class="storage"><div><strong>Saved to the MovieMind database.</strong> Film details and posters are shared by this server. Your watchlist, watched status and theme stay in this browser. Export creates a backup of film details; back up uploaded poster files on the server as well.</div></div>
    </div>`;
  }

  /* --------------------------------------------------------------- modals */
  function modalShell(inner, cls = '') {
    return `<div class="overlay" data-overlay><div class="modal ${cls}" role="dialog" aria-modal="true" aria-labelledby="modal-title">${inner}</div></div>`;
  }
  function draftFrom(f) {
    return f ? { ...f, cast: f.cast.map(c => ({ ...c })), art: { ...f.art }, mode: f.poster ? (f.poster.startsWith('/media/') || f.poster.startsWith('data:') ? 'upload' : 'url') : 'art', upload: f.poster?.startsWith('/media/') || f.poster?.startsWith('data:') ? f.poster : '', url: f.poster && !f.poster.startsWith('/media/') && !f.poster.startsWith('data:') ? f.poster : '' }
      : { id: '', title: '', year: new Date().getFullYear(), genres: [], cast: [], writers: [], art: { palette: Math.floor(Math.random() * PALETTES.length), motif: Object.keys(MOTIFS)[Math.floor(Math.random() * 11)] }, mode: 'art', upload: '', url: '' };
  }
  function formModal() {
    const d = state.draft;
    const editing = !!d.id;
    const field = (name, label, attrs = '', hint = '') => `<label class="field"><span>${label}</span><input name="${name}" ${attrs}>${hint ? `<small>${hint}</small>` : ''}</label>`;
    return modalShell(`<form id="film-form" novalidate>
      <div class="modal-head"><h2 id="modal-title">${editing ? `Edit ${esc(d.title)}` : 'Add a film'}</h2><button type="button" class="icon-btn" data-act="close" aria-label="Close">${icon('close')}</button></div>
      <div class="modal-body form-layout">
        <div class="form-main">
          <fieldset><legend>Basics</legend><div class="fgrid">
            ${field('title', 'Title <b>*</b>', `required maxlength="140" value="${esc(d.title)}" placeholder="e.g. Inception"`, '').replace('class="field"', 'class="field span2"')}
            ${field('originalTitle', 'Original title', `maxlength="140" value="${esc(d.originalTitle || '')}" placeholder="If different from the title"`).replace('class="field"', 'class="field span2"')}
            ${field('year', 'Release year <b>*</b>', `type="number" inputmode="numeric" min="1888" max="2100" required value="${esc(d.year)}"`)}
            ${field('duration', 'Runtime (minutes)', `type="number" inputmode="numeric" min="0" max="999" value="${esc(d.duration || '')}" placeholder="148"`)}
            <label class="field"><span>Age rating</span><select name="ageRating"><option value="">Not set</option>${AGE_RATINGS.map(a => `<option ${d.ageRating === a ? 'selected' : ''}>${a}</option>`).join('')}</select></label>
            ${field('rating', 'Rating (0–10)', `type="number" step="0.1" min="0" max="10" value="${esc(d.rating || '')}" placeholder="8.5"`)}
          </div>
          <div class="field"><span>Genres</span><div class="chips">${[...new Set([...GENRES, ...d.genres])].map(g => `<label class="chip"><input type="checkbox" name="genres" value="${esc(g)}" ${d.genres.includes(g) ? 'checked' : ''}><span>${esc(g)}</span></label>`).join('')}</div></div>
          </fieldset>
          <fieldset><legend>People</legend><div class="fgrid">
            ${field('director', 'Director', `maxlength="140" value="${esc(d.director || '')}" placeholder="Christopher Nolan"`)}
            ${field('composer', 'Music by', `maxlength="120" value="${esc(d.composer || '')}" placeholder="Hans Zimmer"`)}
            ${field('writers', 'Written by', `value="${esc((d.writers || []).join(', '))}" placeholder="Separate names with commas"`).replace('class="field"', 'class="field span2"')}
            <label class="field span2"><span>Cast</span><textarea name="cast" rows="5" placeholder="One person per line: Leonardo DiCaprio as Dom Cobb">${esc(d.cast.map(c => c.role ? `${c.name} as ${c.role}` : c.name).join('\n'))}</textarea><small>One actor per line. Add the character after “as”.</small></label>
          </div></fieldset>
          <fieldset><legend>Story and details</legend><div class="fgrid">
            <label class="field span2"><span>Synopsis</span><textarea name="synopsis" rows="4" maxlength="3000" placeholder="What the film is about, without spoilers">${esc(d.synopsis || '')}</textarea></label>
            <label class="field span2"><span>Awards</span><textarea name="awards" rows="2" maxlength="800" placeholder="e.g. Won 4 Oscars">${esc(d.awards || '')}</textarea></label>
            ${field('country', 'Country', `maxlength="140" value="${esc(d.country || '')}" placeholder="United States"`)}
            ${field('language', 'Language', `maxlength="140" value="${esc(d.language || '')}" placeholder="English"`)}
            ${field('trailerUrl', 'Trailer link', `type="url" value="${esc(d.trailerUrl || '')}" placeholder="https://www.youtube.com/watch?v=…"`, 'Leave empty and the film page will search YouTube for the trailer.').replace('class="field"', 'class="field span2"')}
          </div></fieldset>
        </div>
        <aside class="form-art">
          <div class="art-preview" id="art-preview">${previewHTML()}</div>
          <div class="seg wide" role="tablist" aria-label="Poster source">
            ${[['art', 'Artwork'], ['upload', 'Upload'], ['url', 'Link']].map(([m, l]) => `<button type="button" role="tab" data-act="mode" data-mode="${m}" aria-selected="${d.mode === m}">${l}</button>`).join('')}
          </div>
          <div class="art-panel" ${d.mode === 'art' ? '' : 'hidden'} data-panel="art">
            <span class="mini-label">Colour</span>
            <div class="swatches">${PALETTES.map((p, i) => `<label class="swatch" title="${p.name}"><input type="radio" name="palette" value="${i}" ${d.art.palette === i ? 'checked' : ''}><span style="--a:${p.a};--b:${p.b};--c:${p.acc}"></span><span class="sr">${p.name}</span></label>`).join('')}</div>
            <label class="field"><span>Motif</span><select name="motif">${Object.entries(MOTIFS).map(([k, l]) => `<option value="${k}" ${d.art.motif === k ? 'selected' : ''}>${l}</option>`).join('')}</select></label>
          </div>
          <div class="art-panel" ${d.mode === 'upload' ? '' : 'hidden'} data-panel="upload">
            <div class="drop" id="drop">
              ${icon('image', 24)}<strong>Choose an image</strong><span id="poster-file-hint">or drop it here. JPG, PNG or WebP up to 10 MB. It is resized to save space.</span>
              <input type="file" id="poster-file" accept="image/jpeg,image/png,image/webp" class="poster-file" aria-label="Choose a poster image" aria-describedby="poster-file-hint upload-status">
            </div>
            <p id="upload-status" class="upload-status" role="status" aria-live="polite">${d.upload ? 'Poster uploaded. Save the film to keep it.' : ''}</p>
          </div>
          <div class="art-panel" ${d.mode === 'url' ? '' : 'hidden'} data-panel="url">
            <label class="field"><span>Image link</span><input name="posterUrl" type="url" value="${esc(d.url)}" placeholder="https://…/poster.jpg"><small>Use an HTTPS link to an image you are allowed to use.</small></label>
          </div>
        </aside>
      </div>
      <div class="modal-foot"><p class="form-error" id="form-error" role="alert"></p><button type="button" class="btn btn-ghost" data-act="close">Cancel</button><button type="submit" class="btn btn-primary">${icon('check', 16)}${editing ? 'Save changes' : 'Add film'}</button></div>
    </form>`, 'modal-xl');
  }
  function previewFilm() {
    const d = state.draft;
    const poster = d.mode === 'upload' ? d.upload : d.mode === 'url' ? safeImage(d.url) : '';
    return { id: d.id || 'draft', title: d.title || 'Untitled', year: d.year || '', director: d.director || '', art: d.art, poster };
  }
  function previewHTML() { return poster(previewFilm()); }
  function syncDraft(form) {
    const d = state.draft;
    const data = new FormData(form);
    d.title = str(data.get('title'), 140);
    d.year = data.get('year');
    d.director = str(data.get('director'), 140);
    d.art.palette = Number(data.get('palette') ?? d.art.palette);
    d.art.motif = String(data.get('motif') || d.art.motif);
    d.url = str(data.get('posterUrl'), 2000);
  }
  function updatePreview() { const box = $('#art-preview'); if (box) box.innerHTML = previewHTML(); }

  function authModal() {
    const reg = state.modal.tab === 'register';
    return modalShell(`<div class="modal-head"><h2 id="modal-title">${reg ? 'Create an account' : 'Sign in'}</h2><button class="icon-btn" data-act="close" aria-label="Close">${icon('close')}</button></div>
      <div class="modal-body">
        <div class="seg wide" role="tablist"><button role="tab" data-act="auth-tab" data-tab="signin" aria-selected="${!reg}">Sign in</button><button role="tab" data-act="auth-tab" data-tab="register" aria-selected="${reg}">Create account</button></div>
        ${authSession?.user ? `<p class="notice">Signed in as <strong>${esc(authSession.user.userName)}</strong> (${esc(authSession.user.role)}).</p><button class="btn btn-ghost full" type="button" data-act="sign-out">Sign out</button>` : `<p class="notice">${reg ? 'Create your MovieMind account.' : 'Use your email or username to sign in.'}</p>
        <form id="auth-form" class="stack">
          ${reg ? '<label class="field"><span>Username</span><input name="username" autocomplete="username" minlength="3" maxlength="50" required></label>' : ''}
          <label class="field"><span>${reg ? 'Email' : 'Email or username'}</span><input name="identifier" type="${reg ? 'email' : 'text'}" autocomplete="${reg ? 'email' : 'username'}" required></label>
          <label class="field"><span>Password</span><input name="password" type="password" minlength="8" maxlength="128" autocomplete="${reg ? 'new-password' : 'current-password'}" required></label>
          <p class="form-error" id="auth-error" role="alert"></p>
          <button class="btn btn-primary full" type="submit">${reg ? 'Create account' : 'Sign in'}</button>
        </form>`}
      </div>`, 'modal-sm');
  }
  function confirmModal() {
    const m = state.modal;
    return modalShell(`<div class="modal-head"><h2 id="modal-title">${esc(m.title)}</h2></div>
      <div class="modal-body"><p class="confirm-text">${esc(m.body)}</p></div>
      <div class="modal-foot"><button class="btn btn-ghost" data-act="close">Cancel</button><button class="btn btn-danger-solid" data-act="confirm-yes">${esc(m.yes)}</button></div>`, 'modal-sm');
  }
  function paletteModal() {
    return `<div class="overlay overlay-top" data-overlay><div class="modal modal-cmd" role="dialog" aria-modal="true" aria-label="Search films">
      <label class="cmd-input">${icon('search', 19)}<input id="cmd-q" placeholder="Search films, actors, directors…" autocomplete="off" aria-label="Search" aria-controls="cmd-list"><kbd>Esc</kbd></label>
      <ul class="cmd-list" id="cmd-list" role="listbox">${paletteItems('')}</ul></div></div>`;
  }
  function paletteItems(q) {
    const query = q.toLowerCase().trim();
    const actions = [
      ['add', 'Add a film', 'plus'], ['go:#/library', 'Open library', 'grid'], ['go:#/watchlist', 'Open watchlist', 'bookmark'],
      ['go:#/studio', 'Open studio', 'edit'], ['demo', 'Load demo collection', 'sparkle'],
      ['theme', currentTheme() === 'dark' ? 'Switch to light theme' : 'Switch to dark theme', currentTheme() === 'dark' ? 'sun' : 'moon'],
    ].filter(a => !query || a[1].toLowerCase().includes(query));
    const films = state.films.filter(f => !query || [f.title, f.originalTitle, f.director, f.cast.map(c => c.name).join(' ')].join(' ').toLowerCase().includes(query)).slice(0, 8);
    const items = [
      ...films.map(f => ({ act: `go:#/film/${encodeURIComponent(f.id)}`, html: `${poster(f, 'thumb')}<span><strong>${esc(f.title)}</strong><small>${f.year}${f.director ? `, ${esc(f.director)}` : ''}</small></span>` })),
      ...actions.map(([act, label, ic]) => ({ act, html: `<span class="cmd-ic">${icon(ic, 17)}</span><span><strong>${esc(label)}</strong></span>` })),
    ];
    if (!items.length) return `<li class="cmd-empty">No films match “${esc(q)}”.</li>`;
    return items.map((it, i) => `<li role="option" id="cmd-${i}" aria-selected="${i === 0}" data-cmd="${esc(it.act)}">${it.html}</li>`).join('');
  }

  function openModal(m) {
    state.lastFocus = document.activeElement;
    state.modal = m;
    renderModal();
  }
  function renderModal() {
    let slot = $('#modal-root');
    if (!slot) { slot = document.createElement('div'); slot.id = 'modal-root'; document.body.append(slot); }
    if (!state.modal) { slot.innerHTML = ''; document.body.classList.remove('locked'); return; }
    const t = state.modal.type;
    slot.innerHTML = t === 'form' ? formModal() : t === 'auth' ? authModal() : t === 'confirm' ? confirmModal() : t === 'palette' ? paletteModal() : '';
    document.body.classList.add('locked');
    const focusEl = $('#cmd-q', slot) || $('input:not([type=hidden]):not([type=checkbox]):not([type=radio]), button', $('.modal', slot));
    focusEl?.focus();
  }
  function closeModal() {
    if (state.saving) return;
    state.modal = null;
    state.draft = null;
    renderModal();
    state.lastFocus?.focus?.();
  }
  function confirmThen(title, body, yes, fn) { state.pending = fn; openModal({ type: 'confirm', title, body, yes }); }

  /* ------------------------------------------------------------- actions */
  async function resizeImage(file) {
    if (!['image/jpeg', 'image/png', 'image/webp'].includes(file.type) &&
        !(file.type === '' && /\.(jpe?g|png|webp)$/i.test(file.name))) {
      throw new Error('Choose a JPG, PNG or WebP image.');
    }
    if (!file.size) throw new Error('This image is empty. Choose another one.');
    if (file.size > 10 * 1024 * 1024) throw new Error('This image is larger than 10 MB. Choose a smaller one.');
    let bitmap;
    let objectUrl;
    try {
      if (typeof createImageBitmap === 'function') {
        try { bitmap = await createImageBitmap(file); } catch { /* Fall back to the browser image decoder. */ }
      }
      if (!bitmap) {
        objectUrl = URL.createObjectURL(file);
        bitmap = await new Promise((resolve, reject) => {
          const image = new Image();
          image.onload = () => resolve(image);
          image.onerror = () => reject(new Error('This image could not be read. Choose another JPG, PNG or WebP.'));
          image.src = objectUrl;
        });
      }
      const width = bitmap.naturalWidth || bitmap.width;
      const height = bitmap.naturalHeight || bitmap.height;
      if (!width || !height || width * height > 40000000) throw new Error('Choose an image smaller than 40 megapixels.');
      const scale = Math.min(1, 520 / width, 780 / height);
      const canvas = document.createElement('canvas');
      canvas.width = Math.max(1, Math.round(width * scale));
      canvas.height = Math.max(1, Math.round(height * scale));
      const context = canvas.getContext('2d');
      if (!context) throw new Error('Your browser could not prepare this image. Try another browser.');
      context.fillStyle = '#101826';
      context.fillRect(0, 0, canvas.width, canvas.height);
      context.drawImage(bitmap, 0, 0, canvas.width, canvas.height);
      return await new Promise((resolve, reject) => canvas.toBlob(
        blob => blob ? resolve(blob) : reject(new Error('This image could not be resized.')), 'image/jpeg', 0.8));
    } finally {
      bitmap?.close?.();
      if (objectUrl) URL.revokeObjectURL(objectUrl);
    }
  }
  async function useFile(file) {
    if (!file || !state.draft || state.imageBusy) return;
    const draft = state.draft;
    const status = $('#upload-status');
    const submit = $('#film-form [type="submit"]');
    state.imageBusy = true;
    if (submit) submit.disabled = true;
    if (status) { status.textContent = 'Resizing and uploading image…'; status.classList.remove('form-error'); }
    try {
      const url = await uploadPoster(await resizeImage(file));
      if (state.draft !== draft) return;
      draft.upload = url;
      draft.mode = 'upload';
      updatePreview();
      const drop = $('#drop strong'); if (drop) drop.textContent = file.name;
      if (status) status.textContent = 'Poster uploaded. Save the film to keep it.';
    } catch (error) {
      if (status) { status.textContent = error.message || 'This image could not be read.'; status.classList.add('form-error'); }
    } finally {
      state.imageBusy = false;
      if (submit?.isConnected) submit.disabled = false;
      const input = $('#poster-file'); if (input) input.value = '';
    }
  }
  async function submitFilm(form) {
    if (state.saving || state.imageBusy) return false;
    syncDraft(form);
    const data = new FormData(form);
    const err = $('#form-error');
    const fail = (msg, name) => { err.textContent = msg; form.querySelector(`[name="${name}"]`)?.focus(); return false; };
    const title = str(data.get('title'), 140);
    const year = Number(data.get('year'));
    const rating = data.get('rating') === '' ? 0 : Number(data.get('rating'));
    const duration = data.get('duration') === '' ? 0 : Number(data.get('duration'));
    if (!title) return fail('Enter the film title.', 'title');
    if (!Number.isInteger(year) || year < 1888 || year > 2100) return fail('Enter a release year between 1888 and 2100.', 'year');
    if (!Number.isInteger(duration) || duration < 0 || duration > 999) return fail('Runtime must be between 0 and 999 minutes.', 'duration');
    if (Number.isNaN(rating) || rating < 0 || rating > 10) return fail('Rating must be between 0 and 10.', 'rating');
    const trailer = str(data.get('trailerUrl'), 2000);
    if (trailer && !safeLink(trailer)) return fail('The trailer link must start with https:// or http://.', 'trailerUrl');
    const d = state.draft;
    let posterValue = '';
    if (d.mode === 'upload') {
      posterValue = d.upload;
      if (!posterValue) return fail('Choose an image or select Artwork.', 'title');
    }
    if (d.mode === 'url') {
      posterValue = safeImage(data.get('posterUrl'));
      if (str(data.get('posterUrl')) && !posterValue) return fail('The image link must start with https:// or http://.', 'posterUrl');
    }
    const existing = d.id ? film(d.id) : null;
    const entry = normalize({
      id: d.id || uid(), title, year, duration, rating,
      originalTitle: data.get('originalTitle'), ageRating: data.get('ageRating'),
      genres: data.getAll('genres'), director: data.get('director'), composer: data.get('composer'),
      writers: data.get('writers'), cast: parseCast(data.get('cast')),
      synopsis: data.get('synopsis'), awards: data.get('awards'), country: data.get('country'), language: data.get('language'),
      trailerUrl: trailer, poster: posterValue, art: { ...d.art },
      createdAt: existing?.createdAt || Date.now(), updatedAt: Date.now(),
    });
    // Keep the same ID if a response is lost and the user retries the save.
    d.id = entry.id;
    const button = form.querySelector('[type="submit"]');
    state.saving = true;
    button.disabled = true;
    err.textContent = 'Saving…';
    let saved;
    try {
      saved = await saveFilm(entry);
      persist(existing ? state.films.map(f => f.id === saved.id ? saved : f) : [saved, ...state.films]);
    } catch (error) {
      err.textContent = error.message;
      return false;
    } finally { state.saving = false; button.disabled = false; }
    closeModal();
    if (existing) { render(); toast(`Saved changes to ${saved.title}.`, 'ok'); }
    else { location.hash = `#/film/${encodeURIComponent(saved.id)}`; toast(`Added ${saved.title} to your library.`, 'ok'); }
    return true;
  }
  async function loadDemo() {
    const demo = Array.isArray(window.MOVIEMIND_DEMO) ? window.MOVIEMIND_DEMO : [];
    if (!demo.length) { toast('The demo file (demo-films.js) is not loaded.', 'error'); return; }
    const have = new Set(state.films.map(f => `${f.title.toLowerCase()}|${f.year}`));
    const now = Date.now();
    const fresh = demo.filter(d => !have.has(`${d.title.toLowerCase()}|${d.year}`))
      .map((d, i) => normalize({ ...d, id: uid(), createdAt: now - i * 1000, updatedAt: now - i * 1000 }));
    if (!fresh.length) { toast('The demo films are already in your library.'); return; }
    try { await saveCollection(fresh); } catch (error) { render(); toast(error.message, 'error'); return; }
    state.hero = 0;
    render();
    toast(`Added ${plural(fresh.length, 'demo film')} to your library.`, 'ok', { href: '#/library', label: 'Open library' });
  }
  function exportJSON() {
    const blob = new Blob([JSON.stringify({ app: 'MovieMind', version: CFG.version || '3', exportedAt: new Date().toISOString(), films: state.films }, null, 2)], { type: 'application/json' });
    const a = document.createElement('a');
    a.href = URL.createObjectURL(blob);
    a.download = `moviemind-collection-${new Date().toISOString().slice(0, 10)}.json`;
    document.body.append(a); a.click(); a.remove();
    setTimeout(() => URL.revokeObjectURL(a.href), 1000);
    toast(`Exported ${plural(state.films.length, 'film')}.`, 'ok');
  }
  async function importJSON(file) {
    try {
      const parsed = JSON.parse(await file.text());
      const incoming = (Array.isArray(parsed) ? parsed : parsed?.films || []).map(normalize).filter(Boolean);
      if (!incoming.length) throw new Error('empty');
      await saveCollection(incoming);
      render(); toast(`Imported ${plural(incoming.length, 'film')}.`, 'ok');
    } catch (error) { render(); toast(error.message === 'empty' || error instanceof SyntaxError ? 'Choose a MovieMind JSON export containing films.' : error.message, 'error'); }
  }
  function toggleWatch(id) {
    const f = film(id);
    if (!f) return;
    const on = !state.watch.has(id);
    on ? state.watch.add(id) : state.watch.delete(id);
    if (!persist()) return;
    if (['watchlist', 'film'].includes(state.route.name)) render();
    else {
      $$(`.watch-btn[data-id="${CSS.escape(id)}"]`).forEach(b => b.outerHTML = watchBtn(f, [...b.classList].filter(c => ['lg', 'static'].includes(c)).join(' ')));
      const nav = $('.nav a[href="#/watchlist"]');
      if (nav) nav.innerHTML = `Watchlist${state.watch.size ? `<span class="count">${state.watch.size}</span>` : ''}`;
    }
    toast(on ? `Added ${f.title} to your watchlist.` : `Removed ${f.title} from your watchlist.`);
  }

  const handlers = {
    add: () => { if (!isAdmin()) { toast(authSession?.user ? 'Only an administrator can edit the shared catalog.' : 'Sign in as an administrator to edit the catalog.', 'error'); return; } state.draft = draftFrom(null); openModal({ type: 'form' }); },
    edit: el => { if (!isAdmin()) { toast('Only an administrator can edit the shared catalog.', 'error'); return; } const f = film(el.dataset.id); if (f) { state.draft = draftFrom(f); openModal({ type: 'form' }); } },
    delete: el => {
      if (!isAdmin()) { toast('Only an administrator can edit the shared catalog.', 'error'); return; }
      const f = film(el.dataset.id); if (!f) return;
      confirmThen(`Delete ${f.title}?`, 'This film will be removed from the shared database. This cannot be undone.', 'Delete film', async () => {
        try { await api(`/films/${encodeURIComponent(f.id)}`, { method: 'DELETE' }); }
        catch (error) { toast(error.message, 'error'); return; }
        state.watch.delete(f.id); state.seen.delete(f.id);
        if (persist(state.films.filter(x => x.id !== f.id))) { toast(`Deleted ${f.title}.`); if (state.route.name === 'film') location.hash = '#/library'; else render(); }
      });
    },
    'clear-all': () => { if (!isAdmin()) { toast('Only an administrator can edit the shared catalog.', 'error'); return; } confirmThen('Delete all films?', `All ${plural(state.films.length, 'film')} will be removed from the shared database. Export first if you want a backup.`, 'Delete all', async () => {
      try {
        for (const f of [...state.films]) {
          await api(`/films/${encodeURIComponent(f.id)}`, { method: 'DELETE' });
          state.watch.delete(f.id); state.seen.delete(f.id);
          persist(state.films.filter(x => x.id !== f.id));
        }
        render(); toast('Deleted all films.');
      } catch (error) { render(); toast(error.message, 'error'); }
    }); },
    'confirm-yes': () => { const fn = state.pending; state.pending = null; closeModal(); fn?.(); },
    close: () => closeModal(),
    watch: el => toggleWatch(el.dataset.id),
    seen: el => { const id = el.dataset.id; state.seen.has(id) ? state.seen.delete(id) : state.seen.add(id); if (persist()) render(); },
    'reload-ratings': el => loadFilmRatings(el.dataset.id, true),
    'reload-preferences': () => loadUserPreferences(true),
    'reload-recommendations': () => loadRecommendations(true),
    demo: () => { if (!isAdmin()) { toast('Only an administrator can load films into the shared catalog.', 'error'); return; } loadDemo(); },
    retry: () => loadCatalog(),
    'import-browser': async () => {
      try { await saveCollection(loadFilms()); render(); toast('Browser films saved to the database.', 'ok'); }
      catch (error) { render(); toast(error.message, 'error'); }
    },
    export: () => exportJSON(),
    import: () => $('#import-file')?.click(),
    account: () => openModal({ type: 'auth', tab: 'signin' }),
    'sign-out': () => { try { sessionStorage.removeItem('moviemind.auth'); } catch { /* optional browser storage */ } authSession = null; state.preferences = { data: null, loading: false, loaded: false, error: '', saving: false }; state.recommendations = { data: null, loading: false, loaded: false, error: '' }; closeModal(); render(); toast('You have signed out.'); },
    'auth-tab': el => { state.modal.tab = el.dataset.tab; renderModal(); },
    palette: () => openModal({ type: 'palette' }),
    theme: () => applyTheme(currentTheme() === 'dark' ? 'light' : 'dark'),
    view: el => { state.view = el.dataset.v; try { localStorage.setItem(KEYS.view, JSON.stringify(state.view)); } catch { /* optional */ } $$('.seg [data-act="view"]').forEach(b => b.setAttribute('aria-pressed', String(b === el))); refreshResults(); },
    'clear-filters': () => { state.q = ''; state.genre = ''; state.decade = ''; render(); },
    'hero-prev': () => { state.hero--; swapHero(); },
    'hero-next': () => { state.hero++; swapHero(); },
    'hero-go': el => { state.hero = Number(el.dataset.i); swapHero(); },
    'shelf-left': el => el.closest('.shelf').querySelector('.shelf-track').scrollBy({ left: -innerWidth * 0.7, behavior: 'smooth' }),
    'shelf-right': el => el.closest('.shelf').querySelector('.shelf-track').scrollBy({ left: innerWidth * 0.7, behavior: 'smooth' }),
    mode: el => {
      const form = $('#film-form'); if (form) syncDraft(form);
      state.draft.mode = el.dataset.mode;
      $$('[data-act="mode"]').forEach(b => b.setAttribute('aria-selected', String(b === el)));
      $$('.art-panel').forEach(p => { p.hidden = p.dataset.panel !== state.draft.mode; });
      updatePreview();
    },
  };
  function swapHero() {
    const hero = $('#hero');
    if (!hero) return;
    hero.outerHTML = heroHTML();
    $('#hero')?.classList.add('swap');
  }

  /* ------------------------------------------------------------- routing */
  function parseRoute() {
    const raw = location.hash.replace(/^#\/?/, '');
    const [path, query = ''] = raw.split('?');
    const parts = path.split('/').filter(Boolean);
    const params = new URLSearchParams(query);
    if (parts[0] === 'film' && parts[1]) return { name: 'film', id: decodeURIComponent(parts[1]) };
    if (parts[0] === 'preferences') return { name: 'preferences', id: null };
    if (parts[0] === 'recommendations') return { name: 'recommendations', id: null };
    if (['library', 'watchlist', 'studio'].includes(parts[0])) {
      if (params.has('genre')) { state.genre = params.get('genre'); state.q = ''; state.decade = ''; }
      if (params.has('sort')) state.sort = params.get('sort');
      return { name: parts[0], id: null };
    }
    return { name: 'home', id: null };
  }
  const app = $('#moviemind-app');
  function render() {
    if (!app) return;
    if (state.loading) { app.innerHTML = '<div class="boot" role="status">Loading your film library…</div>'; return; }
    if (state.loadError) {
      app.innerHTML = `<div class="wrap page"><h1>Could not load the library</h1><p role="alert">${esc(state.loadError)}</p><button class="btn btn-primary" data-act="retry">Try again</button></div>`;
      return;
    }
    const r = state.route;
    const page = r.name === 'library' ? catalogPage('library') : r.name === 'watchlist' ? catalogPage('watchlist') : r.name === 'studio' ? studio() : r.name === 'film' ? filmPage(r.id) : r.name === 'preferences' ? preferencesPage() : r.name === 'recommendations' ? recommendationsPage() : home();
    app.innerHTML = `<a class="skip" href="#main">Skip to content</a>${topbar()}<main id="main" tabindex="-1" class="main main-${r.name}">${page}</main>${footer()}`;
    document.title = `${r.name === 'film' ? (film(r.id)?.title || 'Film') : ({ home: 'Home', library: 'Library', watchlist: 'Watchlist', studio: 'Studio', preferences: 'Preferences', recommendations: 'For you' })[r.name]} — MovieMind`;
  }
  function onRoute() {
    const prev = state.route;
    state.route = parseRoute();
    if (state.modal) closeModal();
    render();
    if (state.route.name === 'film') loadFilmRatings(state.route.id);
    if (state.route.name === 'preferences' && authSession?.accessToken) loadUserPreferences();
    if (state.route.name === 'recommendations') loadRecommendations();
    if (prev.name !== state.route.name || prev.id !== state.route.id) window.scrollTo(0, 0);
  }

  /* -------------------------------------------------------------- events */
  document.addEventListener('click', e => {
    const cmd = e.target.closest('[data-cmd]');
    if (cmd) { runCmd(cmd.dataset.cmd); return; }
    const el = e.target.closest('[data-act]');
    if (el && handlers[el.dataset.act]) { e.preventDefault(); handlers[el.dataset.act](el); return; }
    if (e.target.matches('[data-overlay]')) closeModal();
  });
  function runCmd(act) {
    closeModal();
    if (act.startsWith('go:')) location.hash = act.slice(3);
    else handlers[act]?.();
  }
  document.addEventListener('input', e => {
    const t = e.target;
    if (t.id === 'q') { state.q = t.value; refreshResults(); }
    if (t.id === 'cmd-q') $('#cmd-list').innerHTML = paletteItems(t.value);
    if (t.matches('[data-pref-range]')) {
      const row = t.closest('.preference-row');
      const output = row?.querySelector('[data-pref-value]');
      if (output) output.value = Number(t.value).toFixed(1).replace(/\.0$/, '');
      updatePreferencePreview(row, true);
    }
    if (t.form?.id === 'film-form' && ['title', 'year', 'director', 'palette', 'motif', 'posterUrl'].includes(t.name)) { syncDraft(t.form); updatePreview(); }
  });
  document.addEventListener('change', e => {
    const t = e.target;
    if (t.id === 'f-genre') { state.genre = t.value; refreshResults(); }
    if (t.id === 'f-decade') { state.decade = t.value; refreshResults(); }
    if (t.id === 'f-sort') { state.sort = t.value; refreshResults(); }
    if (t.matches('[data-pref-toggle]')) {
      const row = t.closest('.preference-row');
      const range = row?.querySelector('[data-pref-range]');
      if (range) range.disabled = !t.checked;
      row?.classList.toggle('is-custom', t.checked);
      updatePreferencePreview(row, t.checked);
    }
    if (t.id === 'poster-file') useFile(t.files?.[0]);
    if (t.id === 'import-file' && t.files?.[0]) { importJSON(t.files[0]); t.value = ''; }
    if (t.form?.id === 'film-form' && ['palette', 'motif'].includes(t.name)) { syncDraft(t.form); updatePreview(); }
  });
  async function submitAuth(form) {
    const errorBox = $('#auth-error');
    const button = form.querySelector('[type="submit"]');
    const isRegister = state.modal?.tab === 'register';
    const data = new FormData(form);
    if (button) button.disabled = true;
    if (errorBox) errorBox.textContent = '';
    try {
      const result = await api(isRegister ? '/auth/register' : '/auth/login', {
        method: 'POST', headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(isRegister
          ? { userName: data.get('username'), email: data.get('identifier'), password: data.get('password') }
          : { identifier: data.get('identifier'), password: data.get('password') }),
      });
      authSession = { accessToken: result.accessToken, user: { id: result.id, userName: result.userName, email: result.email, role: result.role }, expiresAtUtc: result.expiresAtUtc };
      try { sessionStorage.setItem('moviemind.auth', JSON.stringify(authSession)); } catch { /* token remains in memory for this page */ }
      state.recommendations = { data: null, loading: false, loaded: false, error: '' };
      closeModal(); render();
      if (state.route.name === 'preferences') loadUserPreferences(true);
      loadRecommendations(true);
      toast(isRegister ? 'Account created. You are now signed in.' : `Welcome, ${authSession.user.userName}.`, 'ok');
    } catch (error) {
      if (errorBox) errorBox.textContent = error.message;
      if (button?.isConnected) button.disabled = false;
    }
  }
  async function submitRating(form) {
    if (!authSession?.accessToken) { openModal({ type: 'auth', tab: 'signin' }); return; }
    const id = form.dataset.id;
    const rawScore = new FormData(form).get('score');
    if (rawScore === null || rawScore === '') { toast('Choose a rating from 0 to 10.', 'error'); return; }
    const score = Number(rawScore);
    if (!Number.isFinite(score) || score < 0 || score > 10) { toast('Choose a rating from 0 to 10.', 'error'); return; }
    const mine = state.rating.items.find(item => item.userId === authSession?.user?.id);
    const button = form.querySelector('[type="submit"]');
    if (button) button.disabled = true;
    state.rating.submitting = true;
    try {
      await api('/movies/' + encodeURIComponent(id) + '/ratings' + (mine ? '/me' : ''), {
        method: mine ? 'PUT' : 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ score }),
      });
      state.recommendations.loaded = false;
      if (state.route.name === 'recommendations') loadRecommendations(true);
      toast(mine ? 'Your rating was updated.' : 'Your rating was saved.', 'ok');
      await loadFilmRatings(id, true);
    } catch (error) {
      if (button?.isConnected) button.disabled = false;
      state.rating.submitting = false;
      toast(error.message, 'error');
    }
  }
  async function submitUserPreferences(form) {
    if (!authSession?.accessToken) { openModal({ type: 'auth', tab: 'signin' }); return; }
    const genrePreferences = $$('[data-pref-toggle]:checked', form).map(toggle => ({
      genreId: toggle.value,
      score: Number(toggle.closest('.preference-row')?.querySelector('[data-pref-range]')?.value),
    }));
    const button = form.querySelector('[type="submit"]');
    state.preferences.saving = true;
    state.preferences.error = '';
    if (button) { button.disabled = true; button.textContent = 'Saving…'; }
    const errorBox = $('#preferences-error');
    if (errorBox) errorBox.textContent = '';
    try {
      state.preferences.data = await api('/users/me/preferences', {
        method: 'PUT',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ genrePreferences }),
      });
      state.preferences.loaded = true;
      state.preferences.saving = false;
      state.recommendations.loaded = false;
      if (state.route.name === 'recommendations') loadRecommendations(true);
      render();
      toast('Your taste profile was saved.', 'ok');
    } catch (error) {
      state.preferences.error = error.message;
      state.preferences.saving = false;
      if (errorBox) errorBox.textContent = error.message;
      if (button?.isConnected) { button.disabled = false; button.textContent = 'Save preferences'; }
      toast(error.message, 'error');
    }
  }
  document.addEventListener('submit', e => {
    if (e.target.id === 'film-form') { e.preventDefault(); submitFilm(e.target); }
    if (e.target.id === 'auth-form') { e.preventDefault(); submitAuth(e.target); }
    if (e.target.id === 'rating-form') { e.preventDefault(); submitRating(e.target); }
    if (e.target.id === 'preferences-form') { e.preventDefault(); submitUserPreferences(e.target); }
  });
  ['dragenter', 'dragover'].forEach(ev => document.addEventListener(ev, e => { const d = e.target.closest?.('#drop'); if (d) { e.preventDefault(); d.classList.add('is-over'); } }));
  ['dragleave', 'drop'].forEach(ev => document.addEventListener(ev, e => {
    const d = e.target.closest?.('#drop');
    if (!d) return;
    e.preventDefault();
    if (ev === 'dragleave' && d.contains(e.relatedTarget)) return;
    d.classList.remove('is-over');
    if (ev === 'drop') useFile(e.dataTransfer?.files?.[0]);
  }));
  document.addEventListener('keydown', e => {
    const typing = /^(INPUT|TEXTAREA|SELECT)$/.test(e.target.tagName);
    if ((e.ctrlKey || e.metaKey) && e.key.toLowerCase() === 'k') { e.preventDefault(); if (state.modal?.type !== 'palette') openModal({ type: 'palette' }); return; }
    if (e.key === '/' && !typing && !state.modal) { e.preventDefault(); openModal({ type: 'palette' }); return; }
    if (!state.modal) return;
    if (e.key === 'Escape') { e.preventDefault(); closeModal(); return; }
    if (state.modal.type === 'palette' && ['ArrowDown', 'ArrowUp', 'Enter'].includes(e.key)) {
      const opts = $$('#cmd-list [data-cmd]');
      if (!opts.length) return;
      let i = opts.findIndex(o => o.getAttribute('aria-selected') === 'true');
      if (e.key === 'Enter') { e.preventDefault(); runCmd(opts[Math.max(0, i)].dataset.cmd); return; }
      e.preventDefault();
      i = (i + (e.key === 'ArrowDown' ? 1 : -1) + opts.length) % opts.length;
      opts.forEach((o, j) => o.setAttribute('aria-selected', String(j === i)));
      opts[i].scrollIntoView({ block: 'nearest' });
      $('#cmd-q')?.setAttribute('aria-activedescendant', opts[i].id);
    }
    if (e.key === 'Tab') { // keep focus inside the dialog
      const f = $$('#modal-root .modal :is(a[href],button,input,select,textarea):not([disabled]):not([hidden])').filter(x => x.offsetParent !== null);
      if (!f.length) return;
      if (e.shiftKey && document.activeElement === f[0]) { e.preventDefault(); f[f.length - 1].focus(); }
      else if (!e.shiftKey && document.activeElement === f[f.length - 1]) { e.preventDefault(); f[0].focus(); }
    }
  });
  window.addEventListener('storage', e => {
    if (e.key === KEYS.watch || e.key === KEYS.seen || e.key === null) {
      state.watch = new Set(read(KEYS.watch, [])); state.seen = new Set(read(KEYS.seen, [])); render();
    }
  });
  window.addEventListener('hashchange', onRoute);
  onRoute();
  loadCatalog();
})();

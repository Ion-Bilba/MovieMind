/* Local frontend settings. Set MOVIEMIND_API_ORIGIN before this file to override the API host. */
const movieMindApiOrigin = window.MOVIEMIND_API_ORIGIN
  || (location.protocol === 'https:' ? 'https://localhost:7185' : 'http://localhost:5048');
window.MOVIEMIND_CONFIG = Object.freeze({
  version: '4.0',
  storageMode: 'database',
  apiOrigin: movieMindApiOrigin,
  apiBaseUrl: `${movieMindApiOrigin}/api`,
  authEnabled: true,
});
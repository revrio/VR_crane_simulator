// No Unity loader is evaluated, and no simulation runs, before this barrier.
export async function downloadAssets(manifest, {fetchFile = fetch, cache = null, onProgress = () => {}} = {}) {
  const completed = new Map();
  const total = manifest.files.reduce((sum, file) => sum + file.bytes, 0);
  let loaded = 0;
  for (const file of manifest.files) {
    const url = new URL(file.path, globalThis.location?.href || 'https://portlab.invalid/').href;
    let response = await cache?.match(url);
    const cached = Boolean(response);
    if (!response) response = await fetchFile(url, {cache: 'no-store'});
    if (!response.ok) throw new Error(`Could not download ${file.role} (HTTP ${response.status}).`);
    const chunks = [];
    let bytes = 0;
    if (response.body) {
      const reader = response.body.getReader();
      while (true) {
        const {done, value} = await reader.read();
        if (done) break;
        chunks.push(value); bytes += value.byteLength;
        onProgress(Math.min((loaded + bytes) / total, 1), loaded + bytes, total);
      }
    } else {
      const chunk = new Uint8Array(await response.arrayBuffer());
      chunks.push(chunk); bytes = chunk.byteLength;
    }
    if (bytes !== file.bytes) {
      await cache?.delete(url);
      throw new Error(`Incomplete ${file.role} download. Please retry.`);
    }
    const compressed = new Blob(chunks, {type:'application/octet-stream'});
    const digest = await crypto.subtle.digest('SHA-256', await compressed.arrayBuffer());
    const hash = Array.from(new Uint8Array(digest), b => b.toString(16).padStart(2,'0')).join('');
    if (hash !== file.sha256) {
      await cache?.delete(url);
      throw new Error(`The ${file.role} download failed verification. Please retry.`);
    }
    // Cache is optional; quota/private-mode failures must not block playback.
    if (!cached && cache) { try { await cache.put(url, new Response(compressed)); } catch {} }
    const decoded = file.encoding === 'gzip'
      ? await new Response(compressed.stream().pipeThrough(new DecompressionStream('gzip'))).blob()
      : compressed;
    completed.set(file.role, new Blob([decoded], {type:file.type}));
    loaded += bytes;
    onProgress(loaded / total, loaded, total);
  }
  return completed;
}

async function mount() {
  const status = document.getElementById('status');
  const progress = document.getElementById('progress');
  const action = document.getElementById('action');
  const canvas = document.getElementById('unity-canvas');
  const mb = bytes => (bytes / 1048576).toFixed(1);
  let assets, manifest, urls = [], instance;
  async function prepare() {
    action.disabled = true; action.textContent = 'Downloading assets…';
    status.classList.remove('error'); progress.value = 0;
    try {
      if (!globalThis.WebAssembly || !document.createElement('canvas').getContext('webgl2'))
        throw new Error('This simulator needs a browser with WebGL 2 and WebAssembly enabled.');
      if (!globalThis.DecompressionStream)
        throw new Error('Please update your browser to support compressed asset downloads.');
      const response = await fetch('asset-manifest.json', {cache:'no-store'});
      if (!response.ok) throw new Error('Could not load the asset list. Please retry.');
      manifest = await response.json();
      let cache = null;
      try { cache = await caches.open(`portlab-assets-${manifest.version}`); } catch {}
      assets = await downloadAssets(manifest, {cache, onProgress:(fraction, loaded, total) => {
        progress.value = fraction;
        status.textContent = `Downloading assets · ${Math.floor(fraction * 100)}% · ${mb(loaded)} / ${mb(total)} MB`;
      }});
      for (const role of ['loader','framework','code','data'])
        if (!assets.has(role)) throw new Error(`The build is missing its ${role} asset.`);
      status.textContent = 'All assets downloaded and verified. Ready to start.';
      action.textContent = 'Start simulator'; action.disabled = false;
      action.onclick = start;
      // Only remove previous versions after the new version is complete.
      try { for (const key of await caches.keys()) if (key.startsWith('portlab-assets-') && key !== `portlab-assets-${manifest.version}`) await caches.delete(key); } catch {}
    } catch (error) { fail(error, prepare); }
  }
  function fail(error, retry) {
    status.textContent = error.message || String(error); status.classList.add('error');
    action.textContent = 'Retry'; action.disabled = false; action.onclick = retry;
  }
  async function start() {
    action.disabled = true; action.textContent = 'Starting simulator…';
    status.textContent = 'Starting simulator from downloaded assets…';
    const makeUrl = role => { const url = URL.createObjectURL(assets.get(role)); urls.push(url); return url; };
    try {
      // Blob URLs cannot make an HTTP request. The engine consumes only the
      // verified local bytes, so startup does not race network downloads.
      const config = {dataUrl:makeUrl('data'), frameworkUrl:makeUrl('framework'), codeUrl:makeUrl('code'),
        companyName:'PortLab', productName:'PortLab Operations', productVersion:manifest.version,
        cacheControl:() => 'no-store', matchWebGLToCanvasSize:true,
        devicePixelRatio:Math.min(window.devicePixelRatio || 1, 1.5)};
      await new Promise((resolve, reject) => {
        const script = document.createElement('script'); script.src = makeUrl('loader');
        script.onload = resolve; script.onerror = () => reject(new Error('Could not initialize the downloaded loader.'));
        document.body.append(script);
      });
      canvas.hidden = false;
      instance = await window.createUnityInstance(canvas, config, value => {
        status.textContent = `Starting simulator · ${Math.floor(value * 100)}%`;
      });
      document.body.classList.add('running'); document.getElementById('toolbar').hidden = false;
      canvas.focus();
      for (const url of urls) URL.revokeObjectURL(url); urls = []; assets.clear();
    } catch (error) {
      canvas.hidden = true;
      // A partially initialized WASM runtime is safest to retry in a new page.
      fail(error, () => location.reload());
    }
  }
  document.getElementById('fullscreen').onclick = () => instance?.SetFullscreen(1);
  await prepare();
}
if (typeof document !== 'undefined') mount();

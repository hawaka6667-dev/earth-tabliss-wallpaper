/* ============================================================
   Earth Tabliss Live Wallpaper - Zero-Overhead Static Engine
   Double-Buffered Preload Queue (0 Black Screen, 0ms Next Switch)
   ============================================================ */

(function () {
  'use strict';

  const LOCAL_FALLBACKS = [
    {
      country: 'Windows 桌面原色',
      region: '电脑原生壁纸 (安全兜底)',
      coords: { lat: '0.000000', lng: '0.000000' },
      map: '',
      image: 'assets/original_wallpaper.png'
    },
    {
      country: '蔚蓝星球',
      region: '行星地貌精选 (离线底图)',
      coords: { lat: '-10.040181', lng: '143.560709' },
      map: 'https://earth.google.com/web/',
      image: 'assets/default_earth.jpg'
    }
  ];

  const state = {
    locations: [],
    currentIndex: -1,
    preloadQueue: [], // Preloaded buffer queue [{ item, img }]
    loadingCount: 0,
    maxQueueSize: 4,
    localFallbackIndex: 0,
    activeLayer: 'a',
    isHudVisible: true,
    currentLocation: LOCAL_FALLBACKS[1]
  };

  // Immediate queue seeding with local offline assets so queue length is > 0 from millisecond 0
  LOCAL_FALLBACKS.forEach(item => {
    const img = new Image();
    img.src = item.image;
    state.preloadQueue.push({ item, img });
  });

  const el = {
    bgA: document.getElementById('bg-layer-a'),
    bgB: document.getElementById('bg-layer-b'),
    hudCenter: document.getElementById('hud-center'),
    hudBottom: document.getElementById('hud-bottom'),
    hours: document.getElementById('clock-hours'),
    minutes: document.getElementById('clock-minutes'),
    date: document.getElementById('date-display'),
    greeting: document.getElementById('greeting-display'),
    country: document.getElementById('geo-country'),
    region: document.getElementById('geo-region'),
    valLat: document.getElementById('val-lat'),
    valLng: document.getElementById('val-lng'),
    drawerGemini: document.getElementById('drawer-gemini'),
    btnCloseGemini: document.getElementById('btn-close-gemini'),
    geminiTargetLoc: document.getElementById('gemini-target-loc'),
    geminiTargetCoords: document.getElementById('gemini-target-coords'),
    resGeomorphology: document.getElementById('res-geomorphology'),
    resFormation: document.getElementById('res-formation'),
    resResearchNotes: document.getElementById('res-research-notes'),
    btnRunGemini: document.getElementById('btn-run-gemini-analysis')
  };

  // 1. Clock Engine (Lightweight, 0% CPU)
  function updateClock() {
    const now = new Date();
    el.hours.textContent = String(now.getHours()).padStart(2, '0');
    el.minutes.textContent = String(now.getMinutes()).padStart(2, '0');

    const weekdays = ['星期日', '星期一', '星期二', '星期三', '星期四', '星期五', '星期六'];
    const year = now.getFullYear();
    const month = now.getMonth() + 1;
    const day = now.getDate();
    const weekday = weekdays[now.getDay()];
    el.date.textContent = `${year}年${month}月${day}日 ${weekday}`;

    const h = now.getHours();
    let text = '漫游蔚蓝星球';
    if (h >= 5 && h < 9) text = '清晨好，探索者';
    else if (h >= 9 && h < 12) text = '上午好，俯瞰大地脉络';
    else if (h >= 12 && h < 14) text = '午间好，静观行星流转';
    else if (h >= 14 && h < 18) text = '下午好，发现未知地貌';
    else if (h >= 18 && h < 23) text = '夜晚好，星河与地表辉映';
    else text = '夜深了，地球仍在静默自转';
    el.greeting.textContent = text;
  }

  setInterval(updateClock, 1000);
  updateClock();

  // 2. Data & Coordinates
  function parseCoordinates(mapUrl) {
    if (!mapUrl) return { lat: '0.000000', lng: '0.000000' };
    const match = mapUrl.match(/@(-?\d+\.\d+),(-?\d+\.\d+)/);
    if (match) {
      return {
        lat: parseFloat(match[1]).toFixed(6),
        lng: parseFloat(match[2]).toFixed(6)
      };
    }
    return { lat: '0.000000', lng: '0.000000' };
  }

  // 3. Preload Queue Buffer Engine (Handles Initial Load & Rapid Continuous Next Requests)
  function fillPreloadQueue() {
    if (!state.locations || state.locations.length === 0) return;

    while (state.preloadQueue.length + state.loadingCount < state.maxQueueSize) {
      state.loadingCount++;
      state.currentIndex = (state.currentIndex + 1) % state.locations.length;
      const candidate = state.locations[state.currentIndex];

      if (!candidate || !candidate.image) {
        state.loadingCount--;
        break;
      }

      candidate.coords = parseCoordinates(candidate.map);
      const img = new Image();

      img.onload = () => {
        state.loadingCount--;
        state.preloadQueue.push({ item: candidate, img: img });

        if (state.preloadQueue.length + state.loadingCount < state.maxQueueSize) {
          fillPreloadQueue();
        }
      };

      img.onerror = () => {
        state.loadingCount--;
        // Push local fallback on error so queue is never starved
        const fallback = LOCAL_FALLBACKS[state.localFallbackIndex++ % LOCAL_FALLBACKS.length];
        const fImg = new Image();
        fImg.src = fallback.image;
        state.preloadQueue.push({ item: fallback, img: fImg });
        fillPreloadQueue();
      };

      img.src = candidate.image;
    }
  }

  // 4. Apply Wallpaper & Double-Buffer Crossfade (0ms switch, zero black screen)
  function applyWallpaper(item) {
    if (!item) return;
    state.currentLocation = item;
    if (!item.coords) item.coords = parseCoordinates(item.map);

    // Update HUD typography
    el.country.textContent = item.country || '地球未知秘境';
    el.region.textContent = item.region || '自然地貌卫星观测';
    el.valLat.textContent = item.coords.lat;
    el.valLng.textContent = item.coords.lng;

    // Double-buffer swap with fallback chain (Primary image -> original PC wallpaper -> default earth)
    const targetLayer = state.activeLayer === 'a' ? el.bgB : el.bgA;
    const oldLayer = state.activeLayer === 'a' ? el.bgA : el.bgB;

    targetLayer.style.backgroundImage = `url("${item.image}"), url("assets/original_wallpaper.png"), url("assets/default_earth.jpg")`;
    targetLayer.classList.add('active');
    oldLayer.classList.remove('active');

    state.activeLayer = state.activeLayer === 'a' ? 'b' : 'a';

    // Notify C# Host of current location data
    if (window.chrome && window.chrome.webview) {
      window.chrome.webview.postMessage({
        type: 'location_changed',
        data: {
          id: item.id || '',
          country: item.country || '',
          region: item.region || '',
          coords: item.coords,
          map: item.map || '',
          image: item.image || ''
        }
      });
    }
  }

  function nextWallpaper() {
    if (state.preloadQueue.length > 0) {
      // 1. Instant pop from preloaded memory queue (0ms, zero black screen guaranteed)
      const next = state.preloadQueue.shift();
      applyWallpaper(next.item);
      fillPreloadQueue(); // Replenish buffer queue
      return;
    }

    // 2. Queue is temporarily empty (e.g. rapid user clicks or network delay)
    // NEVER crossfade into a black void! Keep the current image visible while loading candidate.
    if (!state.locations || state.locations.length === 0) {
      const fallback = LOCAL_FALLBACKS[state.localFallbackIndex++ % LOCAL_FALLBACKS.length];
      applyWallpaper(fallback);
      return;
    }

    state.currentIndex = (state.currentIndex + 1) % state.locations.length;
    const candidate = state.locations[state.currentIndex];
    candidate.coords = parseCoordinates(candidate.map);

    const testImg = new Image();
    let isHandled = false;

    // 2.0s network timeout: if download lags, smoothly fall back to local placeholder / original wallpaper
    const timeoutTimer = setTimeout(() => {
      if (isHandled) return;
      isHandled = true;
      const fallback = LOCAL_FALLBACKS[state.localFallbackIndex++ % LOCAL_FALLBACKS.length];
      applyWallpaper(fallback);
      fillPreloadQueue();
    }, 2000);

    testImg.onload = () => {
      if (isHandled) return;
      isHandled = true;
      clearTimeout(timeoutTimer);
      applyWallpaper(candidate);
      fillPreloadQueue();
    };

    testImg.onerror = () => {
      if (isHandled) return;
      isHandled = true;
      clearTimeout(timeoutTimer);
      const fallback = LOCAL_FALLBACKS[state.localFallbackIndex++ % LOCAL_FALLBACKS.length];
      applyWallpaper(fallback);
      fillPreloadQueue();
    };

    testImg.src = candidate.image;
  }

  async function loadEarthLocations() {
    try {
      const resp = await fetch('data/earthview.json');
      state.locations = await resp.json();
      state.currentIndex = Math.floor(Math.random() * state.locations.length);
      fillPreloadQueue();
    } catch (err) {
      console.error('[EarthWallpaper] Failed to load earthview.json:', err);
    }
  }

  // 5. Toggle HUD Visibility
  function toggleHud(visible) {
    if (typeof visible === 'boolean') {
      state.isHudVisible = visible;
    } else {
      state.isHudVisible = !state.isHudVisible;
    }
    const op = state.isHudVisible ? '1' : '0';
    if (el.hudCenter) el.hudCenter.style.opacity = op;
    if (el.hudBottom) el.hudBottom.style.opacity = op;
  }

  // 6. Gemini Analysis
  function openGemini() {
    if (!state.currentLocation) return;
    const loc = state.currentLocation;
    el.geminiTargetLoc.textContent = `${loc.country} ${loc.region ? '- ' + loc.region : ''}`;
    el.geminiTargetCoords.textContent = `${loc.coords.lat}° N, ${loc.coords.lng}° E`;
    el.drawerGemini.classList.add('open');

    // Run simulated / heuristic analysis
    el.resGeomorphology.textContent = `该区域位于 ${loc.country} (${loc.coords.lat}, ${loc.coords.lng})。卫星光学影像呈现出显著的地质风化冲蚀特征，明暗光影展示了断层隆起与沉积水系构造。`;
    el.resFormation.textContent = `地质演变简析：构造应力与长期侵蚀交织，形成了独特的宏观地球脉络。`;
    el.resResearchNotes.innerHTML = `
      <li>推荐在 Google Earth 中调整倾角 (Tilt 45°) 逆光观测高程落差。</li>
      <li>可使用 Google Earth 历史时间轴观测近 30 年局部沉积与水文变迁。</li>
    `;
  }

  function closeGemini() {
    el.drawerGemini.classList.remove('open');
  }

  // 7. IPC Listener from C# Host
  if (window.chrome && window.chrome.webview) {
    window.chrome.webview.addEventListener('message', (event) => {
      const msg = event.data;
      if (!msg) return;
      if (msg.type === 'next_wallpaper') {
        nextWallpaper();
      } else if (msg.type === 'toggle_hud') {
        toggleHud(msg.visible);
      } else if (msg.type === 'open_gemini') {
        openGemini();
      } else if (msg.type === 'set_custom_wallpaper') {
        const item = msg.data;
        if (!item || !item.image) return;
        const img = new Image();
        img.onload = () => applyWallpaper(item);
        img.onerror = () => applyWallpaper({
          country: item.country || '地标收藏',
          region: item.region || '自定义地貌',
          coords: { lat: item.lat || '0.000000', lng: item.lng || '0.000000' },
          map: item.map || '',
          image: 'assets/default_earth.jpg'
        });
        img.src = item.image;
      }
    });
  }

  if (el.btnCloseGemini) el.btnCloseGemini.addEventListener('click', closeGemini);
  if (el.btnRunGemini) el.btnRunGemini.addEventListener('click', openGemini);

  // Notify C# Host of initial default location immediately
  if (window.chrome && window.chrome.webview) {
    window.chrome.webview.postMessage({
      type: 'location_changed',
      data: state.currentLocation
    });
  }

  loadEarthLocations();

  // Auto-switch wallpaper every 5 minutes (0.5s fade, then 0 FPS)
  setInterval(() => {
    nextWallpaper();
  }, 5 * 60 * 1000);

})();

/* ============================================================
   Earth Tabliss Live Wallpaper - Zero-Overhead Static Engine
   Double-Buffered Preload Queue (0 Black Screen, 0ms Next Switch)
   ============================================================ */

(function () {
  'use strict';

  const state = {
    locations: [],
    currentIndex: -1,
    preloadQueue: [], // Preloaded buffer queue [{ item, img }]
    loadingCount: 0,
    maxQueueSize: 3,
    activeLayer: 'a',
    isHudVisible: true,
    currentLocation: {
      country: 'Australia',
      region: 'Torres Strait',
      coords: { lat: '-10.040181', lng: '143.560709' },
      map: 'https://www.google.com/maps/@-10.040181,143.560709,12z/data=!3m1!1e3',
      image: 'assets/default_earth.jpg'
    }
  };

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
      // Pick next sequential/random candidate
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

        // If this is the initial fill and we haven't rendered the first live wallpaper yet
        if (state.currentLocation.image === 'assets/default_earth.jpg' && state.preloadQueue.length === 1) {
          nextWallpaper();
        } else if (state.preloadQueue.length + state.loadingCount < state.maxQueueSize) {
          fillPreloadQueue();
        }
      };

      img.onerror = () => {
        state.loadingCount--;
        fillPreloadQueue(); // Try next on network glitch
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

    // Double-buffer swap
    const targetLayer = state.activeLayer === 'a' ? el.bgB : el.bgA;
    const oldLayer = state.activeLayer === 'a' ? el.bgA : el.bgB;

    targetLayer.style.backgroundImage = `url("${item.image}")`;
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
      // Instant pop from preloaded memory queue!
      const next = state.preloadQueue.shift();
      applyWallpaper(next.item);
      fillPreloadQueue(); // Replenish buffer queue
    } else if (state.locations && state.locations.length > 0) {
      // Fallback if user clicked faster than buffer replenishment
      state.currentIndex = (state.currentIndex + 1) % state.locations.length;
      const fallbackItem = state.locations[state.currentIndex];
      fallbackItem.coords = parseCoordinates(fallbackItem.map);
      applyWallpaper(fallbackItem);
      fillPreloadQueue();
    }
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
        applyWallpaper(item);
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

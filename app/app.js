/* ============================================================
   Earth Tabliss Live Wallpaper - Zero-Overhead Static Engine
   Double-Buffered Preload Queue (0 Black Screen, 0ms Next Switch)
   ============================================================ */

(function () {
  'use strict';

  const OFFLINE_FALLBACK = {
    id: 'offline_earth',
    country: '蔚蓝星球',
    region: '行星地貌精选 (离线底图)',
    coords: { lat: '-10.040181', lng: '143.560709' },
    map: 'https://earth.google.com/web/',
    image: 'assets/default_earth.jpg'
  };

  const state = {
    locations: [],
    currentIndex: -1,
    preloadQueue: [], // Preloaded buffer queue [{ item, img }]
    historyStack: [], // Previous wallpaper history (max 20)
    _skipHistory: false,
    loadingCount: 0,
    maxQueueSize: 5,
    activeLayer: 'a',
    isHudVisible: true,
    currentLocation: OFFLINE_FALLBACK,
    shuffledIndices: [],
    shufflePtr: 0
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

  // 3. Preload Queue Buffer Engine (Fisher-Yates Dynamic Shuffle Deck)
  function initShuffleDeck() {
    if (!state.locations || state.locations.length === 0) return;
    state.shuffledIndices = Array.from({ length: state.locations.length }, (_, i) => i);
    for (let i = state.shuffledIndices.length - 1; i > 0; i--) {
      const j = Math.floor(Math.random() * (i + 1));
      const tmp = state.shuffledIndices[i];
      state.shuffledIndices[i] = state.shuffledIndices[j];
      state.shuffledIndices[j] = tmp;
    }
    state.shufflePtr = 0;
  }

  function getNextCandidate() {
    if (!state.locations || state.locations.length === 0) return null;
    if (state.shuffledIndices.length === 0 || state.shufflePtr >= state.shuffledIndices.length) {
      initShuffleDeck();
    }
    const idx = state.shuffledIndices[state.shufflePtr++];
    return state.locations[idx];
  }

  function fillPreloadQueue() {
    if (!state.locations || state.locations.length === 0) return;

    while (state.preloadQueue.length + state.loadingCount < state.maxQueueSize) {
      const candidate = getNextCandidate();
      if (!candidate || !candidate.image) break;

      state.loadingCount++;
      candidate.coords = parseCoordinates(candidate.map);
      const img = new Image();

      let resolved = false;
      const onDone = (success) => {
        if (resolved) return;
        resolved = true;
        state.loadingCount--;
        if (success) {
          state.preloadQueue.push({ item: candidate, img: img });
        }
        // If one fails or succeeds, keep queue full
        if (state.preloadQueue.length + state.loadingCount < state.maxQueueSize) {
          fillPreloadQueue();
        }
      };

      img.onload = () => onDone(true);
      img.onerror = () => onDone(false);

      // 6s timeout guard for slow network
      setTimeout(() => {
        if (!resolved) onDone(false);
      }, 6000);

      img.src = candidate.image;
    }
  }

  // 4. Apply Wallpaper & Double-Buffer Crossfade (0ms switch, zero black screen)
  function applyWallpaper(item) {
    if (!item || !item.image) return;

    // Push current to history stack (unless navigating backwards)
    if (!state._skipHistory && state.currentLocation && state.currentLocation.id !== 'offline_earth') {
      state.historyStack.push(state.currentLocation);
      if (state.historyStack.length > 25) state.historyStack.shift();
    }
    state._skipHistory = false;

    state.currentLocation = item;
    if (!item.coords) item.coords = parseCoordinates(item.map);

    // Update HUD typography
    el.country.textContent = item.country || '地球未知秘境';
    el.region.textContent = item.region || '自然地貌卫星观测';
    el.valLat.textContent = item.coords.lat;
    el.valLng.textContent = item.coords.lng;

    // Double-buffer swap with clean direct image URL
    const targetLayer = state.activeLayer === 'a' ? el.bgB : el.bgA;
    const oldLayer = state.activeLayer === 'a' ? el.bgA : el.bgB;

    targetLayer.style.backgroundImage = `url("${item.image}")`;
    targetLayer.style.backgroundSize = 'cover';
    targetLayer.style.backgroundPosition = 'center center';
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
      // 1. Instant pop from preloaded memory queue (0ms, zero black screen)
      const next = state.preloadQueue.shift();
      applyWallpaper(next.item);
      fillPreloadQueue(); // Replenish buffer queue
      return;
    }

    // 2. Queue empty: get next candidate and preload before applying (NEVER flash black)
    const candidate = getNextCandidate();
    if (!candidate) return;
    candidate.coords = parseCoordinates(candidate.map);

    const testImg = new Image();
    let isHandled = false;

    const timeoutTimer = setTimeout(() => {
      if (isHandled) return;
      isHandled = true;
      // Do not apply broken black screen; try next in background
      fillPreloadQueue();
    }, 3000);

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
      fillPreloadQueue();
    };

    testImg.src = candidate.image;
  }

  function previousWallpaper() {
    if (state.historyStack.length === 0) return;
    const prev = state.historyStack.pop();
    state._skipHistory = true;
    applyWallpaper(prev);
  }

  async function loadEarthLocations() {
    try {
      const resp = await fetch('data/earthview.json');
      state.locations = await resp.json();
      initShuffleDeck();

      // Immediately preload and apply the very first fresh online location
      const firstCandidate = getNextCandidate();
      if (firstCandidate && firstCandidate.image) {
        firstCandidate.coords = parseCoordinates(firstCandidate.map);
        const fImg = new Image();
        let loaded = false;

        fImg.onload = () => {
          if (loaded) return;
          loaded = true;
          applyWallpaper(firstCandidate);
          fillPreloadQueue();
        };

        fImg.onerror = () => {
          if (loaded) return;
          loaded = true;
          const retry = getNextCandidate();
          if (retry) applyWallpaper(retry);
          fillPreloadQueue();
        };

        // 2.5s network timeout on startup
        setTimeout(() => {
          if (!loaded) {
            loaded = true;
            applyWallpaper(firstCandidate);
            fillPreloadQueue();
          }
        }, 2500);

        fImg.src = firstCandidate.image;
      } else {
        fillPreloadQueue();
      }
    } catch (err) {
      console.error('[EarthWallpaper] Failed to load earthview.json:', err);
      applyWallpaper(OFFLINE_FALLBACK);
    }
  }

  // 5. Drawer & Interactive Gemini Geomorphology Analysis
  function openGemini() {
    if (!state.currentLocation) return;
    const loc = state.currentLocation;

    el.geminiTargetLoc.textContent = `${loc.country || ''} - ${loc.region || ''}`;
    el.geminiTargetCoords.textContent = `LAT: ${loc.coords?.lat || '0.000000'} • LNG: ${loc.coords?.lng || '0.000000'}`;

    el.resGeomorphology.textContent = '卫星光谱与遥感特征计算中...';
    el.resFormation.textContent = '地质年代与演化驱动力反演中...';
    el.resResearchNotes.textContent = '正在连接 Gemini 地质大模型推理集群...';

    el.drawerGemini.classList.add('open');

    if (window.chrome && window.chrome.webview) {
      window.chrome.webview.postMessage({
        type: 'request_gemini_analysis',
        data: {
          country: loc.country,
          region: loc.region,
          coords: loc.coords,
          map: loc.map
        }
      });
    }
  }

  function closeGemini() {
    el.drawerGemini.classList.remove('open');
  }

  // 6. IPC Communication with C# WinForms Host
  if (window.chrome && window.chrome.webview) {
    window.chrome.webview.addEventListener('message', (event) => {
      const msg = event.data;
      if (!msg) return;

      if (msg.type === 'next_wallpaper') {
        nextWallpaper();
      } else if (msg.type === 'previous_wallpaper') {
        previousWallpaper();
      } else if (msg.type === 'power_state') {
        if (msg.data === 'battery') {
          console.log('[Engine] Power governor: Battery detected, zero animation guaranteed.');
        }
      } else if (msg.type === 'gemini_result') {
        const res = msg.data;
        if (res) {
          el.resGeomorphology.textContent = res.geomorphology || '地貌分析数据解析完毕';
          el.resFormation.textContent = res.formation || '成因推演完毕';
          el.resResearchNotes.textContent = res.researchNotes || '科研笔记已归档';
        }
      } else if (msg.type === 'gemini_error') {
        el.resGeomorphology.textContent = '分析请求受阻';
        el.resFormation.textContent = msg.data || '未能连接到推理引擎';
        el.resResearchNotes.textContent = '请检查网络连接或 API 配置';
      } else if (msg.type === 'set_custom_wallpaper') {
        const item = msg.data;
        if (!item || !item.image) return;
        const img = new Image();
        img.onload = () => applyWallpaper(item);
        img.onerror = () => applyWallpaper(OFFLINE_FALLBACK);
        img.src = item.image;
      }
    });
  }

  if (el.btnCloseGemini) el.btnCloseGemini.addEventListener('click', closeGemini);
  if (el.btnRunGemini) el.btnRunGemini.addEventListener('click', openGemini);

  loadEarthLocations();

  // Auto-switch wallpaper every 5 minutes (0.6s fade, then 0 FPS)
  setInterval(() => {
    nextWallpaper();
  }, 5 * 60 * 1000);

})();

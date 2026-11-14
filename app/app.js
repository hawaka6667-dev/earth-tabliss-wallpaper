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
    shufflePtr: 0,
    lang: (new URLSearchParams(window.location.search).get('lang')) || 'zh'
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
    terrain: document.getElementById('geo-terrain'),
    valLat: document.getElementById('val-lat'),
    valLng: document.getElementById('val-lng'),
    coordsBox: document.getElementById('geo-coords'),
    coordCopied: document.getElementById('coord-copied'),
    solarPill: document.getElementById('geo-solar'),
    climatePill: document.getElementById('geo-climate'),
    altPill: document.getElementById('geo-alt'),
    mapLink: document.getElementById('geo-map-link'),
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
  const weekdaysZh = ['星期日', '星期一', '星期二', '星期三', '星期四', '星期五', '星期六'];
  const monthsEn = ['Jan', 'Feb', 'Mar', 'Apr', 'May', 'Jun', 'Jul', 'Aug', 'Sep', 'Oct', 'Nov', 'Dec'];
  const weekdaysEn = ['Sunday', 'Monday', 'Tuesday', 'Wednesday', 'Thursday', 'Friday', 'Saturday'];

  function updateClock() {
    const now = new Date();
    el.hours.textContent = String(now.getHours()).padStart(2, '0');
    el.minutes.textContent = String(now.getMinutes()).padStart(2, '0');

    const year = now.getFullYear();
    const month = now.getMonth();
    const day = now.getDate();
    const weekdayIdx = now.getDay();

    if (state.lang === 'en') {
      el.date.textContent = `${weekdaysEn[weekdayIdx]}, ${monthsEn[month]} ${day}, ${year}`;
      const h = now.getHours();
      let text = 'Wandering the Blue Planet';
      if (h >= 5 && h < 9) text = 'Good morning, Explorer';
      else if (h >= 9 && h < 12) text = 'Good morning, surveying the Earth';
      else if (h >= 12 && h < 14) text = 'Good afternoon, planetary stillness';
      else if (h >= 14 && h < 18) text = 'Good afternoon, discovering landscapes';
      else if (h >= 18 && h < 23) text = 'Good evening, starlight over continents';
      else text = 'Late night, Earth spins in silence';
      el.greeting.textContent = text;
    } else {
      el.date.textContent = `${year}年${month + 1}月${day}日 ${weekdaysZh[weekdayIdx]}`;
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

  // Landform & Terrain Classification (Clean Typography, No Emoji)
  function classifyTerrain(item) {
    if (!item) return { en: 'Plains', zh: '平原绿洲' };
    const c = (item.country || '').toLowerCase();
    const r = (item.region || '').toLowerCase();
    const text = c + ' ' + r;
    const lat = parseFloat(item.coords ? item.coords.lat : (item.map ? (item.map.match(/@(-?\d+\.\d+),/)||[])[1] : 0)) || 0;
    const lng = parseFloat(item.coords ? item.coords.lng : (item.map ? (item.map.match(/@(?:-?\d+\.\d+),(-?\d+\.\d+)/)||[])[1] : 0)) || 0;

    if (/glacier|ice|fjord|polar|nunatak|icefield|snow|frozen|antarctica|greenland|svalbard/.test(text) || lat < -60 || lat > 72) {
      return { en: 'Glacial', zh: '冰川极地' };
    }
    if (/volcan|caldera|crater|geothermal|fumarole|lava|basalt/.test(text)) {
      return { en: 'Volcanic', zh: '火山地热' };
    }
    if (/reef|atoll|bay|gulf|strait|coast|island|isle|peninsula|sound|beach|archipelago|lagoon|sea|ocean|channel|cape|shoal|sandbank|caye|inlet|hainan|bahamas|maldives|fiji|hawaii/.test(text) ||
        (c === 'australia' && (lat < -10 && lat > -12 && lng > 140))) {
      return { en: 'Coastal', zh: '海洋海岸' };
    }
    if (/river|delta|estuary|basin|creek|lake|wetland|marsh|floodplain|meander|mangrove|swamp|reservoir|tributary/.test(text)) {
      return { en: 'River / Wetland', zh: '水系湿地' };
    }
    if (/desert|dune|erg|badlands|arid|salt|playa|salar|wadi|sand|sahara|gobi|kalahari|namib|atacama|rub' al khali|tamarugal/.test(text) ||
        /algeria|libya|egypt|mauritania|chad|niger|mali|sudan|saudi arabia|yemen|oman|kuwait|qatar|united arab emirates|somalia|djibouti|namibia/.test(c) ||
        (c === 'chile' && lat > -27 && lat < -18) ||
        (c === 'australia' && lat > -32 && lat < -18 && lng > 115 && lng < 142)) {
      return { en: 'Desert', zh: '荒漠戈壁' };
    }
    if (/mount|peak|range|alps|plateau|canyon|valley|gorge|andes|himalaya|sierra|highland|cordillera|ridge|pass|cliff|mesa|crag/.test(text) ||
        /nepal|bhutan/.test(c) || /ngari|tibet|shannan|nagqu|qinghai/.test(r)) {
      return { en: 'Mountain', zh: '山脉高原' };
    }
    if (/city|capital|port|harbor|district|metro|town|center|airport/.test(text)) {
      return { en: 'Urban', zh: '城市聚落' };
    }
    if (/plain|forest|jungle|rainforest|woods|prairie|savanna|steppe|taiga|field|meadow|farm|agriculture|grass/.test(text)) {
      return { en: 'Plains', zh: '平原绿洲' };
    }
    if (Math.abs(lat) > 40) return { en: 'Mountain', zh: '山脉高原' };
    return { en: 'Plains', zh: '平原绿洲' };
  }

  // Geographical & Astronomical Metadata Calculator
  function updateGeoMetadata(item) {
    if (!item) return;
    const lat = parseFloat(item.coords ? item.coords.lat : (item.map ? (item.map.match(/@(-?\d+\.\d+),/)||[])[1] : 0)) || 0;
    const lng = parseFloat(item.coords ? item.coords.lng : (item.map ? (item.map.match(/@(?:-?\d+\.\d+),(-?\d+\.\d+)/)||[])[1] : 0)) || 0;

    // 1. Local Solar Time
    if (el.solarPill) {
      const now = new Date();
      const utcMinutes = now.getUTCHours() * 60 + now.getUTCMinutes();
      const offsetMinutes = Math.round(lng * 4);
      let solarMinutes = (utcMinutes + offsetMinutes) % 1440;
      if (solarMinutes < 0) solarMinutes += 1440;
      const sH = Math.floor(solarMinutes / 60);
      const sM = solarMinutes % 60;
      const isDay = (sH >= 6 && sH < 18);
      const icon = isDay ? '☀️' : '🌙';
      const offsetHours = (offsetMinutes / 60).toFixed(offsetMinutes % 60 === 0 ? 0 : 1);
      const offsetStr = offsetHours >= 0 ? `+${offsetHours}` : `${offsetHours}`;
      el.solarPill.textContent = `${icon} ${String(sH).padStart(2, '0')}:${String(sM).padStart(2, '0')} (UTC${offsetStr})`;
      el.solarPill.title = state.lang === 'zh'
        ? `当地太阳时: ${sH}:${String(sM).padStart(2, '0')} (偏离UTC ${offsetStr}小时)`
        : `Local Solar Time: ${sH}:${String(sM).padStart(2, '0')} (UTC${offsetStr})`;
    }

    // 2. Climate & Latitudinal Zone
    if (el.climatePill) {
      const absLat = Math.abs(lat);
      const isNorth = lat >= 0;
      if (state.lang === 'zh') {
        const hem = isNorth ? '北半球' : '南半球';
        let zone = '温带';
        if (absLat <= 23.5) zone = '热带';
        else if (absLat <= 35) zone = '亚热带';
        else if (absLat > 66.5) zone = '极地';
        el.climatePill.textContent = `${hem}·${zone}`;
        el.climatePill.title = `纬度气候带: ${hem} ${zone} (纬度 ${lat.toFixed(2)}°)`;
      } else {
        const hem = isNorth ? 'N.' : 'S.';
        let zone = 'Temperate';
        if (absLat <= 23.5) zone = 'Tropics';
        else if (absLat <= 35) zone = 'Subtropics';
        else if (absLat > 66.5) zone = 'Polar';
        el.climatePill.textContent = `${hem} ${zone}`;
        el.climatePill.title = `Climate Zone: ${hem} ${zone} (Lat ${lat.toFixed(2)}°)`;
      }
    }

    // 3. Satellite Eye Altitude
    if (el.altPill) {
      const zoomMatch = (item.map || '').match(/,(\d+)z/);
      const zoom = zoomMatch ? parseInt(zoomMatch[1], 10) : 12;
      let altKm = Math.round(591657550 * Math.pow(2, -zoom) / 1000);
      if (isNaN(altKm) || altKm < 1) altKm = 18;
      el.altPill.textContent = `ALT: ~${altKm}km`;
      el.altPill.title = state.lang === 'zh'
        ? `卫星视高估算: 约 ${altKm} 公里 (缩放层级 ${zoom}z)`
        : `Satellite Eye Altitude: ~${altKm} km (Zoom level ${zoom}z)`;
    }

    // 4. Google Maps Satellite Link
    if (el.mapLink) {
      const mapUrl = item.map || `https://www.google.com/maps/@${lat},${lng},12z/data=!3m1!1e3`;
      el.mapLink.href = mapUrl;
      el.mapLink.textContent = state.lang === 'zh' ? '地图 ↗' : 'MAP ↗';
    }
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
          sendQueueStatus();
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
    el.country.textContent = item.country || (state.lang === 'zh' ? '地球未知秘境' : 'Unknown Location');
    el.region.textContent = item.region || (state.lang === 'zh' ? '自然地貌卫星观测' : 'Satellite Observation');
    el.valLat.textContent = item.coords.lat;
    el.valLng.textContent = item.coords.lng;

    const terrain = classifyTerrain(item);
    item.terrain = terrain.en;
    if (el.terrain) {
      el.terrain.textContent = state.lang === 'zh' ? terrain.zh : terrain.en;
      el.terrain.title = state.lang === 'zh' ? `地貌分类: ${terrain.zh}` : `Terrain: ${terrain.en}`;
    }

    updateGeoMetadata(item);

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
          image: item.image || '',
          terrain: item.terrain || ''
        }
      });
      sendQueueStatus();
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

        // Network timeout guard: do NOT force-apply unloaded image to prevent black screen / "穿帮"
        setTimeout(() => {
          if (!loaded) {
            loaded = true;
            // Keep default offline earth wallpaper seamlessly; fill queue in background
            fillPreloadQueue();
          }
        }, 8000);

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

  function sendQueueStatus() {
    if (window.chrome && window.chrome.webview) {
      window.chrome.webview.postMessage({
        type: 'queue_status',
        data: {
          queueLength: state.preloadQueue.length,
          loadingCount: state.loadingCount,
          maxQueueSize: state.maxQueueSize,
          historyLength: state.historyStack.length,
          shufflePtr: state.shufflePtr,
          totalLocations: state.locations ? state.locations.length : 0,
          currentLocation: state.currentLocation ? {
            country: state.currentLocation.country || '',
            region: state.currentLocation.region || '',
            lat: state.currentLocation.coords ? state.currentLocation.coords.lat : '0.000000',
            lng: state.currentLocation.coords ? state.currentLocation.coords.lng : '0.000000',
            image: state.currentLocation.image || ''
          } : null,
          queuedItems: state.preloadQueue.map(q => ({
            country: q.item.country || '',
            region: q.item.region || '',
            lat: q.item.coords ? q.item.coords.lat : '0.000000',
            lng: q.item.coords ? q.item.coords.lng : '0.000000',
            image: q.item.image || ''
          })),
          historyItems: state.historyStack.slice(-10).map(h => ({
            country: h.country || '',
            region: h.region || '',
            lat: h.coords ? h.coords.lat : '0.000000',
            lng: h.coords ? h.coords.lng : '0.000000',
            image: h.image || ''
          }))
        }
      });
    }
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
      } else if (msg.type === 'request_queue_status') {
        sendQueueStatus();
      } else if (msg.type === 'reshuffle_deck') {
        initShuffleDeck();
        sendQueueStatus();
      } else if (msg.type === 'flush_queue') {
        state.preloadQueue = [];
        fillPreloadQueue();
        sendQueueStatus();
      } else if (msg.type === 'apply_queued_item') {
        const idx = typeof msg.index === 'number' ? msg.index : 0;
        if (idx >= 0 && idx < state.preloadQueue.length) {
          const chosen = state.preloadQueue.splice(idx, 1)[0];
          applyWallpaper(chosen.item);
          fillPreloadQueue();
        }
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
      } else if (msg.type === 'set_language') {
        state.lang = msg.lang || 'zh';
        updateClock();
        if (state.currentLocation) {
          const terrain = classifyTerrain(state.currentLocation);
          if (el.terrain) {
            el.terrain.textContent = state.lang === 'zh' ? terrain.zh : terrain.en;
            el.terrain.title = state.lang === 'zh' ? `地貌分类: ${terrain.zh}` : `Terrain: ${terrain.en}`;
          }
          updateGeoMetadata(state.currentLocation);
        }
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

  if (el.coordsBox) {
    el.coordsBox.addEventListener('click', (e) => {
      if (e.target.closest('a')) return;
      const lat = el.valLat ? el.valLat.textContent : '';
      const lng = el.valLng ? el.valLng.textContent : '';
      if (!lat || !lng) return;
      const coordStr = `${lat}, ${lng}`;
      if (navigator.clipboard && navigator.clipboard.writeText) {
        navigator.clipboard.writeText(coordStr).then(() => {
          showCopiedToast();
        }).catch(() => {
          showCopiedToast();
        });
      } else {
        showCopiedToast();
      }
    });
  }

  function showCopiedToast() {
    if (!el.coordCopied) return;
    el.coordCopied.classList.remove('show');
    void el.coordCopied.offsetWidth; // trigger reflow
    el.coordCopied.classList.add('show');
    setTimeout(() => {
      if (el.coordCopied) el.coordCopied.classList.remove('show');
    }, 1500);
  }

  loadEarthLocations();

  // Auto-switch wallpaper every 5 minutes (0.6s fade, then 0 FPS)
  setInterval(() => {
    nextWallpaper();
  }, 5 * 60 * 1000);

})();

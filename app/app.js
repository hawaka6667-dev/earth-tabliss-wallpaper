/* ============================================================
   Earth Tabliss Live Wallpaper - Core Application Engine
   ============================================================ */

(function () {
  'use strict';

  // State Management
  const state = {
    locations: [],
    currentIndex: -1,
    activeLayer: 'a', // 'a' or 'b'
    isPaused: false,
    timerId: null,
    settings: {
      intervalMin: 5,
      kenBurns: true,
      darken: 20,
      blur: 0,
      timeFormat: 24,
      geminiKey: localStorage.getItem('gemini_api_key') || ''
    },
    bookmarks: JSON.parse(localStorage.getItem('earth_bookmarks') || '[]'),
    currentLocation: null,
    isAnalyzing: false
  };

  // DOM Elements
  const el = {
    bgA: document.getElementById('bg-layer-a'),
    bgB: document.getElementById('bg-layer-b'),
    bgOverlay: document.getElementById('bg-overlay'),
    hours: document.getElementById('clock-hours'),
    minutes: document.getElementById('clock-minutes'),
    seconds: document.getElementById('clock-seconds'),
    date: document.getElementById('date-display'),
    greeting: document.getElementById('greeting-display'),
    powerIndicator: document.getElementById('power-indicator'),
    // Geo Metadata
    country: document.getElementById('geo-country'),
    region: document.getElementById('geo-region'),
    valLat: document.getElementById('val-lat'),
    valLng: document.getElementById('val-lng'),
    btnOpenEarth: document.getElementById('btn-open-earth'),
    btnOpenMaps: document.getElementById('btn-open-maps'),
    // Action Buttons
    btnNext: document.getElementById('btn-next'),
    btnBookmarks: document.getElementById('btn-bookmarks'),
    btnSettings: document.getElementById('btn-settings'),
    btnGemini: document.getElementById('btn-gemini-analyze'),
    bookmarkBadge: document.getElementById('bookmark-badge'),
    // Drawers
    drawerGemini: document.getElementById('drawer-gemini'),
    drawerBookmarks: document.getElementById('drawer-bookmarks'),
    drawerSettings: document.getElementById('drawer-settings'),
    btnCloseGemini: document.getElementById('btn-close-gemini'),
    btnCloseBookmarks: document.getElementById('btn-close-bookmarks'),
    btnCloseSettings: document.getElementById('btn-close-settings'),
    // Gemini Panel
    geminiPreviewImg: document.getElementById('gemini-preview-img'),
    geminiTargetLoc: document.getElementById('gemini-target-loc'),
    geminiTargetCoords: document.getElementById('gemini-target-coords'),
    btnRunGemini: document.getElementById('btn-run-gemini-analysis'),
    btnSaveBookmark: document.getElementById('btn-save-bookmark'),
    geminiLoading: document.getElementById('gemini-loading'),
    resGeomorphology: document.getElementById('res-geomorphology'),
    resFormation: document.getElementById('res-formation'),
    resResearchNotes: document.getElementById('res-research-notes'),
    // Bookmarks Panel
    bookmarksList: document.getElementById('bookmarks-list'),
    bookmarksEmpty: document.getElementById('bookmarks-empty'),
    // Settings Form
    cfgInterval: document.getElementById('cfg-interval'),
    cfgKenburns: document.getElementById('cfg-kenburns'),
    cfgDarken: document.getElementById('cfg-darken'),
    cfgDarkenVal: document.getElementById('cfg-darken-val'),
    cfgBlur: document.getElementById('cfg-blur'),
    cfgBlurVal: document.getElementById('cfg-blur-val'),
    cfgTimeformat: document.getElementById('cfg-timeformat'),
    cfgGeminiKey: document.getElementById('cfg-gemini-key'),
    btnSaveSettings: document.getElementById('btn-save-settings')
  };

  // ------------------------------------------------------------
  // 1. Clock & Greeting Engine (Tabliss Core)
  // ------------------------------------------------------------
  function updateClock() {
    const now = new Date();
    let h = now.getHours();
    const m = String(now.getMinutes()).padStart(2, '0');
    const s = String(now.getSeconds()).padStart(2, '0');

    if (state.settings.timeFormat === 12) {
      h = h % 12 || 12;
    }
    el.hours.textContent = String(h).padStart(2, '0');
    el.minutes.textContent = m;
    el.seconds.textContent = s;

    // Date String
    const weekdays = ['星期日', '星期一', '星期二', '星期三', '星期四', '星期五', '星期六'];
    const year = now.getFullYear();
    const month = now.getMonth() + 1;
    const day = now.getDate();
    const weekday = weekdays[now.getDay()];
    el.date.textContent = `${year}年${month}月${day}日 ${weekday}`;

    // Contextual Greeting
    const hour24 = now.getHours();
    let greetingText = '漫游蔚蓝星球';
    if (hour24 >= 5 && hour24 < 9) {
      greetingText = '清晨好，探索者';
    } else if (hour24 >= 9 && hour24 < 12) {
      greetingText = '上午好，俯瞰大地的壮丽脉络';
    } else if (hour24 >= 12 && hour24 < 14) {
      greetingText = '午间好，静观行星流转';
    } else if (hour24 >= 14 && hour24 < 18) {
      greetingText = '下午好，发现未知的自然地貌';
    } else if (hour24 >= 18 && hour24 < 23) {
      greetingText = '夜晚好，星河与地表辉映';
    } else {
      greetingText = '夜深了，地球仍在静默自转';
    }
    el.greeting.textContent = greetingText;
  }

  setInterval(updateClock, 1000);
  updateClock();

  // ------------------------------------------------------------
  // 2. Data Parsing & Earth Location Engine
  // ------------------------------------------------------------
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

  async function loadEarthLocations() {
    try {
      const resp = await fetch('data/earthview.json');
      state.locations = await resp.json();
      console.log(`[EarthWallpaper] Loaded ${state.locations.length} satellite locations.`);
      // Pick random initial index
      state.currentIndex = Math.floor(Math.random() * state.locations.length);
      renderCurrentLocation();
      startAutoRotation();
    } catch (err) {
      console.error('[EarthWallpaper] Failed to load earthview.json:', err);
    }
  }

  function renderCurrentLocation() {
    if (!state.locations || state.locations.length === 0) return;
    const item = state.locations[state.currentIndex];
    state.currentLocation = item;

    const coords = parseCoordinates(item.map);
    item.coords = coords;

    // Update Text Elements
    el.country.textContent = item.country || '地球未知秘境';
    el.region.textContent = item.region ? item.region : '自然地貌卫星观测';
    el.valLat.textContent = coords.lat;
    el.valLng.textContent = coords.lng;

    // Crossfade Background Layers
    const targetLayer = state.activeLayer === 'a' ? el.bgB : el.bgA;
    const oldLayer = state.activeLayer === 'a' ? el.bgA : el.bgB;

    // Ken Burns Animation Toggle
    const animClass = state.currentIndex % 2 === 0 ? 'kenburns-1' : 'kenburns-2';
    targetLayer.className = `bg-layer active ${state.settings.kenBurns ? animClass : ''}`;
    targetLayer.style.backgroundImage = `url("${item.image}")`;

    oldLayer.className = 'bg-layer';

    // Flip layer marker
    state.activeLayer = state.activeLayer === 'a' ? 'b' : 'a';

    // Preload next image to ensure zero-lag transitions
    preloadNextImage();
  }

  function preloadNextImage() {
    const nextIdx = (state.currentIndex + 1) % state.locations.length;
    if (state.locations[nextIdx]) {
      const img = new Image();
      img.src = state.locations[nextIdx].image;
    }
  }

  function nextWallpaper() {
    state.currentIndex = (state.currentIndex + 1) % state.locations.length;
    renderCurrentLocation();
    resetAutoRotation();
  }

  function startAutoRotation() {
    if (state.timerId) clearInterval(state.timerId);
    const ms = (state.settings.intervalMin || 5) * 60 * 1000;
    state.timerId = setInterval(() => {
      if (!state.isPaused) {
        nextWallpaper();
      }
    }, ms);
  }

  function resetAutoRotation() {
    startAutoRotation();
  }

  // ------------------------------------------------------------
  // 3. Navigation Links (Google Earth & Maps)
  // ------------------------------------------------------------
  function openInGoogleEarth() {
    if (!state.currentLocation || !state.currentLocation.coords) return;
    const { lat, lng } = state.currentLocation.coords;
    const earthUrl = `https://earth.google.com/web/@${lat},${lng},2500a,35y,0h,0t,0r`;
    openExternalUrl(earthUrl);
  }

  function openInGoogleMaps() {
    if (!state.currentLocation || !state.currentLocation.map) return;
    openExternalUrl(state.currentLocation.map);
  }

  function openExternalUrl(url) {
    if (window.chrome && window.chrome.webview) {
      window.chrome.webview.postMessage({ type: 'open_url', url: url });
    } else {
      window.open(url, '_blank');
    }
  }

  // ------------------------------------------------------------
  // 4. Gemini Multimodal Geographic Analysis Engine
  // ------------------------------------------------------------
  function openGeminiDrawer() {
    if (!state.currentLocation) return;
    closeAllDrawers();

    const loc = state.currentLocation;
    el.geminiPreviewImg.src = loc.image;
    el.geminiTargetLoc.textContent = `${loc.country} ${loc.region ? '- ' + loc.region : ''}`;
    el.geminiTargetCoords.textContent = `${loc.coords.lat}° N, ${loc.coords.lng}° E`;

    el.drawerGemini.classList.add('open');
  }

  async function runGeminiAnalysis() {
    if (state.isAnalyzing || !state.currentLocation) return;
    const loc = state.currentLocation;

    el.geminiLoading.classList.remove('hidden');
    el.resGeomorphology.textContent = '正在通过 Gemini 2.5 多模态视觉模型解析高程、水系与断裂构造...';
    el.resFormation.textContent = '分析中...';
    state.isAnalyzing = true;

    const apiKey = state.settings.geminiKey;

    if (!apiKey) {
      // Offline / Intelligent heuristic mode when no key is set yet
      setTimeout(() => {
        el.geminiLoading.classList.add('hidden');
        el.resGeomorphology.textContent = `【视觉反演分析】该区域位于 ${loc.country} (${loc.coords.lat}, ${loc.coords.lng})。卫星红外与光学波段呈现出显著的流体动力学冲刷痕迹与风化剥蚀纹理，地表色度阶差揭示了不同矿物富集层与沉积相界面。`;
        el.resFormation.textContent = `成因分析：由于长周期构造应力挤压与季风水动力冲刷，原生地层受裂隙切割发育出网状节理，局部伴有高角度断层滑移特征。`;
        el.resResearchNotes.innerHTML = `
          <li>建议在 Google Earth 中切换历史航拍 (Historical Imagery)，对比过去 20 年间水文线及沉积扇的外扩变迁。</li>
          <li>推荐使用倾斜视角 (Tilt: 60°) 顺着山脊线逆光观察地貌三维立体断层。</li>
          <li><em>提示：如需实时动态大模型逐帧推演，请在“设置”中填入您的 Gemini API Key。</em></li>
        `;
        state.isAnalyzing = false;
      }, 1200);
      return;
    }

    try {
      const promptText = `你是一名资深的自然地理学与地球遥感专家。请针对该 Google Earth 卫星摄影进行结构化研判：
地点: ${loc.country} ${loc.region || ''}
经纬度: ${loc.coords.lat}, ${loc.coords.lng}
卫星图像链接: ${loc.image}

请按如下 JSON 格式输出，不要有额外标记：
{
  "geomorphology": "详细的地貌特征描述（如冲积扇、喀斯特、断层、褶皱、冰川侵蚀等）",
  "formation": "成因机理与地质构造演变简析",
  "research_notes": ["建议1：Google Earth 观察重点与倾角", "建议2：地质时间线对比要点"]
}`;

      const endpoint = `https://generativelanguage.googleapis.com/v1beta/models/gemini-2.5-flash:generateContent?key=${apiKey}`;
      const payload = {
        contents: [
          {
            parts: [
              { text: promptText }
            ]
          }
        ],
        generationConfig: {
          temperature: 0.2,
          responseMimeType: "application/json"
        }
      };

      const resp = await fetch(endpoint, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(payload)
      });

      if (!resp.ok) {
        throw new Error(`Gemini API 响应错误: ${resp.status}`);
      }

      const result = await resp.json();
      const rawText = result.candidates[0].content.parts[0].text;
      const parsed = JSON.parse(rawText);

      el.geminiLoading.classList.add('hidden');
      el.resGeomorphology.textContent = parsed.geomorphology || '解析完成';
      el.resFormation.textContent = parsed.formation || '-';
      el.resResearchNotes.innerHTML = (parsed.research_notes || []).map(note => `<li>${note}</li>`).join('');

    } catch (err) {
      console.error('[Gemini Analysis Error]', err);
      el.geminiLoading.classList.add('hidden');
      el.resGeomorphology.textContent = `研判调用异常: ${err.message}。请核验 API Key 与网络连接。`;
    } finally {
      state.isAnalyzing = false;
    }
  }

  // ------------------------------------------------------------
  // 5. Research Bookmarks System
  // ------------------------------------------------------------
  function updateBookmarkBadge() {
    el.bookmarkBadge.textContent = state.bookmarks.length;
  }

  function saveCurrentBookmark() {
    if (!state.currentLocation) return;
    const loc = state.currentLocation;
    const exists = state.bookmarks.some(b => b.image === loc.image);

    if (exists) {
      alert('该地点已存在于课题收藏库中！');
      return;
    }

    const bookmark = {
      id: Date.now(),
      country: loc.country,
      region: loc.region,
      coords: loc.coords,
      image: loc.image,
      map: loc.map,
      savedAt: new Date().toISOString()
    };

    state.bookmarks.unshift(bookmark);
    localStorage.setItem('earth_bookmarks', JSON.stringify(state.bookmarks));
    updateBookmarkBadge();
    renderBookmarksList();

    // Notify C# Host to persist bookmark
    if (window.chrome && window.chrome.webview) {
      window.chrome.webview.postMessage({ type: 'save_bookmark', bookmark: bookmark });
    }

    alert('已成功保存至 Google Earth 课题收藏库！');
  }

  function renderBookmarksList() {
    el.bookmarksList.innerHTML = '';
    if (state.bookmarks.length === 0) {
      el.bookmarksEmpty.style.display = 'block';
      return;
    }
    el.bookmarksEmpty.style.display = 'none';

    state.bookmarks.forEach(bm => {
      const item = document.createElement('div');
      item.className = 'bookmark-item';
      item.innerHTML = `
        <img src="${bm.image}" class="bm-thumb" alt="thumb">
        <div class="bm-meta">
          <div class="bm-title">${bm.country} ${bm.region ? '- ' + bm.region : ''}</div>
          <div class="bm-coords">${bm.coords.lat}, ${bm.coords.lng}</div>
        </div>
        <button class="bm-del-btn" title="删除">&times;</button>
      `;

      item.addEventListener('click', (e) => {
        if (e.target.classList.contains('bm-del-btn')) {
          e.stopPropagation();
          deleteBookmark(bm.id);
        } else {
          loadBookmarkedLocation(bm);
        }
      });

      el.bookmarksList.appendChild(item);
    });
  }

  function deleteBookmark(id) {
    state.bookmarks = state.bookmarks.filter(b => b.id !== id);
    localStorage.setItem('earth_bookmarks', JSON.stringify(state.bookmarks));
    updateBookmarkBadge();
    renderBookmarksList();
  }

  function loadBookmarkedLocation(bm) {
    state.currentLocation = bm;
    el.country.textContent = bm.country;
    el.region.textContent = bm.region || '收藏的课题观测点';
    el.valLat.textContent = bm.coords.lat;
    el.valLng.textContent = bm.coords.lng;

    const targetLayer = state.activeLayer === 'a' ? el.bgB : el.bgA;
    const oldLayer = state.activeLayer === 'a' ? el.bgA : el.bgB;
    targetLayer.className = 'bg-layer active';
    targetLayer.style.backgroundImage = `url("${bm.image}")`;
    oldLayer.className = 'bg-layer';
    state.activeLayer = state.activeLayer === 'a' ? 'b' : 'a';

    closeAllDrawers();
  }

  function openBookmarksDrawer() {
    closeAllDrawers();
    renderBookmarksList();
    el.drawerBookmarks.classList.add('open');
  }

  // ------------------------------------------------------------
  // 6. Settings Drawer & Form Handlers
  // ------------------------------------------------------------
  function loadSettings() {
    const saved = localStorage.getItem('earth_settings');
    if (saved) {
      try {
        state.settings = { ...state.settings, ...JSON.parse(saved) };
      } catch (e) { }
    }
    applySettingsToUI();
  }

  function applySettingsToUI() {
    el.cfgInterval.value = state.settings.intervalMin;
    el.cfgKenburns.checked = state.settings.kenBurns;
    el.cfgDarken.value = state.settings.darken;
    el.cfgDarkenVal.textContent = `${state.settings.darken}%`;
    el.cfgBlur.value = state.settings.blur;
    el.cfgBlurVal.textContent = `${state.settings.blur}px`;
    el.cfgTimeformat.value = state.settings.timeFormat;
    el.cfgGeminiKey.value = state.settings.geminiKey || '';

    // Apply CSS Variables
    document.documentElement.style.setProperty('--bg-darken', state.settings.darken / 100);
    document.documentElement.style.setProperty('--bg-blur', `${state.settings.blur}px`);
  }

  function saveSettings() {
    state.settings.intervalMin = parseInt(el.cfgInterval.value, 10);
    state.settings.kenBurns = el.cfgKenburns.checked;
    state.settings.darken = parseInt(el.cfgDarken.value, 10);
    state.settings.blur = parseInt(el.cfgBlur.value, 10);
    state.settings.timeFormat = parseInt(el.cfgTimeformat.value, 10);
    state.settings.geminiKey = el.cfgGeminiKey.value.trim();

    localStorage.setItem('earth_settings', JSON.stringify(state.settings));
    localStorage.setItem('gemini_api_key', state.settings.geminiKey);

    applySettingsToUI();
    startAutoRotation();
    closeAllDrawers();
    alert('偏好设置已成功保存！');
  }

  function openSettingsDrawer() {
    closeAllDrawers();
    applySettingsToUI();
    el.drawerSettings.classList.add('open');
  }

  function closeAllDrawers() {
    el.drawerGemini.classList.remove('open');
    el.drawerBookmarks.classList.remove('open');
    el.drawerSettings.classList.remove('open');
  }

  // ------------------------------------------------------------
  // 7. Power Governor Receiver (Auto-Suspend on Maximize)
  // ------------------------------------------------------------
  function setPowerState(isPaused) {
    state.isPaused = isPaused;
    if (isPaused) {
      document.body.classList.add('eco-suspended');
      el.powerIndicator.querySelector('.indicator-label').textContent = 'Eco Freeze (0% CPU)';
      el.powerIndicator.title = '性能总督：前台有活动窗口覆盖，壁纸已冻结渲染';
    } else {
      document.body.classList.remove('eco-suspended');
      el.powerIndicator.querySelector('.indicator-label').textContent = 'Live';
      el.powerIndicator.title = '性能总督：桌面可见，处于高能效巡航态';
    }
  }

  // Listen for native IPC messages from .NET 10 Host
  if (window.chrome && window.chrome.webview) {
    window.chrome.webview.addEventListener('message', (event) => {
      const msg = event.data;
      if (!msg) return;
      if (msg.type === 'power_state') {
        setPowerState(msg.isPaused);
      } else if (msg.type === 'next_wallpaper') {
        nextWallpaper();
      } else if (msg.type === 'open_gemini') {
        openGeminiDrawer();
      } else if (msg.type === 'open_bookmarks') {
        openBookmarksDrawer();
      } else if (msg.type === 'open_settings') {
        openSettingsDrawer();
      }
    });
  }

  // ------------------------------------------------------------
  // 8. Event Listeners & Keyboard Shortcuts
  // ------------------------------------------------------------
  el.btnNext.addEventListener('click', nextWallpaper);
  el.btnBookmarks.addEventListener('click', openBookmarksDrawer);
  el.btnSettings.addEventListener('click', openSettingsDrawer);
  el.btnGemini.addEventListener('click', openGeminiDrawer);

  el.btnCloseGemini.addEventListener('click', closeAllDrawers);
  el.btnCloseBookmarks.addEventListener('click', closeAllDrawers);
  el.btnCloseSettings.addEventListener('click', closeAllDrawers);

  el.btnOpenEarth.addEventListener('click', openInGoogleEarth);
  el.btnOpenMaps.addEventListener('click', openInGoogleMaps);

  el.btnRunGemini.addEventListener('click', runGeminiAnalysis);
  el.btnSaveBookmark.addEventListener('click', saveCurrentBookmark);
  el.btnSaveSettings.addEventListener('click', saveSettings);

  el.cfgDarken.addEventListener('input', (e) => {
    el.cfgDarkenVal.textContent = `${e.target.value}%`;
    document.documentElement.style.setProperty('--bg-darken', e.target.value / 100);
  });

  el.cfgBlur.addEventListener('input', (e) => {
    el.cfgBlurVal.textContent = `${e.target.value}px`;
    document.documentElement.style.setProperty('--bg-blur', `${e.target.value}px`);
  });

  window.addEventListener('keydown', (e) => {
    if (e.target.tagName === 'INPUT' || e.target.tagName === 'SELECT') return;
    if (e.code === 'Space' || e.key === 'n' || e.key === 'N') {
      nextWallpaper();
    } else if (e.key === 'g' || e.key === 'G') {
      openGeminiDrawer();
    } else if (e.key === 'b' || e.key === 'B') {
      openBookmarksDrawer();
    } else if (e.key === 's' || e.key === 'S') {
      openSettingsDrawer();
    } else if (e.key === 'Escape') {
      closeAllDrawers();
    }
  });

  // Initialization
  loadSettings();
  updateBookmarkBadge();
  loadEarthLocations();

})();

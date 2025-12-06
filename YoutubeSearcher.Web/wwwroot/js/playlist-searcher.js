let currentChannel = null;
let currentPlaylist = null;
let currentPlaylistVideos = [];
let searchConnection = null;
let selectedVideoIds = [];

initializeSignalR();

document.addEventListener('DOMContentLoaded', function () {
    const searchChannelBtn = document.getElementById('searchChannelBtn');
    const playlistInput = document.getElementById('playlist');
    const downloadPlaylistBtn = document.getElementById('downloadPlaylistBtn');
    const selectAllBtn = document.getElementById('selectAllBtn');
    const deselectAllBtn = document.getElementById('deselectAllBtn');

    playlistInput.addEventListener('keypress', function (e) {
        if (e.key === 'Enter') {
            searchChannel();
        }
    });

    searchChannelBtn.addEventListener('click', searchChannel);
    downloadPlaylistBtn.addEventListener('click', downloadSelectedVideos);
    selectAllBtn.addEventListener('click', selectAllVideos);
    deselectAllBtn.addEventListener('click', deselectAllVideos);
});

async function searchChannel() {
    const playlistInput = document.getElementById('playlist').value.trim();
    const formatSelectInput = document.getElementById('formatSelect');

    if (!playlistInput) {
        alert('Lütfen playlist URL girin');
        return;
    }

    showProgress('Kanal aranıyor...', 0);
    document.getElementById('searchChannelBtn').disabled = true;

    try {

        currentSearchId = 'search_' + Date.now();
        // SignalR grubuna katıl
        await searchConnection.invoke("JoinPlaylistSearchGroup", currentSearchId);

        const response = await fetch('/Channel/StartSearchPlaylist', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/x-www-form-urlencoded',
            },
            body: `playListUrl=${encodeURIComponent(playlistInput)}&searchId=` + currentSearchId + '&type=' + formatSelectInput.value
        });

        const data = await response.json();

    } catch (error) {
        alert('Arama sırasında hata oluştu: ' + error.message);
    } finally {
        document.getElementById('searchChannelBtn').disabled = false;
        hideProgress();
    }
}

function displayChannelInfo(channel) {
    document.getElementById('channelInfo').style.display = 'block';
    document.getElementById('channelTitle').textContent = channel.title || channel.handle || 'Kanal';
}

function displayPlaylists(playlists) {
    const container = document.getElementById('playlistsList');
    container.innerHTML = '';
    document.getElementById('playlistsSection').style.display = 'block';

    if (!playlists || playlists.length === 0) {
        container.innerHTML = '<div class="col-12"><p class="text-muted text-center">Oynatma listesi bulunamadı</p></div>';
        return;
    }

    playlists.forEach(playlist => {
        const col = document.createElement('div');
        col.className = 'col-md-4 col-lg-3';

        const card = document.createElement('div');
        card.className = 'card h-100 playlist-card';
        card.style.cursor = 'pointer';
        card.innerHTML = `
            ${playlist.thumbnailUrl ? `<img src="${playlist.thumbnailUrl}" class="card-img-top" alt="${escapeHtml(playlist.title)}" style="height: 150px; object-fit: cover;">` : ''}
            <div class="card-body">
                <h6 class="card-title">${escapeHtml(playlist.title)}</h6>
                <p class="card-text">
                    <small class="text-muted">${playlist.videoCount} video</small>
                </p>
            </div>
        `;

        card.addEventListener('click', () => loadPlaylistVideos(playlist));
        col.appendChild(card);
        container.appendChild(col);
    });
}

async function loadPlaylistVideos(playlist) {
    currentPlaylist = playlist;
    selectedVideoIds = [];
    currentPlaylistVideos = [];

    showProgress('Playlist yükleniyor...', 0);
    document.getElementById('playlistVideosSection').style.display = 'block';
    document.getElementById('playlistVideos').innerHTML = '';

    try {
        const response = await fetch('/Channel/GetPlaylistVideos', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/x-www-form-urlencoded',
            },
            body: `playlistUrl=${encodeURIComponent(playlist.url)}`
        });

        const data = await response.json();

        if (data.success) {
            currentPlaylistVideos = data.videos || [];
            document.getElementById('playlistTitle').textContent = playlist.title;
            document.getElementById('playlistVideoCount').textContent = `${currentPlaylistVideos.length} şarkı`;
            displayPlaylistVideos(data.videos);
        } else {
            alert('Playlist yüklenemedi: ' + data.message);
        }
    } catch (error) {
        alert('Playlist yüklenirken hata oluştu: ' + error.message);
    } finally {
        hideProgress();
    }
}

function createVideoItem(video) {
    const div = document.createElement('div');
    div.className = 'list-group-item';
    div.innerHTML = `
        <div class="d-flex align-items-center">
            <div class="form-check me-3">
                <input class="form-check-input video-checkbox" type="checkbox" 
                       value="${video.id}" data-video-id="${video.id}"
                       onchange="updateDownloadButton()">
            </div>
            <img src="${video.thumbnailUrl}" alt="Thumbnail" 
                 class="img-thumbnail me-3" style="width: 120px; height: 90px; object-fit: cover;">
            <div class="flex-grow-1">
                <h6 class="mb-1">${escapeHtml(video.title)}</h6>
                <small class="text-muted">${escapeHtml(video.author)}</small><br>
                <small class="text-muted">${formatDuration(video.duration)}</small>
            </div>
            <div>
                <button class="btn btn-sm btn-outline-primary preview-btn" 
                        data-video-url="${video.url}">Önizle</button>
            </div>
        </div>
    `;

    div.querySelector('.preview-btn').addEventListener('click', function () {
        window.open(this.dataset.videoUrl, '_blank');
    });

    return div;
}

function selectAllVideos() {
    const checkboxes = document.querySelectorAll('.video-checkbox');
    checkboxes.forEach(cb => {
        cb.checked = true;
        if (!selectedVideoIds.includes(cb.dataset.videoId)) {
            selectedVideoIds.push(cb.dataset.videoId);
        }
    });
    updateDownloadButton();
}

function deselectAllVideos() {
    const checkboxes = document.querySelectorAll('.video-checkbox');
    checkboxes.forEach(cb => {
        cb.checked = false;
    });
    selectedVideoIds = [];
    updateDownloadButton();
}

function updateDownloadButton() {
    const checkboxes = document.querySelectorAll('.video-checkbox:checked');
    const downloadBtn = document.getElementById('downloadPlaylistBtn');

    selectedVideoIds = Array.from(checkboxes).map(cb => cb.dataset.videoId);

    if (selectedVideoIds.length > 0) {
        downloadBtn.style.display = 'inline-block';
        downloadBtn.textContent = `${selectedVideoIds.length} Seçilenleri İndir`;
    } else {
        downloadBtn.style.display = 'none';
    }
}

async function downloadSelectedVideos() {
    if (selectedVideoIds.length === 0) {
        alert('Lütfen en az bir video seçin');
        return;
    }

    const format = 'mp3'; // Varsayılan MP3

    showProgress(`${selectedVideoIds.length} video indirme kuyruğuna ekleniyor...`, 0);

    try {
        const response = await fetch('/Channel/DownloadPlaylistVideos', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
            },
            body: JSON.stringify({
                videoIds: selectedVideoIds,
                format: format,
                channelName: currentChannel?.title || currentChannel?.handle || '',
                playlistName: currentPlaylist?.title || ''
            })
        });

        const data = await response.json();

        if (data.success) {
            showProgress(data.message, 100);
            setTimeout(hideProgress, 3000);
        } else {
            alert('İndirme hatası: ' + data.message);
            hideProgress();
        }
    } catch (error) {
        alert('İndirme sırasında hata oluştu: ' + error.message);
        hideProgress();
    }
}

function showProgress(text, percent) {
    document.getElementById('progressSection').style.display = 'block';
    document.getElementById('progressText').textContent = text;
    document.getElementById('progressBar').style.width = percent + '%';
    document.getElementById('progressBar').textContent = percent + '%';
}

function hideProgress() {
    document.getElementById('progressSection').style.display = 'none';
}

function formatDuration(duration) {
    if (!duration) return 'N/A';
    const parts = duration.split(':');
    if (parts.length === 2) {
        return parts[0] + ':' + parts[1];
    }
    return duration;
}

function escapeHtml(text) {
    const div = document.createElement('div');
    div.textContent = text;
    return div.innerHTML;
}



//---------------------------------------------

function initializeSignalR() {
    if (typeof signalR === 'undefined') {
        console.error('SignalR yüklenemedi');
        return;
    }

    searchConnection = new signalR.HubConnectionBuilder()
        .withUrl("/searchHub")
        .build();

    searchConnection.on("PlaylistSearchStarted", function (playlistDetail) {
        console.log(playlistDetail);
        renderPlaylistInfo(playlistDetail);

        //    public PlaylistId Id { get; }
        //    public string Url => $"https://www.youtube.com/playlist?list={Id}";
        //    public string Title { get; }
        //    public Author ? Author { get; }
        //    public string Description { get; }
        //    public int ? Count { get; }

        //  currentSearchQuery = query;
        //  currentSearchResults = [];
        //  resultCount = 0;
        //  leftColumnCount = 0;
        //  rightColumnCount = 0;
        //  document.getElementById('searchResults').innerHTML = '';
        //  document.getElementById('resultCount').textContent = '0 sonuç bulundu';
        //  pauseBtn.style.display = 'inline-block';
        //  resumeBtn.style.display = 'none';
        //  isSearchPaused = false;
        //  showProgress('Aranıyor...', 0);
    });

    searchConnection.on("VideoDownloaded", function (video) {
        displayPlaylistVideos(video);
    });

    searchConnection.on("SearchCompleted", function (count) {
        hideProgress();
        pauseBtn.style.display = 'none';
        resumeBtn.style.display = 'none';
        document.getElementById('resultCount').textContent = `${count} sonuç tamamlandı`;
        searchBtn.disabled = false;
    });

    //  searchConnection.on("SearchError", function (error) {
    //      alert('Arama hatası: ' + error);
    //      hideProgress();
    //      pauseBtn.style.display = 'none';
    //      resumeBtn.style.display = 'none';
    //      searchBtn.disabled = false;
    //  });

    searchConnection.start().catch(function (err) {
        console.error('SignalR bağlantı hatası:', err);
    });
}




// En büyük alanlı thumbnail'ı bul
function getBestThumbnail(thumbnails) {
    if (!Array.isArray(thumbnails) || thumbnails.length === 0) return null;
    return thumbnails.reduce((best, cur) => {
        const a = (cur?.resolution?.area ?? 0);
        const b = (best?.resolution?.area ?? 0);
        return a > b ? cur : best;
    }, thumbnails[0]);
}

// Basit XSS kaçışı + satır sonlarını koru
function escapeAndFormat(text) {
    if (!text) return "";
    const esc = String(text)
        .replace(/&/g, "&amp;")
        .replace(/</g, "&lt;")
        .replace(/>/g, "&gt;");
    return esc.replace(/\n/g, "<br/>");
}

// Playlist kartını render et
function renderPlaylistInfo(playlistDetail) {
    const card = document.getElementById("playlistInfo");

    if (!card) return;

    // Önce eski içeriği temizle (header dışını sıfırla)
    // card içinde header var, body'yi yeniden oluşturacağız
    [...card.querySelectorAll(".card-body, .list-group, .card-footer")].forEach(n => n.remove());

    const channelTitle =
        playlistDetail?.author?.channelTitle ||
        playlistDetail?.author?.title ||
        "Kanal";

    const channelUrl =
        playlistDetail?.author?.channelUrl || "#";

    const playlistUrl = playlistDetail?.url || "#";
    const playlistTitle = playlistDetail?.title || "Playlist";
    const count = (typeof playlistDetail?.count === "number") ? playlistDetail.count : "-";
    const descHtml = escapeAndFormat(playlistDetail?.description || "");

    const bestThumb = getBestThumbnail(playlistDetail?.thumbnails || []);
    const thumbUrl = (bestThumb?.url || "").replace(/&amp;/g, "&");

    // Body oluştur
    const body = document.createElement("div");
    body.className = "card-body";

    body.innerHTML = `
  <div class="row g-3">
    <div class="col-12 col-md-5">
      <div class="ratio ratio-16x9">
        <img src="${thumbUrl}" />
      </div>
    </div>

    <div class="col-12 col-md-7 d-flex flex-column limitedheight">
      <h5 class="mb-2">${playlistTitle}</h5>

      <div class="mb-2">
        <span class="badge text-bg-secondary">Toplam Video: ${count}</span>
      </div>

      <div class="mb-2 small">
        <a href="${playlistUrl}" target="_blank">
          Playlisti Aç
        </a>
        &nbsp;•&nbsp;
        <a href="${channelUrl}" target="_blank">
          Kanalı aç
        </a>
      </div>

      <div class="mb-2">
        <div id="playlistDesc" class="text-muted">
          ${descHtml}
        </div>
      </div>
    </div>
  </div>
`;

    // Card’a ekleyin
    card.appendChild(body);

    // Kartı görünür yap
    card.style.display = "block";
}






//--------------------------------------


function formatCount(n) {
    if (n === null || n === undefined) return "0";
    const abs = Math.abs(n);
    if (abs >= 1_000_000_000) return (n / 1_000_000_000).toFixed(1).replace(/\.0$/, "") + "B";
    if (abs >= 1_000_000) return (n / 1_000_000).toFixed(1).replace(/\.0$/, "") + "M";
    if (abs >= 1_000) return (n / 1_000).toFixed(1).replace(/\.0$/, "") + "K";
    return String(n);
}

function escapeHtml(str = "") {
    return String(str)
        .replaceAll("&", "&amp;")
        .replaceAll("<", "&lt;")
        .replaceAll(">", "&gt;")
        .replaceAll('"', "&quot;")
        .replaceAll("'", "&#039;");
}

function ensurePlaylistContainer() {
    const host = document.getElementById("downloadedVideos");
    if (!host) return null;

    // İlk kez hazırlama
    if (!host.dataset.prepared) {
        host.innerHTML = `
        <div class="dv-header">
          <span>📼</span>
          <span>Oynatma Listesi</span>
          <span id="dv-count" style="margin-left:auto;color:#6b7280;font-weight:500;font-size:12px">0 video</span>
        </div>
        <div class="card-body p-2">
          <!-- Bootstrap grid satırları buraya eklenecek -->
        </div>
      `;
        host.dataset.prepared = "1";
        host.style.display = ""; // görünür yap
    }
    return host;
}

function displayPlaylistVideos(video) {
    const host = ensurePlaylistContainer();
    if (!host) return;

    const body = host.querySelector(".card-body");
    const countLabel = host.querySelector("#dv-count");

    // Mevcut son .row'u bul
    let lastRow = body.lastElementChild;
    const isRow = lastRow && lastRow.classList.contains("row");
    if (!isRow) lastRow = null;

    // Son satırdaki kolon sayısını kontrol et (col-md-2)
    const colsInLastRow = lastRow ? lastRow.querySelectorAll(".col-md-2").length : 0;

    // 6 ise yeni row aç
    if (!lastRow || colsInLastRow >= 6) {
        lastRow = document.createElement("div");
        lastRow.className = "row g-2"; // g-2: Bootstrap gap
        body.appendChild(lastRow);
    }

    // Kolon (Bootstrap)
    const col = document.createElement("div");
    col.className = "col-6 col-md-2 d-flex"; // mobilde 2'li, md'de 6'lı; d-flex -> kartın eşit yükseklikte olması için

    // Kart
    const {
        id,
        title,
        author,
        thumbnailUrl,
        duration,
        url,
        viewCount,
        likeCount
    } = video;

    const article = document.createElement("article");
    article.className = "dv-card w-100";
    article.dataset.videoId = id ?? "";

    // Thumbnail
    const thumbWrap = document.createElement("div");
    thumbWrap.className = "dv-thumb-wrap";

    const img = document.createElement("img");
    img.className = "dv-thumb";
    img.loading = "lazy";
    img.alt = title ? `Thumbnail: ${title}` : "Video thumbnail";
    img.src = thumbnailUrl || (id ? `https://img.youtube.com/vi/${id}/hqdefault.jpg` : "");

    const dur = document.createElement("span");
    dur.className = "dv-duration";
    dur.textContent = duration || "";

    thumbWrap.appendChild(img);
    thumbWrap.appendChild(dur);

    // Body
    const bodyInner = document.createElement("div");
    bodyInner.className = "dv-body";

    const a = document.createElement("a");
    a.className = "dv-title";
    a.href = url || (id ? `https://www.youtube.com/watch?v=${id}` : "#");
    a.target = "_blank";
    a.rel = "noopener noreferrer";
    a.textContent = title || "Başlıksız Video";

    const authorEl = document.createElement("div");
    authorEl.className = "dv-author";
    authorEl.textContent = author || "";

    const stats = document.createElement("div");
    stats.className = "dv-stats";
    stats.innerHTML = `
    <span class="dv-stat"><i class="ico-eye"></i> ${formatCount(viewCount)}</span>
    <span class="dv-stat"><i class="ico-like"></i> ${formatCount(likeCount)}</span>
    `;

    bodyInner.appendChild(a);
    if (author) bodyInner.appendChild(authorEl);
    bodyInner.appendChild(stats);

    article.appendChild(thumbWrap);
    article.appendChild(bodyInner);

    col.appendChild(article);
    lastRow.appendChild(col);

    // Sayaç güncelle
    const totalCards = body.querySelectorAll(".dv-card").length;
    if (countLabel) countLabel.textContent = `${totalCards} video`;
}

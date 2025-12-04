let currentChannel = null;
let currentPlaylist = null;
let currentPlaylistVideos = [];
let selectedVideoIds = [];

document.addEventListener('DOMContentLoaded', function() {
    const searchChannelBtn = document.getElementById('searchChannelBtn');
    const channelInput = document.getElementById('channelInput');
    const downloadPlaylistBtn = document.getElementById('downloadPlaylistBtn');
    const selectAllBtn = document.getElementById('selectAllBtn');
    const deselectAllBtn = document.getElementById('deselectAllBtn');

    channelInput.addEventListener('keypress', function(e) {
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
    const channelInput = document.getElementById('channelInput').value.trim();
    if (!channelInput) {
        alert('Lütfen kanal adı veya URL girin');
        return;
    }

    showProgress('Kanal aranıyor...', 0);
    document.getElementById('searchChannelBtn').disabled = true;

    try {
        const response = await fetch('/Channel/SearchChannel', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/x-www-form-urlencoded',
            },
            body: `channelInput=${encodeURIComponent(channelInput)}`
        });

        const data = await response.json();

        if (data.success) {
            currentChannel = data.channel;
            displayChannelInfo(data.channel);
            displayPlaylists(data.playlists);
        } else {
            alert('Kanal bulunamadı: ' + data.message);
        }
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

function displayPlaylistVideos(videos) {
    const container = document.getElementById('playlistVideos');
    container.innerHTML = '';

    if (!videos || videos.length === 0) {
        container.innerHTML = '<div class="col-12"><p class="text-muted text-center">Video bulunamadı</p></div>';
        return;
    }

    // 2 sütunlu grid - 1 sağa 1 sola
    let leftColumn = null;
    let rightColumn = null;
    let leftCount = 0;
    let rightCount = 0;

    videos.forEach((video, index) => {
        if (index === 0 || leftCount > rightCount) {
            // Sağ sütuna ekle
            if (!rightColumn) {
                rightColumn = document.createElement('div');
                rightColumn.className = 'col-md-6';
                rightColumn.id = 'rightColumn';
                const rightList = document.createElement('div');
                rightList.className = 'list-group';
                rightList.id = 'rightList';
                rightColumn.appendChild(rightList);
                container.appendChild(rightColumn);
            }
            document.getElementById('rightList').appendChild(createVideoItem(video));
            rightCount++;
        } else {
            // Sol sütuna ekle
            if (!leftColumn) {
                leftColumn = document.createElement('div');
                leftColumn.className = 'col-md-6';
                leftColumn.id = 'leftColumn';
                const leftList = document.createElement('div');
                leftList.className = 'list-group';
                leftList.id = 'leftList';
                leftColumn.appendChild(leftList);
                container.appendChild(leftColumn);
            }
            document.getElementById('leftList').appendChild(createVideoItem(video));
            leftCount++;
        }
    });

    updateDownloadButton();
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

    div.querySelector('.preview-btn').addEventListener('click', function() {
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


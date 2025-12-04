let currentSearchResults = [];
let currentSearchQuery = "";
let currentSearchId = "";
let searchConnection = null;
let isSearchPaused = false;
let resultCount = 0;
let leftColumnCount = 0;
let rightColumnCount = 0;

document.addEventListener('DOMContentLoaded', function() {
    const searchBtn = document.getElementById('searchBtn');
    const searchQuery = document.getElementById('searchQuery');
    const downloadSelectedBtn = document.getElementById('downloadSelectedBtn');
    const pauseBtn = document.getElementById('pauseBtn');
    const resumeBtn = document.getElementById('resumeBtn');

    // SignalR bağlantısı
    initializeSignalR();

    // Enter tuşu ile arama
    searchQuery.addEventListener('keypress', function(e) {
        if (e.key === 'Enter') {
            performSearch();
        }
    });

    searchBtn.addEventListener('click', performSearch);
    downloadSelectedBtn.addEventListener('click', downloadSelectedVideos);
    pauseBtn.addEventListener('click', pauseSearch);
    resumeBtn.addEventListener('click', resumeSearch);

    function initializeSignalR() {
        if (typeof signalR === 'undefined') {
            console.error('SignalR yüklenemedi');
            return;
        }
        
        searchConnection = new signalR.HubConnectionBuilder()
            .withUrl("/searchHub")
            .build();

        searchConnection.on("SearchStarted", function(query) {
            currentSearchQuery = query;
            currentSearchResults = [];
            resultCount = 0;
            leftColumnCount = 0;
            rightColumnCount = 0;
            document.getElementById('searchResults').innerHTML = '';
            document.getElementById('resultCount').textContent = '0 sonuç bulundu';
            pauseBtn.style.display = 'inline-block';
            resumeBtn.style.display = 'none';
            isSearchPaused = false;
            showProgress('Aranıyor...', 0);
        });

        searchConnection.on("VideoFound", function(video) {
            if (!isSearchPaused) {
                addVideoToGrid(video);
                resultCount++;
                document.getElementById('resultCount').textContent = `${resultCount} sonuç bulundu`;
                currentSearchResults.push(video);
            }
        });

        searchConnection.on("SearchCompleted", function(count) {
            hideProgress();
            pauseBtn.style.display = 'none';
            resumeBtn.style.display = 'none';
            document.getElementById('resultCount').textContent = `${count} sonuç tamamlandı`;
            searchBtn.disabled = false;
        });

        searchConnection.on("SearchError", function(error) {
            alert('Arama hatası: ' + error);
            hideProgress();
            pauseBtn.style.display = 'none';
            resumeBtn.style.display = 'none';
            searchBtn.disabled = false;
        });

        searchConnection.start().catch(function(err) {
            console.error('SignalR bağlantı hatası:', err);
        });
    }

    async function performSearch() {
        const query = searchQuery.value.trim();
        if (!query) {
            alert('Lütfen arama terimi girin');
            return;
        }

        // Yeni arama ID'si oluştur
        currentSearchId = 'search_' + Date.now();
        
        // Önceki sonuçları temizle
        document.getElementById('searchResults').innerHTML = '';
        currentSearchResults = [];
        resultCount = 0;
        leftColumnCount = 0;
        rightColumnCount = 0;

        searchBtn.disabled = true;

        try {
            // SignalR grubuna katıl
            await searchConnection.invoke("JoinSearchGroup", currentSearchId);

            // Arama başlat
            const response = await fetch('/Home/StartSearch', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/x-www-form-urlencoded',
                },
                body: `query=${encodeURIComponent(query)}&searchId=${encodeURIComponent(currentSearchId)}`
            });

            const data = await response.json();
            if (!data.success) {
                alert('Arama başlatma hatası: ' + data.message);
                searchBtn.disabled = false;
            }
        } catch (error) {
            alert('Arama sırasında hata oluştu: ' + error.message);
            searchBtn.disabled = false;
            hideProgress();
        }
    }

    async function pauseSearch() {
        if (currentSearchId) {
            await fetch('/Home/PauseSearch', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/x-www-form-urlencoded',
                },
                body: `searchId=${encodeURIComponent(currentSearchId)}`
            });
            isSearchPaused = true;
            pauseBtn.style.display = 'none';
            resumeBtn.style.display = 'inline-block';
        }
    }

    async function resumeSearch() {
        if (currentSearchId) {
            await fetch('/Home/ResumeSearch', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/x-www-form-urlencoded',
                },
                body: `searchId=${encodeURIComponent(currentSearchId)}`
            });
            isSearchPaused = false;
            pauseBtn.style.display = 'inline-block';
            resumeBtn.style.display = 'none';
        }
    }

    function addVideoToGrid(video) {
        const container = document.getElementById('searchResults');
        
        // İlk video için sütunları oluştur
        if (leftColumnCount === 0 && rightColumnCount === 0) {
            const leftColumn = document.createElement('div');
            leftColumn.className = 'col-md-6';
            leftColumn.id = 'leftColumn';
            const leftList = document.createElement('div');
            leftList.className = 'list-group';
            leftList.id = 'leftList';
            leftColumn.appendChild(leftList);
            container.appendChild(leftColumn);

            const rightColumn = document.createElement('div');
            rightColumn.className = 'col-md-6';
            rightColumn.id = 'rightColumn';
            const rightList = document.createElement('div');
            rightList.className = 'list-group';
            rightList.id = 'rightList';
            rightColumn.appendChild(rightList);
            container.appendChild(rightColumn);
        }

        const item = createVideoItem(video, true);
        
        // 1 sağa 1 sola ekleme mantığı
        if (leftColumnCount <= rightColumnCount) {
            document.getElementById('leftList').appendChild(item);
            leftColumnCount++;
        } else {
            document.getElementById('rightList').appendChild(item);
            rightColumnCount++;
        }

        updateDownloadSelectedButton();
    }

    function createVideoItem(video, showCheckbox) {
        const div = document.createElement('div');
        div.className = 'list-group-item';
        div.innerHTML = `
            <div class="d-flex align-items-center">
                <div class="form-check me-3">
                    <input class="form-check-input video-checkbox" type="checkbox" 
                           value="${video.id}" data-video-id="${video.id}"
                           onchange="updateDownloadSelectedButton()">
                </div>
                <img src="${video.thumbnailUrl}" alt="Thumbnail" 
                     class="img-thumbnail me-3" style="width: 120px; height: 90px; object-fit: cover;">
                <div class="flex-grow-1">
                    <h6 class="mb-1">${escapeHtml(video.title)}</h6>
                    <small class="text-muted">${escapeHtml(video.author)}</small><br>
                    <small class="text-muted">${formatDuration(video.duration)}</small>
                    <div class="mt-2">
                        ${video.viewCount ? `<small class="text-muted me-3"><i class="bi bi-eye"></i> ${formatNumber(video.viewCount)} görüntülenme</small>` : ''}
                        ${video.likeCount ? `<small class="text-muted me-3"><i class="bi bi-hand-thumbs-up"></i> ${formatNumber(video.likeCount)} beğeni</small>` : ''}
                        ${video.dislikeCount ? `<small class="text-muted me-3"><i class="bi bi-hand-thumbs-down"></i> ${formatNumber(video.dislikeCount)}</small>` : ''}
                        ${video.averageRating ? `<small class="text-muted"><i class="bi bi-star"></i> ${video.averageRating.toFixed(1)}/5.0</small>` : ''}
                    </div>
                </div>
                <div>
                    <button class="btn btn-sm btn-outline-primary me-2 preview-btn" 
                            data-video-url="${video.url}">Önizle</button>
                    <button class="btn btn-sm btn-success download-btn" 
                            data-video-id="${video.id}">İndir</button>
                </div>
            </div>
        `;

        // Event listeners
        div.querySelector('.preview-btn').addEventListener('click', function() {
            window.open(this.dataset.videoUrl, '_blank');
        });

        div.querySelector('.download-btn').addEventListener('click', function() {
            downloadVideo(this.dataset.videoId);
        });

        return div;
    }

    async function downloadVideo(videoId) {
        const format = document.getElementById('formatSelect').value;
        
        showProgress('İndiriliyor...', 0);

        try {
            const response = await fetch('/Download/DownloadSingle', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/x-www-form-urlencoded',
                },
                body: `videoId=${encodeURIComponent(videoId)}&format=${format}&searchQuery=${encodeURIComponent(currentSearchQuery)}`
            });

            const data = await response.json();
            
            if (data.success) {
                showProgress('İndirme başlatıldı! Dosyalar: ' + await getDownloadPath(), 100);
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

    async function downloadSelectedVideos() {
        const checkboxes = document.querySelectorAll('.video-checkbox:checked');
        const videoIds = Array.from(checkboxes).map(cb => cb.dataset.videoId);
        
        if (videoIds.length === 0) {
            alert('Lütfen en az bir video seçin');
            return;
        }

        const format = document.getElementById('formatSelect').value;
        
        showProgress(`${videoIds.length} video indirme kuyruğuna ekleniyor...`, 0);

        try {
            const response = await fetch('/Download/DownloadMultiple', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                },
                body: JSON.stringify({
                    videoIds: videoIds,
                    format: format,
                    searchQuery: currentSearchQuery
                })
            });

            const data = await response.json();
            
            if (data.success) {
                showProgress(data.message + ' Dosyalar: ' + await getDownloadPath(), 100);
                setTimeout(hideProgress, 5000);
                
                // Checkbox'ları temizle
                checkboxes.forEach(cb => cb.checked = false);
                updateDownloadSelectedButton();
            } else {
                alert('İndirme hatası: ' + data.message);
                hideProgress();
            }
        } catch (error) {
            alert('İndirme sırasında hata oluştu: ' + error.message);
            hideProgress();
        }
    }

    async function getDownloadPath() {
        try {
            const response = await fetch('/Download/GetDownloadPath');
            const data = await response.json();
            return data.path || 'Bilinmiyor';
        } catch {
            return 'Bilinmiyor';
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

    function formatNumber(num) {
        if (!num) return '0';
        if (num >= 1000000) {
            return (num / 1000000).toFixed(1) + 'M';
        } else if (num >= 1000) {
            return (num / 1000).toFixed(1) + 'K';
        }
        return num.toString();
    }
});

function updateDownloadSelectedButton() {
    const checkboxes = document.querySelectorAll('.video-checkbox:checked');
    const downloadSelectedBtn = document.getElementById('downloadSelectedBtn');
    
    if (checkboxes.length > 0) {
        downloadSelectedBtn.style.display = 'block';
        downloadSelectedBtn.textContent = `${checkboxes.length} Seçilenleri İndir`;
    } else {
        downloadSelectedBtn.style.display = 'none';
    }
}


using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using YoutubeSearcher.Web.Models;
using YoutubeSearcher.Web.Services;
using YoutubeSearcher.Web.Hubs;

namespace YoutubeSearcher.Web.Controllers
{
    public class HomeController : Controller
    {
        private readonly YoutubeService _youtubeService;
        private readonly SearchService _searchService;
        private readonly IHubContext<SearchHub> _hubContext;
        private readonly ILogger<HomeController> _logger;
        private static readonly Dictionary<string, bool> _searchCancellations = new();

        public HomeController(YoutubeService youtubeService, SearchService searchService, IHubContext<SearchHub> hubContext, ILogger<HomeController> logger)
        {
            _youtubeService = youtubeService;
            _searchService = searchService;
            _hubContext = hubContext;
            _logger = logger;
        }

        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public IActionResult StartSearch(string query, string searchId)
        {
            if (string.IsNullOrWhiteSpace(query))
            {
                return Json(new { success = false, message = "Arama terimi boş olamaz" });
            }

            // Cancellation flag'ini temizle
            _searchCancellations[searchId] = false;

            // Arka planda streaming başlat
            _ = Task.Run(async () => await StreamSearchResults(query, searchId));

            return Json(new { success = true, message = "Arama başlatıldı" });
        }

        [HttpPost]
        public IActionResult PauseSearch(string searchId)
        {
            if (_searchCancellations.ContainsKey(searchId))
            {
                _searchCancellations[searchId] = true;
            }
            return Json(new { success = true, message = "Arama duraklatıldı" });
        }

        [HttpPost]
        public IActionResult ResumeSearch(string searchId)
        {
            if (_searchCancellations.ContainsKey(searchId))
            {
                _searchCancellations[searchId] = false;
            }
            return Json(new { success = true, message = "Arama devam ediyor" });
        }

        private async Task StreamSearchResults(string query, string searchId)
        {
            try
            {
                var count = 0;
                const int maxResults = 50;

                await _hubContext.Clients.Group(searchId).SendAsync("SearchStarted", query);

                await foreach (var result in _youtubeService.GetSearchStreamAsync(query))
                {
                    // Pause kontrolü
                    while (_searchCancellations.ContainsKey(searchId) && _searchCancellations[searchId])
                    {
                        await Task.Delay(100);
                    }

                    if (count >= maxResults) break;

                    try
                    {
                        var videoInfo = await _youtubeService.GetVideoInfoAsync(result.Id.Value);
                        if (videoInfo == null) continue;

                        await _hubContext.Clients.Group(searchId).SendAsync("VideoFound", videoInfo);
                        count++;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Video bilgisi alınırken hata");
                    }
                }

                await _hubContext.Clients.Group(searchId).SendAsync("SearchCompleted", count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Arama streaming hatası");
                await _hubContext.Clients.Group(searchId).SendAsync("SearchError", ex.Message);
            }
            finally
            {
                // Cleanup
                _searchCancellations.Remove(searchId);
            }
        }



    }
}

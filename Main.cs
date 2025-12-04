using Google.Apis.YouTube.v3;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using YoutubeSearcher.Services;

namespace YoutubeSearcher
{
    public partial class Main : Form
    {

        private readonly YoutubeService _youtubeService;
        private readonly SearchService _searchService;

        public Main()
        {
            _youtubeService = new YoutubeService();
            _searchService = new SearchService(_youtubeService);
            InitializeComponent();
        }

        private void groupBox1_Enter(object sender, EventArgs e)
        {

        }

        private async void btnSearch_Click(object sender, EventArgs e)
        {
            await PerformSearch();
        }

        private async Task PerformSearch()
        {
            var rs = await _searchService.SearchByArtistAsync("Haluk Levent");
            var rs2 = await _searchService.SearchWithRelatedAsync("https://www.youtube.com/watch?v=Ch6xdV_ZjdU");




        }

    }
}

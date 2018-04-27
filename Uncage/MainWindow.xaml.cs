using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.IO;
using Newtonsoft.Json;
using static Uncage.Util;

namespace Uncage
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private Client Client { get; set; }

        private static Settings GetDefaultSettings()
        {
            Settings settings = new Settings()
            {
                UserId = "spotify_username",
                SpotifyClientId = "SPOTIFY_API_ID_HERE",
                SpotifyClientSecret = "SPOTIFY_API_SECRET_HERE",
                YoutubeKey = "YOUTUBE_API_KEY_HERE"
            };

            return settings;
        }

        private static string GetSettingsFile()
        {
            string jsonPath = Path.Combine(Environment.CurrentDirectory, "settings.json");
            if (!File.Exists(jsonPath))
            {
                try
                {
                    string jsonSkeleton = JsonConvert.SerializeObject(GetDefaultSettings(), Formatting.Indented);
                    File.WriteAllText(jsonPath, jsonSkeleton);
                }
                catch (Exception)
                {
                }
            }
            return jsonPath;
        }

        public Settings GetSettings()
        {
            try
            {
                using (StreamReader reader = new StreamReader(GetSettingsFile()))
                {
                    Settings settings = JsonConvert.DeserializeObject<Settings>(reader.ReadToEnd());
                    return settings;
                }
            }
            catch (Exception)
            {
                return null;
            }
        }

        private void FinishDownloading(string titleText = null, string subtitleText = null)
        {
            btnSync.IsEnabled = true;
            btnSync.Content = "Exit";
            btnSync.Click += (o, ev) =>
            {
                Environment.Exit(0);
            };

            imgCoverArt.ImageSource = null;
            progressBar.Value = 0;
            progressBar.Visibility = Visibility.Hidden;
            txtProgress.Visibility = Visibility.Hidden;
            txtTracksDownloaded.Visibility = Visibility.Hidden;
            txtAlbumName.Visibility = Visibility.Hidden;

            if (titleText != null)
                txtTrackName.Text = titleText;
            else
                txtTrackName.Text = "All songs are up-to-date.";

            if (subtitleText != null)
                txtArtists.Text = subtitleText;
            else
                txtArtists.Text = "You may now exit.";
        }

        private async void OnSync(object sender, RoutedEventArgs e)
        {
            btnSync.IsEnabled = false;
            txtProgress.Visibility = Visibility.Visible;
            txtProgress.Text = "Searching songs on YouTube...";
            txtTrackName.Text = "Getting things ready...";
            progressBar.Visibility = Visibility.Visible;
            progressBar.IsIndeterminate = true;

            List<string> artistNames = new List<string>();
            var progress = new Progress<Models.ProgressVideoModel>(p =>
            {
                if (p.TotalTracks != 0)
                {
                    artistNames.Clear();
                    if (txtTracksDownloaded.Visibility == Visibility.Hidden)
                        txtTracksDownloaded.Visibility = Visibility.Visible;
                    if (txtArtists.Visibility == Visibility.Hidden)
                        txtArtists.Visibility = Visibility.Visible;
                    if (txtAlbumName.Visibility == Visibility.Hidden)
                        txtAlbumName.Visibility = Visibility.Visible;

                    if (progressBar.IsIndeterminate)
                        progressBar.IsIndeterminate = false;

                    foreach (var item in p.VideoModel.TrackInfo.Track.Artists)
                        artistNames.Add(item.Name);

                    imgCoverArt.ImageSource = (ImageSource)new ImageSourceConverter().ConvertFromString(p.VideoModel.TrackInfo.Track.Album.Images.First().Url);
                    txtTrackName.Text = p.VideoModel.TrackInfo.Track.Name;
                    txtArtists.Text = string.Join(" & ", artistNames);

                    progressBar.Value = p.CurrentPercentage;
                    txtProgress.Text = progressBar.Value + "%";

                    txtTracksDownloaded.Text = string.Format("{0} of {1} tracks downloaded.", p.TracksDownloaded, p.TotalTracks);
                    txtAlbumName.Text = p.VideoModel.TrackInfo.Track.Album.Name;

                    if (p.CurrentPercentage == 100)
                        FinishDownloading();
                }

                else
                    FinishDownloading();
            });

            await Task.Run(() => Client.DownloadPlaylistAsync(Client.GetUserPlaylists()[1], progress));
        }

        private async void OnWindowLoaded(object sender, RoutedEventArgs e)
        {
            UserSettings = GetSettings();
            txtTracksDownloaded.Visibility = Visibility.Hidden;
            txtProgress.Visibility = Visibility.Hidden;
            progressBar.Visibility = Visibility.Hidden;
            txtArtists.Visibility = Visibility.Hidden;
            txtAlbumName.Visibility = Visibility.Hidden;

            txtTrackName.Text = "Initializing clients...";
            IsEnabled = false;

            await Task.Run(() =>
            {
                Client = new Client(UserSettings.UserId)
                {
                    MusicDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyMusic), "Uncage")
                };

                Client.Initialize();
            });
            
            txtTrackName.Text = "Ready to sync.";
            IsEnabled = true;
        }
    }
}

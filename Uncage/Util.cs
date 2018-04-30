using System;
using System.Windows;
using static Uncage.Enum;

namespace Uncage
{
    public class Util
    {
        public static Settings UserSettings { get; set; }
        public static void ShowAuthError(MainWindow window, AuthError error)
        {
            string errorString = string.Empty;
            switch (error)
            {
                case AuthError.SPOTIFY_ERROR:
                    errorString = "Incorrect Spotify API ID, Secret or Username";
                    break;
                case AuthError.YOUTUBE_ERROR:
                    errorString = "Incorrect YouTube API Key";
                    break;
                case AuthError.UNKNOWN_ERROR:
                    errorString = "Unknown. Try removing settings.json.";
                    break;
                default:
                    errorString = "Unknown. Try removing settings.json.";
                    break;
            }

            window.Dispatcher.Invoke(delegate ()
            {
                window.txtTrackName.Text = "An authentication error has occured.";
                window.txtArtists.Text = "This is due to an incorrect settings.json.";
                window.txtAlbumName.Text = "Probable reason: " + errorString + ".";

                window.btnSync.IsEnabled = true;
                window.btnSync.Content = "Exit";
                window.btnSync.Click += (o, ev) =>
                {
                    Environment.Exit(0);
                };

                window.txtAlbumName.Height = 80;
                window.txtAlbumName.TextWrapping = TextWrapping.Wrap;

                window.txtArtists.Visibility = Visibility.Visible;
                window.txtAlbumName.Visibility = Visibility.Visible;
                window.imgCoverArt.ImageSource = null;
                window.progressBar.Value = 0;
                window.progressBar.Visibility = Visibility.Hidden;
                window.txtProgress.Visibility = Visibility.Hidden;
                window.txtTracksDownloaded.Visibility = Visibility.Hidden;
            });
        }
    }
}

using Google.Apis.Services;
using Google.Apis.YouTube.v3;
using MediaToolkit;
using SpotifyAPI.Web;
using SpotifyAPI.Web.Auth;
using SpotifyAPI.Web.Enums;
using SpotifyAPI.Web.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using YoutubeExplode;
using YoutubeExplode.Models.MediaStreams;
using static Uncage.Models;
using static Uncage.Util;
using static Uncage.Enum;

namespace Uncage
{
    public class UncageClient
    {
        public string UserId { get; set; }
        public MainWindow Window { get; set; }
        private bool ProcessOutput { get; set; } = true;
        public string MusicDirectory  { get; set; } = Environment.GetFolderPath(Environment.SpecialFolder.MyMusic) + @"\LibMusic";

        public UncageClient(string userId, MainWindow window, bool processOutput = true)
        {
            UserId = userId;
            Window = window;
            ProcessOutput = processOutput;
        }

        public string GetMusicDirectory()
        {
            string path = MusicDirectory;
            DirectoryInfo directoryInfo = new DirectoryInfo(path);
            if (directoryInfo.Exists != true)
                directoryInfo.Create();

            return path;
        }

        #region Unavailable iTunes method
        //public void CopyToItunes(bool deletePrevious = false)
        //{
        //    DirectoryInfo directoryInfo = new DirectoryInfo(GetMusicDirectory());
        //    FileInfo[] musicFiles = directoryInfo.GetFiles("*.mp3", SearchOption.TopDirectoryOnly);
        //    iTunesApp itunes = new iTunesApp();

        //    if (deletePrevious)
        //        foreach (IITTrack item in itunes.LibraryPlaylist.Tracks) item.Delete();

        //    foreach (var file in musicFiles)
        //    {
        //        itunes.LibraryPlaylist.AddFile(file.FullName);
        //    }
        //}
        #endregion

        #region Clients & services
        private SpotifyWebAPI spotify;        
        private ClientCredentialsAuth spotifyAuth = new ClientCredentialsAuth()
        {
            ClientId = UserSettings.SpotifyClientId,
            ClientSecret = UserSettings.SpotifyClientSecret,
            Scope = Scope.PlaylistReadPrivate
        };
        private YoutubeClient youtubeClient;
        private YouTubeService youtubeService;
        #endregion

        private void WriteDebug(string message)
        {
            if (ProcessOutput) Debug.WriteLine("[" + DateTime.Now.ToLongTimeString() + "] " + message);
        }

        private string GetTrackFormat(VideoModel video, bool invalidCharsRemoved = false)
        {
            List<string> artistNames = new List<string>();
            video.TrackInfo.Track.Artists.ForEach(artist => { artistNames.Add(artist.Name); });
            string format = $"{string.Join(" & ", artistNames)} – {video.TrackInfo.Track.Name}";

            if (invalidCharsRemoved)
                return Extensions.RemoveInvalidChars(format);
            else
                return format;
        }

        private string GetTrackPath(VideoModel video, bool invalidCharsRemoved = false)
        {
            string trackPath;
            if(invalidCharsRemoved != true)
                trackPath = Path.Combine(GetMusicDirectory(), Path.ChangeExtension(GetTrackFormat(video), "mp3"));
            else
                trackPath = Path.Combine(GetMusicDirectory(), Path.ChangeExtension(GetTrackFormat(video, true), "mp3"));
            return trackPath;
        }

        public void Initialize()
        {
            WriteDebug("Initializing clients...");
            youtubeClient = new YoutubeClient();
            youtubeService = new YouTubeService(new BaseClientService.Initializer()
            {
                ApiKey = UserSettings.YoutubeKey,
                ApplicationName = new UncageClient(null, null).GetType().ToString()
            });
            WriteDebug("Done initialization of YouTube client.");

            Token token = spotifyAuth.DoAuth();
            spotify = new SpotifyWebAPI()
            {
                TokenType = token.TokenType,
                AccessToken = token.AccessToken,
                UseAuth = true
            };
            WriteDebug("Done initialization of Spotify client.");
            WriteDebug("All clients have been authenticated successfully.");
        }

        public List<SimplePlaylist> GetUserPlaylists()
        {
            Paging<SimplePlaylist> playlistsPaging = spotify.GetUserPlaylists(UserId);
            List<SimplePlaylist> playlists = playlistsPaging.Items;
            WriteDebug($"Successfully retrieved user playlists from user '{UserId}'.");
            return playlists;
        }

        public async Task DownloadPlaylistAsync(SimplePlaylist playlist, IProgress<ProgressVideoModel> progress)
        {
            WriteDebug($"Downloading playlist '{playlist.Name}'...");

            List<PlaylistTrack> tracks = new List<PlaylistTrack>();
            for (int i = 0; i < 10; i++)
            {
                try
                {
                    Paging<PlaylistTrack> tracksPaging = spotify.GetPlaylistTracks(UserId, playlist.Id, offset: (i * 100));
                    tracks.AddRange(tracksPaging.Items);
                }
                catch (Exception)
                {
                }
            }

            List<VideoModel> videos = new List<VideoModel>();
            foreach (var track in tracks)
            {
                string trackPath = GetTrackPath(new VideoModel() { TrackInfo = track }, true);
                if (File.Exists(trackPath)) continue;

                var searchRequest = youtubeService.Search.List("snippet");
                searchRequest.Q = string.Format("{0} - {1}", track.Track.Artists[0].Name, track.Track.Name);
                searchRequest.MaxResults = 1;

                WriteDebug($"Searching YouTube for '{track.Track.Name}'...");

                try
                {
                    var searchResponse = searchRequest.Execute();
                    foreach (var result in searchResponse.Items)
                    {
                        if (result.Id.Kind == "youtube#video")
                        {
                            videos.Add(new VideoModel()
                            {
                                VideoUrl = "https://www.youtube.com/watch?v=" + result.Id.VideoId,
                                TrackInfo = track
                            });
                        }
                    }
                }
                catch (Exception)
                {
                    ShowAuthError(Window, AuthError.YOUTUBE_ERROR);
                    return;
                }
            }

            int downloadCount = 0;
            if (videos.Count == 0 && progress != null)
                progress.Report(new ProgressVideoModel() { TotalTracks = 0 });

            foreach (var video in videos)
            {
                if (videos.Count == 0 && progress != null)
                    progress.Report(new ProgressVideoModel() { CurrentPercentage = 100 });
                
                string trackFormat = GetTrackFormat(video);
                string trackPathValid = GetTrackPath(video, true);
                string trackPath = GetTrackPath(video);
                if (File.Exists(trackPathValid)) continue;

                int percentage = (int)Math.Round((double)(100 * downloadCount) / videos.Count);

                ProgressVideoModel progressVideoModel = new ProgressVideoModel()
                {
                    CurrentPercentage = percentage,
                    TotalTracks = videos.Count,
                    TracksDownloaded = downloadCount,
                    VideoModel = video
                };

                if (progress != null)
                    progress.Report(progressVideoModel);

                WriteDebug($"Downloading '{trackFormat}'...");
                try
                {
                    var info = await GetSavePath(video);

                    VideoModel videoModel = DownloadURLAsync(video).Result;
                    string outputName = ConvertToAudio(videoModel.SavePath, Path.ChangeExtension(videoModel.SavePath, null), videoModel);
                    string fileName = outputName + ".mp3";

                    FullTrack track = videoModel.TrackInfo.Track;
                    List<string> artists = new List<string>();
                    foreach (var artist in track.Artists)
                    {
                        artists.Add(artist.Name);
                    }

                    if (videoModel.TrackInfo.Track.Album != null)
                        ApplyID3Tags(fileName, track.Name, artists.ToArray(), track.Album.Name, new Uri(track.Album.Images.First().Url));
                    else
                        ApplyID3Tags(fileName, track.Name, artists.ToArray(), null);

                    downloadCount = downloadCount + 1;
                    WriteDebug($"'{trackFormat}' downloaded successfully, continuing.");

                    int newPercentage = (int)Math.Round((double)(100 * downloadCount) / videos.Count);
                    if (newPercentage == 100 && progress != null)
                    {
                        progress.Report(new ProgressVideoModel()
                        {
                            CurrentPercentage = newPercentage,
                            TotalTracks = videos.Count,
                            TracksDownloaded = downloadCount,
                            VideoModel = video
                        });
                    }
                }
                catch (Exception ex)
                {
                    WriteDebug($"{video.TrackInfo.Track.Name} is errored and cannot be downloaded.");
                    WriteDebug(ex.Message);
                    WriteDebug(ex.StackTrace);
                }
            }

            string[] message = new string[]
            {
                "",
                "========================",
                " DONE DOWNLOADING SONGS ",
                "========================"
            };

            foreach (var item in message)
                WriteDebug(item);
        }

        private async Task<StreamInfo> GetSavePath(VideoModel video)
        {
            string videoId = YoutubeClient.ParseVideoId(video.VideoUrl);
            var streamInfo = await youtubeClient.GetVideoMediaStreamInfosAsync(videoId);
            var extension = streamInfo.Muxed[0].Container.GetFileExtension();
            var thisVideo = await youtubeClient.GetVideoAsync(videoId);

            string newTitle = Extensions.RemoveInvalidChars(GetTrackFormat(video));
            string savePath = Path.Combine(GetMusicDirectory(), $"{newTitle}.{extension}");

            return new StreamInfo() { StreamInfoSet = streamInfo, SavePath = savePath };
        }

        private async Task<VideoModel> DownloadURLAsync(VideoModel video)
        {
            StreamInfo info = await GetSavePath(video);

            WriteDebug("Downloading video URL stream...");
            await youtubeClient.DownloadMediaStreamAsync(info.StreamInfoSet.Muxed[0], info.SavePath);
            WriteDebug("Video URL stream downloaded.");

            VideoModel newModel = video;
            newModel.SavePath = info.SavePath;
            return newModel;
        }

        private string ConvertToAudio(string inputFile, string outputName, VideoModel videoModel)
        {
            Engine engine = new Engine();
            WriteDebug("Converting stream to mp3 format...");
            engine.CustomCommand("-i \"" + inputFile + "\" -vn -ar 44100 -ac 2 -ab 192k -f mp3 \"" + outputName + ".mp3\"");
            File.Delete(inputFile);
            WriteDebug("Stream converted to mp3 format.");

            return outputName;            
        }

        private void ApplyID3Tags(string fileName, string trackTitle, string[] artists, string album, Uri albumArt = null)
        {
            TagLib.File file = TagLib.File.Create(fileName);
            file.Tag.Title = trackTitle;
            file.Tag.Performers = artists;
            file.Tag.Album = album;

            if (albumArt != null)
            {
                string albumArtPath = GetMusicDirectory() + @"\" +
                    Extensions.RemoveInvalidChars(artists.First()) + "_" +
                    Extensions.RemoveInvalidChars(trackTitle) + "@large.jpg";

                WebClient webClient = new WebClient();
                webClient.DownloadFile(albumArt, albumArtPath);

                TagLib.IPicture coverArt = new TagLib.Picture(albumArtPath)
                {
                    Type = TagLib.PictureType.FrontCover,
                    Description = "Cover",
                    MimeType = System.Net.Mime.MediaTypeNames.Image.Jpeg
                };

                file.Tag.Pictures = new TagLib.IPicture[1] { coverArt };
                FileInfo artInfo = new FileInfo(albumArtPath);
                artInfo.Delete();
            }

            file.Save();
            WriteDebug("ID3 tags saved to file.");
        }
    }
}

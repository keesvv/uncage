using System;
using SpotifyAPI.Web.Models;
using YoutubeExplode.Models.MediaStreams;

namespace Uncage
{
    public class Models
    {
        public struct VideoModel
        {
            public string VideoUrl { get; set; }
            public PlaylistTrack TrackInfo { get; set; }
            public string SavePath { get; set; }
        }

        public struct ProgressVideoModel
        {
            public int CurrentPercentage { get; set; }
            public int TracksDownloaded { get; set; }
            public int TotalTracks { get; set; }
            public VideoModel VideoModel { get; set; }
        }

        public struct StreamInfo
        {
            public MediaStreamInfoSet StreamInfoSet { get; set; }
            public string SavePath { get; set; }
        }

        public struct CredentialsHolder
        {
            public string LastPassKey { get; set; }
            public string LastPassSecret { get; set; }
            public string SpotifyClientId { get; set; }
            public string SpotifyClientSecret { get; set; }
            public string YoutubeApiKey { get; set; }
        }
        
        [Serializable]
        public class VideoNotFoundException : Exception
        {
            public VideoNotFoundException() { }
            public VideoNotFoundException(string message) : base(message) { }
            public VideoNotFoundException(string message, Exception inner) : base(message, inner) { }
            protected VideoNotFoundException(
              System.Runtime.Serialization.SerializationInfo info,
              System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
        }
    }
}

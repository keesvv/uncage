using System;
using Newtonsoft.Json;

namespace Uncage
{
    [Serializable()]
    public class Settings
    {
        [JsonProperty("user_id")]
        public string UserId { get; set; }

        [JsonProperty("spotify_api_id")]
        public string SpotifyClientId { get; set; }

        [JsonProperty("spotify_api_secret")]
        public string SpotifyClientSecret { get; set; }

        [JsonProperty("youtube_api_key")]
        public string YoutubeKey { get; set; }
    }
}
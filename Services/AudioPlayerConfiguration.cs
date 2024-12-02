using System.Collections.Generic;
using System.Runtime.InteropServices;
using LibVLCSharp.Shared;

namespace ModernMusicPlayer.Services
{
    internal static class AudioPlayerConfiguration
    {
        private const int BufferSize = 32768; // 32KB buffer

        public static LibVLC CreateLibVLC()
        {
            var options = GetLibVLCOptions();

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                // On Linux, use system-installed VLC
                return new LibVLC(options.ToArray());
            }
            
            // On Windows or other platforms, use default initialization
            Core.Initialize();
            return new LibVLC(options.ToArray());
        }

        private static List<string> GetLibVLCOptions()
        {
            var options = new List<string>
            {
                "--network-caching=3000",         // Increase network cache to 3 seconds
                "--live-caching=1500",            // Live stream caching
                "--sout-mux-caching=1500",        // Muxer caching
                "--file-caching=1500",            // File caching
                "--http-reconnect",               // Enable HTTP reconnection
                "--no-video",                     // Disable video decoding
                $"--file-caching={BufferSize}",   // File caching buffer size
                "--audio-resampler=soxr",         // High quality audio resampler
                "--clock-jitter=0",               // Minimize clock jitter
                "--clock-synchro=0"               // Disable clock synchro for smoother playback
            };

            // Add platform-specific options
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                options.Add("--aout=mmdevice");   // Use modern audio output on Windows only
            }

            return options;
        }

        public static void ConfigureMedia(Media media)
        {
            media.AddOption(":network-caching=3000");
            media.AddOption(":clock-jitter=0");
            media.AddOption(":clock-synchro=0");
        }
    }
}

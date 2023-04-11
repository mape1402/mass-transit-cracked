namespace MassTransit.Metadata
{
    using System;
    using System.Runtime.InteropServices;


    public static class HostMetadataCache
    {
        static bool? _isRunningInContainer;
    #if !NETFRAMEWORK
        static bool? _isNetFramework;
    #endif
        public static HostInfo Host => Cached.HostInfo;
        public static HostInfo Empty => Cached.EmptyHostInfo;

        public static bool IsRunningInContainer =>
            _isRunningInContainer ??= bool.TryParse(Environment.GetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER"), out var inDocker) && inDocker;

    #if NETFRAMEWORK
        public static bool IsNetFramework => true;
    #else
        public static bool IsNetFramework => _isNetFramework ??= RuntimeInformation.FrameworkDescription.StartsWith(".NET Framework");
    #endif


        static class Cached
        {
            internal static readonly HostInfo HostInfo = new BusHostInfo(true);
            internal static readonly HostInfo EmptyHostInfo = new BusHostInfo();
        }
    }
}

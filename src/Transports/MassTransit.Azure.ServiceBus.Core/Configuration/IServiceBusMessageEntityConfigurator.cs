namespace MassTransit
{
    using System;


    public interface IServiceBusMessageEntityConfigurator :
        IServiceBusEntityConfigurator
    {
        /// <summary>
        /// The entity path
        /// </summary>
        string Path { get; }

        /// <summary>
        /// The base path for the message entity
        /// </summary>
        string BasePath { get; set; }

        /// <summary>
        /// The full path of the message entity
        /// </summary>
        string FullPath { get; }

        /// <summary>
        /// How long of a window to use to detect duplicate messages
        /// </summary>
        TimeSpan? DuplicateDetectionHistoryTimeWindow { set; }

        /// <summary>
        /// Sets a value that indicates whether the queue to be partitioned across multiple message brokers is enabled
        /// </summary>
        bool? EnablePartitioning { set; }

        /// <summary>
        /// Sets the maximum size of the queue in megabytes, which is the size of memory allocated for the queue
        /// </summary>
        long? MaxSizeInMegabytes { set; }

        /// <summary>
        /// Set the maximum message size, in kilobytes
        /// </summary>
        long? MaxMessageSizeInKilobytes { set; }

        /// <summary>
        /// Sets the value indicating if this queue requires duplicate detection.
        /// </summary>
        bool? RequiresDuplicateDetection { set; }

        /// <summary>
        /// Enable duplicate detection on the queue, specifying the time window
        /// </summary>
        /// <param name="historyTimeWindow">The time window for duplicate history</param>
        void EnableDuplicateDetection(TimeSpan historyTimeWindow);
    }
}

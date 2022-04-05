using System;
using System.Collections.Generic;
using System.Linq;
using Azure.Storage.Blobs.Specialized;

namespace AzureHelpers
{
    /// <summary>
    /// A helper class to record progress reported by data movement library.
    /// </summary>
    public class ProgressRecorder : Progress<long>
    {
        string fileName;
        long totalBytes;
        long bytesTransferred;

        Queue<long> timeQueue = new Queue<long>(200);
        Queue<long> bytesQueue = new Queue<long>(200);
        DateTime updateTime = System.DateTime.Now;
        
        public Action<ProgressRecorder> updateCallback;
        
        /// <summary>
        /// A very basic constructor
        /// </summary>
        /// <param name="fileId"></param>
        /// <param name="size"></param>
        public ProgressRecorder(string fileId, long size)
        {
            fileName = fileId;
            totalBytes = size;
        }

        /// <summary>
        /// Gets the current download speed of the file
        /// </summary>
        double TransferSpeed => CalculateSpeed(bytesTransferred);

/// <summary>
/// Returns the current percentage of the download between 0-100
/// </summary>
        public int ProgressPercentage => CalculatePercentage();
            
        public TimeSpan TimeRemaining
        {
            get
            {
                TimeSpan time = new TimeSpan(0, 0, (int)((totalBytes - bytesTransferred) / (TransferSpeed == 0 ? 1 : TransferSpeed)));
                return time;
            }
        }

        public void Report(long l)
        {
            OnReport(l);
        }
        
        protected override void OnReport(long l)
        {
            base.OnReport(l);

            bytesTransferred = l;

            updateCallback?.Invoke(this);
        }
            
        public override string ToString()
        {
            return
                $"Transferred bytes: {bytesTransferred};";
        }

        /// <summary>
        /// Function calculating the loading speed.
        /// Keep the last 200 progress change notifications and use them to calculate the average speed over that duration. 
        /// </summary>
        /// <param name="BytesSent"></param>
        /// <returns></returns>
        private double CalculateSpeed(long BytesSent)
        {
            double speed = 0;

            if (timeQueue.Count >= 200)
            {
                timeQueue.Dequeue();
                bytesQueue.Dequeue();
            }

            timeQueue.Enqueue(System.DateTime.Now.Ticks);
            bytesQueue.Enqueue(BytesSent);

            if (timeQueue.Count > 2)
            {
                updateTime = System.DateTime.Now;
                speed = (bytesQueue.Max() - bytesQueue.Min()) / TimeSpan.FromTicks(timeQueue.Max() - timeQueue.Min()).TotalSeconds;
            }

            return speed;
        }

        /// <summary>
        /// Function calculating the loaded amount out of the total to give a percentage
        /// </summary>
        /// <returns></returns>
        int CalculatePercentage()
        {
            if (totalBytes == 0)
            {
                //catch if the file size is too small to caculate
                return 100;
            }
                    
            return (int)((double) bytesTransferred / totalBytes * 100);
        }
    }
}
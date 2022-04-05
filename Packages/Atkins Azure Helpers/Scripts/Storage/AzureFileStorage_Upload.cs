using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Specialized;
using UnityEngine;

namespace AzureHelpers
{
    public partial class AzureFileStorage
    {
        public static async Task<bool> UploadFile(string containerName, string sourceFile, string blobLocation, CancellationToken token,
                                                  BlobType blobType = BlobType.PAGE, Action<ProgressRecorder> processCallback = null, int tries = 0,
                                                  BlobServiceClient client = null)
        {
            BlobContainerClient container = await GetContainerAsync(containerName, client);
            return await UploadFile(container, sourceFile, blobLocation, token, blobType, processCallback, tries);
        }

        /// <summary>
        /// Upload a file to Azure server async
        /// </summary>
        /// <param name="container">The Blob Container</param>
        /// <param name="sourceFile">The path of the file to upload</param>
        /// <param name="blobLocation">The blob name</param>
        /// <param name="token">The cancellation token. Used only in TransferManager.UploadAsync method.</param>
        /// <param name="blobType">The type of the blob. By default: PAGE</param>
        /// <param name="processCallback">The progress callback element. Used for creating the progress recorder</param>
        /// <param name="tries"></param>
        /// <returns></returns>
        public static async Task<bool> UploadFile(BlobContainerClient container, string sourceFile, string blobLocation, CancellationToken token,
                                                  BlobType blobType = BlobType.PAGE, Action<ProgressRecorder> processCallback = null, int tries = 0)
        {
            try
            {
                await new WaitForUpdate();

                BlobBaseClient cloudBlob = blobType switch
                {
                    BlobType.PAGE => container.GetPageBlobClient(blobLocation),
                    BlobType.APPEND => container.GetAppendBlobClient(blobLocation),
                    BlobType.BLOCK => container.GetBlockBlobClient(blobLocation),
                    _ => throw new ArgumentOutOfRangeException(nameof(blobType), blobType, null)
                };

                UnityEngine.Debug.Log("Container :" + cloudBlob);
                UnityEngine.Debug.Log($"Uploading file {sourceFile} to {blobLocation}");

                FileInfo fileInfo = new FileInfo(sourceFile);
                long uploadFileSize = fileInfo.Length;
                ProgressRecorder progressHandler = new ProgressRecorder(Path.GetFileNameWithoutExtension(sourceFile), uploadFileSize)
                {
                    updateCallback = processCallback
                };

                using (MemoryStream ms = new MemoryStream())
                {
                    using (FileStream file = new FileStream(sourceFile, FileMode.Open, FileAccess.Read))
                    {
                        byte[] bytes = new byte[file.Length];
                        await file.ReadAsync(bytes, 0, (int)file.Length, token);
                        ms.Write(bytes, 0, (int)file.Length);
                    }

                    if (cloudBlob is BlockBlobClient b)
                    {
                        await b.UploadAsync(ms, null, null, null, null, progressHandler, token);
                    }
                    else if (cloudBlob is PageBlobClient p)
                    {
                        await p.UploadPagesAsync(ms, 0, null, null, progressHandler, token);
                    }
                    else if (cloudBlob is AppendBlobClient a)
                    {
                        await a.AppendBlockAsync(ms, null, null, progressHandler, token);
                    }
                }

                return true;
            }
            catch (Exception e)
            {
                UnityEngine.Debug.Log(e);

                return false;
            }
        }
    }
}
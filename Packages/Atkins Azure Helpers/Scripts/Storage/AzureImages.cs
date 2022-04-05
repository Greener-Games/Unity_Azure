using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;
using AsyncIO;

namespace AzureHelpers
{
    /// <summary>
    /// Plays the role of an interface between the Azure system and ICON framework. It allows user/coder to simply upload, download and delete images on server
    /// </summary>
    public class AzureImages : MonoBehaviour
    {
        public const string FolderName = "Images";
        public const string FileFormat = "Image_";
        public const string ContainerName = "images";

        /// <summary>
        /// Upload an image to the database and return its new name
        /// </summary>
        /// <param name="imagePath"></param>
        /// <returns></returns>
        public static async Task<string> UploadImage(string imagePath)
        {
            string newFilePath = new FileInfo(imagePath).Directory.FullName +"/" + Guid.NewGuid() + ".png";
            string newFileName = Path.GetFileName(newFilePath);
                
            await AsyncIO.FileSystem.AsyncFile.CopyAsync(imagePath, newFilePath, true);
            
            await AzureFileStorage.UploadFile(ContainerName, newFilePath,$"{Path.GetFileName(newFilePath)}", new CancellationToken(), AzureFileStorage.BlobType.APPEND);
            await AsyncIO.FileSystem.AsyncFile.DeleteAsync(newFilePath);
            return newFileName;
        }
        
        /// <summary>
        /// Load an image from the cache or download from images container
        /// </summary>
        /// <param name="fileName"></param>
        /// <returns></returns>
        public static async Task<Texture2D> GetCachedImage(string fileName, int forceCacheTime = 300)
        {
            if (!fileName.EndsWith(".png"))
            {
                return null;
            }
            
            string folderPath = Application.persistentDataPath + "/Images";
            int hashName = fileName.GetHashCode();
            string filePath = folderPath + "/" + hashName;
            
            bool useCached;
            useCached = File.Exists(filePath);
            if (useCached)
            {
                //check how old
                DateTime written = File.GetLastWriteTimeUtc(filePath);
                DateTime now = DateTime.UtcNow;
                double totalHours = now.Subtract(written).TotalHours;
                if (totalHours > forceCacheTime)
                {
                    useCached = false;
                }
            }

            if (!useCached && Application.internetReachability != NetworkReachability.NotReachable)
            {
                await AzureFileStorage.DownloadFile(ContainerName, fileName, folderPath);
                
                if (File.Exists(filePath))
                {
                    await AsyncIO.FileSystem.AsyncFile.DeleteAsync(filePath);
                }
                
                await AsyncIO.FileSystem.AsyncFile.MoveAsync(folderPath + "/" +fileName, filePath);
                File.SetLastWriteTimeUtc(filePath, DateTime.UtcNow);
            }

            string pathForWww = "file://" + filePath;
            
            UnityWebRequest unityWebRequest = UnityWebRequestTexture.GetTexture(pathForWww);

            await unityWebRequest.SendWebRequest();

            if (unityWebRequest.isNetworkError || unityWebRequest.isHttpError)
            {
                Debug.Log(unityWebRequest.error);
                return null;
            }
            
            return DownloadHandlerTexture.GetContent(unityWebRequest);
        }

        /// <summary>
        /// removes and image from the server
        /// </summary>
        /// <param name="imgName"></param>
        /// <returns></returns>
        public static async Task DeleteImage(string imgName)
        {
            if (string.IsNullOrEmpty(imgName))
            {
                return;
            }
            
            await AzureFileStorage.DeleteFile(ContainerName, imgName);

            string file = Path.Combine(Application.persistentDataPath + "/Images", imgName.GetHashCode().ToString());
            if (File.Exists(file))
            {
                File.Delete(file);
            }
        }
    }
}

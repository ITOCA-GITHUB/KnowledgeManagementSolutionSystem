using Google.Cloud.Storage.V1;

namespace Google_cloud_storage_solution.Services
{
    public interface ICloudStorageService
    {
        Task UploadFileAsync(Stream fileStream, string fileName, string contentType);
        Task<string> GetFileUrlAsync(string fileName);
        Task UpdateFilePermissionsAsync(string objectName, string email);
        Task<List<StorageObject>> ListObjectsAsync(string bucketName, string prefix = "");
    }

}

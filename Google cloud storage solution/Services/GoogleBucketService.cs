using Google.Apis.Auth.OAuth2;
using Google.Apis.Storage.v1.Data;
using Google.Cloud.Storage.V1;

namespace Google_cloud_storage_solution.Services
{
    public class GoogleCloudStorageService : ICloudStorageService
    {
        private readonly StorageClient _storageClient;
        private readonly string _bucketName = "kms_cloud_storage";

        public GoogleCloudStorageService(string bucketName)
        {
            var credential = GoogleCredential.FromFile("GoogleStorageBucket.json");
            _storageClient = StorageClient.Create(credential);
            _bucketName = bucketName;
        }

        public async Task UploadFileAsync(Stream fileStream, string fileName, string contentType)
        {
            await _storageClient.UploadObjectAsync(_bucketName, fileName, contentType, fileStream);
        }

        [Obsolete]
        public async Task<string> GetFileUrlAsync(string fileName)
        {
            var urlSigner = UrlSigner.FromServiceAccountCredential(
            GoogleCredential.FromFile("GoogleStorageBucket.json").UnderlyingCredential as ServiceAccountCredential);
            var signedUrl = await urlSigner.SignAsync(_bucketName, fileName, TimeSpan.FromHours(1), HttpMethod.Get);
            return signedUrl;
        }

        public async Task UpdateFilePermissionsAsync(string objectName, string email)
        {
            var storageObject = await _storageClient.GetObjectAsync(_bucketName, objectName);
            var acl = storageObject.Acl ?? new List<ObjectAccessControl>();
            acl.Add(new ObjectAccessControl
            {
                Entity = $"user-{"admin@itoca.org"}",
                Role = "READER" // Or "OWNER" depending on your needs
            });
            storageObject.Acl = acl;

            await _storageClient.UpdateObjectAsync(storageObject);
        }
    }
}

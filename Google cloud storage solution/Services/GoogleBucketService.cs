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

            // Add or update ACL entry for the specified email
            acl.Add(new ObjectAccessControl
            {
                Entity = $"user-{email}",
                Role = "READER" // Change to "OWNER" if needed
            });

            storageObject.Acl = acl;
            await _storageClient.UpdateObjectAsync(storageObject);
        }

        public async Task<List<StorageObject>> ListObjectsAsync(string bucketName, string prefix = "")
        {
            var objects = new List<StorageObject>();
            var results = _storageClient.ListObjectsAsync(bucketName, prefix);

            await foreach (var obj in results)
            {
                objects.Add(new StorageObject
                {
                    Name = obj.Name,
                    IsFolder = obj.Name.EndsWith("/"),
                    LastModified = obj.Updated,
                    Size = (long?)obj.Size,
                    Owner = obj.Owner?.EntityId
                });
            }

            return objects;
        }
    }
    public class StorageObject
    {
        public string? Name { get; set; }
        public bool IsFolder { get; set; }
        public DateTime? LastModified { get; set; }
        public long? Size { get; set; }
        public string? Owner { get; set; }
    }
}

using Google.Apis.Auth.OAuth2;
using Google.Apis.Drive.v3;
using Google.Apis.Drive.v3.Data;
using Google.Apis.Services;
using Google.Apis.Util.Store;


namespace Google_cloud_storage_solution.Services
{
    public class GoogleDriveService
    {
        private readonly DriveService _driveService;

        public GoogleDriveService()
        {
            var credential = GetCredentialsAsync().Result;

            _driveService = new DriveService(new BaseClientService.Initializer
            {
                HttpClientInitializer = credential,
                ApplicationName = "YourApplicationName"
            });
        }

        private static async Task<UserCredential> GetCredentialsAsync()
        {
            using var stream = new FileStream("GoogleDrive.json", FileMode.Open, FileAccess.Read);
            var clientSecrets = new ClientSecrets
            {
                ClientId = "1041804329552-6hnripffom7spposekoualcca58riq9i.apps.googleusercontent.com",
                ClientSecret = "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"
            };

            return await GoogleWebAuthorizationBroker.AuthorizeAsync(
                clientSecrets,
                new[] { DriveService.Scope.DriveFile },
                "user",
                CancellationToken.None,
                new FileDataStore("Drive.Auth.Store"));
        }

        public async Task<string> UploadFileAsync(Stream fileStream, string fileName, string mimeType)
        {
            var fileMetadata = new Google.Apis.Drive.v3.Data.File
            {
                Name = fileName
            };

            var request = _driveService.Files.Create(fileMetadata, fileStream, mimeType);
            request.Fields = "id";
            var file = await request.UploadAsync();
            if (file.Status != Google.Apis.Upload.UploadStatus.Completed)
            {
                throw new Exception("File upload failed.");
            }

            var uploadedFile = request.ResponseBody;
            return uploadedFile.Id;
        }

        public string GetFileUrl(string fileId)
        {
            return $"https://drive.google.com/uc?id={fileId}";
        }
    }
}

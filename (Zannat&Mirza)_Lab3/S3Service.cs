using Amazon.S3;
using Amazon.S3.Model;
using Azure;

namespace _Zannat_Mirza__Lab3
{
    public class S3Service
    {
        private readonly IAmazonS3 _s3Client;

        public S3Service(Helper helper)
        {
            _s3Client = helper.GetS3Client();
        }
        // Method to list all movie files in the S3 bucket
        public async Task<List<string>> ListMoviesAsync(string bucketName)
        {
            var movieKeys = new List<string>();

            var request = new ListObjectsV2Request
            {
                BucketName = bucketName,
            };

            ListObjectsV2Response response;
            do
            {
                response = await _s3Client.ListObjectsV2Async(request);

                foreach (var s3Object in response.S3Objects)
                {
                    // Add the key of each object i.e movie file to the list wihc ends with .mp4
                    if (s3Object.Key.EndsWith(".mp4")) 
                    {
                        movieKeys.Add(s3Object.Key);
                    }
                }

                request.ContinuationToken = response.NextContinuationToken;

            } while (response.IsTruncated); // Continue until all objects are retrieved

            return movieKeys;
        }
        // PreSigned Url to get access to the s3 bucket and objects stored in it
        public string GeneratePreSignedUrl(string bucketName, string objectKey, int expirationInMinutes = 80)
        {
            var request = new GetPreSignedUrlRequest
            {
                BucketName = bucketName,
                Key = objectKey,
                Expires = DateTime.UtcNow.AddMinutes(expirationInMinutes),
                Protocol = Protocol.HTTPS
            };

            return _s3Client.GetPreSignedURL(request);
        }


        // boilerplate code 
        public async Task UploadMovieAsyncS3(string bucketName, string key, string filePath)
        {
            var putRequest = new PutObjectRequest
            {
                BucketName = bucketName,
                Key = key,
                FilePath = filePath
            };

            await _s3Client.PutObjectAsync(putRequest);
        }

        public async Task DeleteMovieAsyncS3(string bucketName, string key)
        {
            var deleteRequest = new DeleteObjectRequest
            {
                BucketName = bucketName,
                Key = key
            };

            var response = await _s3Client.DeleteObjectAsync(deleteRequest);
            if (response.HttpStatusCode != System.Net.HttpStatusCode.NoContent)
            {
                throw new Exception("Failed to delete the object from S3.");
            }
        }

    }
}

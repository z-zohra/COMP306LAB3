using Amazon.S3;
using Amazon.S3.Model;

namespace _Zannat_Mirza__Lab3
{
    public class S3Service
    {
        private readonly IAmazonS3 _s3Client;

        public S3Service(Helper helper)
        {
            _s3Client = helper.GetS3Client(); 
        }

        // boiler plate code 
        public async Task UploadMovieAsync(string bucketName, string key, string filePath)
        {
            var putRequest = new PutObjectRequest
            {
                BucketName = bucketName,
                Key = key,
                FilePath = filePath
            };

            await _s3Client.PutObjectAsync(putRequest);
        }

        public async Task DeleteMovieAsync(string bucketName, string key)
        {
            var deleteRequest = new DeleteObjectRequest
            {
                BucketName = bucketName,
                Key = key
            };

            await _s3Client.DeleteObjectAsync(deleteRequest);
        }

    }
}

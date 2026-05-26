using Firebase.Storage;

namespace FirebaseWorkout.Service
{
    public static class FirebaseStorageService
    {
        private const string Bucket = "big17datafb.appspot.com";

        public static async Task<string> UploadProfileImageAsync(string userId, Stream imageStream)
        {
            var storage = new FirebaseStorage(Bucket);
            var downloadUrl = await storage
                .Child("profileImages")
                .Child($"{userId}.jpg")
                .PutAsync(imageStream);

            return downloadUrl;
        }
    }
}

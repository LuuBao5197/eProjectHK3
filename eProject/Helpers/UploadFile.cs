namespace MUSICAPI.Helpers
{
    public class UploadFile
    {
        static readonly string baseFolder = "Uploads";
        static readonly string rootUrl = "http://localhost:5190/";
        public static async Task<string> SaveImage(string subFolder, IFormFile formFile)
        {
            string imageName = Guid.NewGuid().ToString() + "_" + formFile.FileName;
            var imagePath = Path.Combine(Directory.GetCurrentDirectory(), baseFolder, subFolder);
            if (!Directory.Exists(imagePath))
            {
                Directory.CreateDirectory(imagePath);
            }
            var exactPath = Path.Combine(imagePath, imageName);
            using (var fileStream = new FileStream(exactPath, FileMode.Create))
            {
                await formFile.CopyToAsync(fileStream);
            }
            return rootUrl + Path.Combine(baseFolder, subFolder, imageName).Replace("\\", "/");
        }
        public static void DeleteImage(string urlImage)
        {

            var exactPath = urlImage.Substring(rootUrl.Length);

            if (File.Exists(exactPath))
            {
                File.Delete(exactPath);
            }

        }
    }
}

using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SecurityApi.Helpers
{
    public static class UploadFiles
    {
        public static string UploadApplicationDocumentFile(IFormFile file)
        {
            
            string applicantPath = GetResourcesFolderPath();
            string fileWithoutSpace = GetFileNameWithoutSpaces(file);
            string fName = String.Format("{0}_{1}", DateTime.Now.ToString("ddMMyyhhmmss"), fileWithoutSpace);
            string filePath = Path.Combine(applicantPath, fName); // Include the folder path when creating the FileStream
            if (!Directory.Exists(applicantPath))
            {
                Directory.CreateDirectory(filePath);
            }

            

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                file.CopyTo(stream);
            }

            return fName;
        }

        public static string GetFileNameWithoutSpaces(IFormFile file)
        {
            string fileWithoutSpace = string.Empty;
            if (file.FileName.Contains(' '))
            {
                fileWithoutSpace = file.FileName.Replace(" ", "_");
            }
            else
            {
                fileWithoutSpace = file.FileName;
            }
            return fileWithoutSpace;
        }
        private static string GetResourcesFolderPath()
        {
            return Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", @"Resources");
        }
    }
}

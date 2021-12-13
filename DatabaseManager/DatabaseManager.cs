using System;
using Microsoft.EntityFrameworkCore;
using Contract;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace DatabaseManager
{
    public class ImageStoreContext : DbContext
    {
        public DbSet<ProcessedImage> Images { get; set; }
        public string DbPath { get; private set; }
        public ImageStoreContext()
        {
            var folder = Environment.SpecialFolder.LocalApplicationData;
            var path = Environment.GetFolderPath(folder);
            DbPath = $"{path}{System.IO.Path.DirectorySeparatorChar}image_store.db";
        }
        protected override void OnConfiguring(DbContextOptionsBuilder o)
            => o.UseLazyLoadingProxies().UseSqlite($"Data Source={DbPath}");
        private int GetHashCode(ProcessedImage img)
        {
            int res = img.ImageContent[0];
            foreach (var b in img.ImageContent)
            {
                res ^= b;
            }
            return res;
        }
        private bool Equal(ProcessedImage img1, ProcessedImage img2)
        {
            if (img1.ImageHashCode != img2.ImageHashCode)
                return false;
            if (img1 == null || img2 == null)
                return false;
            if (img1.ImageContent == null || img2.ImageContent == null)
                return false;
            if (img1.ImageContent.Length != img2.ImageContent.Length)
                return false;
            for (int i = 0; i < img1.ImageContent.Length; ++i)
            {
                if (img1.ImageContent[i] != img2.ImageContent[i])
                    return false;
            }
            return true;
        }
        public void AddImage(ProcessedImage newImage)
        {
            newImage.ImageHashCode = GetHashCode(newImage);
            bool inDB = false;
            foreach (var image in Images)
            {
                if (Equal(newImage, image))
                {
                    inDB = true;
                    break;
                }
            }
            if (!inDB)
            {
                Add(newImage);
                SaveChanges();
            }
        }
    }
}

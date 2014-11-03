using System;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Resources;

namespace Radar.Util
{
    public sealed class Resources
    {
        private static readonly Object instanceLock = new Object();
        private static Resources instance;

        private readonly ResourceManager resourceManager;

        private Resources()
        {
            try
            {
                resourceManager = new ResourceManager("Radar.Properties.Resources", Assembly.GetExecutingAssembly());
            }
            catch (Exception)
            {
                /* TODO: log */
            }
        }

        public static Resources Instance
        {
            get
            {
                lock (instanceLock)
                {
                    if (instance == null)
                    {
                        instance = new Resources();
                    }

                    return instance;
                }
            }
        }

        public Image GetImage(string resourceName)
        {
            object resource = resourceManager.GetObject(resourceName);

            if (resource == null || !(resource is Image))
            {
                return null;
            }

            return (Image)resource;
        }

        public void ExtractResourceToFile(string resourceName, string filePath)
        {
            object resource = resourceManager.GetObject(resourceName);

            if (resource == null)
            {
                throw new FileNotFoundException("Resource " + resourceName + " was not found");
            }

            if (resource is Image)
            {
                ((Image)resource).Save(filePath);
            }
            else
            {
                throw new InvalidDataException("Unknown resource type for " + resourceName);
            }
        }
    }
}
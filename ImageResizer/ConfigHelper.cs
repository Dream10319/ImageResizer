﻿using System.Configuration;

namespace ImageResizer
{
    public class ConfigHelper
    {
        // Method to save source and destination paths
        public static void SaveFolderPaths(string sourcePath, string destinationPath)
        {
            // Open the configuration file
            Configuration config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);

            // Save the source folder path
            if (config.AppSettings.Settings["SourceFolder"] != null)
            {
                config.AppSettings.Settings["SourceFolder"].Value = sourcePath;
            }
            else
            {
                config.AppSettings.Settings.Add("SourceFolder", sourcePath);
            }

            // Save the destination folder path
            if (config.AppSettings.Settings["DestinationFolder"] != null)
            {
                config.AppSettings.Settings["DestinationFolder"].Value = destinationPath;
            }
            else
            {
                config.AppSettings.Settings.Add("DestinationFolder", destinationPath);
            }

            // Save the configuration changes
            config.Save(ConfigurationSaveMode.Modified);
            ConfigurationManager.RefreshSection("appSettings");
        }

        // Method to get the saved source and destination paths
        public static (string sourcePath, string destinationPath) GetSavedFolderPaths()
        {
            // Retrieve source and destination paths from app.config
            string sourcePath = ConfigurationManager.AppSettings["SourceFolder"];
            string destinationPath = ConfigurationManager.AppSettings["DestinationFolder"];

            return (sourcePath, destinationPath);
        }
    }
}
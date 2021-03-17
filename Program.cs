using System;
using System.IO;
using Newtonsoft.Json.Linq;
using System.Linq;
using Newtonsoft.Json;

namespace FileBackup
{
    class Logger
    {
        string logPath { get; }

        public Logger()
        {
            logPath = $"log_{DateTime.Now}".Replace(" ", "_").Replace(".", "-").Replace(":", "-") + ".txt";
            using (StreamWriter log = File.CreateText(logPath))
            {
                log.WriteLine($"[INFO] - {DateTime.Now}: Copying is started.");
            }
        }

        public void logging(string logType, string logText)
        {
            using (StreamWriter log = File.AppendText(logPath))
            {
                log.WriteLine($"[{logType}] - {DateTime.Now}: {logText}");
            }
        }
    }


    class Program
    {
        private static void DirectoryCopy(string sourceDirName, string newDirName, Logger logger)
        {
            DirectoryInfo sourseDir = new DirectoryInfo(sourceDirName);
            if (!Directory.Exists(newDirName)) Directory.CreateDirectory(newDirName);

            FileInfo[] files = sourseDir.GetFiles();
            foreach (FileInfo file in files)
            {
                string tempPath = Path.Combine(newDirName, file.Name);
                file.CopyTo(tempPath, false);
                logger.logging("INFO", $"File {file.Name} are copied into {newDirName}.");
            }

            DirectoryInfo[] dirs = sourseDir.GetDirectories();
            foreach (DirectoryInfo subDir in dirs)
            {
                string tempPath = Path.Combine(newDirName, subDir.Name);
                DirectoryCopy(subDir.FullName, tempPath, logger);
            }           
        }


        static void Main(string[] args)
        {
            Logger logger = new Logger();

            try
            {
                JObject config;
                if (File.Exists("config.json"))
                {
                    config = JObject.Parse(File.ReadAllText("config.json"));
                }
                else
                {
                    logger.logging("CONFIG FILE ERROR", "config.json does not exist.");
                    return;
                }

                if (config.SelectToken("$.Data.TargetPath").Value<string>() == "")
                {
                    logger.logging("CONFIG FILE ERROR", "Specify the path to the target directory in config.json");
                    return;
                }

                string targetPath = config.SelectToken("$.Data.TargetPath").Value<string>();
                if (Directory.Exists(targetPath))
                {
                    foreach (string initialPath in config.SelectTokens("$.Data.InitialPaths").Values<string>().ToList())
                    {
                        if (initialPath == "")
                        {
                            logger.logging("CONFIG FILE ERROR", "Specify the path to the initial directory in config.json");
                            return;
                        }

                        string createdDirPath;
                        if (Directory.Exists(initialPath))
                        {
                            createdDirPath = Path.Combine(targetPath, new DirectoryInfo(initialPath).Name + $"_{DateTime.Now}".Replace(" ", "_").Replace(".", "-").Replace(":", "-"));
                            Directory.CreateDirectory(createdDirPath);
                            logger.logging("INFO", $"Directory {createdDirPath} created.");
                        }
                        else
                        {
                            logger.logging("DIRECTORY ERROR", $"Directory {initialPath} does not exist.");
                            Console.WriteLine($"Directory {initialPath} does not exist. Continue? [y/n]");
                            string ans;
                            ans = Console.ReadLine();
                            while (!((ans == "y") || (ans == "n"))) ans = Console.ReadLine();
                            if (ans == "n") return;
                            else
                            {
                                logger.logging("DEBUG", $"Continue copying without {initialPath}.");
                                continue;
                            }
                        }

                        DirectoryCopy(initialPath, createdDirPath, logger);
                        logger.logging("INFO", $"The directory {initialPath} are copied.");
                    }
                }
                else
                {
                    logger.logging("DIRECTORY ERROR", $"Directory {targetPath} does not exist.");
                    return;
                }

                logger.logging("INFO", $"Copying is complite.");
            }
            catch (IOException e)
            {
                logger.logging("INPUT/OUTPUT ERROR", $"{e}");
                return;
            }
            catch (JsonException e)
            {
                logger.logging("JSON ERROR", $"{e}");
                return;
            }
            catch (UnauthorizedAccessException e)
            {
                logger.logging("ACCESS ERROR", $"{e}");
                return;
            }
            catch (ArgumentNullException)
            {
                logger.logging("CONFIG FILE ERROR", $"Wrong token name in config.json");
                return;
            }
            catch (Exception e)
            {
                logger.logging("UNEXPECTED ERROR", $"{e}");
            }
        }
    }
}

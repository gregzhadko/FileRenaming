using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.IO;
using System.Linq;
using MetadataExtractor;
using Directory = System.IO.Directory;

namespace FileRenaming
{
    class Program
    {
        private static readonly Regex FileRegex = new Regex(@"\d\d\d\d-\d\d-\d\d \d\d-\d\d-\d\d");
        private static readonly Regex DateTakenRegex = new Regex(@"\d\d\d\d-\d\d-\d\d \d\d-\d\d-\d\d");

        static void Main(string[] args)
        {
            while (true)
            {
                Run();
            }
        }

        private static void Run()
        {
            Console.WriteLine("Please Enter the path:");
            var directoryPath = Console.ReadLine();
            if (!Directory.Exists(directoryPath))
            {
                return;
            }

            var paths = Directory.GetFiles(directoryPath).ToList();
            var unchangedFiles = new List<(string fileName, string message)>();
            var count = 0;
            foreach (var path in paths)
            {
                var directories = ImageMetadataReader.ReadMetadata(path);
                var fileNameWithoutExtension = Path.GetFileNameWithoutExtension(path);
                if (FileRegex.IsMatch(fileNameWithoutExtension))
                {
                    //WriteWarning($"{Path.GetFileName(path)}: The file already has a correct name");
                    continue;
                }

                var formattedDate = GetFormattedDate(directories, Path.GetFileName(path));
                if (formattedDate == null)
                {
                    continue;
                }
                
                var destFileName = $"{Path.GetDirectoryName(path)}\\{formattedDate}{Path.GetExtension(path)}";
                try
                {
                    File.Move(Path.GetFullPath(path), destFileName);
                    Console.WriteLine($"{fileNameWithoutExtension}: moved to {destFileName}");
                }
                catch (IOException exception) when (exception.Message == "Cannot create a file when that file already exists.")
                {
                    WriteError($"{fileNameWithoutExtension}: The file with the name {Path.GetFileName(destFileName)} already exists");
                }

                count++;
            }

            Console.WriteLine($"Total number of files in folder: {paths.Count}. Converted: {count}");
            unchangedFiles.ForEach(f => Console.WriteLine($"{f.fileName}: {f.message}"));
        }

        private static string? GetFormattedDate(IReadOnlyList<MetadataExtractor.Directory> directories, string fileName)
        {
            var extension = Path.GetExtension(fileName);
            if (extension == ".mp4")
            {
                WriteWarning($"{fileName}: We do nothing with video for now");
                return null;
            }

            if (extension != ".jpg" && extension != ".jpeg" && extension != ".png")
            {
                WriteWarning($"We doesn't support this extension {fileName}");
                return null;
            }

            var dateTaken = GetDateTaken(directories);
            
            if (string.IsNullOrWhiteSpace(dateTaken))
            {
                WriteError($"{fileName}: The file doesn't contain information about date taken");
                return null;
            }

            var formattedDate = dateTaken.Replace(':', '-');
            
            return formattedDate;
        }

        private static string? GetDateTaken(IReadOnlyList<MetadataExtractor.Directory> directories)
        {
            var directory = directories.FirstOrDefault(d => d.Name == "Exif SubIFD");
            if (directory == null)
            {
                WriteError($"File doesn't contain 'Exif SubIFD' directory");
                return null;
            }

            var dateTaken = directory.Tags.FirstOrDefault(t => t.Name == "Date/Time Original");
            if (dateTaken == null)
            {
                WriteError($"Directory {directory.Name} doesn't contain 'Date/Time Original' tag");
                return null;
            }
            
            return directories[3].Tags[15].Description;
        }

        private static string ShortMonthStingToIntStringRu(string month)
        {
            return month switch
            {
                "янв" => "01",
                "фев" => "02",
                "мар" => "03",
                "апр" => "04",
                "май" => "05",
                "июн" => "06",
                "июл" => "07",
                "авг" => "08",
                "сен" => "09",
                "окт" => "10",
                "ноя" => "11",
                "дек" => "12",
                _ => ""
            };
        }

        private static void WriteError(string error)
        {
            var temp = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.DarkRed;
            Console.WriteLine(error);
            Console.ForegroundColor = temp;
        }

        private static void WriteWarning(string error)
        {
            var temp = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.DarkYellow;
            Console.WriteLine(error);
            Console.ForegroundColor = temp;
        }
    }
}
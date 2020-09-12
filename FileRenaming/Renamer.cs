using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using MetadataExtractor;
using Directory = System.IO.Directory;

namespace FileRenaming
{
    public class Renamer
    {
        private readonly Regex _fileRegex = new Regex(@"\d\d\d\d-\d\d-\d\d \d\d-\d\d-\d\d");
        private readonly Regex _dateTakenRegex = new Regex(@"\d\d\d\d-\d\d-\d\d \d\d-\d\d-\d\d");

        public void Run()
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
                if (IsStringFitsCorrectDateFormat(fileNameWithoutExtension, out _))
                {
                    continue;
                }

                var formattedDate = GetFormattedDate(directories, Path.GetFileName(path));
                if (formattedDate == null)
                {
                    WriteError($"File {fileNameWithoutExtension} doesn't contain correct information about date in its tags");
                    continue;
                }

                if (!IsStringFitsCorrectDateFormat(formattedDate, out var message))
                {
                    WriteError($"File {fileNameWithoutExtension} contains the wrong information in date taken tag");
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

        public static bool IsStringFitsCorrectDateFormat(string fileName, out string message, char separator = '-')
        {
            var dateTime = ToDateTime(fileName, separator);
            if (dateTime == null)
            {
                message = "Can't convert to date time";
                return false;
            }

            if (dateTime > DateTime.Now)
            {
                message = "Date is in the future";
                return false;
            }

            if (dateTime < new DateTime(1990, 1, 1))
            {
                message = "The date is too old.";
                return false;
            }

            message = "";
            return true;
        }

        private static DateTime? ToDateTime(string fileName, char separator)
        {
            var dateAndTime = fileName.Split(' ');
            if (dateAndTime.Length != 2)
            {
                return null;
            }

            var date = dateAndTime[0].Split(separator);
            if (date.Length != 3)
            {
                return null;
            }

            var dateInts = new int[3];
            for (var i = 0; i < date.Length; i++)
            {
                if (!int.TryParse(date[i], out var result))
                {
                    return null;
                }

                dateInts[i] = result;
            }

            var time = dateAndTime[1].Split(separator);
            if (time.Length != 3)
            {
                return null;
            }

            var timeInts = new int[3];
            for (var i = 0; i < time.Length; i++)
            {
                if (!int.TryParse(time[i], out var result))
                {
                    return null;
                }

                timeInts[i] = result;
            }

            try
            {
                var dateTime = new DateTime(dateInts[0], dateInts[1], dateInts[2], timeInts[0], timeInts[1], timeInts[2]);
                return dateTime;
            }
            catch (ArgumentOutOfRangeException)
            {
                return null;
            }
        }

        private string? GetFormattedDate(IReadOnlyList<MetadataExtractor.Directory> directories, string fileName)
        {
            var extension = Path.GetExtension(fileName);
            if (string.Equals(extension, ".mp4", StringComparison.OrdinalIgnoreCase))
            {
                WriteWarning($"{fileName}: We do nothing with video for now");
                return null;
            }

            if (!string.Equals(extension, ".jpg", StringComparison.OrdinalIgnoreCase) 
                && !string.Equals(extension, ".jpeg", StringComparison.OrdinalIgnoreCase) 
                && !string.Equals(extension, ".png", StringComparison.OrdinalIgnoreCase))
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

            return dateTaken.Replace(':', '-');
        }

        private string? GetDateTaken(IReadOnlyList<MetadataExtractor.Directory> directories)
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

            return dateTaken.Description;
        }

        private string ShortMonthStingToIntStringRu(string month)
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

        private static void WriteInColor(string error, ConsoleColor color)
        {
            var temp = Console.ForegroundColor;
            Console.ForegroundColor = color;
            Console.WriteLine(error);
            Console.ForegroundColor = temp;
        }

        private static void WriteError(string error)
        {
            WriteInColor(error, ConsoleColor.DarkRed);
        }

        private static void WriteWarning(string error)
        {
            WriteInColor(error, ConsoleColor.DarkYellow);
        }
    }
}
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using MetadataExtractor;
using Directory = System.IO.Directory;

namespace FileRenaming
{
    public class Renamer
    {
        internal void Run()
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

                var formattedDate = GetFormattedDate(directories, path);
                if (formattedDate == null)
                {
                    WriteError($"File {fileNameWithoutExtension} doesn't contain correct information about date in its tags");
                    continue;
                }

                if (!IsStringFitsCorrectDateFormat(formattedDate, out _))
                {
                    WriteError($"File {fileNameWithoutExtension} contains the wrong information in date taken tag");
                    continue;
                }

                count += Rename(path, formattedDate);
            }

            Console.WriteLine($"Total number of files in folder: {paths.Count}. Converted: {count}");
            unchangedFiles.ForEach(f => Console.WriteLine($"{f.fileName}: {f.message}"));
        }

        private static int Rename(string originalFilePath, string formattedDate)
        {
            var destFileName = $"{Path.GetDirectoryName(originalFilePath)}\\{formattedDate}";

            if (originalFilePath.Contains("_xvid", StringComparison.InvariantCultureIgnoreCase))
            {
                destFileName += "_xvid";
            }

            var areLastSymbolsInt = Int32.TryParse(destFileName.Substring(destFileName.Length - 2), out var lastNumber);
            var destFileNameWithExt = $"{destFileName}{Path.GetExtension(originalFilePath)}";

            if (!File.Exists(destFileNameWithExt))
            {
                MoveFile(originalFilePath, destFileNameWithExt);
                return 1;
            }

            if (!areLastSymbolsInt)
            {
                WriteError($"{Path.GetFileName(originalFilePath)}: The file with the name {Path.GetFileName(destFileNameWithExt)} already exists");
                return 0;
            }

            //Try to rename

            if (TryToRename(destFileName, originalFilePath, (lastNumber - 1)) || TryToRename(destFileName, originalFilePath, (lastNumber + 1)) ||
                TryToRename(destFileName, originalFilePath, (lastNumber - 2)) || TryToRename(destFileName, originalFilePath, (lastNumber + 2)))
            {
                return 1;
            }

            WriteError($"{Path.GetFileName(originalFilePath)}: The file with the name {Path.GetFileName(destFileNameWithExt)} already exists");
            return 0;
        }

        public static bool TryToRename(string destFileName, string originalFilePath, int lastNumber)
        {
            if (lastNumber < 0)
            {
                return false;
            }

            var newLastSymbol = lastNumber.ToString("00");
            var originalName = destFileName;
            destFileName = destFileName.ReplaceAt(destFileName.Length - 2, 2, newLastSymbol);
            var destFileNameWithExt = $"{destFileName}{Path.GetExtension(originalFilePath)}";
            if (File.Exists(destFileNameWithExt))
            {
                return false;
            }

            WriteWarning($"File will be renamed to {destFileName} instead of {originalName}");
            MoveFile(originalFilePath, destFileNameWithExt);
            return true;
        }

        private static void MoveFile(string originalFilePath, string destFileNameWithExt)
        {
            File.Move(Path.GetFullPath(originalFilePath), destFileNameWithExt);
            Console.WriteLine($"{Path.GetFileName(originalFilePath)}: moved to {Path.GetFileName(destFileNameWithExt)}");
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

        private static string? GetFormattedDate(IReadOnlyList<MetadataExtractor.Directory> directories, string pathToFile)
        {
            var fileName = Path.GetFileName(pathToFile);

            var extension = Path.GetExtension(fileName);
            if (string.Equals(extension, ".mp4", StringComparison.OrdinalIgnoreCase))
            {
                WriteWarning($"{fileName}: We do nothing with mp4 for now");
                return null;
            }

            if (extension.InListCaseIgnore(new[]
            {
                ".jpg", ".jpeg", ".png"
            }))

            {
                var dateTaken = GetImageDateTaken(directories);
                if (string.IsNullOrWhiteSpace(dateTaken))
                {
                    WriteError($"{fileName}: The file doesn't contain information about date taken");
                    return null;
                }

                return dateTaken.Replace(':', '-');
            }

            if (extension.InListCaseIgnore(new[]
            {
                ".avi"
            }))

            {
                var date = File.GetCreationTime(pathToFile);
                return $"{date.Year}-{date.Month:00}-{date.Day:00} {date.Hour:00}-{date.Minute:00}-{date.Second:00}";
            }

            WriteWarning($"We doesn't support this extension {fileName}");
            return null;
        }

        private static string? GetImageDateTaken(IEnumerable<MetadataExtractor.Directory> directories)
        {
            var directory = directories.FirstOrDefault(d => d.Name == "Exif SubIFD");
            if (directory == null)
            {
                WriteError("File doesn't contain 'Exif SubIFD' directory");
                return null;
            }

            var date = directory.Tags.FirstOrDefault(t => t.Name == "Date/Time Original");
            if (date != null)
            {
                return date.Description;
            }

            date = directory.Tags.FirstOrDefault(t => t.Name == "Date/Time Original");
            if (date != null)
            {
                return date.Description;
            }

            WriteError($"Directory {directory.Name} doesn't contain 'Date/Time Original' tag");
            return null;
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

        private static void WriteWarning(string warning)
        {
            WriteInColor(warning, ConsoleColor.DarkYellow);
        }
    }
}
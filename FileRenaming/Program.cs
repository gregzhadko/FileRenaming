namespace FileRenaming
{
    internal static class Program
    {
        private static void Main()
        {
            while (true)
            {
                var renamer = new Renamer();
                renamer.Run();
            }
        }
    }
}
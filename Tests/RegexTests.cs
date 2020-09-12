using System.Text.RegularExpressions;
using NUnit.Framework;

namespace Tests
{
    public class RegexTests
    {
        private static readonly Regex FileRegex = new Regex(@"\d\d\d\d-\d\d-\d\d \d\d-\d\d-\d\d");
        private static readonly Regex DateTakenRegex = new Regex(@"\d\d\d\d-\d\d-\d\d \d\d-\d\d-\d\d");

        [SetUp]
        public void Setup()
        {
        }

        [Test]
        [TestCase("2020-08-10 10-12-15")]
        public void FileRegexTests_CorrectDate_Passed(string input)
        {
            
        }
    }
}
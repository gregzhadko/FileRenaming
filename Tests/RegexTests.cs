using System.Text.RegularExpressions;
using FileRenaming;
using NUnit.Framework;

namespace Tests
{
    public class RegexTests
    {
        private static readonly Regex FileRegex = new Regex(@"\d\d\d\d-\d\d-\d\d \d\d-\d\d-\d\d");
        private static readonly Regex DateTakenRegex = new Regex(@"\d\d\d\d-\d\d-\d\d \d\d-\d\d-\d\d");
        private Renamer _renamer = new Renamer();

        [SetUp]
        public void Setup()
        {
        }

        [Test]
        [TestCase("2020-08-10 10-12-15")]
        [TestCase("2019-12-01 23-12-15")]
        public void FileRegexTests_CorrectFileName_Passed(string input)
        {
            Assert.IsTrue(Renamer.IsStringFitsCorrectDateFormat(input, out var message), message);
        }
        
        [Test]
        [TestCase("2020-08-10 10-12-15222")]
        [TestCase("2020-13-10 10-12-15")]
        [TestCase("2020-12-40 10-12-15")]
        [TestCase("1820-12-40 10-12-15")]
        [TestCase("2050-12-40 10-12-15")]
        [TestCase("9999-08-10 10-12-15")]
        [TestCase("2020 08-10 10-12-15")]
        [TestCase("2020:08-10 10-12-15")]
        [TestCase("test:08-10 10-12-15")]
        [TestCase("2020-12-01 24-12-15")]
        public void FileRegexTests_InCorrectFileName_DidntPass(string input)
        {
            Assert.IsFalse(Renamer.IsStringFitsCorrectDateFormat(input, out _));
        }
    }
}
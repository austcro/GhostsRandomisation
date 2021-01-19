using Microsoft.VisualStudio.TestTools.UnitTesting;
using Ghosts.Client.Handlers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Ghosts.Client.TimelineManager;
using Ghosts.Domain;
using Moq;
using Xunit;
using Ghosts.Contracts.Interfaces;
using System.IO.Abstractions.TestingHelpers;

namespace Ghost.Client.Tests
{
    [TestClass()]
    public class WordTests
    {
        private Mock<IWord> wordObject { get; set; }
        private Timeline _timeline;
        [TestInitialize]
        public void Initialise()
        {
            wordObject = new Mock<IWord>();
        }

        [TestMethod]
        public void Word_Check_WordHandler_CallHandlerAction_Returns_True()
        {
            var mock = new Mock<IWord>();
            var calls = 0;
            var test = mock.Setup(foo => foo.CallHandlerAction(null, null))
            .Callback(() => calls++)
            .Returns(true);


        }
        [TestMethod]
        public void POC_Word_Stream_Test()
        {
            var mockFileSystem = new MockFileSystem();
            var mockFileData = new MockFileData("File content");
            mockFileSystem.AddFile("F:\\mockfiletest.txt", mockFileData);
            var fs = mockFileSystem.FileStream;

        }

    }
}
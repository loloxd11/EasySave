using Microsoft.VisualStudio.TestTools.UnitTesting;
using EasySave;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EasySave.Tests
{
    [TestClass()]
    public class ProgramTests
    {
        [TestMethod()]
        public void MainTest_NoArgs_ShouldNotThrowException()
        {
            // Arrange
            string[] args = Array.Empty<string>();

            // Act & Assert
            try
            {
                Program.Main(args);
            }
            catch (Exception ex)
            {
                Assert.Fail($"Main method threw an exception: {ex.Message}");
            }
        }

        [TestMethod()]
        public void ParseArgsTest_SingleJob_ShouldExecuteCorrectly()
        {
            // Arrange
            string[] args = { "1" };

            // Act & Assert
            try
            {
                Program.ParseArgs(args);
            }
            catch (Exception ex)
            {
                Assert.Fail($"ParseArgs method threw an exception: {ex.Message}");
            }
        }

        [TestMethod()]
        public void ParseArgsTest_RangeOfJobs_ShouldExecuteCorrectly()
        {
            // Arrange
            string[] args = { "1-3" };

            // Act & Assert
            try
            {
                Program.ParseArgs(args);
            }
            catch (Exception ex)
            {
                Assert.Fail($"ParseArgs method threw an exception: {ex.Message}");
            }
        }

        [TestMethod()]
        public void ParseArgsTest_InvalidCommand_ShouldHandleGracefully()
        {
            // Arrange
            string[] args = { "invalid" };

            // Act & Assert
            try
            {
                Program.ParseArgs(args);
            }
            catch (Exception ex)
            {
                Assert.Fail($"ParseArgs method threw an exception: {ex.Message}");
            }
        }
    }
}
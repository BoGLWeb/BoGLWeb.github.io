using System.Text.RegularExpressions;
using System.Threading.Tasks;
using BoGLWeb.EditorHelper;
using Microsoft.Playwright.NUnit;
using NUnit.Framework;
/// NuGet\Install-Package Microsoft.Playwright -Version 1.27.1
/// NuGet\Install-Package Microsoft.Playwright.NUnit -Version 1.27.1

namespace PlaywrightTests;

/// <summary>
/// Runs unit tests for all areas of this program.
/// </summary>
public class Tests
{
    [SetUp]
    public void Setup()
    {
    }

    /// <summary>
    /// Tests the EditionList object used as the undo/redo stack.
    /// </summary>
    [Test]
    public void TestEditionHelper() {
        EditionList<int> editionList = new();
        for (int i = 0; i < 10; i++) {
            editionList.Add(i);
        }
        /// editionList is now {0, 1, 2,..., 9} with cursor at '9'
        Assert.AreEqual(10, editionList.Size());
        Assert.AreEqual(9, editionList.Get());
        for (int i = 0; i < 6; i++) {
            editionList.Prev();
        }
        /// editionList is now {0, 1, 2,..., 9} with cursor at '3'
        Assert.AreEqual(3, editionList.Prev());
        for (int i = 0; i < 4; i++) {
            editionList.Next();
        }
        /// editionList is now {0, 1, 2,..., 9} with cursor at '7'
        Assert.AreEqual(7, editionList.Get());
        editionList.Add(127);
        /// editionList is now {0, 1, 2,..., 6, 7, 127} with cursor at '127'
        Assert.AreEqual(127, editionList.Get());
        Assert.AreEqual(9, editionList.Size());
        editionList.Clear();
        Assert.AreEqual(0, editionList.Size());
        Assert.IsNull(editionList.Get());
    }
}
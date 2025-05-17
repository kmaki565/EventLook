using EventLook.Model;
using System.Linq;

namespace Tests;

[TestClass]
public class SplitText_Tests
{
    readonly string input1 = "This is a \"quoted string\" with some   \"quoted  words\" in it.";
    readonly string[] expected1 = ["This", "is", "a", "quoted string", "with", "some", "quoted  words", "in", "it."];

    readonly string input2 = "This is a \"\" with some \"quoted words\" in it.";
    readonly string[] expected2 = ["This", "is", "a", "with", "some", "quoted words", "in", "it."];

    readonly string input3 = " \"Those not quoted yet.";
    readonly string[] expected3 = ["\"Those", "not", "quoted", "yet."];

    readonly string input4 = ".iu/?!\"Those not quoted yet.";
    readonly string[] expected4 = [".iu/?!\"Those", "not", "quoted", "yet."];

    [TestMethod]
    public void Test1()
    {
        CollectionAssert.AreEqual(expected1, TextHelper.SplitQuotedText(input1).ToArray());
    }
    [TestMethod]
    public void Test2()
    {
        CollectionAssert.AreEqual(expected2, TextHelper.SplitQuotedText(input2).ToArray());
    }
    [TestMethod]
    public void Test3()
    {
        CollectionAssert.AreEqual(expected3, TextHelper.SplitQuotedText(input3).ToArray());
    }
    [TestMethod]
    public void Test4()
    {
        CollectionAssert.AreEqual(expected4, TextHelper.SplitQuotedText(input4).ToArray());
    }
}

[TestClass]
public class IsTextMatched_Tests
{
    readonly string[] samplelogs =
    [
        "The bootmgr spent 0 ms waiting for user input.",
        "There are 0x1 boot options on this system.",
        "The boot type was 0x2.",
        "The boot menu policy was 0x0.",
        "The firmware reported boot metrics."
    ];
    [TestMethod]
    public void Test1()
    {
        AndSearchMaterial andSearch = new AndSearchMaterial("0x");
        Assert.IsFalse(TextHelper.IsTextMatched(samplelogs[0], andSearch));
        Assert.IsTrue(TextHelper.IsTextMatched(samplelogs[1], andSearch));
        Assert.IsTrue(TextHelper.IsTextMatched(samplelogs[2], andSearch));
        Assert.IsTrue(TextHelper.IsTextMatched(samplelogs[3], andSearch));
        Assert.IsFalse(TextHelper.IsTextMatched(samplelogs[4], andSearch));
    }
    [TestMethod]
    public void Test2()
    {
        AndSearchMaterial andSearch = new AndSearchMaterial("0 wa ");
        Assert.IsTrue(TextHelper.IsTextMatched(samplelogs[0], andSearch));
        Assert.IsFalse(TextHelper.IsTextMatched(samplelogs[1], andSearch));
        Assert.IsTrue(TextHelper.IsTextMatched(samplelogs[2], andSearch));
        Assert.IsTrue(TextHelper.IsTextMatched(samplelogs[3], andSearch));
        Assert.IsFalse(TextHelper.IsTextMatched(samplelogs[4], andSearch));
    }
    [TestMethod]
    public void Test3()
    {
        AndSearchMaterial andSearch = new AndSearchMaterial("\"are 0x\"");
        Assert.IsFalse(TextHelper.IsTextMatched(samplelogs[0], andSearch));
        Assert.IsTrue(TextHelper.IsTextMatched(samplelogs[1], andSearch));
        Assert.IsFalse(TextHelper.IsTextMatched(samplelogs[2], andSearch));
        Assert.IsFalse(TextHelper.IsTextMatched(samplelogs[3], andSearch));
        Assert.IsFalse(TextHelper.IsTextMatched(samplelogs[4], andSearch));
    }
    [TestMethod]
    public void Test4()
    {
        AndSearchMaterial andSearch = new AndSearchMaterial("-was");
        Assert.IsTrue(TextHelper.IsTextMatched(samplelogs[0], andSearch));
        Assert.IsTrue(TextHelper.IsTextMatched(samplelogs[1], andSearch));
        Assert.IsFalse(TextHelper.IsTextMatched(samplelogs[2], andSearch));
        Assert.IsFalse(TextHelper.IsTextMatched(samplelogs[3], andSearch));
        Assert.IsTrue(TextHelper.IsTextMatched(samplelogs[4], andSearch));
    }
}

[TestClass]
public class IdFilter_Tests
{
    [TestMethod]
    public void Test1()
    {
        IdFilter idFilter = new IdFilter();
        idFilter.FilterText = "1,-0,-3";
        idFilter.AddFilterId(4, isExclude: false);
        Assert.AreEqual("1,-0,-3,4", idFilter.FilterText);
        CollectionAssert.AreEquivalent(new uint[] { 1, 4 }, idFilter.FilterIds.ToArray());
        CollectionAssert.AreEquivalent(new uint[] { 0, 3 }, idFilter.ExcludeIds.ToArray());
    }
    [TestMethod]
    public void Test2()
    {
        IdFilter idFilter = new IdFilter();
        idFilter.FilterText = "5, 0, -2";
        idFilter.AddFilterId(6, isExclude: true);
        Assert.AreEqual("5, 0, -2,-6", idFilter.FilterText);
        CollectionAssert.AreEquivalent(new uint[] { 5, 0 }, idFilter.FilterIds.ToArray());
        CollectionAssert.AreEquivalent(new uint[] { 2, 6 }, idFilter.ExcludeIds.ToArray());
    }
    [TestMethod]
    public void Test3()
    {
        IdFilter idFilter = new IdFilter();
        Assert.ThrowsException<FormatException>(() => idFilter.FilterText = "abc");
    }
}
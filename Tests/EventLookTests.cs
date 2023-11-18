using EventLook.Model;

namespace Tests;

[TestClass]
public class EventLookTests
{
    string input1 = "This is a \"quoted string\" with some   \"quoted  words\" in it.";
    string[] expected1 = { "This", "is", "a", "quoted string", "with", "some", "quoted  words", "in", "it." };

    string input2 = "This is a \"\" with some \"quoted words\" in it.";
    string[] expected2 = { "This", "is", "a", "with", "some", "quoted words", "in", "it." };

    string input3 = " \"Those not quoted yet.";
    string[] expected3 = { "\"Those", "not", "quoted", "yet." };

    string input4 = ".iu/?!\"Those not quoted yet.";
    string[] expected4 = { ".iu/?!\"Those", "not", "quoted", "yet." };

    [TestMethod]
    public void SplitText_Test1()
    {
        CollectionAssert.AreEqual(expected1, TextHelper.SplitQuotedText(input1).ToArray());
    }
    [TestMethod]
    public void SplitText_Test2()
    {
        CollectionAssert.AreEqual(expected2, TextHelper.SplitQuotedText(input2).ToArray());
    }
    [TestMethod]
    public void SplitText_Test3()
    {
        CollectionAssert.AreEqual(expected3, TextHelper.SplitQuotedText(input3).ToArray());
    }
    [TestMethod]
    public void SplitText_Test4()
    {
        CollectionAssert.AreEqual(expected4, TextHelper.SplitQuotedText(input4).ToArray());
    }
}
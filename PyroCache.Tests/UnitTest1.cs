using NUnit.Framework;

namespace PyroCache.Tests;

public class Tests
{
    [SetUp]
    public void Setup()
    {
    }

    [Test]
    public void Test1()
    {
        Assert.That(2, Is.EqualTo(1 + 1));
    }
}
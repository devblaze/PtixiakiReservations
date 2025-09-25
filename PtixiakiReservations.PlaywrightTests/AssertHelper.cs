using NUnit.Framework;

namespace PtixiakiReservations.PlaywrightTests
{
    /// <summary>
    /// Helper class to provide consistent Assert methods for NUnit 4.0+
    /// </summary>
    public static class AssertHelper
    {
        public static void IsNotNull(object actual, string message = "")
        {
            Assert.That(actual, Is.Not.Null, message);
        }

        public static void IsNull(object actual, string message = "")
        {
            Assert.That(actual, Is.Null, message);
        }

        public static void IsTrue(bool condition, string message = "")
        {
            Assert.That(condition, Is.True, message);
        }

        public static void IsFalse(bool condition, string message = "")
        {
            Assert.That(condition, Is.False, message);
        }

        public static void Greater(int actual, int expected, string message = "")
        {
            Assert.That(actual, Is.GreaterThan(expected), message);
        }

        public static void GreaterOrEqual(int actual, int expected, string message = "")
        {
            Assert.That(actual, Is.GreaterThanOrEqualTo(expected), message);
        }

        public static void Less(int actual, int expected, string message = "")
        {
            Assert.That(actual, Is.LessThan(expected), message);
        }

        public static void LessOrEqual(int actual, int expected, string message = "")
        {
            Assert.That(actual, Is.LessThanOrEqualTo(expected), message);
        }
    }
}
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

/// <summary>
/// This class was obtained from Daniel Vaughan (a fellow WPF Discples blog)
/// http://www.codeproject.com/KB/silverlight/Mtvt.aspx
/// </summary>
namespace Cinch
{
    /// <summary>
    /// Bunch of helper methods for validating arguments
    /// </summary>
    public static class ArgumentValidator
    {
        #region Public Methods
        public static double AssertGreaterThan(double value, double mustBeGreaterThan, string parameterName)
        {
            if (value < mustBeGreaterThan)
            {
                throw new ArgumentOutOfRangeException("Parameter should be greater than " + mustBeGreaterThan, parameterName);
            }
            return value;
        }

        public static int AssertGreaterThan(int value, int greaterThan, string parameterName)
        {
            if (value < greaterThan)
            {
                throw new ArgumentOutOfRangeException("Parameter should be greater than " + greaterThan, parameterName);
            }
            return value;
        }

        public static long AssertGreaterThan(long value, long mustBeGreaterThan, string parameterName)
        {
            if (value < mustBeGreaterThan)
            {
                throw new ArgumentOutOfRangeException("Parameter should be greater than " + mustBeGreaterThan, parameterName);
            }
            return value;
        }

        public static double AssertLessThan(double value, double mustBeLessThan, string parameterName)
        {
            if (value > mustBeLessThan)
            {
                throw new ArgumentOutOfRangeException("Parameter should be less than " + mustBeLessThan, parameterName);
            }
            return value;
        }

        public static T AssertNotNull<T>(T value, string parameterName) where T : class
        {
            if (value == null)
            {
                throw new ArgumentNullException(parameterName);
            }
            return value;
        }

        public static T AssertNotNullAndOfType<T>(object value, string parameterName) where T : class
        {
            if (value == null)
            {
                throw new ArgumentNullException(parameterName);
            }
            T result = value as T;
            if (result == null)
            {
                throw new ArgumentException(string.Format(string.Concat(new object[] { "Expected argument of type ", typeof(T), ", but was ", value.GetType() }), typeof(T), value.GetType()), parameterName);
            }
            return result;
        }

        public static string AssertNotNullOrEmpty(string value, string parameterName)
        {
            if (value == null)
            {
                throw new ArgumentNullException(parameterName);
            }
            if (value.Length < 1)
            {
                throw new ArgumentException("Parameter should not be an empty string.", parameterName);
            }
            return value;
        }
        #endregion
    }


}

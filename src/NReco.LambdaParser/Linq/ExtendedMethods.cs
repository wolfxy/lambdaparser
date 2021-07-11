using NReco.Linq;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Text;

namespace NReco.LambdaParser.Linq
{
    class ExtendedMethods 
    {
        public static bool ExactEquals(object obj1, object obj2)
        {
            object left = obj1;
            object right = obj2;
            if (obj1 is LambdaParameterWrapper parameterWrapper)
            {
                left = parameterWrapper.Value;
            }
            if (obj2 is LambdaParameterWrapper parameterWrapper2)
            {
                right = parameterWrapper2.Value;
            }
            if (left == null && right == null)
            {
                return true;
            }
            if (left != null && right != null)
            {
                return left.Equals(right);
            }
            return false;
        } 
        
        public static bool NotExactEquals(object obj1, object obj2)
        {
            return !ExactEquals(obj1, obj2);
        }
    }
}

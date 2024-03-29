/*************************************************************************
 *     This DoubleCollectionConverter file & interface is part of the 
 *     GraphSynth.BaseClasses Project which is the foundation of the 
 *     GraphSynth Application.
 *     GraphSynth.BaseClasses is protected and copyright under the MIT
 *     License.
 *     Copyright (c) 2011 Matthew Ira Campbell, PhD.
 *
 *     Permission is hereby granted, free of charge, to any person obtain-
 *     ing a copy of this software and associated documentation files 
 *     (the "Software"), to deal in the Software without restriction, incl-
 *     uding without limitation the rights to use, copy, modify, merge, 
 *     publish, distribute, sublicense, and/or sell copies of the Software, 
 *     and to permit persons to whom the Software is furnished to do so, 
 *     subject to the following conditions:
 *     
 *     THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, 
 *     EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF 
 *     MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGE-
 *     MENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE 
 *     FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF 
 *     CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION 
 *     WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
 *     
 *     Please find further details and contact information on GraphSynth
 *     at http://www.GraphSynth.com.
 *************************************************************************/
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;

namespace BoGLWeb.Logistics {
    /// <summary>
    ///   A converter class for changing a collection of doubles into a string and vice-versa.
    /// </summary>
    public class DoubleCollectionConverter : TypeConverter {
        /// <summary>
        ///   Returns whether this converter can convert an object of the given type to the type of this converter, using the specified context.
        /// </summary>
        /// <param name = "context">An <see cref = "T:System.ComponentModel.ITypeDescriptorContext" /> that provides a format context.</param>
        /// <param name = "sourceType">A <see cref = "T:System.Type" /> that represents the type you want to convert from.</param>
        /// <returns>
        ///   true if this converter can perform the conversion; otherwise, false.
        /// </returns>
        public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType) {
            return sourceType == typeof(string);
        }

        /// <summary>
        ///   Converts the given object to the type of this converter, using the specified context and culture information.
        /// </summary>
        /// <param name = "context">An <see cref = "T:System.ComponentModel.ITypeDescriptorContext" /> that provides a format context.</param>
        /// <param name = "culture">The <see cref = "T:System.Globalization.CultureInfo" /> to use as the current culture.</param>
        /// <param name = "value">The <see cref = "T:System.Object" /> to convert.</param>
        /// <returns>
        ///   An <see cref = "T:System.Object" /> that represents the converted value.
        /// </returns>
        /// <exception cref = "T:System.NotSupportedException">The conversion cannot be performed. </exception>
        public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value) {
            if (value is string)
                return convert((string) value);
            return null;
        }

        /// <summary>
        ///   Determines whether this instance [can convert to] the specified context.
        /// </summary>
        /// <param name = "context">The context.</param>
        /// <param name = "sourceType">Type of the source.</param>
        /// <returns>
        ///   <c>true</c> if this instance [can convert to] the specified context; otherwise, <c>false</c>.
        /// </returns>
       /* public override bool CanConvertTo(ITypeDescriptorContext context, Type sourceType)
        {
            return (sourceType.FindInterfaces((type, criteria) => true, null)
                .Any(t => t == typeof (IEnumerable<double>)));

        }*/

        /// <summary>
        ///   Converts to.
        /// </summary>
        /// <param name = "context">The context.</param>
        /// <param name = "culture">The culture.</param>
        /// <param name = "value">The value.</param>
        /// <param name = "s">The s.</param>
        /// <returns></returns>
        public override object ConvertTo(ITypeDescriptorContext context,
                                         CultureInfo culture, object value, Type s) {
            if (value is IEnumerable<double>)
                return convert((IEnumerable<double>) value);
            return null;
        }


        /// <summary>
        ///   Converts the for a string of comma-separated-values to a IEnumerable of doubles.
        /// </summary>
        /// <param name = "value">The value.</param>
        /// <returns></returns>
        public static List<double> convert(string value) {
            var items = new List<double>();
            var charSeparators = new[] { ',', '(', ')', ' ', ':', ';', '/', '\\', '\'', '\"' };
            double temp;
            var results = value.Split(charSeparators);

            for (var i = 0; i < results.GetLength(0); i++)
                if (results[i] != "" && double.TryParse(results[i].Trim(), out temp))
                    items.Add(temp);

            return items;
        }

        /// <summary>
        ///   Converts the specified values from a IEnumerable of doubles to a comma-separated string.
        /// </summary>
        /// <param name = "values">The values.</param>
        /// <returns></returns>
        public static string convert(IEnumerable<double> values) {
            var text = "";
            foreach (var value in values) {
                text += ", " + value.ToString(CultureInfo.InvariantCulture);
            }
            return text.Length < 2 ? text : text.Remove(0, 2);
        }
    }
}
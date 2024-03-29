﻿/*************************************************************************
 *     This file includes MatrixMath functions and is part of the 
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
 *     Similar functions have been created in a more involved matrix library
 *     written by the author known as StarMath (http://starmath.codeplex.com/).
 *     Please find further details and contact information on GraphSynth
 *     at http://www.GraphSynth.com.
 *************************************************************************/

using System;

namespace BoGLWeb.Logistics {
    internal static class MatrixMath {
        /// <summary>
        ///   This is used below in the close enough to zero booleans to match points
        ///   (see below: sameCloseZero). In order to avoid strange round-off issues - 
        ///   even with doubles - I have implemented this function when comparing the
        ///   position of points (mostly in checking for a valid transformation (see
        ///   ValidTransformation) and if other nodes comply (see otherNodesComply).
        /// </summary>
        private const double epsilon = 0.000001;

        internal static bool sameCloseZero(double x1) {
            return Math.Abs(x1) < epsilon;
        }

        internal static bool sameCloseZero(double x1, double x2) {
            return sameCloseZero(x1 - x2);
        }

        internal static double[,] Identity(int size) {
            var identity = new double[size, size];
            for (var i = 0; i < size; i++)
                identity[i, i] = 1.0;
            return identity;
        }

        internal static double[] multiply(double[,] A, double[] x, int size) {
            var b = new double[size];

            for (int m = 0; m != size; m++) {
                b[m] = 0.0;
                for (int n = 0; n != size; n++)
                    b[m] += A[m, n] * x[n];
            }
            return b;
        }

        internal static double[,] multiply(double[,] A, double[,] B, int size) {
            var C = new double[size, size];

            for (int m = 0; m != size; m++)
                for (int n = 0; n != size; n++) {
                    C[m, n] = 0.0;
                    for (int p = 0; p != size; p++)
                        C[m, n] += A[m, p] * B[p, n];
                }
            return C;
        }

    }
}

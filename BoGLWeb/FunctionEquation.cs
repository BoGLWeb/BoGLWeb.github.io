namespace BoGLWeb {
    namespace DifferentialEquationHelper {
        public class FunctionEquation {
            /// <summary>
            /// Stores the <c>Expression</c> objects in this equation.
            /// </summary>
            private readonly Expression f1, f2;

            /// <summary>
            /// Stores the static ID tracker and unique ID for this FunctionEquation.
            /// </summary>
            private static int universalID = 0;
            private readonly int ID;

            /// <summary>
            /// Creates a new <c>FunctionEquation</c>.
            /// </summary>
            /// <param name="f1">
            /// The first <c>Expression</c>.
            /// </param>
            /// <param name="f2">
            /// The second <c>Expression</c>.
            /// </param>
            public FunctionEquation(Expression f1, Expression f2) {
                this.f1 = f1;
                this.f2 = f2;
                this.ID = universalID++;
            }

            /// <summary>
            /// Performs operations on the following 
            /// </summary>
            /// <param name="var"></param>
            /// <returns></returns>
            public Expression Isolate(Expression var) {
                int countF1 = this.f1.CountInstances(var, false);
                int countF2 = this.f2.CountInstances(var, false);
                if (countF1 + countF2 != 1) {
                    throw new("Must have exactly one instance of target var.");
                }
                if (countF1 == 1) {
                    return this.f1.Isolate(this.f2, var);
                } else {
                    return this.f2.Isolate(this.f1, var);
                }
            }

            /// <summary>
            /// Gets the hashCode for this <c>FunctionEquation</c>.
            /// </summary>
            /// <returns>
            /// <c>this.ID</c>
            /// </returns>
            public override int GetHashCode() {
                return this.ID;
            }

            /// <summary>
            /// Converts this <c>FunctionEquation</c> to a printable format.
            /// </summary>
            /// <returns>This <c>FunctionEquation</c> as a <c>string</c>.</returns>
            public override string ToString() {
                return f1.ToString() + "=" + f2.ToString();
            }
        }
    }
}

using System.Linq;

namespace BoGLWeb {
    namespace DifferentialEquationHelper {
        public class StateEquationSet {
            // Stores the state equations in <c>String</c> form.
            private readonly string[] equations;

            /// <summary>
            /// Creates a new <c>StateEquationSet</c>.
            /// </summary>
            /// <param name="graph">The target bond graph.</param>
            public StateEquationSet(BondGraph graph) {
                List<FunctionEquation> equations = new();
                for (int i = 0; i < 3; i++) {
                    equations.Add(new(new(), new()));
                }
                this.equations = new string[equations.Count];
                int index = 0;
                foreach (FunctionEquation equation in equations) {
                    this.equations[index++] = equation.ToString();
                }
                LoadDefaults(); // TODO: remove this line after UI testing
            }

            // TODO: remove this method after UI testing is done.
            private void LoadDefaults() {
                for (int i = 0; i < this.equations.Length; i++) {
                    this.equations[i] = "Dummy equation " + i;
                }
            }

            /// <summary>
            /// Gets the set of state equations from this <c>StateEquationSet</c>.
            /// </summary>
            /// <returns></returns>
            public string[] GetStringEquations() {
                return this.equations;
            }
        }
    }
}

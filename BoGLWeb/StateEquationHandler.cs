namespace BoGLWeb {
    namespace DifferentialEquationHelper {
        public class StateEquationHandler {
            private class TraversableGraph {
                private class LabeledBond {
                    private readonly BondGraph.Bond bond;
                    private int ID;
                    private static int universalID = 0;

                    /// <summary>
                    /// Creates a new <c>LabeledBond</c>.
                    /// </summary>
                    /// <param name="bond"></param>
                    public LabeledBond(BondGraph.Bond bond) {
                        this.bond = bond;
                        this.ID = universalID++;
                    }

                    /// <summary>
                    /// Gets the Bond 
                    /// </summary>
                    /// <returns></returns>
                    public BondGraph.Bond GetBond() {
                        return this.bond;
                    }
                }
            }
        }
    }
}

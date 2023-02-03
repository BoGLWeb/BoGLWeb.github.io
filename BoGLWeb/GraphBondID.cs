namespace BoGLWeb {
    public class GraphBondID {
        // Stores a source ID and target ID
        private readonly int source, target;

        /// <summary>
        /// Creates a new <c>GraphBondID</c>
        /// </summary>
        /// <param name="source">The source element ID</param>
        /// <param name="target">The target element ID</param>
        public GraphBondID(int source, int target) {
            this.source = source;
            this.target = target;
        }

        /// <summary>
        /// Gets the source ID
        /// </summary>
        /// <returns><c>this.source</c></returns>
        public int GetSource() { return this.source; }

        /// <summary>
        /// Gets the target ID
        /// </summary>
        /// <returns><c>target.ID</c></returns>
        public int GetTarget() { return this.target; }
    }
}

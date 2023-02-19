namespace BoGLWeb {
    namespace DifferentialEquationHelper {
        public class GraphValidityHandler {
            /// <summary>
            /// Determines whether a system diagram is valid.
            /// </summary>
            /// <param name="diagram">The diagram.</param>
            /// <returns><c>true</c> if the diagram is connected, else <c>false</c>
            /// if it has two or more disconnected subgraphs.</returns>
            public static bool IsValid(SystemDiagram diagram) {
                Dictionary<int, SystemDiagram.Element> elements = new();
                Dictionary<int, List<SystemDiagram.Edge>> edgesBySource = new();
                Dictionary<int, List<SystemDiagram.Edge>> edgesByTarget= new();
                foreach (SystemDiagram.Element element in diagram.getElements()) {
                    int ID = element.GetID();
                    elements.Add(ID, element);
                    edgesBySource.Add(ID, new());
                    edgesByTarget.Add(ID, new());
                }
                if (elements.Count == 0) {
                    return false;
                }
                foreach (SystemDiagram.Edge edge in diagram.getEdges()) {
                    int sourceID = edge.getE1().GetID(), targetID = edge.getE2().GetID();
                    edgesBySource.GetValueOrDefault(sourceID)?.Add(edge);
                    edgesByTarget.GetValueOrDefault(targetID)?.Add(edge);
                }
                Dictionary<int, SystemDiagram.Element> connectedElements = new();
                Stack<SystemDiagram.Element> nextNeighbors = new(new[] { elements[0] });
                while (nextNeighbors.Count > 0) {
                    SystemDiagram.Element next = nextNeighbors.Pop();
                    connectedElements.Add(next.GetID(), next);
                    //foreach (SystemDiagram.Element target in ) {
                    //}
                }
                return connectedElements.Count == elements.Count;
            }
        }
    }
}

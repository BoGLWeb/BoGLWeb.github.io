namespace BoGLWeb {
    namespace DifferentialEquationHelper {
        public class GraphValidityHandler {
            /// <summary>
            /// Determines whether a system diagram is valid.
            /// </summary>
            /// <param name="diagram">The diagram.</param>
            /// <returns><c>true</c> if the diagram is connected, else <c>false</c>
            /// if it has two or more disconnected subgraphs.</returns>
            public static bool IsInvalid(SystemDiagram? diagram) {
                if (diagram == null) {
                    return true;
                }
                Dictionary<int, SystemDiagram.Element> elements = new();
                Dictionary<int, List<SystemDiagram.Edge>> edgesBySource = new();
                Dictionary<int, List<SystemDiagram.Edge>> edgesByTarget= new();
                int usableID = -1, prevNonGrav = usableID, gravObj = 0;
                foreach (SystemDiagram.Element element in diagram.getElements()) {
                    usableID = element.GetID();
                    elements.Add(usableID, element);
                    edgesBySource.Add(usableID, new());
                    edgesByTarget.Add(usableID, new());
                    if (element.getName().EndsWith("Gravity")) {
                        gravObj++;
                    } else {
                        prevNonGrav = usableID;
                    }
                }
                if (elements.Count == 0) {
                    return true;
                }
                foreach (SystemDiagram.Edge edge in diagram.getEdges()) {
                    int sourceID = edge.getE1().GetID(), targetID = edge.getE2().GetID();
                    edgesBySource.GetValueOrDefault(sourceID)?.Add(edge);
                    edgesByTarget.GetValueOrDefault(targetID)?.Add(edge);
                }
                Dictionary<int, SystemDiagram.Element> connectedElements = new();
                Stack<SystemDiagram.Element> nextNeighbors = new(new[] { elements[prevNonGrav] });
                while (nextNeighbors.Count > 0) {
                    SystemDiagram.Element next = nextNeighbors.Pop();
                    if (!connectedElements.ContainsKey(next.GetID())) {
                        connectedElements.Add(next.GetID(), next);
                        foreach (SystemDiagram.Edge edge in edgesBySource.GetValueOrDefault(next.GetID()) ?? new()) {
                            nextNeighbors.Push(edge.getE2());
                        }
                        foreach (SystemDiagram.Edge edge in edgesByTarget.GetValueOrDefault(next.GetID()) ?? new()) {
                            nextNeighbors.Push(edge.getE1());
                        }
                    }
                }
                return connectedElements.Count != elements.Count - gravObj;
            }
        }
    }
}

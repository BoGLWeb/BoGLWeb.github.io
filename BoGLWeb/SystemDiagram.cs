using Microsoft.Playwright;

namespace BoGLWeb {
    public class SystemDiagram {
        public List<Element> elements;
        public List<Edge> edges;

        public SystemDiagram() {
            elements = new List<Element>();
            edges = new List<Edge>();
        }

        public void addElement(string name) {
            elements.Add(new Element(name));
        }

        public void addEdge(Element e1, Element e2) {
            edges.Add(new Edge(e1, e2));
        }


        public class Element {
            private string name;
            private Dictionary<string, bool> modifiers;
            private string velocityDir;

            public Element(string name) {
                this.name = name;
                this.modifiers = generateModifierDictionary();
                velocityDir = "";
            }

            //TODO Error checking
            public void addModifier(string name) {
                modifiers[name] = true;
            }

            //TODO Error checking
            public void setVelocity(string vel) {
                velocityDir = vel;
            }
        }

        public static Dictionary<string, bool> generateModifierDictionary() {
            Dictionary<string, bool> modifierDictionary = new Dictionary<string, bool>();
            modifierDictionary.Add("Mass", false);
            modifierDictionary.Add("Inertia", false);
            modifierDictionary.Add("Stiffness", false);
            modifierDictionary.Add("Friction", false);
            modifierDictionary.Add("Damping", false);
            modifierDictionary.Add("Parallel", false);
            modifierDictionary.Add("Tooth Wear", false);
            return modifierDictionary;
        }

        public class Edge {
            private readonly Element e1;
            private readonly Element e2;

            public Edge(Element e1, Element e2) {
                this.e1 = e1;
                this.e2 = e2;
            }
        }
    }
}

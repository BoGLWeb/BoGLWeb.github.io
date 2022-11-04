using Microsoft.Playwright;
using Microsoft.VisualStudio.TestPlatform.CoreUtilities.Extensions;
using Newtonsoft.Json;
using System.Linq.Expressions;

namespace BoGLWeb {
    public class SystemDiagram {
        private List<Element> elements;
        private List<Edge> edges;
        private Dictionary<string, double> header;

        public SystemDiagram(Dictionary<string, double> header) {
            elements = new List<Element>();
            edges = new List<Edge>();
            this.header = header;
        }

        private SystemDiagram(Dictionary<string, double> header, List<Element> elements, List<Edge> edges) {
            this.header = header;
            this.elements = elements;
            this.edges = edges;
        }

        public void addElement(string name) {
            elements.Add(new Element(name));
        }

        public void addEdge(Element e1, Element e2) {
            edges.Add(new Edge(e1, e2));
        }

        public Element getElement(int pos) {
            return elements[pos];
        }

        public Edge getEdge(int pos) {
            return edges[pos];
        }

        public List<Element> getElements() {
            return elements;
        }

        public List<Edge> getEdges() {
            return edges;
        }

        //Parsing
        //From XML
        //TODO Figure out if this should be a string
        //TODO Think about refactoring to use only one queue
        public static SystemDiagram generateSystemDiagramFromXML(string xml) {
            List<string> tokens = tokenize(xml);

            //TODO Check if any of these are -1 becuase then we have an error
            int headerPos = findTokenLocation(tokens, "[Header]");
            int elementsPos = findTokenLocation(tokens, "[Elements]");
            int arcsPos = findTokenLocation(tokens, "[Arcs]");

            //Parse Header
            Dictionary<string, double> header = new Dictionary<string, double>();
            for (int i = headerPos + 1; i < elementsPos; i++) {
                //Add the header label and the value to the dictionary
                header.Add(tokens[i], Convert.ToDouble(tokens[++i]));
            }

            //Parse Elements
            List<Element> elements = new List<Element>();
            //TODO Refactor name to elementTokenQueue
            Queue<string> tokenQueue = new Queue<string>();
            for (int i = elementsPos + 1; i < arcsPos; i++) {
                tokenQueue.Enqueue(tokens[i]);
            }

            //Create elements while we have symbols left
            while (tokenQueue.Count > 0) {
                //Find an element by looking for matching braces
                Stack<string> braceStack = new Stack<string>();

                //The head of the queue should always be a brace here so we can pop it and add it to the stack
                string stackTok = tokenQueue.Dequeue();
                //Console.WriteLine(stackTok);
                braceStack.Push(stackTok);

                //We expect the next string in the queue to be "name"
                string name = "";
                double x = 0.0;
                double y = 0.0;
                List<string> modifiers = new List<string>();

                string tok = tokenQueue.Dequeue();
                if (tok.Equals("name")) {
                    name = tokenQueue.Dequeue();
                } else {
                    // The grammar is not being followed for the .bogl file
                    //TODO Figure out how we should handle this error
                    Console.WriteLine("Name was missing. Got: <" + tok + "> instead");
                    return null;
                }

                if (tokenQueue.Dequeue().Equals("x")) {
                    x = Convert.ToDouble(tokenQueue.Dequeue());
                } else {
                    // The grammar is not being followed for the .bogl file
                    //TODO Figure out how we should handle this error
                    Console.WriteLine("X was missing. Got: <" + tok + "> instead");
                    return null;
                }

                if (tokenQueue.Dequeue().Equals("y")) {
                    y = Convert.ToDouble(tokenQueue.Dequeue());
                } else {
                    // The grammar is not being followed for the .bogl file
                    //TODO Figure out how we should handle this error
                    Console.WriteLine("Y was missing. Got: <" + tok + "> instead");
                    return null;
                }

                if (tokenQueue.Dequeue().Equals("modifiers")) {
                    //Add the brace to the stack
                    braceStack.Push(tokenQueue.Dequeue());
                    while (braceStack.Count != 1) {
                        string velTok = tokenQueue.Dequeue();

                        if (velTok.Equals("VELOCITY")) {
                            modifiers.Add(velTok + " " + tokenQueue.Dequeue());
                        } else if (!velTok.Equals("}")) {
                            modifiers.Add(velTok);
                        }

                        //Pop modifier brace off the stack is we find a closing brace
                        if (velTok.Equals("}")) {
                            braceStack.Pop();
                        }
                    }
                } else {
                    // The grammar is not being followed
                    //TODO Figure out how we should handle this error
                    Console.WriteLine("Modifier was missing. Got: <" + tok + "> instead");
                    return null;
                }

                //Add element to element list
                Element e = new Element(name, x, y);
                foreach (string str in modifiers){
                    if (str.Contains("VELOCITY")) {
                        e.setVelocity(str);
                    } else {
                        e.addModifier(str);
                    }
                }

                elements.Add(e);

                if (tokenQueue.Dequeue().Equals("}")) {
                    braceStack.Pop();
                }
            }

            //Parse Arcs
            List<Edge> arcs = new List<Edge>();
            Queue<string> arcsTokenQueue = new Queue<string>();
            for (int i = arcsPos + 1; i < tokens.Count; i++) {
                arcsTokenQueue.Enqueue(tokens[i]);
            }

            while (arcsTokenQueue.Count > 0) {
                bool foundCloseBrace = false;
                if (arcsTokenQueue.Dequeue().Equals("{")) {
                    //Parse
                    //TODO Need to add something that checks if this loop runs more than once because that should be an error
                    while (!foundCloseBrace) {
                        int e1 = 0;
                        int e2 = 0;
                        int velocity = -1;


                        //Check element1
                        string tok = arcsTokenQueue.Dequeue();
                        if (tok.Equals("element1")) {
                            e1 = Convert.ToInt32(arcsTokenQueue.Dequeue());
                        } else {
                            // The grammar is not being followed
                            //TODO Figure out how we should handle this error
                            Console.WriteLine("Element1 was missing. Got: <" + tok + "> instead");
                            return null;
                        }

                        //Check element2
                        tok = arcsTokenQueue.Dequeue();
                        if (tok.Equals("element2")) {
                            e2 = Convert.ToInt32(arcsTokenQueue.Dequeue());
                        } else {
                            // The grammar is not being followed
                            //TODO Figure out how we should handle this error
                            Console.WriteLine("Element2 was missing. Got: <" + tok + "> instead");
                            return null;
                        }

                        //Modifiers
                        tok = arcsTokenQueue.Dequeue();
                        //TODO Confirm that this is the only modifier
                        if (tok.Equals("velocity")) {
                            velocity = Convert.ToInt32(arcsTokenQueue.Dequeue());
                        } else if (tok.Equals("}")) {
                            foundCloseBrace = true;
                        }

                        if (velocity == -1) {
                            arcs.Add(new Edge(elements[e1], elements[e2]));
                        } else {
                            arcs.Add(new Edge(elements[e1], elements[e2], velocity));
                            foundCloseBrace = arcsTokenQueue.Dequeue().Equals("}");
                        }
                    }
                } else {
                    // The grammar is not being followed
                    //TODO Figure out how we should handle this error
                    Console.WriteLine("Missing open brace");
                    return null;
                }
            }

            //Create system diagram
            Console.WriteLine("HERE");
            return new SystemDiagram(header, elements, arcs);
        }

        //Return the position of @token in @tokens if it exists. -1 otherwise
        private static int findTokenLocation(List<string> tokens, string token) {
            for (int i = 0; i < tokens.Count; i++) {
                if (tokens[i].Equals(token)) {
                    return i;
                }
            }
            return -1;
        }

        private static List<string> tokenize(string xml) {
            List<string> tokens = new List<string>();

            string[] lines = xml.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);

            foreach (string line in lines) {
                string[] lineTokens = line.Trim().Split(" ");
                foreach (string token in lineTokens) {
                    if (token.Length > 0) {
                        tokens.Add(token);
                    }
                }
            }

            return tokens;
        }

        //From JSON

        //Convert to GraphSynth
        //TODO Figure out if this should be a string
        public static SystemDiagram generateSystemDiagramFromJSON(string json) {
            var sysDiagram = JsonConvert.DeserializeObject<SystemDiagram>(json);
            if (sysDiagram is null) {
                return sysDiagram;
            } else {
                //TODO Throw error
                return null;
            }
        }

        public static string convertToJson(SystemDiagram sysDiagram) {
            return JsonConvert.SerializeObject(sysDiagram);
        }

        public class Element {
            private string name;
            private Dictionary<string, bool> modifiers;
            private string velocityDir;

            //For graph visualization
            //TODO Create a way to modify these values
            private double x, y;

            public Element(string name) {
                this.name = name;
                this.modifiers = generateModifierDictionary();
                velocityDir = "";
            }

            public Element(string name, double x, double y) {
                this.name = name;
                this.modifiers = generateModifierDictionary();
                velocityDir = "";
                this.x = x;
                this.y = y;
            }

            //TODO Error checking
            public void addModifier(string name) {
                modifiers[name] = true;
            }

            //TODO Error checking
            public void setVelocity(string vel) {
                velocityDir = vel;
            }

            public string getName() {
                return name;
            }

            public string toString() {
                string output = "Element\r\n ";
                output += name + "\r\n";

                foreach (KeyValuePair<string, bool> modifier in modifiers) {
                    output += modifier.Key + " " + modifier.Value + "\r\n";
                }

                return output;
            }
        }

        public static Dictionary<string, bool> generateModifierDictionary() {
            Dictionary<string, bool> modifierDictionary = new Dictionary<string, bool>();
            modifierDictionary.Add("MASS", false);
            modifierDictionary.Add("INTERTIA", false);
            modifierDictionary.Add("STIFFNESS", false);
            modifierDictionary.Add("FRICTION", false);
            modifierDictionary.Add("DAMPING", false);
            modifierDictionary.Add("PARALLEL", false);
            modifierDictionary.Add("TOOTH WEAR", false);
            return modifierDictionary;
        }

        public class Edge {
            private readonly Element e1;
            private readonly Element e2;
            private readonly int velocity;

            public Edge(Element e1, Element e2) {
                this.e1 = e1;
                this.e2 = e2;
                this.velocity = -1;
            }

            public Edge(Element e1, Element e2, int velocity) {
                this.e1 = e1;
                this.e2 = e2;
                this.velocity = velocity;
            }

            public string toString() {
                return velocity == -1 ? "Arc " + e1.getName() + " to " + e2.getName() + "\r\n" : "Arc " + e1.getName() + " to " + e2.getName() + " has velocity " + velocity + "\r\n";
            }
        }
    }
}

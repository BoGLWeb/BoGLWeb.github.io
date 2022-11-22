using Microsoft.Playwright;
using Microsoft.VisualStudio.TestPlatform.CoreUtilities.Extensions;
using Newtonsoft.Json;
using System.Collections;
using System.Collections.Immutable;
using System.Linq.Expressions;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Serialization;
using GraphSynth.Representation;
using BoGLWeb.BaseClasses;
using static BoGLWeb.Graph;
using System.Text.RegularExpressions;

namespace BoGLWeb {
    public class SystemDiagram {
        public static readonly ImmutableDictionary<string, int> modifierIDDict;
        public static readonly ImmutableDictionary<string, int> typeIDDict;

        //Sets up our modifier dictionary
        static SystemDiagram() {

            var idBuilder = ImmutableDictionary.CreateBuilder<string, int>();
            idBuilder.Add("MASS", 0);
            idBuilder.Add("INERTIA", 1);
            idBuilder.Add("STIFFNESS", 2);
            idBuilder.Add("FRICTION", 3);
            idBuilder.Add("DAMPING", 4);
            idBuilder.Add("PARALLEL", 5);
            idBuilder.Add("TOOTH_WEAR", 6);

            modifierIDDict = idBuilder.ToImmutable();

            var typeBuilder = ImmutableDictionary.CreateBuilder<string, int>();
            typeBuilder.Add("System_MT_Mass", 0);
            typeBuilder.Add("System_MT_Spring", 1);
            typeBuilder.Add("System_MT_Damper", 2);
            typeBuilder.Add("System_MT_Ground", 3);
            typeBuilder.Add("System_MT_Force_Input", 4);
            typeBuilder.Add("System_MT_Gravity", 5);
            typeBuilder.Add("System_MT_Velocity_Input", 6);
            typeBuilder.Add("System_MR_Flywheel", 7);
            typeBuilder.Add("System_MR_Spring", 8);
            typeBuilder.Add("System_MR_Damper", 9);
            typeBuilder.Add("System_MR_Torque_Input", 10);
            typeBuilder.Add("System_MR_Velocity_Input", 11);
            typeBuilder.Add("System_MR_Lever", 12);
            // front-end doesn't seem to make distinction between grounded and non-grounded pulley
            typeBuilder.Add("System_MR_Pulley", 13);
            typeBuilder.Add("System_MR_Pulley_Grounded", 13);
            typeBuilder.Add("System_MR_Belt", 14);
            typeBuilder.Add("System_MR_Shaft", 15);
            typeBuilder.Add("System_MR_Gear", 16);
            typeBuilder.Add("System_MR_Gear_Pair", 17);
            typeBuilder.Add("System_MR_Rack", 18);
            typeBuilder.Add("System_MR_Rack_Pinion", 19);
            typeBuilder.Add("System_E_Inductor", 20);
            typeBuilder.Add("System_E_Capacitor", 21);
            typeBuilder.Add("System_E_Resistor", 22);
            typeBuilder.Add("System_E_Transformer", 23);
            typeBuilder.Add("System_E_Junction", 24);
            typeBuilder.Add("System_E_Ground", 25);
            typeBuilder.Add("System_E_Current_Input", 26);
            typeBuilder.Add("System_E_Voltage_Input", 27);
            typeBuilder.Add("System_O_PM_Motor", 28);
            typeBuilder.Add("System_O_VC_Transducer", 29);

            typeIDDict = typeBuilder.ToImmutable();
        }

        [JsonProperty]
        protected List<Element> elements;
        [JsonProperty]
        protected List<Edge> edges;
        // Leaving this out of JSON for now because we're not expecting to use it currently
        protected Dictionary<string, double> header;

        /// <summary>
        /// Creates a system diagram instance
        /// </summary>
        /// <param name="header">A dictionary of headers for the system diagram</param>
        public SystemDiagram(Dictionary<string, double> header) {
            elements = new List<Element>();
            edges = new List<Edge>();
            this.header = header;
        }

        //Creates a system diagram with a list of elements and edges. This is not exposed to other classes because there should be no way to create system diagrams without xml or json
        private SystemDiagram(Dictionary<string, double> header, List<Element> elements, List<Edge> edges) {
            this.header = header;
            this.elements = elements;
            this.edges = edges;
        }

       public Element getElement(int pos) {
            return elements[pos];
        }

        /// <summary>
        /// Returns the edge located at a given position in the list
        /// </summary>
        /// <param name="pos">The position in the list</param>
        /// <returns>The edge as the given position</returns>
        public Edge getEdge(int pos) {
            return edges[pos];
        }

        /// <summary>
        /// Gets the list of elements in the system diagram
        /// </summary>
        /// <returns>The list of elements</returns>
        public List<Element> getElements() {
            return elements;
        }

        /// <summary>
        /// Gets the elements in the system diagram
        /// </summary>
        /// <returns>The list of elements</returns>
        public List<Edge> getEdges() {
            return edges;
        }

        //Parsing
        //From XML
        //TODO Figure out if this should be a string
        //TODO Think about refactoring to use only one queue
        public static SystemDiagram generateSystemDiagramFromXML(string xml) {
            Console.WriteLine("CREATING SYSTEM DIAGRAM FROM XML");
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
            //Element id
            int elementId = 0;
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
                int type = -1;
                List<string> modifiers = new List<string>();

                string tok = tokenQueue.Dequeue();
                if (tok.Equals("name")) {
                    name = tokenQueue.Dequeue();
                    type = typeIDDict.GetValueOrDefault(name);
                    Console.Write("TYPE OUTPUT: " + typeIDDict.GetValueOrDefault(name) + " " + name);
                    name = name.Replace("System_MR_", "").Replace("System_MT_", "").Replace("System_E_", "").Replace("System_O_", "") + elementId;
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
                Element e = new Element(type, name, x, y);
                foreach (string str in modifiers) {
                    Console.WriteLine("I FOUND THIS GUY: " + str);
                    if (str.Contains("VELOCITY")) {
                        e.setVelocity(Int32.Parse(str.Replace("VELOCITY", "")));
                    } else {
                        e.addModifier(str);
                    }
                }

                elements.Add(e);

                if (tokenQueue.Dequeue().Equals("}")) {
                    braceStack.Pop();
                }

                elementId++;
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
                        string velocity = "";

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
                        Console.WriteLine(tok);
                        if (tok.Equals("velocity")) {
                            velocity = "VELOCITY" + Convert.ToInt32(arcsTokenQueue.Dequeue());
                            Console.WriteLine(velocity);
                        } else if (tok.Equals("}")) {
                            foundCloseBrace = true;
                        }

                        if (!velocity.Contains("VELOCITY")) {
                            arcs.Add(new Edge(elements[e1], elements[e2], e1, e2));
                        } else {
                            arcs.Add(new Edge(elements[e1], elements[e2], e1, e2, Convert.ToInt32(velocity.Replace("VELOCITY", ""))));
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

        //Splits the input xml file into tokens
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
        /// <summary>
        /// Creates a system diagram from a JSON string
        /// </summary>
        /// <param name="json">A JSON String</param>
        /// <returns>The system diagram from the json string</returns>
        public static SystemDiagram? generateSystemDiagramFromJSON(string json) {
            var sysDiagram = JsonConvert.DeserializeObject<SystemDiagram>(json);
            if (sysDiagram is not null) {
                return sysDiagram;
            } else {
                //TODO Throw error
                return null;
            }
        }

        /// <summary>
        /// Converts the sytem diagram to a JSON string
        /// </summary>
        /// <returns>A JSON String</returns>
        public string convertToJson() {
            return JsonConvert.SerializeObject(this);
        }

        //To GraphSynth
        /// <summary>
        /// Converts a system diagram to a designGraph
        /// </summary>
        /// <returns>A GraphSynth designGraph</returns>
        //This bulk of this code is from BOGLDesktop
        public designGraph convertToDesignGraph() {
            StringBuilder builder = new StringBuilder();
            string ruleFileName = "system_graph";

            //XML Header
            #region GraphSynth Protocols
            builder.Append("<Page Background=\"#FF000000\" xmlns=\"http://schemas.microsoft.com/winfx/2006/xaml/presentation\"").Append(
          " xmlns:mc=\"http://schemas.openxmlformats.org/markup-compatibility/2006\"").Append(
        " mc:Ignorable=\"GraphSynth\" xmlns:GraphSynth=\"ignorableUri\" Tag=\"Graph\" ><Border BorderThickness=\"1,1,1,1\"").Append(
          " BorderBrush=\"#FFA9A9A9\" HorizontalAlignment=\"Center\" VerticalAlignment=\"Center\"><Viewbox ").Append(
        " StretchDirection=\"Both\" HorizontalAlignment=\"Stretch\" VerticalAlignment=\"Stretch\"><Canvas Background=\"#FFFFFFFF\"").Append(
         "  Width=\"732.314136125654\" Height=\"570.471204188482\" HorizontalAlignment=\"Stretch\" VerticalAlignment=\"Stretch\"").Append(
         "  RenderTransform=\"1,0,0,-1,0,570.471204188482\"><Ellipse Fill=\"#FF000000\" Tag=\"input\" Width=\"5\" Height=\"5\"").Append(
         "  HorizontalAlignment=\"Center\" VerticalAlignment=\"Center\" /><TextBlock Text=\"input (input, pivot, revolute, ground)\"").Append(
         "  FontSize=\"12\" HorizontalAlignment=\"Center\" VerticalAlignment=\"Center\" RenderTransform=\"1,0,0,-1,-14.7816666666667,67.175\" />").Append(
              " <Ellipse Fill=\"#FF000000\" Tag=\"ground\" Width=\"5\" Height=\"5\" HorizontalAlignment=\"Center\" VerticalAlignment=\"Center\" />").Append(
                   " <TextBlock Text=\"ground (ground, link)\" FontSize=\"12\" HorizontalAlignment=\"Center\" VerticalAlignment=\"Center\"").Append(
         " RenderTransform=\"1,0,0,-1,239.111666666667,67.175\" /><Path Stretch=\"None\" Fill=\"#FF000000\" Stroke=\"#FF000000\"").Append(
          " StrokeThickness=\"1\" StrokeStartLineCap=\"Flat\" StrokeEndLineCap=\"Flat\" StrokeDashCap=\"Flat\" StrokeLineJoin=\"Miter\"").Append(
          " StrokeMiterLimit=\"10\" StrokeDashOffset=\"0\" Tag=\"a0,0,0.5,12:StraightArcController,\" LayoutTransform=\"Identity\"").Append(
          " Margin=\"0,0,0,0\" HorizontalAlignment=\"Stretch\" VerticalAlignment=\"Stretch\" RenderTransform=\"Identity\"").Append(
          " RenderTransformOrigin=\"0,0\" Opacity=\"1\" Visibility=\"Visible\" SnapsToDevicePixels=\"False\"> ").Append(
          " <Path.Data><PathGeometry><PathGeometry.Figures><PathFigure StartPoint=\"77,74.5\" IsFilled=\"False\" IsClosed=\"False\"> ").Append(
          " <PathFigure.Segments><LineSegment Point=\"288,74.5\" /></PathFigure.Segments></PathFigure> ").Append(
          " <PathFigure StartPoint=\"288,74.5\" IsFilled=\"True\" IsClosed=\"True\"><PathFigure.Segments><PolyLineSegment ").Append(
              " Points=\"278,70 281,74.5 278,79\" /></PathFigure.Segments></PathFigure></PathGeometry.Figures></PathGeometry> ").Append(
                  " </Path.Data></Path></Canvas></Viewbox></Border> ").Append(
        " <GraphSynth:CanvasProperty BackgroundColor=\"#FFFFFFFF\" AxesColor=\"#FF000000\" AxesOpacity=\"1\" AxesThick=\"0.5\" ").Append(
          " GridColor=\"#FF000000\" GridOpacity=\"1\" GridSpacing=\"24\" GridThick=\"0.25\" SnapToGrid=\"True\"").Append(
          " ScaleFactor=\"1\" ShapeOpacity=\"1\" ZoomToFit=\"False\" ShowNodeName=\"True\" ShowNodeLabel=\"True\"").Append(
          " ShowArcName=\"False\" ShowArcLabel=\"True\" ShowHyperArcName=\"False\" ShowHyperArcLabel=\"True\"").Append(
          " NodeFontSize=\"12\" ArcFontSize=\"12\" HyperArcFontSize=\"12\" NodeTextDistance=\"0\" NodeTextPosition=\"0\"").Append(
          " ArcTextDistance=\"0\" ArcTextPosition=\"0.5\" HyperArcTextDistance=\"0\" HyperArcTextPosition=\"0.5\" GlobalTextSize=\"12\"").Append(
          " CanvasHeight=\"570.47120418848169\" CanvasWidth=\"732.314136125654,732.314136125654,732.314136125654,732.314136125654\"").Append(
          " WindowLeft=\"694.11518324607323\" WindowTop=\"290.5130890052356\" extraAttributes=\"{x:Null}\" Background=\"#FF93CDDD\"").Append(
          " xmlns=\"clr-namespace:GraphSynth.UI;assembly=GraphSynth\" xmlns:x=\"http://schemas.microsoft.com/winfx/2006/xaml\"").Append(
          " xmlns:sx=\"clr-namespace:System.Xml;assembly=System.Xml\" xmlns:av=\"http://schemas.microsoft.com/winfx/2006/xaml/presentation\"").Append(
           " xmlns:gsui=\"clr-namespace:GraphSynth.UI;assembly=GraphSynth.CustomControls\" xmlns:s=\"clr-namespace:System;assembly=mscorlib\"> ").Append(
          " <CanvasProperty.extraData><x:Array Type=\"sx:XmlElement\"><x:Null /></x:Array></CanvasProperty.extraData> ").Append(
               "</GraphSynth:CanvasProperty>").Append("\n");

            builder.Append("<GraphSynth:designGraph xmlns:xsd=\"http://www.w3.org/2001/XMLSchema\" ").Append(
           "xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\">").Append("\n");

            #endregion
            builder.AppendLine("<name>" + ruleFileName + "</name>");

            builder.AppendLine("<globalLabels />");
            builder.AppendLine("<globalVariables />");
            int arc1 = 0;
            int name1 = 0;
            //Add arcs
            if (edges.Count > 0) {
                builder.AppendLine("<arcs>");
                foreach (Edge edge in edges) {
                    string arcname = "arc" + (arc1++);
                    builder.AppendLine("<arc>");
                    builder.AppendLine("<name>" + arcname + "</name>");
                    builder.AppendLine("<localLabels />");
                    builder.AppendLine("<localVariables />");
                    builder.AppendLine("<From>" + edge.getE1().getName() + "</From>");
                    builder.AppendLine("<To>" + edge.getE2().getName() + "</To>");
                    builder.AppendLine("<directed>false</directed>");
                    builder.AppendLine("<doublyDirected>false</doublyDirected>");

                    builder.AppendLine("</arc>");
                }
                builder.AppendLine("</arcs>");
            } else {
                builder.AppendLine("<arcs />");
            }
            builder.AppendLine("<nodes>");
            //Add elements
            foreach (Element element in elements) {
                builder.AppendLine("<node>");
                builder.AppendLine("<name>" + element.getName() + "</name>");
                builder.AppendLine("<localLabels>");
                Regex r = new Regex(@"\d+", RegexOptions.None);
                builder.AppendLine("<string>" + r.Replace(element.getName(), "") + "</string>");
                foreach (var n in element.getLabelList()) {
                    builder.AppendLine("<string>" + n + "</string>");
                }
                builder.AppendLine("</localLabels>");
                builder.AppendLine("<localVariables />");

                builder.AppendLine("<X>0</X>");
                builder.AppendLine("<Y>0</Y>");
                builder.AppendLine("<Z>0</Z>");

                builder.AppendLine("</node>");
            }

            builder.AppendLine("</nodes>");

            builder.AppendLine("<hyperarcs />");

            builder.AppendLine("</GraphSynth:designGraph>");
            builder.AppendLine("</Page>");

            Console.WriteLine("-------- Builder String --------");
            Console.WriteLine(builder.ToString());

            XDocument doc_ = XDocument.Parse(builder.ToString());

            XmlReader do1 = doc_.CreateReader();

            var XGraphAndCanvas = XElement.Load(do1);

            var temp2 = XGraphAndCanvas.Element("{ignorableUri}" + "designGraph");
            var temp = RemoveXAMLns(RemoveIgnorablePrefix(temp2.ToString()));
            //Convert the xmlString into a designGraph
            designGraph systemGraph;
            {
                var stringReader = new StringReader(temp.ToString());
                var graphDeserializer = new XmlSerializer(typeof(designGraph));

                systemGraph = (designGraph) graphDeserializer.Deserialize(stringReader);
                systemGraph.internallyConnectGraph();
                removeNullWhiteSpaceEmptyLabels(systemGraph);
            }
            Console.WriteLine("Loaded");


            return systemGraph;
        }

        //From BoGL Desktop
        private string RemoveXAMLns(string str) {
            return str.Replace("xmlns=\"http://schemas.microsoft.com/winfx/2006/xaml/presentation\"", "");
        }

        //From BoGL Desktop
        private string RemoveIgnorablePrefix(string str) {
            return str.Replace("GraphSynth:", "").Replace("xmlns=\"ignorableUri\"", "");
        }

        //From BoGL Desktop
        private void removeNullWhiteSpaceEmptyLabels(designGraph g) {
            g.globalLabels.RemoveAll(string.IsNullOrWhiteSpace);
            foreach (var a in g.arcs) {
                a.localLabels.RemoveAll(string.IsNullOrWhiteSpace);
            }
            foreach (var a in g.nodes) {
                a.localLabels.RemoveAll(string.IsNullOrWhiteSpace);
            }
            foreach (var a in g.hyperarcs) {
                a.localLabels.RemoveAll(string.IsNullOrWhiteSpace);
            }
        }

        //Create .bogl string
        public string generateBoGLString() {
            //TODO Implement
            return "";
        }

        public class Element {
            protected string name;
            [JsonProperty]
            protected int type;
            [JsonProperty]
            protected double x;
            [JsonProperty]
            protected double y;
            [JsonProperty]
            protected List<int> modifiers;
            [JsonProperty]
            protected int velocity;
            protected string velocityDir;

            /// <summary>
            /// Creates an element of the system diagram
            /// </summary>
            /// TODO Add type
            /// <param name="name">The name of the element</param>
            /// <param name="x">The x position of the element</param>
            /// <param name="y">The y position of the element</param>
            public Element(int type, string name, double x, double y) {
                this.type = type;
                this.name = name;
                this.x = x;
                this.y = y;
                modifiers = new();
            }

            //TODO Error checking
            /// <summary>
            /// Adds a modifier to the element
            /// </summary>
            /// <param name="name">The name of the modifier to add</param>
            public void addModifier(string name) {
                modifiers.Add(modifierIDDict.GetValueOrDefault(name));
            }

            //TODO Error checking
            /// <summary>
            /// Adds a velocity to the eleemtn
            /// </summary>
            /// <param name="vel">The direction of the velocity</param>
            public void setVelocity(int vel) {
                velocity = vel;
            }

            /// <summary>
            /// Returns the name of the element 
            /// </summary>
            /// <returns>The name of the element</returns>
            public string getName() {
                return name;
            }

            /// <summary>
            /// Returns a list of strings representing the labels the element had
            /// </summary>
            /// <returns>A list</returns>
            public List<string> getLabelList() {
                List<string> strings = new List<string>();

                foreach (var modifier in modifiers) {
                    strings.Add(modifier.ToString());
                }

                if (this.velocity != 0){
                    strings.Add("veladded");
                    strings.Add("vel" + velocity);
                }

                return strings;
            }

            /// <summary>
            /// Creates a string representation of the element
            /// </summary>
            /// <returns>A string</returns>
            public string toString() {
                string output = "Element\r\n ";
                output += name + "\r\n";

                foreach (int modifier in modifiers) {
                    output += modifier + "\r\n";
                }

                output += velocity + "\r\n";

                return output;
            }
        }

        public class Edge {
            protected readonly Element e1;
            protected readonly Element e2;
            [JsonProperty]
            protected readonly int source;
            [JsonProperty]
            protected readonly int target;
            [JsonProperty]
            protected readonly int velocity;

            /// <summary>
            /// Creates an edge between two elements
            /// </summary>
            /// <param name="e1">The first element</param>
            /// <param name="e2">The second element</param>
            public Edge(Element e1, Element e2, int source, int target) {
                this.e1 = e1;
                this.e2 = e2;
                this.source = source;
                this.target = target;
                this.velocity = 0;
            }

            /// <summary>
            /// Creates an edge between two elements witha  velocity
            /// </summary>
            /// <param name="e1">The first element</param>
            /// <param name="e2">The second element</param>
            /// <param name="velocity">The velocity of the element</param>
            public Edge(Element e1, Element e2, int source, int target, int velocity) {
                this.e1 = e1;
                this.e2 = e2;
                this.source = source;
                this.target = target;
                this.velocity = velocity;
            }

            /// <summary>
            /// Gets the first element
            /// </summary>
            /// <returns>An element</returns>
            public Element getE1() {
                return e1;
            }

            /// <summary>
            /// Gets the second element
            /// </summary>
            /// <returns>An element</returns>
            public Element getE2() {
                return e2;
            }

            /// <summary>
            /// Returns a string representation of the edge
            /// </summary>
            /// <returns>A string</returns>
            public string toString() {
                return velocity == 0 ? "Arc " + e1.getName() + " to " + e2.getName() + "\r\n" : "Arc " + e1.getName() + " to " + e2.getName() + " has velocity " + velocity + "\r\n";
            }
        }
    }
}

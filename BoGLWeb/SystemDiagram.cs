using Newtonsoft.Json;
using System.Collections.Immutable;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Serialization;
using BoGLWeb.BaseClasses;
using System.Text.RegularExpressions;

namespace BoGLWeb {
    public class SystemDiagram {
        private static readonly ImmutableDictionary<string, int> modifierIDDict;
        private static readonly ImmutableDictionary<int, string> modifierIDDictReverse;
        private static readonly ImmutableDictionary<string, int> typeIDDict;
        private static readonly ImmutableDictionary<int, string> typeIDDictReverse;

        //Sets up our modifier dictionary
        static SystemDiagram() {

            ImmutableDictionary<string, int>.Builder idBuilder = ImmutableDictionary.CreateBuilder<string, int>();
            idBuilder.Add("MASS", 0);
            idBuilder.Add("INERTIA", 1);
            idBuilder.Add("STIFFNESS", 2);
            idBuilder.Add("FRICTION", 3);
            idBuilder.Add("DAMPING", 4);
            idBuilder.Add("PARALLEL", 5);
            idBuilder.Add("TOOTH_WEAR", 6);

            modifierIDDict = idBuilder.ToImmutable();
            
            ImmutableDictionary<int, string>.Builder idBuilderReverse = ImmutableDictionary.CreateBuilder<int, string>();
            idBuilderReverse.Add(0, "MASS");
            idBuilderReverse.Add(1, "INERTIA");
            idBuilderReverse.Add(2, "STIFFNESS");
            idBuilderReverse.Add(3, "FRICTION");
            idBuilderReverse.Add(4, "DAMPING");
            idBuilderReverse.Add(5, "PARALLEL");
            idBuilderReverse.Add(6, "TOOTH_WEAR");

            modifierIDDictReverse = idBuilderReverse.ToImmutable();

            ImmutableDictionary<string, int>.Builder typeBuilder = ImmutableDictionary.CreateBuilder<string, int>();
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
            typeBuilder.Add("System_MR_Pulley", 13);
            typeBuilder.Add("System_MR_Pulley_Grounded", 30);
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
            
            ImmutableDictionary<int, string>.Builder typeBuilderReverse = ImmutableDictionary.CreateBuilder<int, string>();
            typeBuilderReverse.Add(0, "System_MT_Mass");
            typeBuilderReverse.Add(1, "System_MT_Spring");
            typeBuilderReverse.Add(2, "System_MT_Damper");
            typeBuilderReverse.Add(3, "System_MT_Ground");
            typeBuilderReverse.Add(4, "System_MT_Force_Input");
            typeBuilderReverse.Add(5, "System_MT_Gravity");
            typeBuilderReverse.Add(6, "System_MT_Velocity_Input");
            typeBuilderReverse.Add(7, "System_MR_Flywheel");
            typeBuilderReverse.Add(8, "System_MR_Spring");
            typeBuilderReverse.Add(9, "System_MR_Damper");
            typeBuilderReverse.Add(10, "System_MR_Torque_Input");
            typeBuilderReverse.Add(11, "System_MR_Velocity_Input");
            typeBuilderReverse.Add(12, "System_MR_Lever");
            typeBuilderReverse.Add(13, "System_MR_Pulley");
            typeBuilderReverse.Add(30, "System_MR_Pulley_Grounded");
            typeBuilderReverse.Add(14, "System_MR_Belt");
            typeBuilderReverse.Add(15, "System_MR_Shaft");
            typeBuilderReverse.Add(16, "System_MR_Gear");
            typeBuilderReverse.Add(17, "System_MR_Gear_Pair");
            typeBuilderReverse.Add(18, "System_MR_Rack");
            typeBuilderReverse.Add(19, "System_MR_Rack_Pinion");
            typeBuilderReverse.Add(20, "System_E_Inductor");
            typeBuilderReverse.Add(21, "System_E_Capacitor");
            typeBuilderReverse.Add(22, "System_E_Resistor");
            typeBuilderReverse.Add(23, "System_E_Transformer");
            typeBuilderReverse.Add(24, "System_E_Junction");
            typeBuilderReverse.Add(25, "System_E_Ground");
            typeBuilderReverse.Add(26, "System_E_Current_Input");
            typeBuilderReverse.Add(27, "System_E_Voltage_Input");
            typeBuilderReverse.Add(28, "System_O_PM_Motor");
            typeBuilderReverse.Add(29, "System_O_VC_Transducer");

            typeIDDictReverse = typeBuilderReverse.ToImmutable();
        }

        [JsonProperty]
        protected List<Element> elements;
        [JsonProperty]
        protected List<Edge> edges;
        // Leaving this out of JSON for now because we're not expecting to use it currently
        private Dictionary<string, double> header;

        /// <summary>
        /// Creates a system diagram instance
        /// </summary>
        /// <param name="header">A dictionary of headers for the system diagram</param>
        public SystemDiagram(Dictionary<string, double> header) {
            this.elements = new List<Element>();
            this.edges = new List<Edge>();
            this.header = header;
        }

        //Creates a system diagram with a list of elements and edges. This is not exposed to other classes because there should be no way to create system diagrams without xml or json
        private SystemDiagram(Dictionary<string, double> header, List<Element> elements, List<Edge> edges) {
            this.header = header;
            this.elements = elements;
            this.edges = edges;
        }

        private Element getElement(int pos) {
            return this.elements[pos];
        }

        /// <summary>
        /// Returns the edge located at a given position in the list
        /// </summary>
        /// <param name="pos">The position in the list</param>
        /// <returns>The edge as the given position</returns>
        public Edge getEdge(int pos) {
            return this.edges[pos];
        }

        /// <summary>
        /// Gets the list of elements in the system diagram
        /// </summary>
        /// <returns>The list of elements</returns>
        public List<Element> getElements() {
            return this.elements;
        }

        /// <summary>
        /// Gets the elements in the system diagram
        /// </summary>
        /// <returns>The list of elements</returns>
        public List<Edge> getEdges() {
            return this.edges;
        }

        /// <summary>
        /// Parses an xml string into a system diagram
        /// </summary>
        /// <param name="xml">An xml string</param>
        /// <returns>A system diagram from the xml string</returns>
        /// <exception cref="ArgumentException">Thrown if input xml was invalid</exception>
        //Parsing
        //TODO Think about refactoring to use only one queue
        public static SystemDiagram generateSystemDiagramFromXML(string xml) {
            string errorMessage = "You have attempted to load an invalid .bogl file. Please ensure that you have the correct file and try again. File must have been saved using BoGL Web or BoGL Desktop to be valid.";
            List<string> tokens = tokenize(xml);

            //TODO Check if any of these are -1 because then we have an error
            int headerPos = findTokenLocation(tokens, "[Header]");
            int elementsPos = findTokenLocation(tokens, "[Elements]");
            int arcsPos = findTokenLocation(tokens, "[Arcs]");

            //Parse Header
            Dictionary<string, double> header = new();
            for (int i = headerPos + 1; i < elementsPos; i++) {
                //Add the header label and the value to the dictionary
                header.Add(tokens[i], Convert.ToDouble(tokens[++i]));
            }

            //Parse Elements
            List<Element> elements = new();
            //TODO Refactor name to elementTokenQueue
            Queue<string> tokenQueue = new();
            for (int i = elementsPos + 1; i < arcsPos; i++) {
                tokenQueue.Enqueue(tokens[i]);
            }

            //Create elements while we have symbols left
            //Element id
            int elementId = 0;
            while (tokenQueue.Count > 0) {
                //Find an element by looking for matching braces
                Stack<string> braceStack = new();

                //The head of the queue should always be a brace here so we can pop it and add it to the stack
                string stackTok = tokenQueue.Dequeue();
                braceStack.Push(stackTok);

                //We expect the next string in the queue to be "name"
                string name;
                double x;
                double y;
                int type;
                List<string> modifiers = new();

                string tok = tokenQueue.Dequeue();
                if (tok.Equals("name")) {
                    name = tokenQueue.Dequeue();
                    type = typeIDDict.GetValueOrDefault(name);
                    name = name.Replace("System_MR_", "").Replace("System_MT_", "").Replace("System_E_", "").Replace("System_O_", "") + elementId;
                } else {
                    // The grammar is not being followed for the .bogl file
                    Console.WriteLine("Name was missing. Got: <" + tok + "> instead");
                    throw new ArgumentException(errorMessage);
                }

                if (tokenQueue.Dequeue().Equals("x")) {
                    x = Convert.ToDouble(tokenQueue.Dequeue());
                } else {
                    // The grammar is not being followed for the .bogl file
                    Console.WriteLine("X was missing. Got: <" + tok + "> instead");
                    throw new ArgumentException(errorMessage);
                }

                if (tokenQueue.Dequeue().Equals("y")) {
                    y = Convert.ToDouble(tokenQueue.Dequeue());
                } else {
                    // The grammar is not being followed for the .bogl file
                    //TODO Figure out how we should handle this error
                    Console.WriteLine("Y was missing. Got: <" + tok + "> instead");
                    throw new ArgumentException(errorMessage);
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
                    throw new ArgumentException(errorMessage);
                }

                //Add element to element list
                Element e = new(type, name, x, y);
                foreach (string str in modifiers) {
                    if (str.Contains("VELOCITY")) {
                        e.setVelocity(int.Parse(str.Replace("VELOCITY", "")));
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
            List<Edge> arcs = new();
            Queue<string> arcsTokenQueue = new();
            for (int i = arcsPos + 1; i < tokens.Count; i++) {
                arcsTokenQueue.Enqueue(tokens[i]);
            }

            while (arcsTokenQueue.Count > 0) {
                bool foundCloseBrace = false;
                if (arcsTokenQueue.Dequeue().Equals("{")) {
                    //Parse
                    //TODO Need to add something that checks if this loop runs more than once because that should be an error
                    while (!foundCloseBrace) {
                        int e1;
                        int e2;
                        string velocity = "";

                        //Check element1
                        string tok = arcsTokenQueue.Dequeue();
                        if (tok.Equals("element1")) {
                            e1 = Convert.ToInt32(arcsTokenQueue.Dequeue());
                        } else {
                            // The grammar is not being followed
                            Console.WriteLine("Element1 was missing. Got: <" + tok + "> instead");
                            throw new ArgumentException(errorMessage);
                        }

                        //Check element2
                        tok = arcsTokenQueue.Dequeue();
                        if (tok.Equals("element2")) {
                            e2 = Convert.ToInt32(arcsTokenQueue.Dequeue());
                        } else {
                            // The grammar is not being followed
                            Console.WriteLine("Element2 was missing. Got: <" + tok + "> instead");
                            throw new ArgumentException(errorMessage);
                        }

                        //Modifiers
                        tok = arcsTokenQueue.Dequeue();
                        switch (tok) {
                            //TODO Confirm that this is the only modifier
                            case "velocity":
                                velocity = "VELOCITY" + Convert.ToInt32(arcsTokenQueue.Dequeue());
                                break;
                            case "}":
                                foundCloseBrace = true;
                                break;
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
                    Console.WriteLine("Missing open brace");
                    throw new ArgumentException(errorMessage);
                }
            }

            //Create system diagram
            return new SystemDiagram(header, elements, arcs);
        }

        //Return the position of @token in @tokens if it exists. -1 otherwise
        private static int findTokenLocation(IReadOnlyList<string> tokens, string token) {
            for (int i = 0; i < tokens.Count; i++) {
                if (tokens[i].Equals(token)) {
                    return i;
                }
            }
            return -1;
        }

        //Splits the input xml file into tokens
        private static List<string> tokenize(string xml) {
            string[] lines = xml.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);

            return (from line in lines from token in line.Trim().Split(" ") where token.Length > 0 select token).ToList();
        }

        //From JSON
        //Convert to GraphSynth
        /// <summary>
        /// Creates a system diagram from a JSON string
        /// </summary>
        /// <param name="json">A JSON String</param>
        /// <returns>The system diagram from the json string</returns>
        public static SystemDiagram? generateSystemDiagramFromJSON(string json) {
            SystemDiagram? sysDiagram = JsonConvert.DeserializeObject<SystemDiagram>(json);
            dynamic? parsedJSON = JsonConvert.DeserializeObject<dynamic>(json);

            if (sysDiagram is null) {
                return null;
            }

            foreach (dynamic? bond in parsedJSON.bonds) {
                sysDiagram.edges.Add(new Edge(sysDiagram.getElement(int.Parse(bond.source.id.ToString())), 
                    sysDiagram.getElement(int.Parse(bond.target.id.ToString())), int.Parse(bond.source.id.ToString()), int.Parse(bond.target.id.ToString()), 
                    int.Parse(bond.velocity.ToString())));
            }
                
            return sysDiagram;
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
            StringBuilder builder = new();
            const string ruleFileName = "system_graph";

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
            //Add arcs
            if (this.edges.Count > 0) {
                builder.AppendLine("<arcs>");
                foreach (Edge edge in this.edges) {
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
            foreach (Element element in this.elements) {
                builder.AppendLine("<node>");
                builder.AppendLine("<name>" + element.getName() + "</name>");
                builder.AppendLine("<localLabels>");
                Regex r = new(@"\d+", RegexOptions.None);
                builder.AppendLine("<string>" + r.Replace(element.getName(), "") + "</string>");
                foreach (string n in element.getLabelList()) {
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

            XDocument doc = XDocument.Parse(builder.ToString());

            XmlReader do1 = doc.CreateReader();

            XElement XGraphAndCanvas = XElement.Load(do1);

            XElement? temp2 = XGraphAndCanvas.Element("{ignorableUri}" + "designGraph");
            string temp = this.RemoveXAMLns(RemoveIgnorablePrefix(temp2.ToString()));
            //Convert the xmlString into a designGraph
            designGraph systemGraph;
            {
                StringReader stringReader = new StringReader(temp);
                XmlSerializer graphDeserializer = new XmlSerializer(typeof(designGraph));

                systemGraph = (designGraph) graphDeserializer.Deserialize(stringReader);
                systemGraph.internallyConnectGraph();
                this.removeNullWhiteSpaceEmptyLabels(systemGraph);
            }

            return systemGraph;
        }

        //From BoGL Desktop
        private string RemoveXAMLns(string str) {
            return str.Replace("xmlns=\"http://schemas.microsoft.com/winfx/2006/xaml/presentation\"", "");
        }

        //From BoGL Desktop
        private static string RemoveIgnorablePrefix(string str) {
            return str.Replace("GraphSynth:", "").Replace("xmlns=\"ignorableUri\"", "");
        }

        //From BoGL Desktop
        private void removeNullWhiteSpaceEmptyLabels(designGraph g) {
            g.globalLabels.RemoveAll(string.IsNullOrWhiteSpace);
            foreach (arc a in g.arcs) {
                a.localLabels.RemoveAll(string.IsNullOrWhiteSpace);
            }
            foreach (node a in g.nodes) {
                a.localLabels.RemoveAll(string.IsNullOrWhiteSpace);
            }
            foreach (hyperarc a in g.hyperarcs) {
                a.localLabels.RemoveAll(string.IsNullOrWhiteSpace);
            }
        }

        /// <summary>
        /// Creates a string that represents the system diagram. This will be used for saving .bogl files.
        /// </summary>
        /// <returns>A string</returns>
        public string generateBoGLString() {
            StringBuilder sb = new();

            sb.Append("[Header]\n");
            if (header == null) {
                this.header = new Dictionary<string, double> { { "panX", 0.0 }, { "panY", 0.0 }, { "zoom", 0.0 } };
            }
            foreach (KeyValuePair<string, double> entry in this.header) {
                sb.Append(entry.Key);
                sb.Append(" ");
                sb.Append(entry.Value);
                sb.Append('\n');
            }

            sb.Append("[Elements]\n");
            foreach (Element e in this.elements) {
                sb.Append("{\n");
                sb.Append("name");
                sb.Append(" ");
                sb.Append(typeIDDictReverse[e.getType()]);
                sb.Append("\n");
                sb.Append("x");
                sb.Append(" ");
                sb.Append(e.getX());
                sb.Append("\n");
                sb.Append("y");
                sb.Append(" ");
                sb.Append(e.getY());
                sb.Append('\n');
                sb.Append("modifiers {\n");
                foreach (int mod in e.getModifiers()) {
                    sb.Append(modifierIDDictReverse[mod]);
                    sb.Append('\n');
                }
                
                if (e.getVelocity() != 0){
                    sb.Append("VELOCITY");
                    sb.Append(" ");
                    sb.Append(e.getVelocity());
                    sb.Append('\n');
                }

                sb.Append("}\n");
                sb.Append("}\n");
            }

            sb.Append("[Arcs]");
            sb.Append('\n');
            foreach (Edge edge in this.edges) {
                sb.Append('{');
                sb.Append('\n');
                sb.Append("element1 ");
                sb.Append(edge.getSource());
                sb.Append('\n');
                sb.Append("element2 ");
                sb.Append(edge.getTarget());
                if (edge.getVelocity() != 0) {
                    sb.Append('\n');
                    sb.Append("velocity ");
                    sb.Append(edge.getVelocity());
                }
                sb.Append('\n');
                sb.Append('}');
                sb.Append('\n');
            }
            

            return sb.ToString();
        }

        public class Element {
            private readonly string name;
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

            private static int universalID = 0;
            private int? ID;

            /// <summary>
            /// Creates an element of the system diagram
            /// </summary>
            /// <param name="type">The id of the object</param>
            /// <param name="name">The name of the element</param>
            /// <param name="x">The x position of the element</param>
            /// <param name="y">The y position of the element</param>
            public Element(int type, string name, double x, double y) {
                this.type = type;
                this.name = name;
                this.x = x;
                this.y = y;
                modifiers = new();
                AssignID(0, true);
            }

            /// <summary>
            /// Adds a modifier to the element
            /// </summary>
            /// <param name="name">The name of the modifier to add</param>
            public void addModifier(string name) {
                this.modifiers.Add(modifierIDDict.GetValueOrDefault(name));
            }

            //TODO Error checking
            /// <summary>
            /// Adds a velocity to the eleemtn
            /// </summary>
            /// <param name="vel">The direction of the velocity</param>
            public void setVelocity(int vel) {
                this.velocity = vel;
            }

            /// <summary>
            /// Returns the modifiers on an element
            /// </summary>
            /// <returns>A list</returns>
            public List<int> getModifiers() {
                return this.modifiers;
            }

            /// <summary>
            /// Returns the velocity modifier on an element
            /// </summary>
            /// <returns>An integer</returns>
            public int getVelocity() {
                return this.velocity;
            }

            /// <summary>
            /// Returns the name of the element 
            /// </summary>
            /// <returns>The name of the element</returns>
            public string getName() {
                return this.name;
            }

            /// <summary>
            /// Returns the type of the element
            /// </summary>
            /// <returns>An integer</returns>
            public int getType() {
                return this.type;
            }

            /// <summary>
            /// Returns the x position of the element
            /// </summary>
            /// <returns>A double</returns>
            public double getX() {
                return this.x;
            }

            /// <summary>
            /// Returns the y position of the element
            /// </summary>
            /// <returns>A double</returns>
            public double getY() {
                return this.y;
            }

            /// <summary>
            /// Returns a list of strings representing the labels the element had
            /// </summary>
            /// <returns>A list</returns>
            public List<string> getLabelList() {
                List<string> strings = this.modifiers.Select(modifier => modifier.ToString()).ToList();

                if (this.velocity == 0) {
                    return strings;
                }

                strings.Add("veladded");
                strings.Add("vel" + this.velocity);

                return strings;
            }

            /// <summary>
            /// Assigns an ID to this <c>Element</c>.
            /// </summary>
            /// <param name="ID">
            /// A candidate ID.
            /// </param>
            /// <param name="isDistinct">
            /// <c>true</c> if the ID of this <c>Element</c> should be unique,
            /// else <c>false</c>.
            /// </param>
            private void AssignID(int? ID, bool isDistinct) {
                if (this.ID == null | isDistinct) {
                    this.ID = universalID++;
                } else {
                    this.ID = ID;
                }
            }

            /// <summary>
            /// Makes a copy of this <c>Element</c>.
            /// </summary>
            /// <param name="isDistinct">
            /// <c>true</c> if the copy should have its own ID, else <c>false</c>.
            /// </param>
            /// <returns>
            /// The copy.
            /// </returns>
            public Element Copy(bool isDistinct) {
                Element copy = new(this.type, this.name, this.x, this.y) {
                    modifiers = new List<int>()
                };
                this.modifiers.AddRange(this.modifiers);
                copy.AssignID(this.ID, isDistinct);
                return copy;
            }

            /// <summary>
            /// Gets the ID of this <c>Element</c>.
            /// </summary>
            /// <returns>
            /// <c>this.ID</c>
            /// </returns>
            public int GetID() {
                return (this.ID is int ID) ? ID : 0;
            }

            /// <summary>
            /// Creates a string representation of the element
            /// </summary>
            /// <returns>A string</returns>
            public string toString() {
                string output = "Element\r\n ";
                output += this.name + "\r\n";

                output = this.modifiers.Aggregate(output, (current, modifier) => current + (modifier + "\r\n"));

                output += this.velocity + "\r\n";

                return output;
            }
        }

        public class Edge {
            private readonly Element e1;
            private readonly Element e2;
            [JsonProperty]
            protected readonly int source;
            [JsonProperty]
            protected readonly int target;
            [JsonProperty]
            protected readonly int velocity;

            /// <summary>
            /// Tracks the undo/redo and general IDs for this <c>Edge</c>.
            /// </summary>
            private static int universalID = 0;
            private int? ID;

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
                return this.e1;
            }

            /// <summary>
            /// Gets the second element
            /// </summary>
            /// <returns>An element</returns>
            public Element getE2() {
                return this.e2;
            }
            
            /// <summary>
            /// Returns the id of the source
            /// </summary>
            /// <returns>An integer</returns>
            public int getSource() {
                return this.source;
            }

            /// <summary>
            /// Returns the id of the target (sink)
            /// </summary>
            /// <returns>An integer</returns>
            public int getTarget() {
                return this.target;
            }

            /// <summary>
            /// Returns the velocity modifier of the edge
            /// </summary>
            /// <returns>An integer</returns>
            public int getVelocity() {
                return this.velocity;
            }

            /// <summary>
            /// Assigns an ID to this <c>Edge</c>.
            /// </summary>
            /// <param name="ID">
            /// A candidate ID.
            /// </param>
            /// <param name="isDistinct">
            /// <c>true</c> if the ID of this <c>Edge</c> should be unique,
            /// else <c>false</c>.
            /// </param>
            private void AssignID(int? ID, bool isDistinct) {
                if (this.ID == null | isDistinct) {
                    this.ID = universalID++;
                } else {
                    this.ID = ID;
                }
            }

            /// <summary>
            /// Makes a copy of this <c>Edge</c>.
            /// </summary>
            /// <param name="isDistinct">
            /// <c>true</c> if the copy should have its own ID, else <c>false</c>.
            /// </param>
            /// <returns>
            /// The copy.
            /// </returns>
            public Edge Copy(bool isDistinct) {
                Edge copy = new(this.e1, this.e2, this.source, this.target, this.velocity);
                copy.AssignID(this.ID, isDistinct);
                return copy;
            }

            /// <summary>
            /// Gets the ID of this <c>Edge</c>.
            /// </summary>
            /// <returns>
            /// <c>this.ID</c>
            /// </returns>
            public int GetID() {
                return (this.ID is int ID) ? ID : 0;
            }

            /// <summary>
            /// Returns a string representation of the edge
            /// </summary>
            /// <returns>A string</returns>
            public string toString() {
                return this.velocity == 0 ? "Arc " + this.e1.getName() + " to " + this.e2.getName() + "\r\n" : "Arc " + this.e1.getName() + " to " + this.e2.getName() + " has velocity " + this.velocity + "\r\n";
            }
        }
    }
}

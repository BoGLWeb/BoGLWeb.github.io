using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Input;
using static AVL_Prototype_1.Graph;

namespace AVL_Prototype_1
{
    public class GraphElement
    {
        // Constants
        public const double imageSizeMax = 100.0;
        public const double imageCanvasBorder = 30.0;
        public const double connectPointCircleRadius = 5.0;
        public const double borderThickness = 2.0;

        // Element details
        public String elementName = null;
        public String componentName = null;
        public Dictionary<Graph.ModifierType, int> modifiers;
        public String nodeName = null;
        public List<String> labels;

        //TODO These might be important, might need to figure out how to bring them back
       // Applicable velocity modifiers
        //public List<TextBlock> velocities;
        //public TextBlock upLeft;   // 1
        //public TextBlock upRight;  // 2
        //public TextBlock leftUp;   // 3
        //public TextBlock leftDown; // 4
        //public TextBlock rightUp;  // 5
        //public TextBlock rightDown;// 6
        //public TextBlock downLeft; // 7
        //public TextBlock downRight;// 8

        public Dictionary<String, int> velocityNum = new Dictionary<string, int>();

        // Parent canvas
        protected Graph graph;

        // List of arcs
        public List<Arc> connections;

        public bool deleted = false;

        // Automatically handle wether or not this element is selected
        public bool selected
        {
            get => graph.selectedElements.Contains(this);
            set
            {
                if (deleted)
                    return;

                if (value)
                {
                    if (!graph.selectedElements.Contains(this))
                    {
                        graph.selectedElements.Add(this);
                    }
                }
                else
                {
                    if (graph.selectedElements.Contains(this))
                    {
                        graph.selectedElements.Remove(this);
                    }
                }
            }
        }

        protected GraphElement()
        {
            // Default constructor...
        }

        public GraphElement(Graph graph, String elementName, bool topLeft = false, string specialCases = null)
        {
            // Assign references
            this.graph = graph;
            this.elementName = elementName;           
            this.componentName = findComponentName(elementName, specialCases);
            labels = new List<String>();

            labels.Add(componentName);

            // Initialize list of arcs
            connections = new List<Arc>();

            // Initialize the list of modifiers
            modifiers = new Dictionary<Graph.ModifierType, int>();

            if (modifiers.ContainsKey(Graph.ModifierType.VELOCITY))
            {
                //combinations
                //rightup, left up, right down, left down
                //2,7,3,6
                //upright, downright,upleft, downleft
                //1,4,8,5

                velocityNum["upRight"] = 1;
                velocityNum["rightUp"] = 2;
                velocityNum["rightDown"] = 3;
                velocityNum["downRight"] = 4;
                velocityNum["downLeft"] = 5;
                velocityNum["leftDown"] = 6;
                velocityNum["leftUp"] = 7;
                velocityNum["upLeft"] = 8;

                double arrowHorizWidth = 44;
                double arrowVertHeight = 74;

                double arrowHorizDist = 17;
                double arrowVertDist = 17;

            }

            // Add this element to the graph's list of elements
            graph.elements.Add(this);
        }

        protected string findComponentName(string elementName, string specialcases)
        {
            if (elementName.Contains("System_MT_"))
            {
                string NewString = elementName.Remove(0, 10);
                return NewString;
            }
            else if (elementName.Contains("System_MR_") && specialcases==null)
            {
                string NewString = elementName.Remove(0, 10);
                return NewString;
            }
            else if (elementName.Contains("System_E_"))
            {
                string NewString = elementName.Remove(0, 9);
                return NewString;
            }
            else if (specialcases!=null && elementName.Contains("System_MR_") && elementName.Contains("_Rack"))
            {
                return "Gear_Rack";
            }
            else if (specialcases!=null && elementName.Contains("_Gear"))
            {
                return "Gear_Pinion";
            }
            else if (elementName.Contains("_Gear"))
            {
                return "Gear";
            }
            else // if (elementName.Contains("System_O_")
            {
                string NewString = elementName.Remove(0, 9);
                return NewString;
            }
        }

        // Deletes all of the WPF controls that make up this element - also deletes all arcs
        // WARNING - do not use this element after calling this function - it should be garbage collected immediately
        public void delete()
        {
            if (deleted)
                return;

            // Remove all arcs connected to this element
            List<Arc> connectionsCopy = new List<Arc>(connections);
            foreach (Arc a in connectionsCopy)
            {
                a.delete();
            }
            connectionsCopy.Clear();
            connections.Clear();

            graph.elements.Remove(this);

            deleted = true;
        }

        // Returns if this element should be able to connect to a given element
        public bool canConnectTo(GraphElement other)
        {
            // Can't connect to ourselves
            if (other == this)
                return false;

            // Can't connect to something we've connected to already
            foreach (Arc a in connections)
            {
                if (a.element1 == other || a.element2 == other)
                    return false;
            }

            // Both elements must accept connections
            if (!(canAcceptConnections() && other.canAcceptConnections()))
                return false;

            // We're good
            return true;
        }

        public bool canAcceptConnections()
        {
            return true;
        }

        // Returns a string representation of this element
        public string serialize()
        {
            StringBuilder sb = new StringBuilder();

            sb.AppendLine("{");

            sb.AppendLine("\tname " + elementName);
            // Modifiers
            sb.AppendLine("\tmodifiers {");
            foreach (KeyValuePair<Graph.ModifierType, int> modifier in modifiers)
            {
                if (modifier.Value > 0)
                {
                    if (modifier.Key == Graph.ModifierType.VELOCITY)
                    {
                        sb.AppendLine("\t\t" + modifier.Key.ToString() + " " + modifier.Value);
                    }
                    else
                    {
                        sb.AppendLine("\t\t" + modifier.Key.ToString());
                    }
                }
            }
            sb.AppendLine("\t}");

            sb.AppendLine("}");

            return sb.ToString();
        }

        // Sets this element's velocity and shows/hides the arrows as needed
        public void setVelocity(int velocity)
        {
            if (!modifiers.ContainsKey(Graph.ModifierType.VELOCITY))
                return;

            if (velocity < 0 || velocity > 8)
                velocity = 0;

            modifiers[ModifierType.VELOCITY] = velocity;
        }

    }
}

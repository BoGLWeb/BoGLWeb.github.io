using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Input;

namespace AVL_Prototype_1
{
    public class GraphElement {
        public Dictionary<String, int> velocityNum = new Dictionary<string, int>();

        // Parent canvas
        protected Graph graph;

        // List of arcs
        public List<Arc> connections;

        public bool deleted = false;

        // Automatically handle wether or not this element is selected
        public bool selected {
            get => graph.selectedElements.Contains(this);
            set {
                if (deleted)
                    return;

                if (value) {
                    if (!graph.selectedElements.Contains(this)) {
                        graph.selectedElements.Add(this);
                    }
                } else {
                    if (graph.selectedElements.Contains(this)) {
                        graph.selectedElements.Remove(this);
                    }
                }
            }
        }

        protected GraphElement() {
            // Default constructor...
        }

        public GraphElement(Graph graph, String elementName, bool topLeft = false, string specialCases = null) {
            // Assign references
            this.graph = graph;
            // Initialize list of arcs
            connections = new List<Arc>();

            // Add this element to the graph's list of elements
            graph.elements.Add(this);
        }

        protected string findComponentName(string elementName, string specialcases) {
            if (elementName.Contains("System_MT_")) {
                string NewString = elementName.Remove(0, 10);
                return NewString;
            } else if (elementName.Contains("System_MR_") && specialcases == null) {
                string NewString = elementName.Remove(0, 10);
                return NewString;
            } else if (elementName.Contains("System_E_")) {
                string NewString = elementName.Remove(0, 9);
                return NewString;
            } else if (specialcases != null && elementName.Contains("System_MR_") && elementName.Contains("_Rack")) {
                return "Gear_Rack";
            } else if (specialcases != null && elementName.Contains("_Gear")) {
                return "Gear_Pinion";
            } else if (elementName.Contains("_Gear")) {
                return "Gear";
            } else // if (elementName.Contains("System_O_")
              {
                string NewString = elementName.Remove(0, 9);
                return NewString;
            }
        }

        // Deletes all of the WPF controls that make up this element - also deletes all arcs
        // WARNING - do not use this element after calling this function - it should be garbage collected immediately
        public void delete() {
            if (deleted)
                return;

            // Remove all arcs connected to this element
            List<Arc> connectionsCopy = new List<Arc>(connections);
            foreach (Arc a in connectionsCopy) {
                a.delete();
            }
            connectionsCopy.Clear();
            connections.Clear();

            graph.elements.Remove(this);

            deleted = true;
        }

        // Returns a string representation of this element
        public string serialize() {
            StringBuilder sb = new StringBuilder();

            sb.AppendLine("{");

            sb.AppendLine("\tname " + elementName);
            sb.AppendLine("\tx " + Canvas.GetLeft(miniCanvas));
            sb.AppendLine("\ty " + Canvas.GetTop(miniCanvas));

            // Modifiers
            sb.AppendLine("\tmodifiers {");
            foreach (KeyValuePair<ModifierType, int> modifier in modifiers) {
                if (modifier.Value > 0) {
                    if (modifier.Key == ModifierType.VELOCITY) {
                        sb.AppendLine("\t\t" + modifier.Key.ToString() + " " + modifier.Value);
                    } else {
                        sb.AppendLine("\t\t" + modifier.Key.ToString());
                    }
                }
            }
            sb.AppendLine("\t}");

            sb.AppendLine("}");

            return sb.ToString();
        }

        // Sets this element's velocity and shows/hides the arrows as needed
        public void setVelocity(int velocity) {
            if (!modifiers.ContainsKey(ModifierType.VELOCITY))
                return;

            upLeft.Visibility = Visibility.Hidden;
            upRight.Visibility = Visibility.Hidden;
            downLeft.Visibility = Visibility.Hidden;
            downRight.Visibility = Visibility.Hidden;
            leftUp.Visibility = Visibility.Hidden;
            leftDown.Visibility = Visibility.Hidden;
            rightUp.Visibility = Visibility.Hidden;
            rightDown.Visibility = Visibility.Hidden;

            if (velocity < 0 || velocity > 8)
                velocity = 0;

            modifiers[ModifierType.VELOCITY] = velocity;

            if (velocity == 1)
                upRight.Visibility = Visibility.Visible;
            else if (velocity == 2)
                rightUp.Visibility = Visibility.Visible;
            else if (velocity == 3)
                rightDown.Visibility = Visibility.Visible;
            else if (velocity == 4)
                downRight.Visibility = Visibility.Visible;
            else if (velocity == 5)
                downLeft.Visibility = Visibility.Visible;
            else if (velocity == 6)
                leftDown.Visibility = Visibility.Visible;
            else if (velocity == 7)
                leftUp.Visibility = Visibility.Visible;
            else if (velocity == 8)
                upLeft.Visibility = Visibility.Visible;
        }
    }
}

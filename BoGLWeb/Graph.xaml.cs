using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Windows;
using GraphSynth.Representation;

namespace AVL_Prototype_1
{
    /// <summary>
    /// Interaction logic for Graph.xaml
    /// </summary>
    public class Graph
    {
        public enum ModifierType {
            VELOCITY, FRICTION, PARALLEL, INERTIA, TOOTH_WEAR, DAMPING, STIFFNESS, MASS
        };

        // Variables for element connection
        public bool connectingMode = false;
        public bool draggingMode = false;
        public List<GraphElement> elements = null;
        public GraphElement connectingElement = null;
        public List<GraphElement> selectedElements = null;
        public List<Arc> arcs = null;
        public List<Arc> selectedArcs = null;

        public bool selectingInRect = false;

        public string previousState = "";
        public string futureState = "";

        public Graph()
        {
            elements = new List<GraphElement>();
            selectedElements = new List<GraphElement>();
            arcs = new List<Arc>();
            selectedArcs = new List<Arc>();
        }

        // Selects all elements
        public void selectAll()
        {
            elements.ForEach(element => element.selected = true);
            arcs.ForEach(arc => arc.selected = true);
        }

        // Deselects all elements
        public void deselectAll()
        {
            // Make a copy of the list so that we don't iterate through a list that we're modifying
            List<GraphElement> oldSelectedElements = new List<GraphElement>(selectedElements);
            List<Arc> oldSelectedArcs = new List<Arc>(selectedArcs);

            oldSelectedElements.ForEach(element => element.selected = false);
            oldSelectedArcs.ForEach(arc => arc.selected = false);
        }

        // Deletes all elements that are currently selected
        public void deleteSelected(bool overrideConfirm = false)
        {
            String currentState = serialize();

            bool confirmed = true;

            if (confirmed)
            {
                selectedArcs.ForEach(arc => arc.delete());
                selectedElements.ForEach(el => el.delete());

                selectedElements.Clear();
                selectedArcs.Clear();
            }

        }

        // Returns a string containing a text representation of the graph in saved form
        public string serialize()
        {
            StringBuilder sb = new StringBuilder();

            // Serialize elements
            sb.AppendLine();
            sb.AppendLine("[Elements]");

            elements.ForEach(element =>
            {
                sb.Append(element.serialize());
            });

            // Serialize arcs
            sb.AppendLine();
            sb.AppendLine("[Arcs]");

            arcs.ForEach(arc =>
            {
                sb.Append(arc.serialize());
            });

            return sb.ToString();
        }

        public string serializeSelected()
        {
            StringBuilder sb = new StringBuilder();

            // Serialize elements
            sb.AppendLine();
            sb.AppendLine("[Elements]");

            selectedElements.ForEach(element =>
            {
                sb.Append(element.serialize());
            });

            // Serialize arcs
            sb.AppendLine();
            sb.AppendLine("[Arcs]");

            selectedArcs.ForEach(arc =>
            {
                if (arc.element1.selected && arc.element2.selected)
                    sb.Append(arc.serialize(selectedElements));
            });

            return sb.ToString();
        }

        // Backup the current graph, try to deserialize. Return if successful
        public bool load(List<string> tokens)
        {
            try
            {
                deserialize(tokens);
                //MainWindow.unSaved = false;
                return true;
            }
            catch (Exception e)
            {
                // Should we do this?? Delete the partially loaded file
                //new List<GraphElement>(elements).ForEach(element => element.delete());
                //new List<Arc>(arcs).ForEach(arc => arc.delete());

                string str = "Error - corrupt input file or unable to load";
                if (e.Message != null)
                    str += ": " + e.Message;
                return false;
            }
        }

        // Reconstruct the graph based on a list of tokens
        public void deserialize(List<string> tokens)
        {
            List<string> elementTokens = new List<string>();
            List<string> arcTokens = new List<string>();

            // Parse the header (containing the matrix
            if (tokens[0] != "[Header]")
                throw new Exception("Expected '[Header]'");

            if (tokens[1] != "panX")
                throw new Exception("Expected 'panX'");

            double panX = Double.Parse(tokens[2]);

            if (tokens[3] != "panY")
                throw new Exception("Expected 'panX'");

            double panY = Double.Parse(tokens[4]);

            if (tokens[5] != "zoom")
                throw new Exception("Expected 'zoom'");

            double zoom = Double.Parse(tokens[6]);

            // Get all of the tokens belonging to elements
            if (tokens[7] != "[Elements]")
                throw new Exception("Expected '[Elements]'");

            int index = 8;

            try
            {
                while (tokens[index] != "[Arcs]")
                {
                    elementTokens.Add(tokens[index]);
                    index++;
                }
            }
            catch (IndexOutOfRangeException e)
            {
                throw new Exception("Expected '[Arcs]'");
            }

            parseElements(elementTokens);

            // Get all tokens belonging to arcs
            for (int i = index + 1; i < tokens.Count; i++)
            {
                arcTokens.Add(tokens[i]);
            }

            parseArcs(arcTokens);
        }

        public void parseElements(List<string> elementTokens, List<GraphElement> newGraphElements = null)
        {
            // Seperate all of the {}s into different elements
            List<List<string>> newElements = new List<List<String>>();
            List<string> currentElement = null;
            int level = 0;
            for (int i = 0; i < elementTokens.Count; i++)
            {
                string token = elementTokens[i];
                if (token == "{")
                {
                    if (level == 0)
                        currentElement = new List<string>();
                    else
                        currentElement.Add(token);

                    level++;
                }
                else if (token == "}")
                {
                    if (level == 0)
                    {
                        throw new Exception("Unexpected '}'");
                    }
                    if (level == 1)
                    {
                        newElements.Add(currentElement);
                        currentElement = null;
                    }
                    else
                    {
                        currentElement.Add(token);
                    }

                    level--;
                }
                else
                {
                    currentElement.Add(token);
                }
            }

            if (level > 0)
                throw new Exception("No matching '}' found");

            // Create all the GraphElements from the tokens
            newElements.ForEach(element => parseElement(element, newGraphElements));
        }

        public void parseElement(List<String> elementTokens, List<GraphElement> newGraphElements = null)
        {
            // Read in the name, x, and y
            if (elementTokens[0] != "name")
                throw new Exception("Element name expected");

            string name = elementTokens[1];

            if (elementTokens[2] != "x")
                throw new Exception("Element x expected");

            double x = Double.Parse(elementTokens[3]);

            if (elementTokens[4] != "y")
                throw new Exception("Element y expected");

            double y = Double.Parse(elementTokens[5]);

            // If we're pasting - then shift them over a bit
            if (newGraphElements != null)
            {
                x += 100;
                y += 100;
            }

            // Create the GraphElement
            GraphElement ge = new GraphElement(this, name, true);

            if (newGraphElements != null)
                newGraphElements.Add(ge);

            // Check modifiers
            if (elementTokens.Count > 6)
            {
                if (elementTokens[6] != "modifiers" || elementTokens[7] != "{")
                    throw new Exception("Invalid element");

                for (int i = 8; i < elementTokens.Count; i++)
                {
                    string token = elementTokens[i];
                    if (i == elementTokens.Count - 1)
                    {
                        if (token != "}")
                            throw new Exception("Element modifiers must end with '}'");
                    }
                    else
                    {
                        bool success = Enum.TryParse<ModifierType>(token, out ModifierType type);
                        if (!success)
                            throw new Exception("Unknown modifier '" + token + "'");
                        if (type == ModifierType.VELOCITY)
                        {
                            i++;
                            int num = Int32.Parse(elementTokens[i]);
                            if (num < 0 || num > 8)
                                throw new Exception("Invalid velocity number " + num + ", expected in range [0, 8]");

                            ge.setVelocity(num);
                        }
                        else
                        {
                            ge.modifiers[type] = 1;
                        }
                    }
                }
            }
        }

        public void parseArcs(List<string> arcTokens, List<GraphElement> newGraphElements = null, List<Arc> newGraphArcs = null)
        {
            // Seperate all of the {}s into different elements
            List<List<string>> newArcs = new List<List<String>>();
            List<string> currentArc = null;
            int level = 0;
            for (int i = 0; i < arcTokens.Count; i++)
            {
                string token = arcTokens[i];
                if (token == "{")
                {
                    if (level == 0)
                        currentArc = new List<string>();
                    else
                        throw new Exception("Unexpected '{'");

                    level++;
                }
                else if (token == "}")
                {
                    if (level == 0)
                    {
                        throw new Exception("Unexpected '}'");
                    }
                    if (level == 1)
                    {
                        newArcs.Add(currentArc);
                        currentArc = null;
                    }
                    else
                    {
                        throw new Exception("How did yo uget here this isn't supposed to hapen>????");
                    }

                    level--;
                }
                else
                {
                    currentArc.Add(token);
                }
            }

            if (level > 0)
                throw new Exception("No matching '}' found");

            // Create all the arcs from the tokens
            newArcs.ForEach(arc => parseArc(arc, newGraphElements, newGraphArcs));
        }

        public void parseArc(List<string> arcTokens, List<GraphElement> newGraphElements = null, List<Arc> newGraphArcs = null)
        {
            if (arcTokens.Count != 4 && arcTokens.Count != 6)
                throw new Exception("Unexpected number of tokens in arc");

            if (arcTokens[0] != "element1")
                throw new Exception("Expected 'element1'");

            List<GraphElement> relativeList = newGraphElements == null ? elements : newGraphElements;

            int index1 = Int32.Parse(arcTokens[1]);
            GraphElement element1 = relativeList[index1];

            if (arcTokens[2] != "element2")
                throw new Exception("Expected 'element2'");

            int index2 = Int32.Parse(arcTokens[3]);
            GraphElement element2 = relativeList[index2];

            if (index1 == index2)
                throw new Exception("Cannot use same element indices in arc");

            if (!element1.canConnectTo(element2))
                throw new Exception("Incompatible element types in arc");

            Arc a = new Arc(element1, element2);

            if (arcTokens.Count == 6)
            {
                if (arcTokens[4] != "velocity")
                    throw new Exception("Expected 'velocity'");

                if (!a.canHaveVelocity)
                    throw new Exception("Specific arc cannot have velocity");

                int num = Int32.Parse(arcTokens[5]);
                if (num < 0 || num > 8)
                    throw new Exception("Invalid velocity number " + num + ", expected in range [0, 8]");
            }

            if (newGraphArcs != null)
                newGraphArcs.Add(a);
        }

        // Updates the modifiers panel
        public void setSelectedElementsVelocity(int velocity)
        {
            String currentState = serialize();

            selectedElements.ForEach(element =>
            {
                if (element.modifiers.ContainsKey(ModifierType.VELOCITY))
                {
                    element.setVelocity(velocity);
                }
            });
            selectedArcs.ForEach(arc =>
            {
                if (arc.canHaveVelocity)
                {
                }
            });
        }

        /*
        private void modCollapseButton_Click(object sender, RoutedEventArgs e)
        {
            Visibility newVisibility = (modStackPanel.Visibility == Visibility.Visible) ? Visibility.Collapsed : Visibility.Visible;
            String newContent = (modStackPanel.Visibility == Visibility.Visible) ? "v" : "^";

            // Set all 4 graphs
            MainWindow mw = MainWindow.getInstance();
            new List<Graph> { mw.Graph_System, mw.Graph_BG1, mw.Graph_BG2, mw.Graph_BG3 }.ForEach(graph =>
            {
                graph.modStackPanel.Visibility = newVisibility;
                graph.modCollapseButton.Content = newContent;
            });
        }
        */

        // Helper functions
        public static double clamp(double value, double min, double max)
        {
            if (value < min)
                return min;
            else if (value > max)
                return max;
            else
                return value;
        }
    }
}

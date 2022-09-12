using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using GraphSynth.Representation;

namespace AVL_Prototype_1
{
    /// <summary>
    /// Interaction logic for Graph.xaml
    /// </summary>
    public partial class Graph : UserControl
    {
        // Variables for element connection
        public bool connectingMode = false;
        public bool draggingMode = false;
        public List<GraphElement> elements = null;
        public GraphElement connectingElement = null;
        public List<GraphElement> selectedElements = null;
        public List<Arc> arcs = null;
        public List<Arc> selectedArcs = null;

        public bool selectingInRect = false;
        public Point selectionRectStart;

        public string previousState = "";
        public string futureState = "";

        public Graph()
        {
            InitializeComponent();

            elements = new List<GraphElement>();
            selectedElements = new List<GraphElement>();
            arcs = new List<Arc>();
            selectedArcs = new List<Arc>();

            resetView();

            CompositionTarget.Rendering += Graph_HandleArrowKeys;
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

            if (!overrideConfirm && (MainWindow.getInstance().confirmDelete.IsChecked && (selectedElements.Count > 2 || selectedArcs.Count > 2)))
            {
                MessageBoxResult result = MessageBox.Show("You are about to delete multiple items. Are you sure? \n (This warning can be turned off in the Help menu.)", "Warning", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                if (result != MessageBoxResult.Yes)
                    confirmed = false;
            }

            if (confirmed)
            {
                selectedArcs.ForEach(arc => arc.delete());
                selectedElements.ForEach(el => el.delete());

                selectedElements.Clear();
                selectedArcs.Clear();
            }

            unsavedAction(currentState);

            updateModifiers();
        }

        public void clear()
        {
            // Delete the current graph
            deselectAll();
            new List<GraphElement>(elements).ForEach(element => element.delete());
            new List<Arc>(arcs).ForEach(arc => arc.delete());
            elements.Clear();
            arcs.Clear();

            resetView();

            previousState = "";
            futureState = "";

            if (this == MainWindow.getInstance().Graph_System)
                bigCanvas.AllowDrop = true;
            else
                bigCanvas.AllowDrop = false;
        }

        // Returns a string containing a text representation of the graph in saved form
        public string serialize()
        {
            StringBuilder sb = new StringBuilder();

            // Header
            sb.AppendLine("[Header]");
            sb.AppendLine("panX " + (mt.Matrix.OffsetX - (bigCanvas.ActualWidth / 2)));
            sb.AppendLine("panY " + (mt.Matrix.OffsetY - (bigCanvas.ActualHeight / 2)));
            sb.AppendLine("zoom " + (theCanvas.LayoutTransform as ScaleTransform).ScaleX);

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

        // Deserializes the given string, but pastes it over the current graph rather than reload the entire thing
        public void deserializePaste(string str)
        {
            List<string> tokens = MainWindow.tokenize(str);

            List<string> elementTokens = new List<string>();
            List<string> arcTokens = new List<string>();

            // Get all of the tokens belonging to elements
            if (tokens[0] != "[Elements]")
                throw new Exception("Expected '[Elements]'");

            int index = 1;

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

            List<GraphElement> newElements = new List<GraphElement>();

            parseElements(elementTokens, newElements);

            // Get all tokens belonging to arcs
            for (int i = index + 1; i < tokens.Count; i++)
            {
                arcTokens.Add(tokens[i]);
            }

            List<Arc> newArcs = new List<Arc>();

            parseArcs(arcTokens, newElements, newArcs);

            // Select all of the newly created elements/arcs
            deselectAll();
            newElements.ForEach(element => element.selected = true);
            newArcs.ForEach(arc => arc.selected = true);
        }

        // Backup the current graph, try to deserialize. Return if successful
        public bool load(List<string> tokens)
        {
            clear();

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
                MessageBox.Show(str);
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

            Matrix mat = new Matrix();
            mat.Translate(panX + (bigCanvas.ActualWidth / 2), panY + (bigCanvas.ActualHeight / 2));
            mt.Matrix = mat;

            theCanvas.LayoutTransform = new ScaleTransform(zoom, zoom);

            sliZoom.Value = 100 * zoom;

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
            GraphElement ge = new GraphElement(this, name, new Point(x, y), true);

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
                            ge.modifiedIndicator.Visibility = Visibility.Visible;
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
                a.setVelocity(num);
            }

            if (newGraphArcs != null)
                newGraphArcs.Add(a);
        }

        public void unsavedAction(string savedState)
        {
            MainWindow.unSaved = true;
            previousState = savedState;
            futureState = "";
            MainWindow.updateCommandButtons();
        }

        public void disableAllCheckBoxes()
        {
            modcheckbox_friction.IsEnabled = false;
            modcheckbox_friction.IsChecked = false;
            modcheckbox_friction.Opacity = 0.4;

            modcheckbox_parallel.IsEnabled = false;
            modcheckbox_parallel.IsChecked = false;
            modcheckbox_parallel.Opacity = 0.4;

            modcheckbox_inertia.IsEnabled = false;
            modcheckbox_inertia.IsChecked = false;
            modcheckbox_inertia.Opacity = 0.4;

            modcheckbox_tooth_wear.IsEnabled = false;
            modcheckbox_tooth_wear.IsChecked = false;
            modcheckbox_tooth_wear.Opacity = 0.4;

            modcheckbox_dampening.IsEnabled = false;
            modcheckbox_dampening.IsChecked = false;
            modcheckbox_dampening.Opacity = 0.4;

            modcheckbox_stiffness.IsEnabled = false;
            modcheckbox_stiffness.IsChecked = false;
            modcheckbox_stiffness.Opacity = 0.4;

            modcheckbox_mass.IsEnabled = false;
            modcheckbox_mass.IsChecked = false;
            modcheckbox_mass.Opacity = 0.4;

            // Velocity stuff
            modpanel_velocity.IsEnabled = false;
            modpanel_velocity.Opacity = 0.4;
            SolidColorBrush grayArrow = new SolidColorBrush(Colors.Gray);

            downLeft.Foreground = grayArrow;
            downRight.Foreground = grayArrow;
            rightUp.Foreground = grayArrow;
            rightDown.Foreground = grayArrow;
            leftUp.Foreground = grayArrow;
            leftDown.Foreground = grayArrow;
            upLeft.Foreground = grayArrow;
            upRight.Foreground = grayArrow;
        }

        // Updates the modifiers panel
        public void updateModifiers()
        {
            disableAllCheckBoxes();

            modExpanderIndicator.Visibility = Visibility.Hidden;

            // Loop through all selected objects
            selectedElements.ForEach(element =>
            {
                foreach (KeyValuePair<ModifierType, int> modifier in element.modifiers)
                {
                    // Velocity is special cased
                    if (modifier.Key == ModifierType.VELOCITY)
                    {
                        if (!modpanel_velocity.IsEnabled)
                        {
                            modpanel_velocity.IsEnabled = true;
                            modpanel_velocity.Opacity = 1;
                        }

                        if (modifier.Value > 0)
                        {
                            TextBlock arrow;

                            if (modifier.Value == 1)
                                arrow = upRight;
                            else if (modifier.Value == 2)
                                arrow = rightUp;
                            else if (modifier.Value == 3)
                                arrow = rightDown;
                            else if (modifier.Value == 4)
                                arrow = downRight;
                            else if (modifier.Value == 5)
                                arrow = downLeft;
                            else if (modifier.Value == 6)
                                arrow = leftDown;
                            else if (modifier.Value == 7)
                                arrow = leftUp;
                            else //if (modifier.Value == 8)
                                arrow = upLeft;

                            arrow.Foreground = new SolidColorBrush(Colors.Black);
                        }
                    }
                    else
                    {
                        if (modifier.Value == 1)
                            modExpanderIndicator.Visibility = Visibility.Visible;

                        CheckBox checkBox = getCheckBox(modifier.Key);
                        if (checkBox.IsEnabled)
                        {
                            if (checkBox.IsChecked != null)
                            {
                                if (checkBox.IsChecked != (modifier.Value == 1))
                                    checkBox.IsChecked = null;
                            }
                        }
                        else
                        {
                            checkBox.IsEnabled = true;
                            checkBox.Opacity = 1;
                            checkBox.IsChecked = modifier.Value == 1;
                        }
                    }
                }
            });
            selectedArcs.ForEach(arc =>
            {
                if (arc.canHaveVelocity)
                {
                    if (!modpanel_velocity.IsEnabled)
                    {
                        modpanel_velocity.IsEnabled = true;
                        modpanel_velocity.Opacity = 1;
                    }

                    if (arc.velocity > 0)
                    {
                        TextBlock arrow;

                        if (arc.velocity == 1)
                            arrow = upRight;
                        else if (arc.velocity == 2)
                            arrow = rightUp;
                        else if (arc.velocity == 3)
                            arrow = rightDown;
                        else if (arc.velocity == 4)
                            arrow = downRight;
                        else if (arc.velocity == 5)
                            arrow = downLeft;
                        else if (arc.velocity == 6)
                            arrow = leftDown;
                        else if (arc.velocity == 7)
                            arrow = leftUp;
                        else //if (arc.velocity == 8)
                            arrow = upLeft;

                        arrow.Foreground = new SolidColorBrush(Colors.Black);
                    }
                }
            });
        }

        public CheckBox getCheckBox(ModifierType type)
        {
            switch (type)
            {
                case ModifierType.FRICTION:
                    return modcheckbox_friction;
                case ModifierType.PARALLEL:
                    return modcheckbox_parallel;
                case ModifierType.INERTIA:
                    return modcheckbox_inertia;
                case ModifierType.TOOTH_WEAR:
                    return modcheckbox_tooth_wear;
                case ModifierType.DAMPING:
                    return modcheckbox_dampening;
                case ModifierType.STIFFNESS:
                    return modcheckbox_stiffness;
                case ModifierType.MASS:
                    return modcheckbox_mass;

                default:
                    return null;
            }
        }

        public ModifierType getModifierType(CheckBox checkBox)
        {
            // Switch-case doesn't work here
            if (checkBox == modcheckbox_friction)
                return ModifierType.FRICTION;
            else if (checkBox == modcheckbox_parallel)
                return ModifierType.PARALLEL;
            else if (checkBox == modcheckbox_inertia)
                return ModifierType.INERTIA;
            else if (checkBox == modcheckbox_tooth_wear)
                return ModifierType.TOOTH_WEAR;
            else if (checkBox == modcheckbox_dampening)
                return ModifierType.DAMPING;
            else if (checkBox == modcheckbox_stiffness)
                return ModifierType.STIFFNESS;
            else if (checkBox == modcheckbox_mass)
                return ModifierType.MASS;
            else
                return new ModifierType();  // Not sure how to handle the error case because null doesn't work
        }

        public void setSelectedElementsModifier(ModifierType type, bool enabled)
        {
            String currentState = serialize();

            selectedElements.ForEach(element =>
            {
                if (element.modifiers.ContainsKey(type))
                {
                    element.modifiers[type] = enabled ? 1 : 0;

                    if (enabled)
                    {
                        element.modifiedIndicator.Visibility = Visibility.Visible;
                    }
                    else
                    {
                        // Check if we should disable the asterisk
                        bool seen = false;
                        foreach (KeyValuePair<ModifierType, int> entry in element.modifiers)
                        {
                            if (entry.Value != 0 && entry.Key != ModifierType.VELOCITY)
                            {
                                seen = true;
                                break;
                            }
                        }
                        if (!seen)
                            element.modifiedIndicator.Visibility = Visibility.Hidden;
                    }
                }
            });

            unsavedAction(currentState);
        }

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
                    arc.setVelocity(velocity);
                }
            });

            unsavedAction(currentState);
        }

        // Resets the panning and zoom level of this graph
        public void resetView()
        {
            theCanvas.LayoutTransform = new ScaleTransform(1, 1);
            sliZoom.Value = 100;
            Matrix mat = new Matrix();
            mat.Translate(bigCanvas.ActualWidth / 2, bigCanvas.ActualHeight / 2);
            mt.Matrix = mat;
        }

        // Zooms the view to the given zoom level - using the given point as the expansion point
        public void zoomOnPoint(double x, double y, double newZoom)
        {
            double distToCenter = Math.Sqrt((x * x) + (y * y));
            double angleToCenter = Math.Atan2(-y, -x);

            double currentZoom = (theCanvas.LayoutTransform as ScaleTransform).ScaleX;

            double newDistToCenter = distToCenter * (newZoom / currentZoom);
            double distDiff = newDistToCenter - distToCenter;

            double xDiff = distDiff * Math.Cos(angleToCenter);
            double yDiff = distDiff * Math.Sin(angleToCenter);

            Matrix mat = mt.Matrix;
            mat.Translate(xDiff * currentZoom, yDiff * currentZoom);
            mt.Matrix = mat;

            theCanvas.LayoutTransform = new ScaleTransform(newZoom, newZoom);
        }

        private void Graph_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(typeof(string)) && MainWindow.getInstance().canAlways())
            {
                e.Effects = DragDropEffects.Copy;
            }
            else
            {
                e.Effects = DragDropEffects.None;
            }
            e.Handled = true;
        }

        /*
         * Create Element when ElementTemplate is dragged onto the canvas
         */
        private void Graph_Drop(object sender, DragEventArgs e)
        {
            String currentState = serialize();

            Canvas canvas = (Canvas)sender;

            List<GraphElement> newElements = new List<GraphElement>();
            List<Arc> newArcs = new List<Arc>();

            String etName = (String)e.Data.GetData(typeof(String));
            Point position = e.GetPosition(theCanvas);
            switch (etName)
            {
                case "System_MR_Gear_Pair":
                    Point pos1 = new Point(position.X - 75, position.Y);
                    Point pos2 = new Point(position.X + 75, position.Y);

                    GraphElement el1 = new GraphElement(this, "System_MR_Gear", pos1);
                    GraphElement el2 = new GraphElement(this, "System_MR_Gear", pos2);

                    newElements.Add(el1);
                    newElements.Add(el2);

                    Arc a1 = new Arc(el1, el2);
                    newArcs.Add(a1);

                    break;

                case "System_MR_Rack_Pinion":
                    pos1 = new Point(position.X, position.Y + 75);
                    pos2 = new Point(position.X, position.Y - 75);

                    el1 = new GraphElement(this, "System_MR_Rack", pos1, false);
                    el2 = new GraphElement(this, "System_MR_Gear", pos2, false);

                    newElements.Add(el1);
                    newElements.Add(el2);

                    a1 = new Arc(el1, el2);
                    newArcs.Add(a1);

                    break;

                default:
                    newElements.Add(new GraphElement(this, etName, position));

                    break;
            }

            if (!Keyboard.IsKeyDown(Key.LeftCtrl) && !Keyboard.IsKeyDown(Key.RightCtrl))
            {
                deselectAll();
            }

            newElements.ForEach(element => element.selected = true);
            newArcs.ForEach(arc => arc.selected = true);

            unsavedAction(currentState);
        }

        private void Graph_MouseDown(object sender, MouseButtonEventArgs e)
        {
            // Don't do anything if the mouse is over something else already
            foreach (GraphElement el in elements)
            {
                if (el.miniCanvas.IsMouseOver)
                    return;
            }
            foreach (Arc a in arcs)
            {
                if (a.hitBoxLine.IsMouseOver)
                    return;
            }
            if (modifierList.IsMouseOver)
                return;
            if (zoomPanel.IsMouseOver)
                return;

            if (e.ChangedButton == MouseButton.Left)
            {
                if (connectingMode)
                {
                    connectingMode = false;
                    connectingElement = null;
                    connectLine.Visibility = Visibility.Hidden;
                    bigCanvas.AllowDrop = true;
                    Mouse.OverrideCursor = null;    // HACK, to stop hand icon on other GraphElement's image
                    MainWindow.updateCommandButtons();
                }
                else if (!selectingInRect)
                {
                    // Start selecting a rectangle of elements
                    selectionRectStart = Mouse.GetPosition(theCanvas);
                    Canvas.SetLeft(selectionRectangle, selectionRectStart.X);
                    Canvas.SetTop(selectionRectangle, selectionRectStart.Y);

                    selectionRectangle.Width = 0;
                    selectionRectangle.Height = 0;

                    selectionRectangle.Visibility = Visibility.Visible;
                    selectingInRect = true;
                }
            }
        }

        private void Graph_MouseMove(object sender, MouseEventArgs e)
        {
            Canvas canvas = (Canvas)sender;

            if (connectingMode)
            {
                // check if you are inside a mini-canvas and snap the line to the two elements
                Point mousePos;
                double angle;
                foreach (GraphElement el in elements)
                {
                    if (el.miniCanvas.IsMouseOver)
                    {
                        if (!connectingElement.canConnectTo(el))
                            break;

                        double e1x = Canvas.GetLeft(connectingElement.miniCanvas) + Canvas.GetLeft(connectingElement.border) + connectingElement.border.Width / 2;
                        double e1y = Canvas.GetTop(connectingElement.miniCanvas) + Canvas.GetTop(connectingElement.border) + connectingElement.border.Height / 2;

                        double e2x = Canvas.GetLeft(el.miniCanvas) + Canvas.GetLeft(el.border) + el.border.Width / 2;
                        double e2y = Canvas.GetTop(el.miniCanvas) + Canvas.GetTop(el.border) + el.border.Height / 2;

                        angle = Math.Atan2(e2y - e1y, e2x - e1x);

                        Point el1Pos = connectingElement.rectangleIntersect(angle);
                        Point el2Pos = el.rectangleIntersect(angle + Math.PI);

                        connectLine.X1 = Canvas.GetLeft(connectingElement.miniCanvas) + el1Pos.X;
                        connectLine.Y1 = Canvas.GetTop(connectingElement.miniCanvas) + el1Pos.Y;
                        connectLine.X2 = Canvas.GetLeft(el.miniCanvas) + el2Pos.X;
                        connectLine.Y2 = Canvas.GetTop(el.miniCanvas) + el2Pos.Y;

                        // Snap the connect point circle to the correct spot as well
                        Canvas.SetLeft(el.connectPointCircle, el2Pos.X - el.connectPointCircle.Width / 2);
                        Canvas.SetTop(el.connectPointCircle, el2Pos.Y - el.connectPointCircle.Height / 2);

                        return;
                    }
                }

                mousePos = e.GetPosition(theCanvas);
                connectLine.X2 = mousePos.X;
                connectLine.Y2 = mousePos.Y;

                // Snap point along edge of element
                double ex = Canvas.GetLeft(connectingElement.miniCanvas) + Canvas.GetLeft(connectingElement.border) + connectingElement.border.Width / 2;
                double ey = Canvas.GetTop(connectingElement.miniCanvas) + Canvas.GetTop(connectingElement.border) + connectingElement.border.Height / 2;

                angle = Math.Atan2(mousePos.Y - ey, mousePos.X - ex);

                Point elPos = connectingElement.rectangleIntersect(angle);
                connectLine.X1 = Canvas.GetLeft(connectingElement.miniCanvas) + elPos.X;
                connectLine.Y1 = Canvas.GetTop(connectingElement.miniCanvas) + elPos.Y;
            }
            else if (selectingInRect)
            {
                Point mousePos = Mouse.GetPosition(theCanvas);
                Vector diff = mousePos - selectionRectStart;

                if (diff.X >= 0)
                {
                    Canvas.SetLeft(selectionRectangle, selectionRectStart.X);
                    selectionRectangle.Width = diff.X;
                }
                else
                {
                    Canvas.SetLeft(selectionRectangle, selectionRectStart.X + diff.X);
                    selectionRectangle.Width = -diff.X;
                }

                if (diff.Y >= 0)
                {
                    Canvas.SetTop(selectionRectangle, selectionRectStart.Y);
                    selectionRectangle.Height = diff.Y;
                }
                else
                {
                    Canvas.SetTop(selectionRectangle, selectionRectStart.Y + diff.Y);
                    selectionRectangle.Height = -diff.Y;
                }
            }
        }

        private void Graph_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                if (selectingInRect)
                {
                    if (!Keyboard.IsKeyDown(Key.LeftCtrl) && !Keyboard.IsKeyDown(Key.RightCtrl))
                        deselectAll();

                    // Calculate the position and size of the rectangle
                    Point rectPos = new Point(0, 0);
                    Point rectSize = new Point(0, 0);

                    Point mousePos = Mouse.GetPosition(theCanvas);
                    Vector diff = mousePos - selectionRectStart;

                    if (diff.X >= 0)
                    {
                        rectPos.X = selectionRectStart.X;
                        rectSize.X = diff.X;
                    }
                    else
                    {
                        rectPos.X = selectionRectStart.X + diff.X;
                        rectSize.X = -diff.X;
                    }

                    if (diff.Y >= 0)
                    {
                        rectPos.Y = selectionRectStart.Y;
                        rectSize.Y = diff.Y;
                    }
                    else
                    {
                        rectPos.Y = selectionRectStart.Y + diff.Y;
                        rectSize.Y = -diff.Y;
                    }

                    foreach (GraphElement el in elements)
                    {
                        Point elPos = new Point(Canvas.GetLeft(el.miniCanvas) + Canvas.GetLeft(el.border), Canvas.GetTop(el.miniCanvas) + Canvas.GetTop(el.border));
                        Point elSize = new Point(el.border.Width, el.border.Height);

                        if (doRectanglesIntersect(elPos, elSize, rectPos, rectSize))
                        {
                            el.selected = !el.selected;
                        }
                    }

                    foreach (Arc a in arcs)
                    {
                        if (doRectangleArcIntersect(rectPos, rectSize, a.connectionLine))
                        {
                            a.selected = !a.selected;
                        }
                    }

                    selectionRectangle.Visibility = Visibility.Hidden;
                    selectingInRect = false;
                }
            }
        }

        // Keep the center consistent
        private void Graph_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            double xDiff = e.NewSize.Width - e.PreviousSize.Width;
            double yDiff = e.NewSize.Height - e.PreviousSize.Height;

            Matrix mat = mt.Matrix;
            mat.Translate(xDiff / 2, yDiff / 2);
            mt.Matrix = mat;
        }

        private void Graph_HandleArrowKeys(object sender, EventArgs e)
        {
            // Check for user keypresses
            int dx = 0;
            int dy = 0;
            if (Keyboard.IsKeyDown(Key.Left))
                dx--;
            if (Keyboard.IsKeyDown(Key.Right))
                dx++;
            if (Keyboard.IsKeyDown(Key.Up))
                dy--;
            if (Keyboard.IsKeyDown(Key.Down))
                dy++;

            // Pan the screen
            if (dx != 0 || dy != 0)
            {
                // TODO - make speed based on monitor refresh rate
                double speed = 3;

                Matrix mat = mt.Matrix;
                mat.Translate(dx * speed, dy * speed);
                mt.Matrix = mat;
            }
        }

        private void ModCheckBox_Click(object sender, RoutedEventArgs e)
        {
            CheckBox checkBox = (CheckBox)sender;
            ModifierType modifier = getModifierType(checkBox);
            setSelectedElementsModifier(modifier, checkBox.IsChecked == true);

            // Check to see if we need the indicator
            if (checkBox.IsChecked == true)
            {
                modExpanderIndicator.Visibility = Visibility.Visible;
            }
            else
            {
                modExpanderIndicator.Visibility = Visibility.Hidden;

                foreach (object obj in modStackPanel.Children)
                {
                    if (obj is CheckBox)
                    {
                        CheckBox box = (CheckBox)obj;
                        if (box.IsChecked == true)
                        {
                            modExpanderIndicator.Visibility = Visibility.Visible;
                            break;
                        }
                    }
                }
            }
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

        private void velocityModifier_MouseUp(object sender, MouseEventArgs e)
        {
            TextBlock clickedArrow = (TextBlock)sender;
            SolidColorBrush grayArrow = new SolidColorBrush(Colors.Gray);

            downLeft.Foreground = grayArrow;
            downRight.Foreground = grayArrow;
            rightUp.Foreground = grayArrow;
            rightDown.Foreground = grayArrow;
            leftUp.Foreground = grayArrow;
            leftDown.Foreground = grayArrow;
            upLeft.Foreground = grayArrow;
            upRight.Foreground = grayArrow;

            clickedArrow.Foreground = new SolidColorBrush(Colors.Black);

            // Get which one the user clicked on
            int velocity = 0;
            if (clickedArrow == upRight)
                velocity = 1;
            else if (clickedArrow == rightUp)
                velocity = 2;
            else if (clickedArrow == rightDown)
                velocity = 3;
            else if (clickedArrow == downRight)
                velocity = 4;
            else if (clickedArrow == downLeft)
                velocity = 5;
            else if (clickedArrow == leftDown)
                velocity = 6;
            else if (clickedArrow == leftUp)
                velocity = 7;
            else if (clickedArrow == upLeft)
                velocity = 8;

            setSelectedElementsVelocity(velocity);
        }

        private void clearVelocity_Click(object sender, RoutedEventArgs e)
        {
            SolidColorBrush grayArrow = new SolidColorBrush(Colors.Gray);

            downLeft.Foreground = grayArrow;
            downRight.Foreground = grayArrow;
            rightUp.Foreground = grayArrow;
            rightDown.Foreground = grayArrow;
            leftUp.Foreground = grayArrow;
            leftDown.Foreground = grayArrow;
            upLeft.Foreground = grayArrow;
            upRight.Foreground = grayArrow;

            setSelectedElementsVelocity(0);
        }

        //zoom in / zoom out
        private void sliZoom_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            // Make sure the control's are all ready.
            if (!IsInitialized) return;

            // Get the scale factor as a fraction 0.25 - 2.00.
            double currentZoom = (theCanvas.LayoutTransform as ScaleTransform).ScaleX;
            double newZoom = (double)(sliZoom.Value / 100.0);

            // Calculate graph's center pos

            // Convert from pixel-space into unit-space
            double unitWidth = bigCanvas.ActualWidth / currentZoom;
            double unitHeight = bigCanvas.ActualHeight / currentZoom;
            double unitOriginX = mt.Matrix.OffsetX / currentZoom;
            double unitOriginY = mt.Matrix.OffsetY / currentZoom;

            double centerX = (unitWidth / 2) - unitOriginX;
            double centerY = (unitHeight / 2) - unitOriginY;

            // Scale the graph.
            zoomOnPoint(centerX, centerY, newZoom);
        }

        //pan and zoom reset 
        private void canvasReset(Object sender, EventArgs e)
        {
            Button btnSender = (Button)sender;
            if (btnSender == zoomFit)
            {
                resetView();
            }
        }

        //panning

        private bool isPanning;
        Point _last;

        protected override void OnMouseRightButtonDown(MouseButtonEventArgs e)
        {
            base.OnMouseRightButtonDown(e);
            CaptureMouse();
            Mouse.OverrideCursor = Cursors.SizeAll;
            //_last = e.GetPosition(canvas);
            _last = e.GetPosition(this);
            isPanning = true;
        }

        protected override void OnMouseRightButtonUp(MouseButtonEventArgs e)
        {
            base.OnMouseRightButtonUp(e);
            // change mouse type
            Mouse.OverrideCursor = null;
            ReleaseMouseCapture();
            _last = e.GetPosition(theCanvas);
            isPanning = false;
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            if (!isPanning)
                return;

            base.OnMouseMove(e);
            if (e.RightButton == MouseButtonState.Pressed && IsMouseCaptured)
            {
                var pos = e.GetPosition(this);
                var matrix = mt.Matrix; // it's a struct
                matrix.Translate(pos.X - _last.X, pos.Y - _last.Y);
                mt.Matrix = matrix;
                _last = pos;
            }
        }

        private void generateButton_Clicked(object sender, EventArgs e)
        {
            MainWindow.getInstance().generateBondGraph();
        }

        // Let users zoom using the mouse wheel
        private void Graph_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            Point pos = e.GetPosition(theCanvas);
            double x = pos.X;
            double y = pos.Y;

            double newZoom = (theCanvas.LayoutTransform as ScaleTransform).ScaleX + (Math.Sign(e.Delta) * 0.05);
            newZoom = clamp(newZoom, 0.25, 1.75);

            zoomOnPoint(x, y, newZoom);

            sliZoom.Value = newZoom * 100;
        }

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

        // Code taken from https://www.geeksforgeeks.org/find-two-rectangles-overlap/
        public static bool doRectanglesIntersect(Point rect1XY, Point rect1WH, Point rect2XY, Point rect2WH)
        {
            double l1x = rect1XY.X;
            double l1y = rect1XY.Y;
            double r1x = rect1XY.X + rect1WH.X;
            double r1y = rect1XY.Y + rect1WH.Y;

            double l2x = rect2XY.X;
            double l2y = rect2XY.Y;
            double r2x = rect2XY.X + rect2WH.X;
            double r2y = rect2XY.Y + rect2WH.Y;

            if (l1x > r2x || l2x > r1x)
                return false;

            if (l1y > r2y || l2y > r1y)
                return false;

            return true;
        }

        public static bool doRectangleArcIntersect(Point rectXY, Point rectWH, Line line)
        {
            double lx = rectXY.X;
            double ly = rectXY.Y;
            double rx = rectXY.X + rectWH.X;
            double ry = rectXY.Y + rectWH.Y;

            // Check if first point is inside box
            if ((line.X1 >= lx) && (line.X1 <= rx) && (line.Y1 >= ly) && (line.Y1 <= ry))
                return true;

            // Check if second point is inside box
            if ((line.X2 >= lx) && (line.X2 <= rx) && (line.Y2 >= ly) && (line.Y2 <= ry))
                return true;

            // Neither of the points are in the box!
            // Check to see if the line intersects the 4 edges of the box
            Line tempLine = new Line();

            // Top line
            tempLine.X1 = lx;
            tempLine.Y1 = ly;
            tempLine.X2 = rx;
            tempLine.Y2 = ly;
            if (doLinesIntersect(line, tempLine))
                return true;

            // Right line
            tempLine.X1 = rx;
            tempLine.Y1 = ly;
            tempLine.X2 = rx;
            tempLine.Y2 = ry;
            if (doLinesIntersect(line, tempLine))
                return true;

            // Bottom line
            tempLine.X1 = lx;
            tempLine.Y1 = ry;
            tempLine.X2 = rx;
            tempLine.Y2 = ry;
            if (doLinesIntersect(line, tempLine))
                return true;

            // Left line
            tempLine.X1 = lx;
            tempLine.Y1 = ly;
            tempLine.X2 = lx;
            tempLine.Y2 = ry;
            if (doLinesIntersect(line, tempLine))
                return true;

            return false;
        }

        /*
         * LINE INTERSECTION CODE TAKEN FROM
         * https://www.geeksforgeeks.org/check-if-two-given-line-segments-intersect/
         */

        // Given three colinear points p, q, r, the function checks if
        // point q lies on line segment 'pr'
        public static bool onSegment(Point p, Point q, Point r)
        {
            if (q.X <= Math.Max(p.X, r.X) && q.X >= Math.Min(p.X, r.X) &&
                q.Y <= Math.Max(p.Y, r.Y) && q.Y >= Math.Min(p.Y, r.Y))
                return true;

            return false;
        }

        // To find orientation of ordered triplet (p, q, r).
        // The function returns following values
        // 0 --> p, q and r are colinear
        // 1 --> Clockwise
        // 2 --> Counterclockwise
        static int orientation(Point p, Point q, Point r)
        {
            // See https://www.geeksforgeeks.org/orientation-3-ordered-points/
            // for details of below formula.
            double val = (q.Y - p.Y) * (r.X - q.X) -
                    (q.X - p.X) * (r.Y - q.Y);

            if (val == 0) return 0; // colinear

            return (val > 0) ? 1 : 2; // clock or counterclock wise
        }

        // The main function that returns true if line segment 'p1q1'
        // and 'p2q2' intersect.
        static Boolean doLinesIntersect(Line l1, Line l2)
        {
            Point p1 = new Point(l1.X1, l1.Y1);
            Point q1 = new Point(l1.X2, l1.Y2);
            Point p2 = new Point(l2.X1, l2.Y1);
            Point q2 = new Point(l2.X2, l2.Y2);

            // Find the four orientations needed for general and
            // special cases
            int o1 = orientation(p1, q1, p2);
            int o2 = orientation(p1, q1, q2);
            int o3 = orientation(p2, q2, p1);
            int o4 = orientation(p2, q2, q1);

            // General case
            if (o1 != o2 && o3 != o4)
                return true;

            // Special Cases
            // p1, q1 and p2 are colinear and p2 lies on segment p1q1
            if (o1 == 0 && onSegment(p1, p2, q1)) return true;

            // p1, q1 and q2 are colinear and q2 lies on segment p1q1
            if (o2 == 0 && onSegment(p1, q2, q1)) return true;

            // p2, q2 and p1 are colinear and p1 lies on segment p2q2
            if (o3 == 0 && onSegment(p2, p1, q2)) return true;

            // p2, q2 and q1 are colinear and q1 lies on segment p2q2
            if (o4 == 0 && onSegment(p2, q1, q2)) return true;

            return false; // Doesn't fall in any of the above cases
        }

        private void CausalOptions_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            MainWindow.getInstance().causaloptions_selection();
        }
    }
}

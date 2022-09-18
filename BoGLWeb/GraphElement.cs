using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;

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
        public Dictionary<ModifierType, int> modifiers;
        public String nodeName = null;
        public List<String> labels;

        // Variables for dragging elements
        protected Point anchorPoint;
        protected Point currentPoint;
        protected bool isInDrag = false;

        // Screen elements that make up this graph element
        public Canvas miniCanvas;
        public Border border;
        public Image image;
        public Ellipse connectPointCircle;
        public TextBlock connectPointX;
        public TextBlock modifiedIndicator;

        // Applicable velocity modifiers
        public List<TextBlock> velocities;
        public TextBlock upLeft;   // 1
        public TextBlock upRight;  // 2
        public TextBlock leftUp;   // 3
        public TextBlock leftDown; // 4
        public TextBlock rightUp;  // 5
        public TextBlock rightDown;// 6
        public TextBlock downLeft; // 7
        public TextBlock downRight;// 8

        public Dictionary<String, int> velocityNum = new Dictionary<string, int>();

        // Different border brushes
        protected SolidColorBrush unselectedBorderBrush;
        protected SolidColorBrush selectedBorderBrush;

        // Parent canvas
        protected Graph graph;
        protected Canvas canvas;

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
                    border.BorderBrush = selectedBorderBrush;

                    if (!graph.selectedElements.Contains(this))
                    {
                        graph.selectedElements.Add(this);
                        graph.updateModifiers();
                    }
                }
                else
                {
                    border.BorderBrush = unselectedBorderBrush;

                    if (graph.selectedElements.Contains(this))
                    {
                        graph.selectedElements.Remove(this);
                        graph.updateModifiers();
                    }
                }

                MainWindow.updateCommandButtons();
            }
        }

        protected GraphElement()
        {
            // Default constructor...
        }

        public GraphElement(Graph graph, String elementName, Point position, bool topLeft = false, string specialCases = null)
        {
            // Assign references
            this.graph = graph;
            canvas = graph.theCanvas;
            this.elementName = elementName;           
            this.componentName = findComponentName(elementName, specialCases);
            labels = new List<String>();

            labels.Add(componentName);

            // Initialize list of arcs
            connections = new List<Arc>();

            // Initialize the list of modifiers
            modifiers = new Dictionary<ModifierType, int>();
            try
            {
                MainWindow.elementModifiers[elementName].ForEach(modifier =>
                {
                    modifiers[modifier] = 0;
                });
            }
            catch (KeyNotFoundException e)
            {
                // Do nothing
            }

            // Setup element image
            ImageSource imageSource = MainWindow.imageSources[elementName];

            image = new Image();

            image.Source = imageSource;

            double oldWidth = image.Source.Width;
            double oldHeight = image.Source.Height;

            double aspectRatio = oldWidth / oldHeight;

            double imageSize = (elementName == "System_E_Junction") ? Math.Ceiling(imageSizeMax / 5) : imageSizeMax;

            if (aspectRatio > 1)
            {
                image.Width = imageSize;
                image.Height = Math.Ceiling(imageSize / aspectRatio);
            }
            else if (aspectRatio < 1)
            {
                image.Width = Math.Ceiling(imageSize * aspectRatio);
                image.Height = imageSize;
            }
            else // perfect square
            {
                image.Width = imageSize;
                image.Height = imageSize;
            }

            image.MouseDown += Image_MouseDown;
            image.MouseUp += Image_MouseUp;
            image.MouseMove += Image_MouseMove;
            image.MouseEnter += Image_MouseEnter;
            image.MouseLeave += Image_MouseLeave;
            image.Cursor = Cursors.Hand;

            RenderOptions.SetBitmapScalingMode(image, BitmapScalingMode.HighQuality);

            // Setup the border surrounding image
            border = new Border();

            // Define different border brushes
            unselectedBorderBrush = new SolidColorBrush(Colors.Black);
            unselectedBorderBrush.Opacity = 0.3;
            selectedBorderBrush = new SolidColorBrush(Colors.Blue);
            selectedBorderBrush.Opacity = 0.7;

            border.BorderBrush = unselectedBorderBrush;
            border.BorderThickness = new Thickness(borderThickness);
            border.Width = image.Width + border.BorderThickness.Left + border.BorderThickness.Right;
            border.Height = image.Height + border.BorderThickness.Top + border.BorderThickness.Bottom;
            border.Child = image;

            // Setup miniCanvas - area that user hovers over to start connections
            miniCanvas = new Canvas();
            miniCanvas.Background = new SolidColorBrush(Colors.Transparent);
            miniCanvas.Width = image.Width + imageCanvasBorder + border.BorderThickness.Left + border.BorderThickness.Right;
            miniCanvas.Height = image.Height + imageCanvasBorder + border.BorderThickness.Top + border.BorderThickness.Bottom;

            miniCanvas.MouseEnter += MiniCanvas_MouseEnter;
            miniCanvas.MouseLeave += MiniCanvas_MouseLeave;
            miniCanvas.MouseMove += MiniCanvas_MouseMove;
            miniCanvas.MouseDown += MiniCanvas_MouseDown;
            miniCanvas.Children.Add(border);
            Canvas.SetLeft(border, imageCanvasBorder / 2);
            Canvas.SetTop(border, imageCanvasBorder / 2);

            // Create the connection point circle
            connectPointCircle = new Ellipse();

            connectPointCircle.Width = 2 * connectPointCircleRadius;
            connectPointCircle.Height = 2 * connectPointCircleRadius;
            connectPointCircle.Fill = new SolidColorBrush(Colors.Green);
            connectPointCircle.IsHitTestVisible = false;
            connectPointCircle.Visibility = Visibility.Hidden;

            miniCanvas.Children.Add(connectPointCircle);

            //Create the X that indicates the element cannot form a connection
            connectPointX = new TextBlock();
            connectPointX.Text = "X";
            connectPointX.FontSize = 15;
            connectPointX.FontWeight = FontWeights.Bold;
            connectPointX.Foreground = new SolidColorBrush(Colors.Red);
            connectPointX.IsHitTestVisible = false;
            connectPointX.Visibility = Visibility.Hidden;

            miniCanvas.Children.Add(connectPointX);

            if (topLeft)
            {
                Canvas.SetLeft(miniCanvas, position.X);
                Canvas.SetTop(miniCanvas, position.Y);
            }
            else
            {
                Canvas.SetLeft(miniCanvas, position.X - (miniCanvas.Width / 2));
                Canvas.SetTop(miniCanvas, position.Y - (miniCanvas.Height / 2));
            }

            // Create the modifier asterisk indicator
            modifiedIndicator = new TextBlock();
            modifiedIndicator.Text = "*";
            modifiedIndicator.FontSize = 32;
            modifiedIndicator.IsHitTestVisible = false;
            modifiedIndicator.Visibility = Visibility.Hidden;

            miniCanvas.Children.Add(modifiedIndicator);
            Canvas.SetLeft(modifiedIndicator, Canvas.GetLeft(border) + border.Width - 15);
            Canvas.SetTop(modifiedIndicator, Canvas.GetTop(border) - 9);

            if (modifiers.ContainsKey(ModifierType.VELOCITY))
            {
                // Create the velocity arrows
                upLeft = new TextBlock();
                upLeft.Text = "\u2ba2";
                upRight = new TextBlock();
                upRight.Text = "\u2ba3";
                leftUp = new TextBlock();
                leftUp.Text = "\u2ba4";
                leftDown = new TextBlock();
                leftDown.Text = "\u2ba6";
                rightUp = new TextBlock();
                rightUp.Text = "\u2ba5";
                rightDown = new TextBlock();
                rightDown.Text = "\u2ba7";
                downLeft = new TextBlock();
                downLeft.Text = "\u2ba0";
                downRight = new TextBlock();
                downRight.Text = "\u2ba1";

                SolidColorBrush arrowBrush = new SolidColorBrush(Colors.Black);

                velocities = new List<TextBlock>();
                velocities.Add(upLeft);
                velocities.Add(upRight);
                velocities.Add(leftUp);
                velocities.Add(leftDown);
                velocities.Add(rightUp);
                velocities.Add(rightDown);
                velocities.Add(downLeft);
                velocities.Add(downRight);

                foreach (TextBlock v in velocities)
                {
                    v.Foreground = arrowBrush;
                    v.FontSize = 50;
                    v.IsHitTestVisible = false;
                    v.Visibility = Visibility.Hidden;
                    miniCanvas.Children.Add(v);
                }

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

                Canvas.SetLeft(upLeft, Canvas.GetLeft(border) + (border.Width / 2) - (arrowHorizWidth / 2) - arrowHorizDist);
                Canvas.SetTop(upLeft, Canvas.GetTop(border) - 54);

                Canvas.SetLeft(upRight, Canvas.GetLeft(border) + (border.Width / 2) - (arrowHorizWidth / 2) + arrowHorizDist);
                Canvas.SetTop(upRight, Canvas.GetTop(border) - 54);

                Canvas.SetLeft(leftUp, Canvas.GetLeft(border) - 32);
                Canvas.SetTop(leftUp, Canvas.GetTop(border) + (border.Height / 2) - (arrowVertHeight / 2) - arrowVertDist);

                Canvas.SetLeft(leftDown, Canvas.GetLeft(border) - 32);
                Canvas.SetTop(leftDown, Canvas.GetTop(border) + (border.Height / 2) - (arrowVertHeight / 2) + arrowVertDist);

                Canvas.SetLeft(rightUp, Canvas.GetLeft(border) + border.Width - 4);
                Canvas.SetTop(rightUp, Canvas.GetTop(border) + (border.Height / 2) - (arrowVertHeight / 2) - arrowVertDist);

                Canvas.SetLeft(rightDown, Canvas.GetLeft(border) + border.Width - 4);
                Canvas.SetTop(rightDown, Canvas.GetTop(border) + (border.Height / 2) - (arrowVertHeight / 2) + arrowVertDist);

                Canvas.SetLeft(downLeft, Canvas.GetLeft(border) + (border.Width / 2) - (arrowHorizWidth / 2) - arrowHorizDist);
                Canvas.SetTop(downLeft, Canvas.GetTop(border) + border.Height - 18);

                Canvas.SetLeft(downRight, Canvas.GetLeft(border) + (border.Width / 2) - (arrowHorizWidth / 2) + arrowHorizDist);
                Canvas.SetTop(downRight, Canvas.GetTop(border) + border.Height - 18);
            }

            // Add miniCanvas to graph's canvas
            canvas.Children.Add(miniCanvas);

            // Set the canvas Z-Index properties
            // TODO - this won't fully work - fix this
            Canvas.SetZIndex(miniCanvas, 2);
            Canvas.SetZIndex(border, 5);
            Canvas.SetZIndex(image, 6);
            Canvas.SetZIndex(connectPointCircle, 8);

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

            // Remove all screen elements
            // Is this enough? Will this properly free all of the memory?
            border.Child = null;
            miniCanvas.Children.Clear();
            canvas.Children.Remove(miniCanvas);

            border = null;
            miniCanvas = null;
            image = null;
            connectPointCircle = null;
            connectPointX = null;

            deleted = true;
        }

        // Get the point along the perimeter that intersects with the given angle
        // Point is relative to the miniCanvas
        // WARNING - weird trigonometry ahead
        public Point rectangleIntersect(double angle)
        {
            Point point = new Point(Canvas.GetLeft(border) + border.Width / 2, Canvas.GetTop(border) + border.Height / 2);

            // MAKE SURE that angle is in range [0, 2*pi)
            while (angle < 0)
                angle = angle + (2 * Math.PI);
            while (angle >= (2 * Math.PI))
                angle = angle - (2 * Math.PI);

            // Based on the dimensions of the rectangles, the intersect face may change
            double baseAngleCutoff = Math.Asin(border.Width / Math.Sqrt((border.Height) * (border.Height) + (border.Width) * (border.Width)));

            // Calculate what angles will intersect with the exact corners of the rectangle
            double angleCutoffBottomRight = (Math.PI / 2) - baseAngleCutoff;
            double angleCutoffBottomLeft = Math.PI - angleCutoffBottomRight;
            double angleCutoffTopLeft = Math.PI + angleCutoffBottomRight;
            double angleCutoffTopRight = (2 * Math.PI) - angleCutoffBottomRight;

            if ((angle < angleCutoffBottomRight) || (angle > angleCutoffTopRight))
            {
                point.X += border.Width / 2;
                point.Y += Math.Tan(angle) * (border.Width / 2);
            }
            else if (angle < angleCutoffBottomLeft)
            {
                point.X -= Math.Tan(angle - (Math.PI / 2)) * (border.Height / 2);
                point.Y += border.Height / 2;
            }
            else if (angle < angleCutoffTopLeft)
            {
                point.X -= border.Width / 2;
                point.Y -= Math.Tan(angle) * (border.Width / 2);
            }
            else // (angle < angleCutoffTopRight)
            {
                point.X += Math.Tan(angle - (Math.PI / 2)) * (border.Height / 2);
                point.Y -= border.Height / 2;
            }

            point.X = Math.Round(point.X);
            point.Y = Math.Round(point.Y);
            return point;
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

            // Element must be compatible with the other element
            if (!MainWindow.elementCompatibility[elementName].Contains(other.elementName))
                return false;

            // We're good
            return true;
        }

        public bool canAcceptConnections()
        {
            if (MainWindow.maxConnections.ContainsKey(elementName))
            {
                return MainWindow.maxConnections[elementName] > connections.Count;
            }

            return true;
        }

        // Returns a string representation of this element
        public string serialize()
        {
            StringBuilder sb = new StringBuilder();

            sb.AppendLine("{");

            sb.AppendLine("\tname " + elementName);
            sb.AppendLine("\tx " + Canvas.GetLeft(miniCanvas));
            sb.AppendLine("\ty " + Canvas.GetTop(miniCanvas));

            // Modifiers
            sb.AppendLine("\tmodifiers {");
            foreach (KeyValuePair<ModifierType, int> modifier in modifiers)
            {
                if (modifier.Value > 0)
                {
                    if (modifier.Key == ModifierType.VELOCITY)
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

        protected string preMovedState = "";

        // Element dragging code mostly taken from:
        //   https://stackoverflow.com/questions/6284056/dragging-a-wpf-user-control
        // Modified to use Canvas.SetXXXX rather than using a TranslateTransform
        public void Image_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (deleted)
                return;

            if (e.ChangedButton == MouseButton.Left)
            {
                if (!graph.connectingMode)
                {
                    if (Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl))
                    {
                        if (selected)
                        {
                            selected = false;
                            e.Handled = true;
                            return;
                        }
                        else
                        {
                            selected = true;
                        }

                    }
                    else
                    {
                        if (!selected)
                        {
                            graph.deselectAll();
                            selected = true;
                        }
                    }

                    anchorPoint = e.GetPosition(canvas);
                    image.CaptureMouse();
                    isInDrag = true;
                    graph.draggingMode = true;
                    graph.bigCanvas.AllowDrop = false;
                    image.Cursor = Cursors.SizeAll;
                    preMovedState = graph.serialize();
                    e.Handled = true;

                    MainWindow.updateCommandButtons();
                }
            }
        }

        public void Image_MouseMove(object sender, MouseEventArgs e)
        {
            if (deleted)
                return;

            if (isInDrag)
            {
                currentPoint = e.GetPosition(canvas);

                // Get the diff of the position
                double leftDiff = currentPoint.X - anchorPoint.X;
                double topDiff = currentPoint.Y - anchorPoint.Y;

                // Translate all elements
                List<GraphElement> elementsToMove = new List<GraphElement>();
                foreach (GraphElement el in graph.selectedElements)
                {
                    elementsToMove.Add(el);
                }

                if (!selected)
                    elementsToMove.Add(this);

                foreach (GraphElement el in elementsToMove)
                {
                    Canvas.SetLeft(el.miniCanvas, Canvas.GetLeft(el.miniCanvas) + leftDiff);
                    Canvas.SetTop(el.miniCanvas, Canvas.GetTop(el.miniCanvas) + topDiff);

                    // Move all line connection points
                    foreach (Arc a in el.connections)
                    {
                        a.updateLinePostion();
                    }

                    // If we're connecting this element, move the line connect point
                    if (graph.connectingMode && (el == graph.connectingElement))
                    {
                        graph.connectLine.X2 += leftDiff;
                        graph.connectLine.Y2 += topDiff;
                    }
                }

                anchorPoint = currentPoint;
            }
        }

        public void Image_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (deleted)
                return;

            if (e.ChangedButton == MouseButton.Left)
            {
                if (isInDrag)
                {
                    Image element = (Image)sender;
                    element.ReleaseMouseCapture();
                    isInDrag = false;
                    graph.draggingMode = false;
                    graph.bigCanvas.AllowDrop = true;
                    image.Cursor = Cursors.Hand;
                    e.Handled = true;

                    graph.unsavedAction(preMovedState);
                    preMovedState = "";

                    MainWindow.updateCommandButtons();
                }
            }
        }

        protected void Image_MouseEnter(object sender, MouseEventArgs e)
        {
            if (deleted)
                return;

            //border.BorderBrush = new SolidColorBrush(Colors.Transparent);
            if (!graph.connectingMode)
            {
                connectPointCircle.Visibility = Visibility.Hidden;
                connectPointX.Visibility = Visibility.Hidden;
            }
        }

        protected void Image_MouseLeave(object sender, MouseEventArgs e)
        {
            if (deleted)
                return;

            //border.BorderBrush = new SolidColorBrush(Colors.BlueViolet);
            if (!image.IsMouseOver && !graph.selectingInRect)
            {
                if (graph.connectingMode ? graph.connectingElement.canConnectTo(this) : canAcceptConnections())
                    connectPointCircle.Visibility = Visibility.Visible;
                else if (!(this == graph.connectingElement && graph.connectingMode))
                    connectPointX.Visibility = Visibility.Visible;
            }
        }

        protected void MiniCanvas_MouseEnter(object sender, MouseEventArgs e)
        {
            if (deleted)
                return;

            //border.BorderBrush = new SolidColorBrush(Colors.BlueViolet);
            if (!image.IsMouseOver && !graph.selectingInRect)
            {
                if (graph.connectingMode ? graph.connectingElement.canConnectTo(this) : canAcceptConnections())
                    connectPointCircle.Visibility = Visibility.Visible;
                else if (!(this == graph.connectingElement && graph.connectingMode))
                    connectPointX.Visibility = Visibility.Visible;
            }
        }

        protected void MiniCanvas_MouseLeave(object sender, MouseEventArgs e)
        {
            if (deleted)
                return;

            //border.BorderBrush = new SolidColorBrush(Colors.Transparent);
            connectPointCircle.Visibility = Visibility.Hidden;
            connectPointX.Visibility = Visibility.Hidden;
        }

        protected void MiniCanvas_MouseMove(object sender, MouseEventArgs e)
        {
            if (deleted)
                return;

            Point mousePos = e.GetPosition(miniCanvas);

            // Snap point along edge of element
            double ex = Canvas.GetLeft(border) + border.Width / 2;
            double ey = Canvas.GetTop(border) + border.Height / 2;

            double angle = Math.Atan2(mousePos.Y - ey, mousePos.X - ex);

            Point elPos = rectangleIntersect(angle);

            // If we're in connecting mode, then other code handles moving the connect point circle 
            if (!graph.connectingMode)
            {
                Canvas.SetLeft(connectPointCircle, elPos.X - connectPointCircleRadius);
                Canvas.SetTop(connectPointCircle, elPos.Y - connectPointCircleRadius);
            }

            Canvas.SetLeft(connectPointX, elPos.X - 5.5);
            Canvas.SetTop(connectPointX, elPos.Y - 10);
        }

        protected void MiniCanvas_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (deleted)
                return;

            if (e.ChangedButton == MouseButton.Left)
            {
                if (graph.connectingMode)
                {
                    if (graph.connectingElement.canConnectTo(this))
                    {
                        String currentState = graph.serialize();

                        Arc newArc = new Arc(this, graph.connectingElement);
                        graph.connectingMode = false;
                        graph.bigCanvas.AllowDrop = true;
                        graph.connectLine.Visibility = Visibility.Hidden;
                        Mouse.OverrideCursor = null;    // HACK, to stop hand icon on other GraphElement's image
                        MainWindow.updateCommandButtons();

                        if (!Keyboard.IsKeyDown(Key.LeftCtrl) && !Keyboard.IsKeyDown(Key.RightCtrl))
                        {
                            graph.deselectAll();
                        }

                        if (image.IsMouseOver)
                        {
                            connectPointCircle.Visibility = Visibility.Hidden;
                            connectPointX.Visibility = Visibility.Hidden;   // Shouldn't be needed, but here in case
                        }

                        newArc.selected = true;

                        graph.unsavedAction(currentState);
                    }
                }
                else
                {
                    if (canAcceptConnections())
                    {
                        graph.deselectAll();
                        graph.connectingMode = true;
                        graph.connectingElement = this;
                        graph.bigCanvas.AllowDrop = false;
                        Mouse.OverrideCursor = Cursors.Arrow;    // HACK, to stop hand icon on other GraphElement's image
                        MainWindow.updateCommandButtons();

                        Point mousePos = e.GetPosition(canvas);

                        graph.connectLine.X1 = Canvas.GetLeft(miniCanvas) + Canvas.GetLeft(connectPointCircle) + connectPointCircle.Width / 2;
                        graph.connectLine.Y1 = Canvas.GetTop(miniCanvas) + Canvas.GetTop(connectPointCircle) + connectPointCircle.Height / 2;
                        graph.connectLine.X2 = mousePos.X;
                        graph.connectLine.Y2 = mousePos.Y;

                        graph.connectLine.Visibility = Visibility.Visible;
                        connectPointCircle.Visibility = Visibility.Hidden;
                    }
                }
            }
        }

        protected void element_toolTip(Graph graph, ImageSource imageSource)
        {
            var toolTip = "hi";
            graph.ToolTip = toolTip;
        }
    }
}

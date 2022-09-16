using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;

namespace AVL_Prototype_1
{
    public class Arc
    {
        public GraphElement element1;
        public GraphElement element2;

        public int? velocity;
        public bool canHaveVelocity
        {
            get => velocity != null;
        }

        public Line connectionLine;
        public Line hitBoxLine;

        // Velocity arrows (for compatible arcs)
        public TextBlock upLeft = null;
        public TextBlock upRight = null;
        public TextBlock leftUp = null;
        public TextBlock leftDown = null;
        public TextBlock rightUp = null;
        public TextBlock rightDown = null;
        public TextBlock downLeft = null;
        public TextBlock downRight = null;

        public bool deleted = false;

        public SolidColorBrush arcColor = new SolidColorBrush(Colors.Black);

        protected Graph graph;

        public bool selected
        {
            get => graph.selectedArcs.Contains(this);
            set
            {
                if (deleted)
                    return;

                if (value)
                {
                    connectionLine.Stroke = new SolidColorBrush(Colors.Blue);
                    connectionLine.StrokeThickness = 2;

                    if (!graph.selectedArcs.Contains(this))
                    {
                        graph.selectedArcs.Add(this);
                        graph.updateModifiers();
                    }
                }
                else
                {
                    connectionLine.Stroke = arcColor;
                    connectionLine.StrokeThickness = 1;

                    if (graph.selectedArcs.Contains(this))
                    {
                        graph.selectedArcs.Remove(this);
                        graph.updateModifiers();
                    }
                }

                MainWindow.updateCommandButtons();
            }
        }

        protected Arc()
        {
            // Default constructor...
        }

        public Arc(GraphElement element1, GraphElement element2)
        {
            this.element1 = element1;
            this.element2 = element2;

            // Create the connection line
            Canvas canvas = (Canvas)element1.miniCanvas.Parent;

            connectionLine = new Line();
            connectionLine.Stroke = arcColor;
            connectionLine.IsHitTestVisible = false;

            hitBoxLine = new Line();
            hitBoxLine.Stroke = new SolidColorBrush(Colors.Transparent);
            hitBoxLine.StrokeThickness = 22;
            hitBoxLine.Cursor = Cursors.Hand;
            hitBoxLine.MouseDown += HitBoxLine_MouseDown;

            canvas.Children.Add(connectionLine);
            canvas.Children.Add(hitBoxLine);

            graph = (Graph)((Canvas)canvas.Parent).Parent;

            // Add this arc to the elements' list of connections
            element1.connections.Add(this);
            element2.connections.Add(this);

            // Check to see if this arc supports velocity
            if (element1.modifiers.ContainsKey(ModifierType.VELOCITY) && element2.modifiers.ContainsKey(ModifierType.VELOCITY))
                velocity = 0;
            else
                velocity = null;

            // Create the velocity arrows if needed
            if (canHaveVelocity)
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

                List<TextBlock> velocities = new List<TextBlock>();
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
                    graph.theCanvas.Children.Add(v);
                }
            }

            updateLinePostion();

            // Add this arc to the graph's list of arcs
            graph.arcs.Add(this);
        }

        // Deletes all WPF controls and references to this arc
        public virtual void delete()
        {
            element1.connections.Remove(this);
            element2.connections.Remove(this);

            element1 = null;
            element2 = null;

            graph.arcs.Remove(this);

            // Removes the WPF line from the canvas that it's on
            graph.theCanvas.Children.Remove(connectionLine);
            graph.theCanvas.Children.Remove(hitBoxLine);
            connectionLine = null;
            hitBoxLine = null;

            if (canHaveVelocity)
            {
                graph.theCanvas.Children.Remove(upRight);
                graph.theCanvas.Children.Remove(rightUp);
                graph.theCanvas.Children.Remove(rightDown);
                graph.theCanvas.Children.Remove(downRight);
                graph.theCanvas.Children.Remove(downLeft);
                graph.theCanvas.Children.Remove(leftDown);
                graph.theCanvas.Children.Remove(leftUp);
                graph.theCanvas.Children.Remove(upLeft);
            }

            deleted = true;
        }

        public void moveLines(double x1, double y1, double x2, double y2)
        {
            connectionLine.X1 = x1;
            connectionLine.Y1 = y1;
            connectionLine.X2 = x2;
            connectionLine.Y2 = y2;

            hitBoxLine.X1 = x1;
            hitBoxLine.Y1 = y1;
            hitBoxLine.X2 = x2;
            hitBoxLine.Y2 = y2;
        }

        public void moveArrows(double x, double y)
        {
            double arrowHorizWidth = 44;
            double arrowVertHeight = 74;

            double arrowHorizDist = 17;
            double arrowVertDist = 17;

            Canvas.SetLeft(upLeft, x - (arrowHorizWidth / 2) - arrowHorizDist);
            Canvas.SetTop(upLeft, y - 54);

            Canvas.SetLeft(upRight, x - (arrowHorizWidth / 2) + arrowHorizDist);
            Canvas.SetTop(upRight, y - 54);

            Canvas.SetLeft(leftUp, x - 32);
            Canvas.SetTop(leftUp, y - (arrowVertHeight / 2) - arrowVertDist);

            Canvas.SetLeft(leftDown, x - 32);
            Canvas.SetTop(leftDown, y - (arrowVertHeight / 2) + arrowVertDist);

            Canvas.SetLeft(rightUp, x - 4);
            Canvas.SetTop(rightUp, y - (arrowVertHeight / 2) - arrowVertDist);

            Canvas.SetLeft(rightDown, x - 4);
            Canvas.SetTop(rightDown, y - (arrowVertHeight / 2) + arrowVertDist);

            Canvas.SetLeft(downLeft, x - (arrowHorizWidth / 2) - arrowHorizDist);
            Canvas.SetTop(downLeft, y - 18);

            Canvas.SetLeft(downRight, x - (arrowHorizWidth / 2) + arrowHorizDist);
            Canvas.SetTop(downRight, y - 18);
        }

        public virtual void updateLinePostion()
        {
            double e1x = Canvas.GetLeft(element1.miniCanvas) + Canvas.GetLeft(element1.border) + element1.border.Width / 2;
            double e1y = Canvas.GetTop(element1.miniCanvas) + Canvas.GetTop(element1.border) + element1.border.Height / 2;

            double e2x = Canvas.GetLeft(element2.miniCanvas) + Canvas.GetLeft(element2.border) + element2.border.Width / 2;
            double e2y = Canvas.GetTop(element2.miniCanvas) + Canvas.GetTop(element2.border) + element2.border.Height / 2;

            double angle = Math.Atan2(e2y - e1y, e2x - e1x);

            Point el1Pos = element1.rectangleIntersect(angle);
            Point el2Pos = element2.rectangleIntersect(angle + Math.PI);

            moveLines(Canvas.GetLeft(element1.miniCanvas) + el1Pos.X,
                Canvas.GetTop(element1.miniCanvas) + el1Pos.Y,
                Canvas.GetLeft(element2.miniCanvas) + el2Pos.X,
                Canvas.GetTop(element2.miniCanvas) + el2Pos.Y);

            if (canHaveVelocity)
                moveArrows((Canvas.GetLeft(element1.miniCanvas) + el1Pos.X + Canvas.GetLeft(element2.miniCanvas) + el2Pos.X) / 2,
                    (Canvas.GetTop(element1.miniCanvas) + el1Pos.Y + Canvas.GetTop(element2.miniCanvas) + el2Pos.Y) / 2);
        }

        public void setVelocity(int newVelocity)
        {
            if (!canHaveVelocity)
                return;

            if (newVelocity < 0 || newVelocity > 8)
                newVelocity = 0;

            velocity = newVelocity;

            upLeft.Visibility = Visibility.Hidden;
            upRight.Visibility = Visibility.Hidden;
            downLeft.Visibility = Visibility.Hidden;
            downRight.Visibility = Visibility.Hidden;
            leftUp.Visibility = Visibility.Hidden;
            leftDown.Visibility = Visibility.Hidden;
            rightUp.Visibility = Visibility.Hidden;
            rightDown.Visibility = Visibility.Hidden;

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

        public string serialize(List<GraphElement> relativeList)
        {
            StringBuilder sb = new StringBuilder();

            sb.AppendLine("{");

            // Put in the two element indicies
            sb.AppendLine("\telement1 " + relativeList.IndexOf(element1));
            sb.AppendLine("\telement2 " + relativeList.IndexOf(element2));

            if (canHaveVelocity && velocity > 0)
                sb.AppendLine("\tvelocity " + velocity);

            sb.AppendLine("}");

            return sb.ToString();
        }

        public string serialize()
        {
            return serialize(graph.elements);
        }

        public void HitBoxLine_MouseDown(object sender, MouseButtonEventArgs e)
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
                }
            }
        }
    }
}

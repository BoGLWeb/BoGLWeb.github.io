using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;

namespace AVL_Prototype_1
{
    class BondGraphArc : Arc
    {
        private static double arrowLength = 20;
        private static double arrowAngleOffset = Math.PI / 5;
        private static double causalLength = 20;

        public int arrowDir;
        public int causalDir;

        public Line arrowLine = null;
        public Line causalLine = null;

        public BondGraphArc(GraphElement element1, GraphElement element2, SolidColorBrush arcColor, int arrowDir, int causalDir)
        {
            this.element1 = element1;
            this.element2 = element2;

            this.arcColor = arcColor;

            this.arrowDir = arrowDir;
            this.causalDir = causalDir;

            // Create the connection line
            Canvas canvas = (Canvas)element1.miniCanvas.Parent;

            this.arcColor = arcColor;

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

            velocity = null;

            if (arrowDir != 0)
            {
                arrowLine = new Line();
                arrowLine.Stroke = arcColor;
                arrowLine.IsHitTestVisible = false;
                canvas.Children.Add(arrowLine);
            }

            if (causalDir != 0)
            {
                causalLine = new Line();
                causalLine.Stroke = arcColor;
                causalLine.IsHitTestVisible = false;
                canvas.Children.Add(causalLine);
            }

            updateLinePostion();

            // Add this arc to the graph's list of arcs
            graph.arcs.Add(this);
        }

        public override void updateLinePostion()
        {
            double e1x = Canvas.GetLeft(element1.miniCanvas) + Canvas.GetLeft(element1.border) + element1.border.Width / 2;
            double e1y = Canvas.GetTop(element1.miniCanvas) + Canvas.GetTop(element1.border) + element1.border.Height / 2;

            double e2x = Canvas.GetLeft(element2.miniCanvas) + Canvas.GetLeft(element2.border) + element2.border.Width / 2;
            double e2y = Canvas.GetTop(element2.miniCanvas) + Canvas.GetTop(element2.border) + element2.border.Height / 2;

            double angle = Math.Atan2(e2y - e1y, e2x - e1x);

            Point el1Pos = element1.rectangleIntersect(angle);
            Point el2Pos = element2.rectangleIntersect(angle + Math.PI);

            double canvasX1 = Canvas.GetLeft(element1.miniCanvas) + el1Pos.X;
            double canvasY1 = Canvas.GetTop(element1.miniCanvas) + el1Pos.Y;
            double canvasX2 = Canvas.GetLeft(element2.miniCanvas) + el2Pos.X;
            double canvasY2 = Canvas.GetTop(element2.miniCanvas) + el2Pos.Y;

            moveLines(canvasX1, canvasY1, canvasX2, canvasY2);

            if (arrowDir == 1)
            {
                arrowLine.X1 = canvasX1;
                arrowLine.Y1 = canvasY1;

                arrowLine.X2 = canvasX1 + arrowLength * Math.Cos(angle + arrowAngleOffset);
                arrowLine.Y2 = canvasY1 + arrowLength * Math.Sin(angle + arrowAngleOffset);
            }
            else if (arrowDir == 2)
            {
                arrowLine.X1 = canvasX2;
                arrowLine.Y1 = canvasY2;

                arrowLine.X2 = canvasX2 - arrowLength * Math.Cos(angle + arrowAngleOffset);
                arrowLine.Y2 = canvasY2 - arrowLength * Math.Sin(angle + arrowAngleOffset);
            }

            if (causalDir == 1)
            {
                double halfCausalLength = causalLength / 2;

                causalLine.X1 = canvasX1 + halfCausalLength * Math.Cos(angle + (Math.PI / 2));
                causalLine.Y1 = canvasY1 + halfCausalLength * Math.Sin(angle + (Math.PI / 2));
                causalLine.X2 = canvasX1 - halfCausalLength * Math.Cos(angle + (Math.PI / 2));
                causalLine.Y2 = canvasY1 - halfCausalLength * Math.Sin(angle + (Math.PI / 2));
            }
            else if (causalDir == 2)
            {
                double halfCausalLength = causalLength / 2;

                causalLine.X1 = canvasX2 + halfCausalLength * Math.Cos(angle + (Math.PI / 2));
                causalLine.Y1 = canvasY2 + halfCausalLength * Math.Sin(angle + (Math.PI / 2));
                causalLine.X2 = canvasX2 - halfCausalLength * Math.Cos(angle + (Math.PI / 2));
                causalLine.Y2 = canvasY2 - halfCausalLength * Math.Sin(angle + (Math.PI / 2));
            }
        }

        public override void delete()
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

            if (arrowLine != null)
            {
                graph.theCanvas.Children.Remove(arrowLine);
                arrowLine = null;
            }

            if (causalLine != null)
            {
                graph.theCanvas.Children.Remove(causalLine);
                causalLine = null;
            }

            deleted = true;
        }
    }
}

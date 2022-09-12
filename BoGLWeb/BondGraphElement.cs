using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace AVL_Prototype_1
{
    class BondGraphElement : GraphElement
    {
        string bondGraphText;

        Border bgTextBlockBorder;
        TextBlock bgTextBlock;

        public BondGraphElement(Graph graph, string elementName, string bondGraphText, Point position, bool topLeft = false)
        {
            this.graph = graph;
            canvas = graph.theCanvas;
            this.elementName = elementName;
            this.bondGraphText = bondGraphText;

            labels = new List<string>();

            labels.Add(componentName);

            // Initialize list of arcs
            connections = new List<Arc>();

            // Create BondGraph text block
            bgTextBlock = new TextBlock();

            bgTextBlock.Text = bondGraphText;

            // Force TextBlock.ActualWidth and Height to calculate
            bgTextBlock.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
            bgTextBlock.Arrange(new Rect(0, 0, bgTextBlock.DesiredSize.Width, bgTextBlock.DesiredSize.Height));

            // Add some padding to the text so that it is bigger
            bgTextBlockBorder = new Border();
            bgTextBlockBorder.BorderThickness = new Thickness(8);
            bgTextBlockBorder.BorderBrush = new SolidColorBrush(Colors.Transparent);
            bgTextBlockBorder.Cursor = Cursors.Hand;

            //bgTextBlockBorder.MouseEnter += Image_MouseEnter;
            //bgTextBlockBorder.MouseLeave += Image_MouseLeave;
            bgTextBlockBorder.MouseMove += Image_MouseMove;
            bgTextBlockBorder.MouseDown += BGTextBlock_MouseDown;
            bgTextBlockBorder.MouseUp += BGTextBlock_MouseUp;

            // Setup the border surrounding image
            border = new Border();

            // Define different border brushes
            unselectedBorderBrush = new SolidColorBrush(Colors.Black);
            unselectedBorderBrush.Opacity = 0.3;
            selectedBorderBrush = new SolidColorBrush(Colors.Blue);
            selectedBorderBrush.Opacity = 0.7;

            border.BorderBrush = unselectedBorderBrush;
            border.BorderThickness = new Thickness(borderThickness);
            border.Width = bgTextBlock.ActualWidth + border.BorderThickness.Left + border.BorderThickness.Right + bgTextBlockBorder.BorderThickness.Left + bgTextBlockBorder.BorderThickness.Right;
            border.Height = bgTextBlock.ActualHeight + border.BorderThickness.Top + border.BorderThickness.Bottom + bgTextBlockBorder.BorderThickness.Top + bgTextBlockBorder.BorderThickness.Bottom;
            border.Child = bgTextBlockBorder;
            bgTextBlockBorder.Child = bgTextBlock;

            // Setup miniCanvas - area that user hovers over to start connections
            miniCanvas = new Canvas();
            miniCanvas.Background = new SolidColorBrush(Colors.Transparent);
            miniCanvas.Width = bgTextBlock.ActualWidth + imageCanvasBorder + border.BorderThickness.Left + border.BorderThickness.Right + bgTextBlockBorder.BorderThickness.Left + bgTextBlockBorder.BorderThickness.Right;
            miniCanvas.Height = bgTextBlock.ActualHeight + imageCanvasBorder + border.BorderThickness.Top + border.BorderThickness.Bottom + bgTextBlockBorder.BorderThickness.Top + bgTextBlockBorder.BorderThickness.Bottom;

            //miniCanvas.MouseEnter += MiniCanvas_MouseEnter;
            //miniCanvas.MouseLeave += MiniCanvas_MouseLeave;
            //miniCanvas.MouseMove += MiniCanvas_MouseMove;
            //miniCanvas.MouseDown += MiniCanvas_MouseDown;
            miniCanvas.Children.Add(border);
            Canvas.SetLeft(border, imageCanvasBorder / 2);
            Canvas.SetTop(border, imageCanvasBorder / 2);

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

            // Add miniCanvas to graph's canvas
            canvas.Children.Add(miniCanvas);

            // Set the canvas Z-Index properties
            // TODO - this won't fully work - fix this
            Canvas.SetZIndex(miniCanvas, 2);
            Canvas.SetZIndex(border, 5);

            // TODO - REMOVE THIS HACK BECAUSE IT DOESN'T MEAN ANYTHING
            modifiers = new Dictionary<ModifierType, int>();

            // Add this element to the graph's list of elements
            graph.elements.Add(this);
        }

        private void BGTextBlock_MouseDown(object sender, MouseButtonEventArgs e)
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
                    bgTextBlock.CaptureMouse();
                    isInDrag = true;
                    graph.draggingMode = true;
                    graph.bigCanvas.AllowDrop = false;
                    bgTextBlockBorder.Cursor = Cursors.SizeAll;
                    preMovedState = graph.serialize();
                    e.Handled = true;

                    MainWindow.updateCommandButtons();
                }
            }
        }

        private void BGTextBlock_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (deleted)
                return;

            if (e.ChangedButton == MouseButton.Left)
            {
                if (isInDrag)
                {
                    bgTextBlock.ReleaseMouseCapture();
                    isInDrag = false;
                    graph.draggingMode = false;
                    graph.bigCanvas.AllowDrop = true;
                    bgTextBlockBorder.Cursor = Cursors.Hand;
                    e.Handled = true;

                    graph.unsavedAction(preMovedState);
                    preMovedState = "";

                    MainWindow.updateCommandButtons();
                }
            }
        }
    }
}

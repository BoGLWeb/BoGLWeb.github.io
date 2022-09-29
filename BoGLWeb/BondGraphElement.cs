using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Input;

namespace AVL_Prototype_1
{
    class BondGraphElement : GraphElement
    {
        string bondGraphText;
        public BondGraphElement(Graph graph, string elementName, string bondGraphText, bool topLeft = false)
        {
            this.graph = graph;
            this.elementName = elementName;
            this.bondGraphText = bondGraphText;

            labels = new List<string>();

            labels.Add(componentName);

            // Initialize list of arcs
            connections = new List<Arc>();

            // TODO - REMOVE THIS HACK BECAUSE IT DOESN'T MEAN ANYTHING
            modifiers = new Dictionary<Graph.ModifierType, int>();

            // Add this element to the graph's list of elements
            graph.elements.Add(this);
        }

    }
}

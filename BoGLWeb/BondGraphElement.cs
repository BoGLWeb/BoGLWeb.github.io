using NUnit.Framework.Constraints;
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

        /// <summary>
        /// Creates a copy of this <code>BondGraphElement</code>.
        /// </summary>
        /// <returns>
        /// The copy
        /// </returns>
        public override BondGraphElement Copy() {
            BondGraphElement e = new(this.graph, this.elementName, this.bondGraphText, false) {
                nodeName = this.nodeName,
                componentName = this.componentName,
                deleted = this.deleted,
                labels = new List<string>(),
                connections = new List<Arc>(),
                modifiers = new Dictionary<Graph.ModifierType, int>()
            };
            foreach (string label in this.labels) {
                e.labels.Add(label);
            }
            foreach (Arc arc in this.connections) {
                e.connections.Add(arc);
            }
            foreach (KeyValuePair<Graph.ModifierType, int> modifier in this.modifiers) {
                e.modifiers[modifier.Key] = modifier.Value;
            }
            return e;
        }
    }
}

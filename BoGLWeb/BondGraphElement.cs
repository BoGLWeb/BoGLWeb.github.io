using NUnit.Framework.Constraints;
using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Input;

namespace BoGLWeb {
    class BondGraphElement : GraphElement {
        string bondGraphText;
        public BondGraphElement(Graph graph, string elementName, string bondGraphText, bool topLeft = false) {
            this.graph = graph;
            this.elementName = elementName;
            this.bondGraphText = bondGraphText;
            AssignID(0, true);
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
        /// <param name="isDistinct">
        /// <code>true</code> if this Arc should have its own ID, else
        /// <code>false</code>
        /// </param>
        /// <returns>
        /// The copy
        /// </returns>
        public override BondGraphElement Copy(bool isDistinct) {
            BondGraphElement copy = new(this.graph, this.elementName, this.bondGraphText, false) {
                nodeName = this.nodeName,
                componentName = this.componentName,
                deleted = this.deleted,
                labels = new List<string>(),
                connections = new List<Arc>(),
                modifiers = new Dictionary<Graph.ModifierType, int>(),
                velocityNum = new Dictionary<string, int>()
            };
            foreach (string label in this.labels) {
                copy.labels.Add(label);
            }
            foreach (Arc arc in this.connections) {
                copy.connections.Add(arc);
            }
            foreach (KeyValuePair<Graph.ModifierType, int> modifier in this.modifiers) {
                copy.modifiers[modifier.Key] = modifier.Value;
            }
            foreach (KeyValuePair<string, int> velocity in this.velocityNum) {
                copy.velocityNum.Add(velocity.Key, velocity.Value);
            }
            copy.AssignID(this.ID, isDistinct);
            return copy;
        }
    }
}

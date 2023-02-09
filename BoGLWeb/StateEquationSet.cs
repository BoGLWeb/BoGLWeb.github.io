﻿using System.Linq;
using System.Text;

namespace BoGLWeb {
    namespace DifferentialEquationHelper {
        public class StateEquationSet {
            // Stores the state equations in <c>String</c> form.
            private readonly string[] equations;

            /// <summary>
            /// Creates a new <c>StateEquationSet</c>.
            /// </summary>
            /// <param name="graph">The target bond graph.</param>
            public StateEquationSet(BondGraph graph) {
                List<CausalPackager> packages = CausalPackager.GenerateList(graph);
                foreach (CausalPackager causalPackager in packages) {
                    Console.WriteLine(causalPackager);
                }
                this.equations = new string[] { "Dummy 21", "Dummy 5", "Dummy 15", "Dummy 74" };
            }

            /// <summary>
            /// Gets the set of state equations from this <c>StateEquationSet</c>.
            /// </summary>
            /// <returns><c>this.equations</c></returns>
            public string[] GetStringEquations() {
                return this.equations;
            }

            /// <summary>
            /// Converts this <c>StateEquationSet</c> to a printable format.
            /// </summary>
            /// <returns>This <c>StateEquationSet</c> as a <c>string</c>.</returns>
            public override string ToString() {
                return '[' + string.Join(", ", this.equations) + ']';
            }

            /// <summary>
            /// Creates a causal packager that moves with or against the causal flow 
            /// in the graph to create a map for differential equation generation.
            /// Each <c>CausalPackager</c> is a transitive tree (directed connected 
            /// acyclic graph) that radiates outward from the element field.
            /// </summary>
            private class CausalPackager {
                // Stores the element at this node.
                private readonly BondGraph.Element element;
                // Stores the direction of energy flow.
                private readonly bool isSource;
                // Stores all neighboring elements ahead in the flow.
                private readonly List<CausalPackager> neighbors;

                /// <summary>
                /// Creates a new CausalPackager with the specified criteria.
                /// </summary>
                /// <param name="element"></param>
                /// <param name="isSource"><c>true</c> if this <c>Element</c> is
                /// the source of the connector <c>Bond</c>, else <c>false</c>.</param>
                private CausalPackager(BondGraph.Element element, bool isSource) {
                    this.element = element;
                    this.isSource = isSource;
                    this.neighbors = new();
                }

                /// <summary>
                /// Recursively generates a new CausalPackager.
                /// </summary>
                /// <param name="element"></param>
                /// <param name="followsCausality"></param>
                /// <param name="bondsBySource"></param>
                /// <param name="bondsByTarget"></param>
                private static CausalPackager GeneratePackager(BondGraph.Element element, bool followsCausality, Dictionary<int, List<BondGraph.Bond>> bondsBySource, Dictionary<int, List<BondGraph.Bond>> bondsByTarget) {
                    CausalPackager returnValue = new(element, true);
                    Stack<CausalPackager> packagerStack = new(new[] { returnValue });
                    while (packagerStack.Count > 0) {
                        CausalPackager packager = packagerStack.Pop();
                        int elementID = packager.element.GetID();
                        List<BondGraph.Bond>? bondsToTarget = bondsBySource.GetValueOrDefault(elementID);
                        if (bondsToTarget != null) {
                            foreach (BondGraph.Bond bond in bondsToTarget) {
                                if (followsCausality == bond.GetCausalDirection()) {
                                    CausalPackager neighborPackager = new(bond.getSink(), true);
                                    packager.neighbors.Add(neighborPackager);
                                    packagerStack.Push(neighborPackager);
                                }
                            }
                        }
                        List<BondGraph.Bond>? bondsToSource = bondsByTarget.GetValueOrDefault(elementID);
                        if (bondsToSource != null) {
                            foreach (BondGraph.Bond bond in bondsToSource) {
                                if (followsCausality ^ bond.GetCausalDirection()) {
                                    CausalPackager neighborPackager = new(bond.getSource(), false);
                                    packager.neighbors.Add(neighborPackager);
                                    packagerStack.Push(neighborPackager);
                                }
                            }
                        }
                    }
                    return returnValue;
                }

                /// <summary>
                /// Creates a new <c>CausalPackager</c> from a <c>BondGraph</c>.
                /// </summary>
                /// <param name="graph">The model bond graph.</param>
                public static List<CausalPackager> GenerateList(BondGraph graph) {
                    Dictionary<string, BondGraph.Element> elements = graph.getElements();
                    Dictionary<int, List<BondGraph.Bond>> bondsBySource = new();
                    Dictionary<int, List<BondGraph.Bond>> bondsByTarget = new();
                    foreach (BondGraph.Bond bond in graph.getBonds()) {
                        int source = bond.getSource().GetID();
                        List<BondGraph.Bond>? sourceBondsByElement = bondsBySource.GetValueOrDefault(source);
                        if (sourceBondsByElement == null) {
                            bondsBySource.Add(source, new List<BondGraph.Bond> { bond });
                        } else {
                            sourceBondsByElement.Add(bond);
                        }
                        int target = bond.getSink().GetID();
                        List<BondGraph.Bond>? targetBondsByElement = bondsByTarget.GetValueOrDefault(target);
                        if (targetBondsByElement == null) {
                            bondsByTarget.Add(target, new List<BondGraph.Bond> { bond });
                        } else {
                            targetBondsByElement.Add(bond);
                        }
                    }
                    Console.WriteLine(string.Join(", ", bondsBySource.Select(pair => pair.Key + " [" + string.Join(", ", pair.Value) + "]")));
                    Console.WriteLine(string.Join(", ", bondsByTarget.Select(pair => pair.Key + " [" + string.Join(", ", pair.Value) + "]")));
                    List<CausalPackager> packagerList = new();
                    foreach (KeyValuePair<string, BondGraph.Element> pair in elements) {
                        string label = pair.Value.GetLabel();
                        if (label.Length > 1) {
                            char indicator = label[^2];
                            bool isIStorage = indicator == 'I';
                            if (isIStorage | indicator == 'C') {
                                packagerList.Add(GeneratePackager(pair.Value, isIStorage, bondsBySource, bondsByTarget));
                            }
                        }
                    }
                    return packagerList;
                }

                /// <summary>
                /// Converts this <c>CausalPackager</c> to a printable format.
                /// </summary>
                /// <returns>This <c>CausalPackager</c> as a <c>string</c></returns>
                public override string ToString() {
                    StringBuilder print = new();
                    Stack<CausalPackager> packageStack = new(new[] {this});
                    Stack<bool> checkStack = new(new[] {false});
                    while (packageStack.Count > 0) {
                        CausalPackager packager = packageStack.Pop();
                        if (checkStack.Pop()) {
                            print.Append(']');
                        } else {
                            print.Append('[').Append(packager.element);
                            packageStack.Push(packager);
                            checkStack.Push(true);
                            foreach (CausalPackager neighbor in packager.neighbors) {
                                packageStack.Push(neighbor);
                                checkStack.Push(false);
                            }
                        }
                    }
                    return print.ToString();
                }
            }
        }
    }
}

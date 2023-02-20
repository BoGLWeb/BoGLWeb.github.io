using NUnit.Framework;
using System.Linq;
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
                int count = graph.GetDifferentialElements().Count;
                List<CausalPackager> packagers = CausalPackager.GenerateList(graph);
                Dictionary<string, Expression> substitutionDictionary = new();
                foreach (CausalPackager packager in packagers) {
                    KeyValuePair<string, Expression> pair = packager.GetExpression();
                    substitutionDictionary.Add(pair.Key, pair.Value);
                }
                //GetRemainingSubstitutes();
                this.equations = new string[3];
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
                private readonly bool isSource, isEffort;
                // Stores all neighboring elements ahead in the flow.
                private readonly List<CausalPackager> neighbors;
                // Stores the current state equation.
                private Expression stateEquation;

                /// <summary>
                /// Creates a new CausalPackager with the specified criteria.
                /// </summary>
                /// <param name="element">The <c>BondGraph.Element</c> stored in this
                /// <c>CausalPackager</c>.</param>
                /// <param name="isSource"><c>true</c> if this <c>Element</c> is
                /// the source of the connector <c>Bond</c>, else <c>false</c>.</param>
                private CausalPackager(BondGraph.Element element, bool isSource, bool isEffort) {
                    this.element = element;
                    this.isSource = isSource;
                    this.isEffort = isEffort;
                    this.stateEquation = new(GenerateVariableName());
                    this.neighbors = new();
                }

                /// <summary>
                /// Generates the variable name for the Expression associated with this CausalPackager.
                /// </summary>
                /// <returns></returns>
                public string GenerateVariableName() {
                    return (this.isEffort ? "E" : "F") + this.element.GetID();
                }

                /// <summary>
                /// Recursively generates a new CausalPackager.
                /// </summary>
                /// <param name="element"></param>
                /// <param name="isEffort"></param>
                /// <param name="bondsBySource"></param>
                /// <param name="bondsByTarget"></param>
                private static CausalPackager GeneratePackager(BondGraph.Element element, bool isEffort, Dictionary<int, List<BondGraph.Bond>> bondsBySource, Dictionary<int, List<BondGraph.Bond>> bondsByTarget) {
                    CausalPackager returnValue = new(element, true, isEffort);
                    Stack<CausalPackager> packagerStack = new(new[] { returnValue });
                    while (packagerStack.Count > 0) {
                        CausalPackager packager = packagerStack.Pop();
                        int elementID = packager.element.GetID();
                        List<BondGraph.Bond>? bondsToTarget = bondsBySource.GetValueOrDefault(elementID);
                        if (bondsToTarget != null) {
                            foreach (BondGraph.Bond bond in bondsToTarget) {
                                if (isEffort == bond.GetCausalDirection()) {
                                    CausalPackager neighborPackager = new(bond.getSink(), true, isEffort);
                                    packager.neighbors.Add(neighborPackager);
                                    packagerStack.Push(neighborPackager);
                                }
                            }
                        }
                        List<BondGraph.Bond>? bondsToSource = bondsByTarget.GetValueOrDefault(elementID);
                        if (bondsToSource != null) {
                            foreach (BondGraph.Bond bond in bondsToSource) {
                                if (isEffort ^ bond.GetCausalDirection()) {
                                    CausalPackager neighborPackager = new(bond.getSource(), false, isEffort);
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
                    //Console.WriteLine(string.Join(", ", bondsBySource.Select(pair => pair.Key + " [" + string.Join(", ", pair.Value) + "]")));
                    //Console.WriteLine(string.Join(", ", bondsByTarget.Select(pair => pair.Key + " [" + string.Join(", ", pair.Value) + "]")));
                    List<CausalPackager> packagerList = new();
                    foreach (KeyValuePair<string, BondGraph.Element> pair in elements) {
                        char indicator = pair.Value.GetTypeChar();
                        if ("IRC".Contains(indicator)) {
                            packagerList.Add(GeneratePackager(pair.Value, indicator == 'I', bondsBySource, bondsByTarget));
                        }
                        Console.WriteLine(pair.Value);
                    }
                    return packagerList;
                }

                /// <summary>
                /// Forms an incomplete state equation.
                /// </summary>
                /// <returns></returns>
                public KeyValuePair<string, Expression> GetExpression() {
                    Stack<CausalPackager> packageStack = new(new[] { this });
                    Stack<bool> checkStack = new(new[] { false });
                    while (packageStack.Count > 0) {
                        CausalPackager packager = packageStack.Pop();
                        if (checkStack.Pop()) {
                            if (packager.neighbors.Count > 0) {
                                packager.stateEquation = new Expression();
                                CausalPackager loneChild = packager.neighbors[0];
                                char typeChar = packager.element.GetTypeChar();
                                switch (typeChar) {
                                    case '0':
                                    case '1':
                                        if (typeChar == '1' == packager.isEffort) {
                                            foreach (CausalPackager child in packager.neighbors) {
                                                if (packager.isSource == child.isSource) {
                                                    packager.stateEquation = packager.stateEquation.Add(child.stateEquation);
                                                } else {
                                                    packager.stateEquation = packager.stateEquation.Subtract(child.stateEquation);
                                                }
                                                child.stateEquation = new(child.GenerateVariableName());
                                            }
                                        } else {
                                            packager.stateEquation = loneChild.stateEquation;
                                            loneChild.stateEquation = new(loneChild.GenerateVariableName());
                                        }
                                        break;
                                    default:
                                        packager.stateEquation = loneChild.stateEquation;
                                        loneChild.stateEquation = new(loneChild.GenerateVariableName());
                                        break;
                                }
                            }
                        } else {
                            packageStack.Push(packager);
                            checkStack.Push(true);
                            foreach (CausalPackager child in packager.neighbors) {
                                packageStack.Push(child);
                                checkStack.Push(false);
                            }
                        }
                    }
                    Expression stateEquation = this.stateEquation;
                    string varName = GenerateVariableName();
                    this.stateEquation = new(varName);
                    return new(varName, stateEquation);
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
                            print.Append("[ ").Append(packager.element).Append(' ');
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

            /// <summary>
            /// Updates the list of substitutes in the chat with all 
            /// </summary>
            private static void GetRemainingSubstitutes(BondGraph graph, Dictionary<string, Expression> subs) {
                //foreach () {
                //}
            }
        }
    }
}

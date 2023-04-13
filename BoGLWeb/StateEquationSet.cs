using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using NUnit.Framework;
using System.Diagnostics;
using System.IO.Pipes;
using System.Linq;
using System.Text;

namespace BoGLWeb {
    namespace DifferentialEquationHelper {
        public class StateEquationSet {
            // Stores the initial equations in <c>String</c> form.
            private readonly string[] initialEquations;
            // Stores the state equations in <c>String</c> form.
            private readonly string[] finalDifferentialStateEquations;

            /// <summary>
            /// Creates a new <c>StateEquationSet</c>.
            /// </summary>
            /// <param name="graph">The target bond graph.</param>
            public StateEquationSet(BondGraph graph) {
                int count = graph.GetDifferentialElements().Count;
                BondGraph.BondGraphWrapper graphWrapper = new(graph);
                //List<Equation> initialEquations = GetInitialEquations(graphWrapper);
                //this.initialEquations = new string[initialEquations.Count];
                //int initialIndex = 0;
                //foreach (Equation equation in initialEquations) {
                //    this.initialEquations[initialIndex++] = equation.ToString();
                //}
                this.initialEquations = new string[0];
                List<CausalGraphWrapper> wrappers = CausalGraphWrapper.GenerateList(graphWrapper);
                Dictionary<string, Expression> substitutionDictionary = new();
                HashSet<string> usedSet = new();
                foreach (CausalGraphWrapper wrapper in wrappers) {
                    KeyValuePair<string, Expression> pair = wrapper.GetExpression();
                    substitutionDictionary.Add(pair.Key, pair.Value);
                }
                GetRemainingGeneralizedSubstitutes(graph, substitutionDictionary);
                int prevUsedQuantity;
                do {
                    prevUsedQuantity = usedSet.Count;
                    foreach (KeyValuePair<string, Expression> pair in substitutionDictionary) {
                        foreach (string item in pair.Value.SubstituteAllVariables(substitutionDictionary)) {
                            usedSet.Add(item);
                        }
                    }
                } while (prevUsedQuantity < usedSet.Count);
                foreach (string key in usedSet) {
                    substitutionDictionary.Remove(key);
                }
                this.finalDifferentialStateEquations = new string[count];
                int index = 0;
                foreach (KeyValuePair<string, Expression> pair in substitutionDictionary) {
                    this.finalDifferentialStateEquations[index++] = pair.Key + "=" + pair.Value.ToLatexString();
                }
            }

            /// <summary>
            /// Gets the set of initial equations for this bond graph.
            /// </summary>
            /// <returns><c>this.initialEquations</c></returns>
            public string[] GetInitialEquations() {
                return this.initialEquations;
            }

            /// <summary>
            /// Gets the set of state equations from this <c>StateEquationSet</c>.
            /// </summary>
            /// <returns><c>this.finalDifferentialStateEquations</c></returns>
            public string[] GetFinalEquations() {
                return this.finalDifferentialStateEquations;
            }

            /// <summary>
            /// Converts this <c>StateEquationSet</c> to a printable format.
            /// </summary>
            /// <returns>This <c>StateEquationSet</c> as a <c>string</c>.</returns>
            public override string ToString() {
                return '[' + string.Join(", ", this.initialEquations) + "][" + string.Join(", ", this.finalDifferentialStateEquations) + ']';
            }

            /// <summary>
            /// Creates a causal graphWrapper that moves with or against the causal flow 
            /// in the graph to create a map for differential equation generation.
            /// Each <c>CausalGraphWrapper</c> is a transitive tree (directed connected 
            /// acyclic graph) that radiates outward from the element field.
            /// </summary>
            private class CausalGraphWrapper {
                // Stores the element at this node.
                private readonly BondGraph.Element element;
                // Stores the direction of energy flow.
                private readonly bool isSource, isEffort;
                // Stores all neighboring elements ahead in the flow.
                private readonly List<CausalGraphWrapper> neighbors;
                // Stores the current state equation.
                private Expression stateEquation;

                /// <summary>
                /// Creates a new CausalGraphWrapper with the specified criteria.
                /// </summary>
                /// <param name="element">The <c>BondGraph.Element</c> stored in this
                /// <c>CausalGraphWrapper</c>.</param>
                /// <param name="isSource"><c>true</c> if this <c>Element</c> is
                /// the source of the connector <c>Bond</c>, else <c>false</c>.</param>
                private CausalGraphWrapper(BondGraph.Element element, bool isSource, bool isEffort) {
                    this.element = element;
                    this.isSource = isSource;
                    this.isEffort = isEffort;
                    this.stateEquation = new(GenerateVariableName());
                    this.neighbors = new();
                }

                /// <summary>
                /// Generates the variable name for the Expression associated with this CausalGraphWrapper.
                /// </summary>
                /// <returns></returns>
                public string GenerateVariableName() {
                    return (this.isEffort ? "E" : "F") + this.element.GetID();
                }

                /// <summary>
                /// Recursively generates a new CausalGraphWrapper.
                /// </summary>
                /// <param name="element"></param>
                /// <param name="isEffort"></param>
                /// <param name="bondsBySource"></param>
                /// <param name="bondsByTarget"></param>
                private static CausalGraphWrapper GenerateWrapper(BondGraph.Element element, bool isEffort, Dictionary<int, List<BondGraph.Bond>> bondsBySource, Dictionary<int, List<BondGraph.Bond>> bondsByTarget) {
                    CausalGraphWrapper returnValue = new(element, true, isEffort);
                    Stack<CausalGraphWrapper> wrapperStack = new(new[] { returnValue });
                    HashSet<int> used = new();
                    while (wrapperStack.Count > 0) {
                        CausalGraphWrapper wrapper = wrapperStack.Pop();
                        int elementID = wrapper.element.GetID();
                        if (used.Contains(elementID)) {
                            throw new Exception("State finalDifferentialStateEquations cannot be derived in looped causality.");
                        }
                        used.Add(elementID);
                        List<BondGraph.Bond>? bondsToTarget = bondsBySource.GetValueOrDefault(elementID);
                        if (bondsToTarget != null) {
                            foreach (BondGraph.Bond bond in bondsToTarget) {
                                if (isEffort == bond.GetCausalDirection()) {
                                    CausalGraphWrapper neighborWrapper = new(bond.getSink(), true, isEffort);
                                    wrapper.neighbors.Add(neighborWrapper);
                                    wrapperStack.Push(neighborWrapper);
                                }
                            }
                        }
                        List<BondGraph.Bond>? bondsToSource = bondsByTarget.GetValueOrDefault(elementID);
                        if (bondsToSource != null) {
                            foreach (BondGraph.Bond bond in bondsToSource) {
                                if (isEffort ^ bond.GetCausalDirection()) {
                                    CausalGraphWrapper neighborWrapper = new(bond.getSource(), false, isEffort);
                                    wrapper.neighbors.Add(neighborWrapper);
                                    wrapperStack.Push(neighborWrapper);
                                }
                            }
                        }
                    }
                    return returnValue;
                }

                /// <summary>
                /// Creates a new <c>CausalGraphWrapper</c> from a <c>BondGraph</c>.
                /// </summary>
                /// <param name="wrapper">The model bond graph graphWrapper.</param>
                public static List<CausalGraphWrapper> GenerateList(BondGraph.BondGraphWrapper wrapper) {
                    List<CausalGraphWrapper> wrapperList = new();
                    Dictionary<int, List<BondGraph.Bond>> sourceMap = wrapper.GetBondsBySource();
                    Dictionary<int, List<BondGraph.Bond>> targetMap = wrapper.GetBondsByTarget();
                    foreach (KeyValuePair<string, BondGraph.Element> pair in wrapper.GetElements()) {
                        char indicator = pair.Value.GetTypeChar();
                        bool isEffort = true;
                        List<BondGraph.Bond>? sourceList = sourceMap.GetValueOrDefault(pair.Value.GetID());
                        if (sourceList != null) {
                            isEffort = sourceList[0].GetCausalDirection();
                        } else {
                            List<BondGraph.Bond>? targetList = targetMap.GetValueOrDefault(pair.Value.GetID());
                            if (targetList != null) {
                                isEffort = !targetList[0].GetCausalDirection();
                            }
                        } // Note that isEffort will experience exactly one of the two rewrites above.
                        if ("IRC".Contains(indicator)) {
                            wrapperList.Add(GenerateWrapper(pair.Value, isEffort, sourceMap, targetMap));
                        }
                    }
                    return wrapperList;
                }

                /// <summary>
                /// Forms an incomplete state equation.
                /// </summary>
                /// <returns></returns>
                public KeyValuePair<string, Expression> GetExpression() {
                    Stack<CausalGraphWrapper> packageStack = new(new[] { this });
                    Stack<bool> checkStack = new(new[] { false });
                    while (packageStack.Count > 0) {
                        CausalGraphWrapper wrapper = packageStack.Pop();
                        if (checkStack.Pop()) {
                            if (wrapper.neighbors.Count > 0) {
                                wrapper.stateEquation = new Expression();
                                CausalGraphWrapper loneChild = wrapper.neighbors[0];
                                char typeChar = wrapper.element.GetTypeChar();
                                switch (typeChar) {
                                    case '0':
                                    case '1':
                                        if (typeChar == '1' == wrapper.isEffort) {
                                            foreach (CausalGraphWrapper child in wrapper.neighbors) {
                                                if (wrapper.isSource == child.isSource) {
                                                    wrapper.stateEquation = wrapper.stateEquation.Add(child.stateEquation);
                                                } else {
                                                    wrapper.stateEquation = wrapper.stateEquation.Subtract(child.stateEquation);
                                                }
                                                child.stateEquation = new(child.GenerateVariableName());
                                            }
                                        } else {
                                            wrapper.stateEquation = loneChild.stateEquation;
                                            loneChild.stateEquation = new(loneChild.GenerateVariableName());
                                        }
                                        break;
                                    case 'T':
                                        Expression expr = new("T" + wrapper.element.GetID());
                                        if (isEffort == isSource) {
                                            wrapper.stateEquation = loneChild.stateEquation.Multiply(expr);
                                        } else {
                                            wrapper.stateEquation = loneChild.stateEquation.Divide(expr);
                                        }
                                        loneChild.stateEquation = new(loneChild.GenerateVariableName());
                                        break;
                                    default:
                                        wrapper.stateEquation = loneChild.stateEquation;
                                        loneChild.stateEquation = new(loneChild.GenerateVariableName());
                                        break;
                                }
                            }
                        } else {
                            packageStack.Push(wrapper);
                            checkStack.Push(true);
                            foreach (CausalGraphWrapper child in wrapper.neighbors) {
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
                /// Converts this <c>CausalGraphWrapper</c> to a printable format.
                /// </summary>
                /// <returns>This <c>CausalGraphWrapper</c> as a <c>string</c></returns>
                public override string ToString() {
                    StringBuilder print = new();
                    Stack<CausalGraphWrapper> packageStack = new(new[] {this});
                    Stack<bool> checkStack = new(new[] {false});
                    while (packageStack.Count > 0) {
                        CausalGraphWrapper wrapper = packageStack.Pop();
                        if (checkStack.Pop()) {
                            print.Append(']');
                        } else {
                            print.Append("[ ").Append(wrapper.element).Append(' ');
                            packageStack.Push(wrapper);
                            checkStack.Push(true);
                            foreach (CausalGraphWrapper neighbor in wrapper.neighbors) {
                                packageStack.Push(neighbor);
                                checkStack.Push(false);
                            }
                        }
                    }
                    return print.ToString();
                }
            }

            /// <summary>
            /// Loads the initial equations for this <c>StateEquationSet</c>.
            /// </summary>
            /// <param name="wrapper">The graphWrapper containing the information
            /// for the target bond graph.</param>
            private static List<Equation> GetInitialEquations(BondGraph.BondGraphWrapper wrapper) {
                Dictionary<int, List<BondGraph.Bond>> bbs = wrapper.GetBondsBySource();
                Dictionary<int, List<BondGraph.Bond>> bbt = wrapper.GetBondsByTarget();
                List<Equation> equations = new();
                foreach (KeyValuePair<string, BondGraph.Element> pair in wrapper.GetElements()) {
                    char typeChar = pair.Value.GetTypeChar();
                    switch (typeChar) {
                        case '0':
                        case '1':
                            bool isOneJunc = typeChar == '1';
                            String zeroSum = "f", equal = "e";
                            if (isOneJunc) {
                                zeroSum = "e";
                                equal = "f";
                            }
                            Expression negSum = new("0"), posSum = new("0");
                            int inwardCausalBondCount = 0, outwardCausalBondCount = 0;
                            BondGraph.Bond? inwardCausalBond = null, outwardCausalBond = null;
                            foreach (BondGraph.Bond bond in bbs.GetValueOrDefault(pair.Value.GetID()) ?? new()) {
                                posSum = posSum.Add(new(zeroSum + bond.GetID()));
                                if (bond.GetCausalDirection()) {
                                    outwardCausalBondCount++;
                                    outwardCausalBond = bond;
                                } else {
                                    if (inwardCausalBondCount == 1) {
                                        //throw new Exception("Junction causality not assigned correctly at element " + pair.Value.GetID() + ".");
                                    }
                                    inwardCausalBondCount = 1;
                                    inwardCausalBond = bond;
                                }
                            }
                            foreach (BondGraph.Bond bond in bbt.GetValueOrDefault(pair.Value.GetID()) ?? new()) {
                                negSum = negSum.Add(new(zeroSum + bond.GetID()));
                                if (bond.GetCausalDirection()) {
                                    inwardCausalBondCount++;
                                    inwardCausalBond = bond;
                                } else {
                                    if (outwardCausalBondCount == 1) {
                                        //throw new Exception("Junction causality not assigned correctly at element " + pair.Value.GetID() + ".");
                                    }
                                    outwardCausalBondCount = 1;
                                    outwardCausalBond = bond;
                                }
                            }
                            equations.Add(new(posSum, negSum));
                            BondGraph.Bond? equalBond = (inwardCausalBondCount == 1) ? inwardCausalBond : outwardCausalBond;
                            int equalID = equalBond?.GetID() ?? 0;
                            Expression firstExp = new(equal + equalID);
                            foreach (BondGraph.Bond bond in bbs.GetValueOrDefault(pair.Value.GetID()) ?? new()) {
                                int bondID = bond.GetID();
                                if (bondID != equalID) {
                                    equations.Add(new(firstExp, new(equal + bondID)));
                                }
                            }
                            break;
                        default:
                            break;
                    }
                }
                return equations;
            }

            /// <summary>
            /// Updates the list of substitutes in the chat with replacements for I, 
            /// R, and C elements.
            /// </summary>
            private static void GetRemainingGeneralizedSubstitutes(BondGraph graph, Dictionary<string, Expression> subs) {
                foreach (KeyValuePair<string, BondGraph.Element> pair in graph.getElements()) {
                    int ID = pair.Value.GetID();
                    char type = pair.Value.GetTypeChar();
                    string flowVar = "F" + ID, effortVar = "E" + ID;
                    string cVar = "C" + ID, rVar = "R" + ID, iVar = "I" + ID;
                    if (subs.ContainsKey(effortVar)) {
                        type = char.ToLower(type);
                    }
                    switch (type) {
                        case 'C':
                            subs.Add(effortVar, new("Q" + ID + "/" + cVar));
                            break;
                        case 'c':
                            subs.Add(flowVar, new(effortVar + "'*" + cVar));
                            break;
                        case 'I':
                            subs.Add(effortVar, new(flowVar + "'*" + iVar));
                            break;
                        case 'i':
                            subs.Add(flowVar, new("P" + ID + "/" + iVar));
                            break;
                        case 'R':
                            subs.Add(effortVar, new(flowVar + "*" + rVar));
                            break;
                        case 'r':
                            subs.Add(flowVar, new(effortVar + "/" + rVar));
                            break;
                    }
                }
            }

            /// <summary>
            /// Gets a mapping for domain variables.
            /// </summary>
            /// <param name="wrapper">The <c>BondGraphWrapper</c> containing information on 
            /// connections between the </param>
            /// <returns>The <c>Dictionary</c> mapping.</returns>
            // Note that any replacement mappings determined by the user should be applied here.
            public static Dictionary<string, string> GetDomainVariables(BondGraph.BondGraphWrapper wrapper) {
                Dictionary<string, string> vars = new();
                //
                return vars;
            }
        }
    }
}

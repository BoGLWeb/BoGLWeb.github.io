using System.Text;

namespace BoGLWeb {
    namespace DifferentialEquationHelper {
        /// <summary>
        /// Represents a single named stage in the step-by-step derivation of a state equation.
        /// </summary>
        public sealed class DerivationStep {
            /// <summary>Display heading for this derivation step (e.g., "1. Junction Equation").</summary>
            public string Title { get; }
            /// <summary>Ordered list of LaTeX-formatted equations shown under this step.</summary>
            public List<string> LatexEquations { get; }

            public DerivationStep(string title, List<string> latexEquations) {
                Title = title;
                LatexEquations = latexEquations;
            }
        }

        /// <summary>
        /// Stores the complete derivation trace and final isolated LaTeX state equation
        /// for a single integral-causality storage element.
        /// </summary>
        public sealed class EquationDerivation {
            /// <summary>Internal state variable key (e.g., "P10" or "Q5").</summary>
            public string StateVarKey { get; }
            /// <summary>Plain-text state variable name used in dropdown labels (e.g., "h10").</summary>
            public string StateVarName { get; }
            /// <summary>LaTeX-formatted state variable name (e.g., "h_{10}").</summary>
            public string StateVarLatex { get; }
            /// <summary>LaTeX-formatted state derivative (e.g., "h_{10}'").</summary>
            public string StateDerivativeLatex { get; }
            /// <summary>Final isolated state equation in LaTeX.</summary>
            public string FinalEquationLatex { get; }
            /// <summary>True if derivative causality affected this equation's derivation.</summary>
            public bool HasDerivativeCausality { get; }
            /// <summary>Ordered derivation steps leading to <see cref="FinalEquationLatex"/>.</summary>
            public List<DerivationStep> Steps { get; }

            public EquationDerivation(
                string stateVarKey,
                string stateVarName,
                string stateVarLatex,
                string stateDerivativeLatex,
                string finalEquationLatex,
                bool hasDerivativeCausality,
                List<DerivationStep> steps) {
                StateVarKey = stateVarKey;
                StateVarName = stateVarName;
                StateVarLatex = stateVarLatex;
                StateDerivativeLatex = stateDerivativeLatex;
                FinalEquationLatex = finalEquationLatex;
                HasDerivativeCausality = hasDerivativeCausality;
                Steps = steps;
            }
        }

        public class StateEquationSet {
            private readonly string[] initialEquations;
            private readonly string[] finalDifferentialStateEquations;
            private readonly List<EquationDerivation> derivations;

            /// <summary>
            /// Creates a new <c>StateEquationSet</c> for the given causal bond graph,
            /// supporting both integral and derivative causality as well as step-by-step derivations.
            /// </summary>
            public StateEquationSet(BondGraph graph) {
                BondGraph.BondGraphWrapper graphWrapper = new(graph);
                graphWrapper.InferDomainsIfNeeded();

                SolverContext ctx = new(graph, graphWrapper);
                ctx.Solve();

                this.derivations = ctx.Derivations;
                this.initialEquations = ctx.InitialEquations.ToArray();
                this.finalDifferentialStateEquations = this.derivations
                    .Select(d => d.FinalEquationLatex)
                    .ToArray();
            }

            /// <summary>Gets the Step 1 junction equations in LaTeX.</summary>
            public string[] GetInitialEquations() => this.initialEquations;

            /// <summary>Gets the final isolated state equations in LaTeX.</summary>
            public string[] GetFinalEquations() => this.finalDifferentialStateEquations;

            /// <summary>Gets the full step-by-step derivations for all state equations.</summary>
            public IReadOnlyList<EquationDerivation> GetDerivations() => this.derivations;

            /// <summary>Gets plain-text state variable names for UI selection lists.</summary>
            public string[] GetStateVariableNames() =>
                this.derivations.Select(d => d.StateVarName).ToArray();

            /// <summary>Gets LaTeX-formatted state variable names.</summary>
            public string[] GetStateVariableLatex() =>
                this.derivations.Select(d => d.StateVarLatex).ToArray();

            public override string ToString() {
                return '[' + string.Join(", ", this.initialEquations) + "][" + string.Join(", ", this.finalDifferentialStateEquations) + ']';
            }

            private sealed class StateInfo {
                public BondGraph.Element Element { get; }
                public BondGraph.Bond Bond { get; }
                public bool IsInductor { get; }
                public string VarKey { get; }
                public string DerivKey => VarKey + "'";
                public string PlainName { get; }
                public string LatexName { get; }
                public string DerivLatex => LatexName + "'";

                public StateInfo(
                    BondGraph.Element element,
                    BondGraph.Bond bond,
                    bool isInductor,
                    string varKey,
                    string plainName,
                    string latexName) {
                    Element = element;
                    Bond = bond;
                    IsInductor = isInductor;
                    VarKey = varKey;
                    PlainName = plainName;
                    LatexName = latexName;
                }
            }

            private sealed class BondVarLabels {
                public string EffortLatex { get; }
                public string FlowLatex { get; }
                public BondVarLabels(string effortLatex, string flowLatex) {
                    EffortLatex = effortLatex;
                    FlowLatex = flowLatex;
                }
            }

            private sealed class SolverContext {
                private readonly BondGraph graph;
                private readonly BondGraph.BondGraphWrapper wrapper;
                private readonly Dictionary<int, List<BondGraph.Bond>> bondsByElement = new();
                private readonly Dictionary<BondGraph.Bond, BondVarLabels> bondLabels = new(ReferenceEqualityComparer.Instance);
                private readonly Dictionary<string, string> varKeyToLatex = new(StringComparer.Ordinal);
                private readonly List<StateInfo> states = new();
                private readonly Dictionary<int, string> rLoopPlaceholders = new();
                private readonly List<(int ElementId, bool WantEffort, LinearForm Rhs)> rEquations = new();

                public List<string> InitialEquations { get; } = new();
                public List<EquationDerivation> Derivations { get; } = new();

                public SolverContext(BondGraph graph, BondGraph.BondGraphWrapper wrapper) {
                    this.graph = graph;
                    this.wrapper = wrapper;

                    foreach (BondGraph.Element el in graph.getElements().Values) {
                        bondsByElement[el.GetID()] = new List<BondGraph.Bond>();
                    }
                    foreach (BondGraph.Bond bond in graph.getBonds()) {
                        int srcId = bond.getSource().GetID();
                        int dstId = bond.getSink().GetID();
                        if (!bondsByElement.TryGetValue(srcId, out var srcList)) {
                            srcList = new List<BondGraph.Bond>();
                            bondsByElement[srcId] = srcList;
                        }
                        srcList.Add(bond);
                        if (dstId != srcId) {
                            if (!bondsByElement.TryGetValue(dstId, out var dstList)) {
                                dstList = new List<BondGraph.Bond>();
                                bondsByElement[dstId] = dstList;
                            }
                            dstList.Add(bond);
                        }
                    }

                    ClassifyStatesAndSources();
                    AssignBondDisplayLabels();
                }

                /// <summary>
                /// Returns true when the causal stroke directs effort into <paramref name="el"/>
                /// (causalStrokeDirection == true means stroke is at the bond's source).
                /// </summary>
                private static bool ElementReceivesEffort(BondGraph.Bond bond, BondGraph.Element el) {
                    return bond.isSource(el) ? bond.GetCausalDirection() : !bond.GetCausalDirection();
                }

                /// <summary>
                /// Returns true when the power half-arrow points into <paramref name="el"/>.
                /// </summary>
                private static bool IsPowerIntoElement(BondGraph.Bond bond, BondGraph.Element el) {
                    return bond.isSink(el);
                }

                private static BondGraph.Element GetOtherElement(BondGraph.Bond bond, BondGraph.Element el) {
                    return bond.isSource(el) ? bond.getSink() : bond.getSource();
                }

                private string FormatSubscript(string baseSymbol, string subscript) {
                    string baseLatex = Expression.FormatLatexIdentifier(baseSymbol);
                    return $"{baseLatex}_{{{subscript}}}";
                }

                private string GetParamKey(BondGraph.Element el) {
                    string sym = BondGraph.BondGraphWrapper.GetElementParamSymbol(el);
                    return sym + el.GetID();
                }

                private void ClassifyStatesAndSources() {
                    HashSet<string> usedPlainNames = new(StringComparer.Ordinal);
                    foreach (BondGraph.Element el in graph.getElements().Values) {
                        int id = el.GetID();
                        char type = char.ToUpper(el.GetTypeChar());
                        if (!bondsByElement.TryGetValue(id, out var elBonds) || elBonds.Count == 0) {
                            continue;
                        }
                        if (type == 'I' || type == 'C') {
                            BondGraph.Bond bond = elBonds[0];
                            bool receivesEffort = ElementReceivesEffort(bond, el);
                            bool isIntegral = (type == 'I') ? receivesEffort : !receivesEffort;
                            if (isIntegral) {
                                string prefix = wrapper.GetStateVarPrefix(el);
                                string varKey = (type == 'I' ? "P" : "Q") + id;
                                int displaySubId = id;
                                if (type == 'C') {
                                    BondGraph.Element other = GetOtherElement(bond, el);
                                    if (other.GetTypeChar() is '0' or '1') {
                                        displaySubId = other.GetID();
                                    }
                                }
                                string plainName = prefix + displaySubId;
                                if (!usedPlainNames.Add(plainName)) {
                                    displaySubId = id;
                                    plainName = prefix + displaySubId;
                                    usedPlainNames.Add(plainName);
                                }
                                string latexName = FormatSubscript(prefix, displaySubId.ToString());
                                varKeyToLatex[varKey] = latexName;
                                states.Add(new StateInfo(el, bond, type == 'I', varKey, plainName, latexName));
                            }
                        } else if (type == 'E' || type == 'F') {
                            string prefix = BondGraph.BondGraphWrapper.GetElementParamSymbol(el);
                            string key = (type == 'E' ? "Se" : "Sf") + id;
                            varKeyToLatex[key] = FormatSubscript(prefix, id.ToString());
                        }
                    }
                }

                private void AssignBondDisplayLabels() {
                    foreach (BondGraph.Bond bond in graph.getBonds()) {
                        BondGraph.Element src = bond.getSource();
                        BondGraph.Element dst = bond.getSink();
                        char srcType = char.ToUpper(src.GetTypeChar());
                        char dstType = char.ToUpper(dst.GetTypeChar());

                        BondGraph.Element? j1 = srcType == '1' ? src : (dstType == '1' ? dst : null);
                        BondGraph.Element? j0 = srcType == '0' ? src : (dstType == '0' ? dst : null);
                        BondGraph.Element? onePort = IsOnePort(srcType) ? src : (IsOnePort(dstType) ? dst : null);
                        BondGraph.Element? twoPort = (srcType == 'T' || srcType == 'G') ? src : ((dstType == 'T' || dstType == 'G') ? dst : null);

                        // Flow label matching BondGraph.AssignBondLabels
                        string flowLatex;
                        if (onePort != null && char.ToUpper(onePort.GetTypeChar()) == 'C' && !ElementReceivesEffort(bond, onePort)) {
                            string qKey = "Q" + onePort.GetID();
                            flowLatex = varKeyToLatex.TryGetValue(qKey, out string? qLatex)
                                ? qLatex + "'"
                                : FormatSubscript(wrapper.GetStateVarPrefix(onePort), onePort.GetID().ToString()) + "'";
                        } else if (j1 != null) {
                            string flwPre = BondGraph.BondGraphWrapper.GetFlowPrefix(wrapper.GetElementDomain(j1));
                            flowLatex = FormatSubscript(flwPre, j1.GetID().ToString());
                        } else if (j0 != null && onePort != null) {
                            string flwPre = BondGraph.BondGraphWrapper.GetFlowPrefix(wrapper.GetElementDomain(onePort));
                            flowLatex = FormatSubscript(flwPre, onePort.GetID().ToString());
                        } else if (twoPort != null) {
                            BondGraph.Element jEnd = ReferenceEquals(twoPort, src) ? dst : src;
                            string flwPre = BondGraph.BondGraphWrapper.GetFlowPrefix(wrapper.GetElementDomain(jEnd));
                            flowLatex = FormatSubscript(flwPre, twoPort.GetID().ToString());
                        } else {
                            string flwPre = BondGraph.BondGraphWrapper.GetFlowPrefix(wrapper.GetElementDomain(src));
                            flowLatex = FormatSubscript(flwPre, $"{src.GetID()},{dst.GetID()}");
                        }

                        // Effort label matching BondGraph.AssignBondLabels
                        string effLatex;
                        if (onePort != null && char.ToUpper(onePort.GetTypeChar()) == 'I' && ElementReceivesEffort(bond, onePort)) {
                            string pKey = "P" + onePort.GetID();
                            effLatex = varKeyToLatex.TryGetValue(pKey, out string? pLatex)
                                ? pLatex + "'"
                                : FormatSubscript(wrapper.GetStateVarPrefix(onePort), onePort.GetID().ToString()) + "'";
                        } else if (j0 != null) {
                            string effPre = BondGraph.BondGraphWrapper.GetEffortPrefix(wrapper.GetElementDomain(j0));
                            effLatex = FormatSubscript(effPre, j0.GetID().ToString());
                        } else if (onePort != null) {
                            char opType = char.ToUpper(onePort.GetTypeChar());
                            string effPre = (opType == 'E')
                                ? BondGraph.BondGraphWrapper.GetElementParamSymbol(onePort)
                                : BondGraph.BondGraphWrapper.GetEffortPrefix(wrapper.GetElementDomain(onePort));
                            effLatex = FormatSubscript(effPre, onePort.GetID().ToString());
                        } else if (twoPort != null) {
                            BondGraph.Element jEnd = ReferenceEquals(twoPort, src) ? dst : src;
                            string effPre = BondGraph.BondGraphWrapper.GetEffortPrefix(wrapper.GetElementDomain(jEnd));
                            var tpBonds = bondsByElement[twoPort.GetID()];
                            bool sameDomainOnBothSides = tpBonds.Count == 2 &&
                                wrapper.GetElementDomain(GetOtherElement(tpBonds[0], twoPort)) ==
                                wrapper.GetElementDomain(GetOtherElement(tpBonds[1], twoPort));
                            string sub = sameDomainOnBothSides
                                ? $"{twoPort.GetID()},{(bond.isSink(twoPort) ? "1" : "2")}"
                                : twoPort.GetID().ToString();
                            effLatex = FormatSubscript(effPre, sub);
                        } else {
                            string effPre = BondGraph.BondGraphWrapper.GetEffortPrefix(wrapper.GetElementDomain(src));
                            effLatex = FormatSubscript(effPre, $"{src.GetID()},{dst.GetID()}");
                        }

                        bondLabels[bond] = new(effLatex, flowLatex);
                    }
                }

                private static bool IsOnePort(char type) => type is 'I' or 'C' or 'R' or 'E' or 'F';

                private string GetBondVarLatex(BondGraph.Bond bond, bool effort) {
                    BondVarLabels lbl = bondLabels[bond];
                    return effort ? lbl.EffortLatex : lbl.FlowLatex;
                }

                private string FormatDynamicVarKey(string varKey) {
                    if (varKeyToLatex.TryGetValue(varKey, out string? existing)) {
                        return existing;
                    }
                    int primeCount = 0;
                    string baseKey = varKey;
                    while (baseKey.EndsWith("'")) {
                        primeCount++;
                        baseKey = baseKey[..^1];
                    }
                    string baseLatex = varKeyToLatex.TryGetValue(baseKey, out string? mapped)
                        ? mapped
                        : Expression.FormatLatexIdentifier(baseKey);
                    string primes = new string('\'', primeCount);
                    return baseLatex + primes;
                }

                /// <summary>
                /// Evaluates all integral-causality state equations, resolves any R-loop algebraic cycles,
                /// and isolates state derivatives via direct division (diagonal) or Gaussian elimination (coupled).
                /// </summary>
                public void Solve() {
                    if (states.Count == 0) {
                        return;
                    }

                    int n = states.Count;
                    LinearForm[] rawRhs = new LinearForm[n];
                    List<List<DerivationStep>> preSteps = new(n);
                    List<string> step3Latex = new(n);

                    for (int i = 0; i < n; i++) {
                        StateInfo st = states[i];
                        bool wantEffort = st.IsInductor;
                        List<string> branchTraceLines = new();
                        HashSet<(int, bool, int)> visiting = new();
                        HashSet<int> visitingR = new();

                        (LinearForm expr, string step1Rhs, string step3Rhs) = EvaluateFromStateRoot(
                            st, wantEffort, branchTraceLines, visiting, visitingR);

                        List<string> dedupTrace = branchTraceLines.Distinct().ToList();
                        rawRhs[i] = expr;
                        step3Latex.Add(step3Rhs);
                        InitialEquations.Add($"{st.DerivLatex} = {step1Rhs}");

                        List<DerivationStep> steps = new() {
                            new DerivationStep("1. Junction Equation", new List<string> { $"{st.DerivLatex} = {step1Rhs}" })
                        };
                        if (dedupTrace.Count > 0) {
                            steps.Add(new DerivationStep("2. Constitutive & Causal Expansions", dedupTrace));
                        }
                        preSteps.Add(steps);
                    }

                    // If any R-loop placeholders were created, solve the R linear system and substitute back.
                    if (rEquations.Count > 0) {
                        Dictionary<string, LinearForm> solvedRs = SolveRLoopSystem();
                        for (int i = 0; i < n; i++) {
                            foreach (var kv in solvedRs) {
                                rawRhs[i] = rawRhs[i].Substitute(kv.Key, kv.Value);
                            }
                        }
                    }

                    // Build M * x' = b across all integral states
                    SymbolicRational[,] matrix = new SymbolicRational[n, n];
                    LinearForm[] rhsVec = new LinearForm[n];
                    HashSet<string> allStateDerivKeys = new(states.Select(s => s.DerivKey), StringComparer.Ordinal);

                    bool hasAnyOffDiagonalCoupling = false;
                    for (int i = 0; i < n; i++) {
                        for (int j = 0; j < n; j++) {
                            SymbolicRational coeff = rawRhs[i].GetCoeff(states[j].DerivKey);
                            SymbolicRational mEntry = (i == j)
                                ? SymbolicRational.One.Subtract(coeff)
                                : coeff.Negate();
                            matrix[i, j] = mEntry;
                            if (i != j && !mEntry.IsZero) {
                                hasAnyOffDiagonalCoupling = true;
                            }
                        }
                        rhsVec[i] = rawRhs[i].WithoutVars(allStateDerivKeys.Contains);
                    }

                    LinearForm[]? coupledSolved = hasAnyOffDiagonalCoupling
                        ? LinearForm.SolveLinearSystem(matrix, rhsVec)
                        : null;

                    for (int i = 0; i < n; i++) {
                        StateInfo st = states[i];
                        List<DerivationStep> steps = preSteps[i];
                        bool rowHasDeriv = false;
                        for (int j = 0; j < n; j++) {
                            if (!rawRhs[i].GetCoeff(states[j].DerivKey).IsZero) {
                                rowHasDeriv = true;
                                break;
                            }
                        }

                        if (rEquations.Count > 0 || !rowHasDeriv) {
                            string substEq = $"{st.DerivLatex} = {step3Latex[i]}";
                            if (steps.Count == 1 || steps[0].LatexEquations[0] != substEq) {
                                steps.Add(new DerivationStep("3. Substitute into State Equation", new List<string> { substEq }));
                            }
                        } else {
                            steps.Add(new DerivationStep("3. Substitute into State Equation", new List<string> { $"{st.DerivLatex} = {step3Latex[i]}" }));
                        }

                        string finalEqLatex;
                        if (!rowHasDeriv) {
                            finalEqLatex = $"{st.DerivLatex} = {rhsVec[i].ToLatex(FormatDynamicVarKey)}";
                        } else if (!hasAnyOffDiagonalCoupling) {
                            SymbolicRational selfCoeffRhs = rawRhs[i].GetCoeff(st.DerivKey);
                            SymbolicRational lhsExtra = selfCoeffRhs.Negate();
                            string lhsFactorLatex = lhsExtra.IsNegative
                                ? $"1 - {lhsExtra.Negate().ToLatex()}"
                                : $"1 + {lhsExtra.ToLatex()}";
                            string rhsLatex = rhsVec[i].ToLatex(FormatDynamicVarKey);

                            List<string> step4Lines = new() {
                                $"{st.DerivLatex} = {rawRhs[i].ToLatex(FormatDynamicVarKey)}",
                                $"{st.DerivLatex} \\left({lhsFactorLatex}\\right) = {rhsLatex}"
                            };
                            steps.Add(new DerivationStep("4. Expand & Collect Derivative Terms", step4Lines));
                            finalEqLatex = $"{st.DerivLatex} = \\frac{{{rhsLatex}}}{{{lhsFactorLatex}}}";
                        } else {
                            StringBuilder lhsBuilder = new();
                            bool firstTerm = true;
                            for (int j = 0; j < n; j++) {
                                SymbolicRational entry = matrix[i, j];
                                if (entry.IsZero) continue;
                                bool neg = entry.IsNegative;
                                SymbolicRational absEntry = neg ? entry.Negate() : entry;
                                if (!firstTerm) {
                                    lhsBuilder.Append(neg ? " - " : " + ");
                                } else if (neg) {
                                    lhsBuilder.Append('-');
                                }
                                firstTerm = false;
                                if (absEntry.IsOne) {
                                    lhsBuilder.Append(states[j].DerivLatex);
                                } else {
                                    lhsBuilder.Append($"\\left({absEntry.ToLatex()}\\right) {states[j].DerivLatex}");
                                }
                            }
                            List<string> step4Lines = new() {
                                $"{st.DerivLatex} = {rawRhs[i].ToLatex(FormatDynamicVarKey)}",
                                $"{lhsBuilder} = {rhsVec[i].ToLatex(FormatDynamicVarKey)}"
                            };
                            steps.Add(new DerivationStep("4. Collect Coupled State Derivatives", step4Lines));
                            finalEqLatex = $"{st.DerivLatex} = {coupledSolved![i].ToLatex(FormatDynamicVarKey)}";
                        }

                        Derivations.Add(new EquationDerivation(
                            st.VarKey,
                            st.PlainName,
                            st.LatexName,
                            st.DerivLatex,
                            finalEqLatex,
                            rowHasDeriv,
                            steps));
                    }
                }

                private Dictionary<string, LinearForm> SolveRLoopSystem() {
                    int m = rLoopPlaceholders.Count;
                    List<(int id, string placeholder)> ordered = rLoopPlaceholders
                        .OrderBy(kv => kv.Key)
                        .Select(kv => (kv.Key, kv.Value))
                        .ToList();
                    Dictionary<string, int> idxMap = new(StringComparer.Ordinal);
                    for (int i = 0; i < m; i++) {
                        idxMap[ordered[i].placeholder] = i;
                    }

                    SymbolicRational[,] a = new SymbolicRational[m, m];
                    LinearForm[] b = new LinearForm[m];
                    for (int i = 0; i < m; i++) {
                        for (int j = 0; j < m; j++) {
                            a[i, j] = (i == j) ? SymbolicRational.One : SymbolicRational.Zero;
                        }
                        b[i] = LinearForm.Zero;
                    }

                    foreach (var eq in rEquations) {
                        string pKey = rLoopPlaceholders[eq.ElementId];
                        int row = idxMap[pKey];
                        for (int j = 0; j < m; j++) {
                            SymbolicRational coeff = eq.Rhs.GetCoeff(ordered[j].placeholder);
                            a[row, j] = (row == j ? SymbolicRational.One : SymbolicRational.Zero).Subtract(coeff);
                        }
                        b[row] = eq.Rhs.WithoutVars(idxMap.ContainsKey);
                    }

                    LinearForm[] solved = LinearForm.SolveLinearSystem(a, b);
                    Dictionary<string, LinearForm> result = new(StringComparer.Ordinal);
                    for (int i = 0; i < m; i++) {
                        result[ordered[i].placeholder] = solved[i];
                    }
                    return result;
                }

                private (LinearForm Form, string Step1Rhs, string Step3Rhs) EvaluateFromStateRoot(
                    StateInfo st,
                    bool wantEffort,
                    List<string> traceLines,
                    HashSet<(int, bool, int)> visiting,
                    HashSet<int> visitingR) {
                    BondGraph.Element other = GetOtherElement(st.Bond, st.Element);
                    char otherType = char.ToUpper(other.GetTypeChar());

                    if (otherType is '0' or '1') {
                        bool isSumming = (otherType == '1' && wantEffort) || (otherType == '0' && !wantEffort);
                        if (isSumming) {
                            List<BondGraph.Bond> neighbors = bondsByElement[other.GetID()]
                                .Where(b => !ReferenceEquals(b, st.Bond))
                                .ToList();
                            StringBuilder s1 = new();
                            StringBuilder s3 = new();
                            LinearForm sum = LinearForm.Zero;
                            bool first = true;
                            foreach (BondGraph.Bond b in neighbors) {
                                bool neg = IsPowerIntoElement(b, other) == IsPowerIntoElement(st.Bond, other);
                                string termName = GetBondVarLatex(b, wantEffort);
                                LinearForm branchForm = EvaluateBranchWithTrace(b, other, wantEffort, traceLines, visiting, visitingR, out string branchInline);

                                sum = neg ? sum.Subtract(branchForm) : sum.Add(branchForm);
                                AppendSignedTerm(s1, termName, neg, first);
                                AppendSignedTerm(s3, WrapTermForSubstitution(branchInline, neg && !first ? true : neg), neg, first);
                                first = false;
                            }
                            if (first) {
                                return (LinearForm.Zero, "0", "0");
                            }
                            return (sum, s1.ToString(), s3.ToString());
                        }
                    }

                    // Single shared variable (0-junction effort, 1-junction flow, or direct 2-port/1-port)
                    if (otherType is '0' or '1') {
                        BondGraph.Bond? drivingBond = FindDrivingBond(other, st.Bond, wantEffort);
                        if (drivingBond != null) {
                            string sym = GetBondVarLatex(drivingBond, wantEffort);
                            LinearForm form = EvaluateBranchWithTrace(drivingBond, other, wantEffort, traceLines, visiting, visitingR, out string inline);
                            return (form, sym, inline);
                        }
                    }

                    string directSym = GetBondVarLatex(st.Bond, wantEffort);
                    LinearForm directForm = EvaluateBondOutput(st.Bond, st.Element, wantEffort, traceLines, visiting, visitingR, emitTrace: true, out string directInline);
                    return (directForm, directSym, directInline);
                }

                private LinearForm EvaluateBranchWithTrace(
                    BondGraph.Bond bond,
                    BondGraph.Element requester,
                    bool wantEffort,
                    List<string> traceLines,
                    HashSet<(int, bool, int)> visiting,
                    HashSet<int> visitingR,
                    out string inlineLatex) {
                    return EvaluateBondOutput(bond, requester, wantEffort, traceLines, visiting, visitingR, emitTrace: true, out inlineLatex);
                }

                private BondGraph.Bond? FindDrivingBond(BondGraph.Element junction, BondGraph.Bond excludeBond, bool wantEffort) {
                    foreach (BondGraph.Bond b in bondsByElement[junction.GetID()]) {
                        if (ReferenceEquals(b, excludeBond)) continue;
                        bool receivesEffort = ElementReceivesEffort(b, junction);
                        if (wantEffort ? receivesEffort : !receivesEffort) {
                            return b;
                        }
                    }
                    return null;
                }

                private LinearForm EvaluateBondOutput(
                    BondGraph.Bond bond,
                    BondGraph.Element requester,
                    bool wantEffort,
                    List<string> traceLines,
                    HashSet<(int, bool, int)> visiting,
                    HashSet<int> visitingR,
                    bool emitTrace,
                    out string inlineLatex) {
                    BondGraph.Element provider = GetOtherElement(bond, requester);
                    int pId = provider.GetID();
                    char type = char.ToUpper(provider.GetTypeChar());
                    string thisVarLatex = GetBondVarLatex(bond, wantEffort);

                    if (type is '0' or '1') {
                        var visitKey = (pId, wantEffort, -1);
                        if (!visiting.Add(visitKey)) {
                            throw new InvalidOperationException("State equations cannot be derived in looped causality.");
                        }
                        try {
                            bool isSumming = (type == '1' && wantEffort) || (type == '0' && !wantEffort);
                            if (isSumming) {
                                List<BondGraph.Bond> others = bondsByElement[pId].Where(b => !ReferenceEquals(b, bond)).ToList();
                                StringBuilder symSum = new();
                                LinearForm sum = LinearForm.Zero;
                                bool first = true;
                                List<string> subLines = new();

                                foreach (BondGraph.Bond b in others) {
                                    bool neg = IsPowerIntoElement(b, provider) == IsPowerIntoElement(bond, provider);
                                    AppendSignedTerm(symSum, GetBondVarLatex(b, wantEffort), neg, first);
                                    first = false;
                                }
                                if (emitTrace) {
                                    traceLines.Add($"{thisVarLatex} = {symSum}");
                                }

                                first = true;
                                StringBuilder expanded = new();
                                foreach (BondGraph.Bond b in others) {
                                    bool neg = IsPowerIntoElement(b, provider) == IsPowerIntoElement(bond, provider);
                                    LinearForm childForm = EvaluateBondOutput(b, provider, wantEffort, subLines, visiting, visitingR, emitTrace, out string childInline);
                                    sum = neg ? sum.Subtract(childForm) : sum.Add(childForm);
                                    AppendSignedTerm(expanded, WrapTermForSubstitution(childInline, neg), neg, first);
                                    first = false;
                                }
                                if (emitTrace) {
                                    traceLines.AddRange(subLines);
                                    string combined = sum.ToLatex(FormatDynamicVarKey);
                                    traceLines.Add($"\\Rightarrow {thisVarLatex} = {combined}");
                                }
                                inlineLatex = sum.ToLatex(FormatDynamicVarKey);
                                return sum;
                            } else {
                                BondGraph.Bond? driving = FindDrivingBond(provider, bond, wantEffort);
                                if (driving == null) {
                                    throw new InvalidOperationException($"Incomplete causality at junction {pId}.");
                                }
                                LinearForm res = EvaluateBondOutput(driving, provider, wantEffort, traceLines, visiting, visitingR, emitTrace, out inlineLatex);
                                return res;
                            }
                        } finally {
                            visiting.Remove(visitKey);
                        }
                    }

                    if (type == 'T') {
                        var tpBonds = bondsByElement[pId];
                        if (tpBonds.Count != 2) {
                            throw new InvalidOperationException($"Transformer {pId} must have exactly 2 bonds.");
                        }
                        int portIdx = ReferenceEquals(tpBonds[0], bond) ? 0 : 1;
                        var visitKey = (pId, wantEffort, portIdx);
                        if (!visiting.Add(visitKey)) {
                            throw new InvalidOperationException("State equations cannot be derived in looped causality.");
                        }
                        try {
                            BondGraph.Bond otherBond = ReferenceEquals(tpBonds[0], bond) ? tpBonds[1] : tpBonds[0];
                            BondGraph.Bond inBond = tpBonds.FirstOrDefault(b => b.isSink(provider)) ?? tpBonds[0];
                            bool isBondIn = ReferenceEquals(bond, inBond);
                            bool samePowerDir = IsPowerIntoElement(bond, provider) == IsPowerIntoElement(otherBond, provider);

                            string mName = GetParamKey(provider);
                            SymbolicRational m = SymbolicRational.FromVar(mName);
                            string mLatex = Expression.FormatLatexIdentifier(mName);
                            bool multiply = (wantEffort && isBondIn) || (!wantEffort && !isBondIn);

                            // For a rack-and-pinion TF:R connecting translational ("F") and rotational ("τ") domains,
                            // radius R satisfies v_trans = R * ω_rot and τ_rot = R * F_trans (i.e. effective modulus is 1/R when in=trans, out=rot).
                            string inDomain = wrapper.GetElementDomain(GetOtherElement(inBond, provider));
                            string outDomain = wrapper.GetElementDomain(GetOtherElement(ReferenceEquals(tpBonds[0], inBond) ? tpBonds[1] : tpBonds[0], provider));
                            if (inDomain == "F" && outDomain == "τ") {
                                multiply = !multiply;
                            }

                            string otherVarLatex = GetBondVarLatex(otherBond, wantEffort);
                            string factorLatex = multiply ? mLatex : $"\\frac{{1}}{{{mLatex}}}";
                            string signStr = samePowerDir ? "-" : "";

                            if (emitTrace) {
                                traceLines.Add($"{thisVarLatex} = {signStr}{factorLatex} {otherVarLatex}");
                            }

                            LinearForm inner = EvaluateBondOutput(otherBond, provider, wantEffort, traceLines, visiting, visitingR, emitTrace, out string innerInline);
                            LinearForm scaled = multiply ? inner.Scale(m) : inner.DivideBy(m);
                            if (samePowerDir) {
                                scaled = scaled.Negate();
                            }

                            bool needsParens = inner.Terms.Count > 1 || innerInline.Contains(" + ") || innerInline.Contains(" - ");
                            string wrappedInner = needsParens ? $"\\left({innerInline}\\right)" : innerInline;
                            inlineLatex = $"{signStr}{factorLatex} {wrappedInner}";

                            if (emitTrace && !inner.IsZero && needsParens) {
                                traceLines.Add($"\\Rightarrow {thisVarLatex} = {inlineLatex}");
                            }
                            return scaled;
                        } finally {
                            visiting.Remove(visitKey);
                        }
                    }

                    if (type == 'G') {
                        var gyBonds = bondsByElement[pId];
                        if (gyBonds.Count != 2) {
                            throw new InvalidOperationException($"Gyrator {pId} must have exactly 2 bonds.");
                        }
                        int portIdx = ReferenceEquals(gyBonds[0], bond) ? 0 : 1;
                        var visitKey = (pId, wantEffort, portIdx);
                        if (!visiting.Add(visitKey)) {
                            throw new InvalidOperationException("State equations cannot be derived in looped causality.");
                        }
                        try {
                            BondGraph.Bond otherBond = ReferenceEquals(gyBonds[0], bond) ? gyBonds[1] : gyBonds[0];
                            bool samePowerDir = IsPowerIntoElement(bond, provider) == IsPowerIntoElement(otherBond, provider);

                            string rName = GetParamKey(provider);
                            SymbolicRational r = SymbolicRational.FromVar(rName);
                            string rLatex = Expression.FormatLatexIdentifier(rName);
                            bool multiply = wantEffort;

                            string otherVarLatex = GetBondVarLatex(otherBond, !wantEffort);
                            string factorLatex = multiply ? rLatex : $"\\frac{{1}}{{{rLatex}}}";
                            string signStr = samePowerDir ? "-" : "";

                            if (emitTrace) {
                                traceLines.Add($"{thisVarLatex} = {signStr}{factorLatex} {otherVarLatex}");
                            }

                            LinearForm inner = EvaluateBondOutput(otherBond, provider, !wantEffort, traceLines, visiting, visitingR, emitTrace, out string innerInline);
                            LinearForm scaled = multiply ? inner.Scale(r) : inner.DivideBy(r);
                            if (samePowerDir) {
                                scaled = scaled.Negate();
                            }

                            bool needsParens = inner.Terms.Count > 1 || innerInline.Contains(" + ") || innerInline.Contains(" - ");
                            string wrappedInner = needsParens ? $"\\left({innerInline}\\right)" : innerInline;
                            inlineLatex = $"{signStr}{factorLatex} {wrappedInner}";

                            if (emitTrace && !inner.IsZero && needsParens) {
                                traceLines.Add($"\\Rightarrow {thisVarLatex} = {inlineLatex}");
                            }
                            return scaled;
                        } finally {
                            visiting.Remove(visitKey);
                        }
                    }

                    if (type == 'I') {
                        bool receivesEffort = ElementReceivesEffort(bond, provider);
                        string iName = GetParamKey(provider);
                        SymbolicRational iParam = SymbolicRational.FromVar(iName);
                        string iLatex = Expression.FormatLatexIdentifier(iName);

                        if (receivesEffort && !wantEffort) {
                            string pKey = "P" + pId;
                            LinearForm res = LinearForm.FromScaledVar(pKey, SymbolicRational.One.Divide(iParam));
                            string pLatex = FormatDynamicVarKey(pKey);
                            inlineLatex = $"\\frac{{{pLatex}}}{{{iLatex}}}";
                            if (emitTrace) {
                                string flowSym = GetBondVarLatex(bond, false);
                                traceLines.Add($"{flowSym} = {inlineLatex}");
                            }
                            return res;
                        }
                        if (!receivesEffort && wantEffort) {
                            // Derivative causality on I: e = d/dt(I * f_in) = I * d/dt(f_in)
                            string flowSym = GetBondVarLatex(bond, false);
                            List<string> innerTrace = new();
                            LinearForm fIn = EvaluateBondOutput(bond, provider, false, innerTrace, visiting, visitingR, emitTrace, out string fInInline);
                            if (emitTrace) {
                                traceLines.AddRange(innerTrace);
                            }
                            LinearForm deriv = fIn.Differentiate().Scale(iParam);
                            string derivLatex = deriv.ToLatex(FormatDynamicVarKey);
                            if (emitTrace) {
                                traceLines.Add($"{thisVarLatex} = \\frac{{d}}{{dt}}\\left({iLatex} {flowSym}\\right) = {iLatex} \\frac{{d}}{{dt}}\\left({fInInline}\\right) = {derivLatex}");
                            }
                            inlineLatex = derivLatex;
                            return deriv;
                        }
                        throw new InvalidOperationException($"Causal mismatch on I element {pId}.");
                    }

                    if (type == 'C') {
                        bool receivesEffort = ElementReceivesEffort(bond, provider);
                        string cName = GetParamKey(provider);
                        bool isStiffnessK = BondGraph.BondGraphWrapper.GetElementParamSymbol(provider) is "K" or "κ";
                        SymbolicRational cConst = SymbolicRational.FromVar(cName);
                        string cLatex = Expression.FormatLatexIdentifier(cName);

                        if (!receivesEffort && wantEffort) {
                            string qKey = "Q" + pId;
                            LinearForm res = isStiffnessK
                                ? LinearForm.FromScaledVar(qKey, cConst)
                                : LinearForm.FromScaledVar(qKey, SymbolicRational.One.Divide(cConst));
                            string qLatex = FormatDynamicVarKey(qKey);
                            inlineLatex = isStiffnessK ? $"{cLatex} {qLatex}" : $"\\frac{{{qLatex}}}{{{cLatex}}}";
                            if (emitTrace) {
                                traceLines.Add($"{thisVarLatex} = {inlineLatex}");
                            }
                            return res;
                        }
                        if (receivesEffort && !wantEffort) {
                            // Derivative causality on C: f = d/dt(e_in / K) or d/dt(C * e_in)
                            string effSym = GetBondVarLatex(bond, true);
                            List<string> innerTrace = new();
                            LinearForm eIn = EvaluateBondOutput(bond, provider, true, innerTrace, visiting, visitingR, emitTrace, out string eInInline);
                            if (emitTrace) {
                                traceLines.AddRange(innerTrace);
                            }
                            LinearForm deriv = isStiffnessK
                                ? eIn.Differentiate().DivideBy(cConst)
                                : eIn.Differentiate().Scale(cConst);
                            string derivLatex = deriv.ToLatex(FormatDynamicVarKey);
                            if (emitTrace) {
                                string body = isStiffnessK
                                    ? $"\\frac{{{effSym}}}{{{cLatex}}}"
                                    : $"{cLatex} {effSym}";
                                string expandedBody = isStiffnessK
                                    ? $"\\frac{{1}}{{{cLatex}}} \\frac{{d}}{{dt}}\\left({eInInline}\\right)"
                                    : $"{cLatex} \\frac{{d}}{{dt}}\\left({eInInline}\\right)";
                                traceLines.Add($"{thisVarLatex} = \\frac{{d}}{{dt}}\\left({body}\\right) = {expandedBody} = {derivLatex}");
                            }
                            inlineLatex = derivLatex;
                            return deriv;
                        }
                        throw new InvalidOperationException($"Causal mismatch on C element {pId}.");
                    }

                    if (type == 'R') {
                        string rName = GetParamKey(provider);
                        SymbolicRational rParam = SymbolicRational.FromVar(rName);
                        string rLatex = Expression.FormatLatexIdentifier(rName);

                        if (!visitingR.Add(pId)) {
                            string placeholder = "__R" + pId;
                            rLoopPlaceholders[pId] = placeholder;
                            inlineLatex = thisVarLatex;
                            return LinearForm.FromVar(placeholder);
                        }
                        try {
                            string conjSym = GetBondVarLatex(bond, !wantEffort);
                            List<string> innerTrace = new();
                            // Start a fresh junction/2-port loop check inside R so algebraic loops through R are caught by visitingR.
                            LinearForm conjForm = EvaluateBondOutput(bond, provider, !wantEffort, innerTrace, new HashSet<(int, bool, int)>(), visitingR, emitTrace, out string conjInline);
                            LinearForm res = wantEffort ? conjForm.Scale(rParam) : conjForm.DivideBy(rParam);

                            if (rLoopPlaceholders.TryGetValue(pId, out string? loopPlaceholder)) {
                                if (!rEquations.Any(e => e.ElementId == pId)) {
                                    rEquations.Add((pId, wantEffort, res));
                                }
                                inlineLatex = thisVarLatex;
                                return LinearForm.FromVar(loopPlaceholder);
                            }

                            string resLatex = res.ToLatex(FormatDynamicVarKey);
                            if (emitTrace) {
                                traceLines.AddRange(innerTrace);
                                if (wantEffort) {
                                    traceLines.Add($"{thisVarLatex} = {rLatex} {conjSym} = {rLatex} \\left({conjInline}\\right) = {resLatex}");
                                } else {
                                    traceLines.Add($"{thisVarLatex} = \\frac{{{conjSym}}}{{{rLatex}}} = \\frac{{{conjInline}}}{{{rLatex}}} = {resLatex}");
                                }
                            }
                            inlineLatex = resLatex;
                            return res;
                        } finally {
                            visitingR.Remove(pId);
                        }
                    }

                    if (type == 'E') {
                        if (!wantEffort) {
                            throw new InvalidOperationException($"Causal mismatch: effort source {pId} cannot output flow.");
                        }
                        string seKey = "Se" + pId;
                        inlineLatex = FormatDynamicVarKey(seKey);
                        return LinearForm.FromVar(seKey);
                    }

                    if (type == 'F') {
                        if (wantEffort) {
                            throw new InvalidOperationException($"Causal mismatch: flow source {pId} cannot output effort.");
                        }
                        string sfKey = "Sf" + pId;
                        inlineLatex = FormatDynamicVarKey(sfKey);
                        return LinearForm.FromVar(sfKey);
                    }

                    throw new InvalidOperationException($"Unsupported element type '{provider.label}' in equation generation.");
                }

                private static string WrapTermForSubstitution(string exprLatex, bool precededByMinus) {
                    if (precededByMinus && (exprLatex.Contains(" + ") || exprLatex.Contains(" - ") || exprLatex.StartsWith("-"))) {
                        return $"\\left({exprLatex}\\right)";
                    }
                    return exprLatex;
                }

                private static void AppendSignedTerm(StringBuilder sb, string termLatex, bool neg, bool first) {
                    if (first) {
                        if (neg) sb.Append('-');
                        sb.Append(termLatex);
                    } else {
                        sb.Append(neg ? " - " : " + ");
                        sb.Append(termLatex);
                    }
                }
            }
        }
    }
}

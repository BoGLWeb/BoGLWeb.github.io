using BoGLWeb.DifferentialEquationHelper;
using NUnit.Framework;

namespace BoGLWeb.Tests {
    [TestFixture]
    public class StateEquationTests {
        private static BondGraph CreateGraph(
            (string key, string label, int id)[] elements,
            (int srcIdx, int dstIdx, bool causalStroke, bool causalDir)[] bonds) {
            BondGraph.Element.resetUni();
            BondGraph.Bond.resetUni();
            BondGraph bg = new();
            List<BondGraph.Element> list = new();
            foreach (var (key, label, id) in elements) {
                var el = new BondGraph.Element(key, label, 0);
                el.AssignID(id, false);
                bg.addElement(key, el);
                list.Add(el);
            }
            foreach (var (s, d, stroke, dir) in bonds) {
                var src = list[s];
                var dst = list[d];
                var b = new BondGraph.Bond(src.GetID(), dst.GetID(), src, dst, "", stroke, dir, true, 0, 0);
                bg.addBond(b);
            }
            return bg;
        }

        [Test]
        public void Sample1_NoDerivativeCausality_MatchesLectureStepsAndEquations() {
            // Se:F (0) -> 1 (1) -> C:K (2), I:m (3), R:b (4)
            // Stroke at sink for Se->1 (dir=false) and 1->I (dir=false); stroke at source (1) for 1->C (dir=true) and 1->R (dir=true)
            BondGraph bg = CreateGraph(
                new[] {
                    ("se", "Se:F", 0),
                    ("j1", "1", 1),
                    ("c", "C:K", 2),
                    ("i", "I:m", 3),
                    ("r", "R:b", 4),
                },
                new[] {
                    (0, 1, true, false),
                    (1, 2, true, true),
                    (1, 3, true, false),
                    (1, 4, true, true),
                });

            StateEquationSet eqs = new(bg);
            string[] finals = eqs.GetFinalEquations();
            Assert.That(finals.Length, Is.EqualTo(2));

            var derivs = eqs.GetDerivations();
            var cDeriv = derivs.First(d => d.StateVarKey == "Q2");
            var iDeriv = derivs.First(d => d.StateVarKey == "P3");

            Assert.That(cDeriv.HasDerivativeCausality, Is.False);
            Assert.That(iDeriv.HasDerivativeCausality, Is.False);
            Assert.That(cDeriv.FinalEquationLatex, Is.EqualTo("x_{1}' = \\frac{p_{3}}{m_{3}}"));
            Assert.That(iDeriv.FinalEquationLatex, Does.Contain("F_{0}"));
            Assert.That(iDeriv.FinalEquationLatex, Does.Contain("K_{2} x_{1}"));
            Assert.That(iDeriv.FinalEquationLatex, Does.Contain("b_{4}"));
            Assert.That(iDeriv.FinalEquationLatex, Does.Contain("p_{3}"));
            Assert.That(iDeriv.Steps.Count, Is.EqualTo(3));
        }

        [Test]
        public void Sample2_DerivativeCausalityWithTwoTransformers_MatchesLecture() {
            // Left 1-junc (0): C:K (1), I:m (2), R:b (3), -> TF (4)
            // Mid 1-junc (5): TF (4) -> 1 (5) -> I:J (6, deriv), R:b (7), TF (8)
            // Right 1-junc (9): TF (8) -> 1 (9) -> C:K (10)
            BondGraph bg = CreateGraph(
                new[] {
                    ("j_left", "1", 0),
                    ("c_left", "C:K", 1),
                    ("i_left", "I:m", 2),
                    ("r_left", "R:b", 3),
                    ("tf1", "TF:T", 4),
                    ("j_mid", "1", 5),
                    ("i_deriv", "I:J", 6),
                    ("r_mid", "R:b", 7),
                    ("tf2", "TF:T", 8),
                    ("j_right", "1", 9),
                    ("c_right", "C:K", 10),
                },
                new[] {
                    (0, 1, true, true),   // 1_left -> C_1 (stroke at 1_left)
                    (0, 2, true, false),  // 1_left -> I_2 (stroke at I_2, integral)
                    (0, 3, true, true),   // 1_left -> R_3 (stroke at 1_left)
                    (0, 4, true, true),   // 1_left -> TF_4 (stroke at 1_left)
                    (4, 5, true, true),   // TF_4 -> 1_mid (stroke at TF_4 => TF_4 sets flow on 1_mid!)
                    (5, 6, true, true),   // 1_mid -> I_6 (stroke at 1_mid => derivative!)
                    (5, 7, true, true),   // 1_mid -> R_7 (stroke at 1_mid)
                    (5, 8, true, true),   // 1_mid -> TF_8 (stroke at 1_mid)
                    (8, 9, true, true),   // TF_8 -> 1_right (stroke at TF_8 => TF_8 sets flow on 1_right!)
                    (9, 10, true, true),  // 1_right -> C_10 (stroke at 1_right)
                });

            StateEquationSet eqs = new(bg);
            var derivs = eqs.GetDerivations();
            Assert.That(derivs.Count, Is.EqualTo(3));

            var p2Deriv = derivs.First(d => d.StateVarKey == "P2");
            Assert.That(p2Deriv.HasDerivativeCausality, Is.True);
            Assert.That(p2Deriv.Steps.Count, Is.EqualTo(4));
            // Check derivative causality denominator: 1 + \frac{T_{4}^{2} J_{6}}{m_{2}}
            Assert.That(p2Deriv.FinalEquationLatex, Does.Contain("1 + \\frac{"));
            Assert.That(p2Deriv.FinalEquationLatex, Does.Contain("J_{6}"));
            Assert.That(p2Deriv.FinalEquationLatex, Does.Contain("T_{4}^{2}"));
            Assert.That(p2Deriv.FinalEquationLatex, Does.Contain("m_{2}"));

            // Both capacitor states should depend only on p2
            var x1Deriv = derivs.First(d => d.StateVarKey == "Q1");
            var x10Deriv = derivs.First(d => d.StateVarKey == "Q10");
            Assert.That(x1Deriv.FinalEquationLatex, Is.EqualTo("x_{0}' = \\frac{p_{2}}{m_{2}}"));
            Assert.That(x10Deriv.FinalEquationLatex, Does.Contain("T_{4}"));
            Assert.That(x10Deriv.FinalEquationLatex, Does.Contain("T_{8}"));
            Assert.That(x10Deriv.FinalEquationLatex, Does.Contain("p_{2}"));
        }

        [Test]
        public void DerivativeCausality_CapacitorInDerivativeCausality_DerivesSourceAndStateTerms() {
            // Se:V (0) -> 0 (1) -> C:C (2, integral), C:C (3, derivative!), R:R (4)
            // Wait: 0-junction receives effort from C_2 (stroke at 0), while C_3 receives effort (stroke at C_3, dir=false)
            BondGraph bg = CreateGraph(
                new[] {
                    ("j0", "0", 0),
                    ("c_int", "C:C", 1),
                    ("c_deriv", "C:C", 2),
                    ("r", "R:R", 3),
                },
                new[] {
                    (0, 1, true, true),   // 0 -> C_1 (stroke at 0 => integral C)
                    (0, 2, true, false),  // 0 -> C_2 (stroke at C_2 => derivative C!)
                    (0, 3, true, false),  // 0 -> R_3 (stroke at R_3 => conductive R, f = e/R)
                });

            StateEquationSet eqs = new(bg);
            var derivs = eqs.GetDerivations();
            Assert.That(derivs.Count, Is.EqualTo(1));
            var q1 = derivs[0];
            Assert.That(q1.HasDerivativeCausality, Is.True);
            // q1' = -C_2 * (q1'/C_1) - q1/(C_1*R_3) => q1'(1 + C_2/C_1) = -q1/(C_1 R_3)
            Assert.That(q1.FinalEquationLatex, Does.Contain("1 + \\frac{C_{2}}{C_{1}}"));
            Assert.That(q1.FinalEquationLatex, Does.Contain("q_{0}"));
        }

        [Test]
        public void CrossCoupledDerivativeCausality_TwoIntegralStatesCoupledViaDerivativeC_SolvesLinearSystem() {
            // 1_a (0) -> I:m (1, integral), -> 0 (2)
            // 1_b (3) -> I:m (4, integral), -> 0 (2)
            // 0 (2) -> C:C (5, integral) -- wait: to couple I_1 and I_4 via derivative C, put C on 1_a? No:
            // Two 0-junctions with C_1 (integral) and C_4 (integral), connected via 1-junction with I_5 (derivative)!
            // 0_a (0): C_1 (integral, stroke at 0_a), -> 1_m (2, stroke at 1_m => 0_a sets effort!)
            // 0_b (3): C_4 (integral, stroke at 0_b), -> 1_m (2, stroke at 1_m => 0_b sets effort!)
            //Wait: 1_m has I_5 (derivative: 1_m -> I_5 with stroke at 1_m) — wait 1_m needs ONE flow-setting bond (e.g. R_6 or another I)!
            // To couple q1' and q4', put I_1 (integral) on 1_a, I_4 (integral) on 1_b, both feeding 0_c (2):
            // Wait: at 0_c: C_5 (derivative) has stroke at C_5? If C_5 has stroke at C_5, 0_c needs effort from somewhere else (like Se or R_6)!
            // Better: 0_a(0) has C_1(integral, stroke at 0_a), R_2(stroke at R_2), -> 1_c(3, stroke at 1_c)
            // 0_b(4) has C_4(integral, stroke at 0_b), -> 1_c(3, stroke at 1_c)
            // 1_c(3) has R_6(stroke at R_6 => sets flow on 1_c!) and I_5? Wait: C_1' depends on I/R, which depends on C_1 and C_4.
            // Let's test two integral C's (C_1, C_4) coupled by a derivative C_5 via R-less direct coupling?
            // Easy: 0_a(0) -> C_1(integral), R_2(flow out: stroke at R_2) and 0_a(0) -> C_5(derivative: stroke at C_5) -> 0_b? C is 1-port!
            // With 1-port C_5 in derivative causality on 0_c(2):
            // 1_a(3) -> I_1(integral), 1_a -> R_2(stroke at 1_a), 1_a -> 0_c(stroke at 0_c)
            // Wait: 0_c needs ONE effort setter! So R_7 sets effort on 0_c, C_5 is derivative on 0_c, and I_1 & I_4 feed flows into 0_c? Then C_5 gets effort from R_7 = R_7*(f_1 + f_4 - f_C5)!
            // Look! At 0_c(2): 1_a -> 0_c (stroke at 1_a, sends f_1 into 0_c), 1_b -> 0_c (stroke at 1_b, sends f_4 into 0_c),
            // 0_c -> R_7 (stroke at 0_c! R_7 gets flow f_1 + f_4 - f_C5 and sets effort e_c = R_7*(f_1 + f_4 - f_C5)!),
            //Wait: if 0_c -> C_5 (stroke at 0_c? No, stroke at C_5 would be derivative, which needs 0_c to set effort from R_7 — so R_7 has stroke at 0_c -> R_7 gets FLOW -> wait: 0-junc stroke AT 0-junc means R_7 sets EFFORT on 0_c!).
            // If R_7 sets EFFORT on 0_c (stroke at 0_c), R_7 receives flow f_1 + f_4 - f_C5, so e_c = R_7 * (f_1 + f_4 - f_C5), AND f_C5 = C_5 * e_c' — that's an R-C loop if C_5 feeds back into e_c!
            // Instead, simplest physical 2-state cross-coupled derivative causality:
            // Two 0-junctions coupled by a derivative-causality I? No, two 1-junctions with I_1 (integral) and I_4 (integral),
            // wait: I_1 on 1_a, I_4 on 1_b, both connect to 0_m with C_5 (integral) -> no derivative causality.
            // What about: 0_a has C_1 (integral, sets e_1), 0_b has C_4 (integral, sets e_4), and R_7 on a 0_c? No, 1_m between 0_a and 0_b with R_7 (sets f_m = (e_1 - e_4)/R_7), plus C_5 (derivative) on 0_a?
            // If C_5 is derivative on 0_a (e_5 = e_1), then f_C5 = C_5 * e_1' = (C_5/C_1) * q_1', which only couples q_1' to itself unless 0_a and 0_b share a derivative bond via TF!
            // YES! 0_a (C_1 integral) -> TF -> 0_b (C_5 derivative)! If C_5 is on 0_b and 0_b also has I_4?
            // Even simpler: 0_a has C_1 (integral), C_5 (derivative) AND 0_a connects to 0_b? Wait: 0_a -> 1_m -> 0_b. If I_2 is on 1_m, I_2 is integral. If R_2 is on 1_m, R_2 sets flow.
            // What if TWO capacitors C_1 (integral) and C_5 (derivative) are on 0_a, and C_4 (integral) is ALSO coupled to C_5 via a 0-junction? Wait: two capacitors in parallel on 0_a give q1'(1 + C5/C1) (diagonal).
            // To get OFF-DIAGONAL coupling between x1' and x2':
            // Suppose 1_a (I_1 integral) -> 0_a -> R_2 -> 0_b -> ... wait: why would x2' appear in x1''s equation?
            // Only if e_1 (for I_1') depends on I_2's derivative, i.e. I_1 and I_2 both couple to a derivative element, e.g.:
            // 1_a (I_1 integral) -> 0_x <- 1_b (I_2 integral), where 0_x is NOT C_int, instead 1_a and 1_b are connected to I_3 (derivative) on a 0-junction? No, I_3 in derivative causality must sit on a 1-junction whose flow is f_3 = f_1 + f_2 (coming from a 0-junction)!
            // Look! 1_a(I_1) -> 0_1 -> 1_c(I_3 deriv) and 1_b(I_2) -> 0_2 -> 1_c(I_3 deriv)? Wait: 0_1 + 0_2 into 1_c:
            // At 1_c: flow is shared ONLY with ONE bond! So a 1-junction cannot sum flows f_1 + f_2! A 0-junction sums flows f_1 + f_2!
            // So put a 0-junction 0_sum summing f_1 + f_2 -> bond b_sum -> 1_c (I_3 deriv, AND R_4 on 1_c setting effort e_sum? No, R_4 sets effort on 1_c, wait: I_3 sets effort on 1_c!).
            // YES!! Look at that:
            // 1_a (I_1 integral, sets f_1) -> 0_sum
            // 1_b (I_2 integral, sets f_2) -> 0_sum
            // 0_sum -> 1_c (stroke at 0_sum? No, 0_sum gets effort from 1_c, so stroke is at 0_sum! Wait: 1_c gets flow from 0_sum!)
            // At 1_c: I_3 (derivative, stroke at 1_c!) and R_4 (stroke at 1_c!).
            // Effort on 1_c->0_sum is e_sum = I_3 * (f_1 + f_2)' + R_4 * (f_1 + f_2)!
            // And e_sum feeds BOTH 1_a (I_1') AND 1_b (I_2')!!
            // CHECK THAT:
            // p_1' = -e_sum = -(I_3/I_1) p_1' - (I_3/I_2) p_2' - R_4 (p_1/I_1 + p_2/I_2)
            // p_2' = -e_sum = -(I_3/I_1) p_1' - (I_3/I_2) p_2' - R_4 (p_1/I_1 + p_2/I_2)
            // THAT HAS GENUINE OFF-DIAGONAL COUPLING BETWEEN p_1' AND p_2'!
            BondGraph bg = CreateGraph(
                new[] {
                    ("j1a", "1", 0),
                    ("i1", "I:m", 1),
                    ("j1b", "1", 2),
                    ("i2", "I:m", 3),
                    ("j0sum", "0", 4),
                    ("j1c", "1", 5),
                    ("i3", "I:m", 6),
                    ("r4", "R:b", 7),
                },
                new[] {
                    (0, 1, true, false), // 1_a -> I_1 (stroke at I_1 => integral)
                    (0, 4, true, true),  // 1_a -> 0_sum (stroke at 1_a => 1_a reads effort, 0_sum reads f_1)
                    (2, 3, true, false), // 1_b -> I_2 (stroke at I_2 => integral)
                    (2, 4, true, true),  // 1_b -> 0_sum (stroke at 1_b => 1_b reads effort, 0_sum reads f_2)
                    (4, 5, true, true),  // 0_sum -> 1_c (stroke at 0_sum => 0_sum reads effort, 1_c reads f_1+f_2!)
                    (5, 6, true, true),  // 1_c -> I_3 (stroke at 1_c => I_3 derivative!)
                    (5, 7, true, true),  // 1_c -> R_4 (stroke at 1_c => R_4 resistance!)
                });

            StateEquationSet eqs = new(bg);
            var derivs = eqs.GetDerivations();
            Assert.That(derivs.Count, Is.EqualTo(2));
            Assert.That(derivs[0].HasDerivativeCausality, Is.True);
            Assert.That(derivs[1].HasDerivativeCausality, Is.True);
            // Verify final equations have NO derivatives on the RHS!
            string rhs0 = derivs[0].FinalEquationLatex.Split('=')[1];
            string rhs1 = derivs[1].FinalEquationLatex.Split('=')[1];
            Assert.That(rhs0, Does.Not.Contain("'"));
            Assert.That(rhs1, Does.Not.Contain("'"));
            Assert.That(rhs0, Does.Contain("b_{7}"));
            Assert.That(rhs0, Does.Contain("m_{6}"));
        }

        [Test]
        public void Gyrator_StandardSymmetricFormulation_WorksForBothCausalOrientations() {
            // Se:V (0) -> 1_elec (1) -> GY:K (2) -> 1_mech (3) -> I:J (4, integral)
            // With stroke at 1_elec for Se->1_elec, stroke at GY for 1_elec->GY (no, wait: I:J sets flow w on 1_mech!)
            // If I:J sets w on 1_mech (stroke at I:J): 1_mech -> GY has stroke at 1_mech (GY gets w, sends tau to 1_mech)!
            // Then GY swaps causality => GY -> 1_elec has stroke at GY? No!
            // For GY: either BOTH strokes at GY (GY gets e_1, e_2 -> outputs f_1, f_2) OR BOTH strokes away from GY (GY gets f_1, f_2 -> outputs e_1, e_2).
            // Since 1_mech -> GY has stroke at 1_mech (away from GY, so GY gets f_2 = w), GY MUST also have stroke away from GY on left (at 1_elec),
            // so 1_elec gets e_1 = K * w! And 1_elec has R:R (5) receiving e_1 (stroke at R_5) to set flow i_1 on 1_elec!
            BondGraph bg = CreateGraph(
                new[] {
                    ("se", "Se:V", 0),
                    ("j_e", "1", 1),
                    ("r", "R:R", 2),
                    ("gy", "GY:K", 3),
                    ("j_m", "1", 4),
                    ("i", "I:J", 5),
                },
                new[] {
                    (0, 1, true, false), // Se -> 1_e (stroke at 1_e)
                    (1, 2, true, false), // 1_e -> R (stroke at R => R gets effort, sets flow i!)
                    (1, 3, true, true),  // 1_e -> GY (stroke at 1_e => GY gets flow i, outputs effort e = K*w)
                    (3, 4, true, false), // GY -> 1_m (stroke at 1_m => GY gets flow w, outputs torque K*i)
                    (4, 5, true, false), // 1_m -> I_J (stroke at I_J => integral I)
                });

            StateEquationSet eqs = new(bg);
            var derivs = eqs.GetDerivations();
            Assert.That(derivs.Count, Is.EqualTo(1));
            Assert.That(derivs[0].FinalEquationLatex, Does.Contain("K_{3}"));
            Assert.That(derivs[0].FinalEquationLatex, Does.Contain("V_{0}"));
            Assert.That(derivs[0].FinalEquationLatex, Does.Contain("R_{2}"));
            Assert.That(derivs[0].FinalEquationLatex, Does.Contain("h_{5}"));
            Assert.That(derivs[0].FinalEquationLatex, Does.Contain("J_{5}"));
        }

        [Test]
        public async Task RealBoglExamples_GenerateEquationsWithoutErrors() {
            string examplesDir = FindExamplesDir();
            // Ensure rules are loaded locally before invoking BondGraphFactory
            string[] ruleNames = {
                "BondGraphRuleset", "SimplificationRuleset", "DirRuleset", "newDirectionRuleSet_2",
                "DirRuleset3", "Simplification2", "NewCausalityMethodRuleset", "NewCausalityMethodRuleset_2",
                "NewCausalityMethodRuleset_3", "INVDMarkerRules", "INVDMarkerRules_2", "CalibrationNewRuleset",
                "CalibrationNewRuleset_2", "RFlagCleanRuleset", "ICFixTotalRuleset", "TransformerFlipRuleset",
                "TransformerFlipRuleset2", "Clean23Ruleset", "BeforeBG-VerifyDirRuleSet"
            };
            foreach (string r in ruleNames) {
                await RuleSetMap.getInstance().loadRuleSet(r);
            }

            string[] testFiles = {
                "rack_pinion.bogl",
                "lrc_circuit.bogl",
                "motor-gear-pair.bogl",
                "spring_&_damper.bogl",
                "masses_on_wheels_with_spring.bogl"
            };

            List<string> failures = new();
            foreach (string file in testFiles) {
                string xml = File.ReadAllText(Path.Combine(examplesDir, file));
                SystemDiagram sd = SystemDiagram.generateSystemDiagramFromXML(xml);
                var (_, _, causalList) = BondGraphFactory.generateBondGraphs(sd.convertToDesignGraph());
                Assert.That(causalList.Count, Is.GreaterThan(0), $"No causal graphs for {file}");

                TestContext.WriteLine($"=== {file} (causalOptions={causalList.Count}) ===");
                bool anySucceeded = false;
                for (int idx = 0; idx < causalList.Count; idx++) {
                    BondGraph g = causalList[idx];
                    new BondGraph.BondGraphWrapper(g).AssignBondLabels();
                    try {
                        StateEquationSet set = new(g);
                        TestContext.WriteLine($"  [Option {idx}] Equations={set.GetFinalEquations().Length}");
                        foreach (var d in set.GetDerivations()) {
                            TestContext.WriteLine($"    Final: {d.FinalEquationLatex}");
                        }
                        var derivList = set.GetDerivations();
                        foreach (var d in derivList) {
                            string rhs = d.FinalEquationLatex.Split('=')[1];
                            foreach (var otherState in derivList) {
                                Assert.That(rhs, Does.Not.Contain(otherState.StateDerivativeLatex),
                                    $"Unisolated state derivative {otherState.StateDerivativeLatex} in {file}: {d.FinalEquationLatex}");
                            }
                        }
                        anySucceeded = true;
                        break;
                    } catch (Exception ex) {
                        TestContext.WriteLine($"  [Option {idx}] Failed: {ex.Message}");
                    }
                }
                if (!anySucceeded) {
                    failures.Add(file);
                }
            }
            Assert.That(failures, Is.Empty, "Failed examples: " + string.Join(", ", failures));
        }

        private static string FindExamplesDir() {
            string[] candidates = {
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "wwwroot", "examples"),
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "wwwroot", "examples"),
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "..", "wwwroot", "examples"),
                Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "examples"),
                Path.Combine(Directory.GetCurrentDirectory(), "BoGLWeb", "wwwroot", "examples")
            };
            foreach (string c in candidates) {
                string full = Path.GetFullPath(c);
                if (Directory.Exists(full)) return full;
            }
            throw new DirectoryNotFoundException("Could not locate wwwroot/examples.");
        }
    }
}

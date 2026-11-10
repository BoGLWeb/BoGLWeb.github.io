using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using BoGLWeb;

namespace BoGLWeb.PlaywrightTests {
    [TestFixture]
    public class BondGraphLayoutTests {

        [Test]
        public void TestEmptyBondGraph() {
            BondGraph bg = new();
            BondGraphEmbedder embedder = new(bg);
            Assert.That(embedder.isOptimized(), Is.False);
            embedder.embedBondGraph();
            Assert.That(embedder.isOptimized(), Is.True);
            Assert.That(embedder.getBondGraph().getElements().Count, Is.EqualTo(0));
        }

        [Test]
        public void TestSingleNodeBondGraph() {
            BondGraph bg = new();
            bg.addElement("n1", new BondGraph.Element("n1", "1", 0));
            BondGraphEmbedder embedder = new(bg);
            embedder.embedBondGraph();
            Assert.That(embedder.isOptimized(), Is.True);
            var el = bg.getElements()["n1"];
            Assert.That(el.getX(), Is.EqualTo(1000.0));
            Assert.That(el.getY(), Is.EqualTo(1000.0));
        }

        [Test]
        public void TestTwoNodeOrthogonalBond() {
            BondGraph bg = new();
            var e1 = new BondGraph.Element("e1", "1", 0);
            var e2 = new BondGraph.Element("e2", "Se:F", 0);
            bg.addElement("e1", e1);
            bg.addElement("e2", e2);
            bg.addBond(new BondGraph.Bond(0, 1, e1, e2, "b1", false, false, true, 0, 0));

            BondGraphEmbedder embedder = new(bg);
            embedder.embedBondGraph();
            Assert.That(embedder.isOptimized(), Is.True);

            // Assert either horizontal or vertical
            bool isHorizontal = Math.Abs(e1.getY() - e2.getY()) < 0.001;
            bool isVertical = Math.Abs(e1.getX() - e2.getX()) < 0.001;
            Assert.That(isHorizontal || isVertical, Is.True, "Bond between e1 and e2 must be orthogonal");
            Assert.That(Math.Abs(e1.getX() - e2.getX()) > 50 || Math.Abs(e1.getY() - e2.getY()) > 50, Is.True, "Nodes must not overlap");
        }

        [Test]
        public void TestOrientationInference_HorizontalMechanical() {
            // Horizontal mechanical layout: Delta X > Delta Y
            var el1 = new SystemDiagram.Element(0, "System_MT_Mass", -375.0, -26.0);
            var el2 = new SystemDiagram.Element(1, "System_MT_Spring", -241.0, -51.0);
            var el3 = new SystemDiagram.Element(0, "System_MT_Mass", -98.0, -38.0);
            var el4 = new SystemDiagram.Element(4, "System_MT_Force_Input", 388.0, -2.0);

            var elements = new List<SystemDiagram.Element> { el1, el2, el3, el4 };
            LayoutOrientation orientation = BondGraphEmbedder.ResolveOrientation(elements);

            Assert.That(orientation, Is.EqualTo(LayoutOrientation.Horizontal), "Mechanical system with DeltaX > DeltaY must resolve to Horizontal");
        }

        [Test]
        public void TestOrientationInference_VerticalMechanical() {
            // Vertical mechanical layout: Delta Y >= Delta X
            var el1 = new SystemDiagram.Element(0, "System_MT_Mass", 10.0, 50.0);
            var el2 = new SystemDiagram.Element(1, "System_MT_Spring", 12.0, 200.0);
            var el3 = new SystemDiagram.Element(0, "System_MT_Mass", 8.0, 400.0);

            var elements = new List<SystemDiagram.Element> { el1, el2, el3 };
            LayoutOrientation orientation = BondGraphEmbedder.ResolveOrientation(elements);

            Assert.That(orientation, Is.EqualTo(LayoutOrientation.Vertical), "Mechanical system with DeltaY >= DeltaX must resolve to Vertical");
        }

        [Test]
        public void TestOrientationInference_ElectricalDomain() {
            // Electrical domain: Contains System_E_*
            var el1 = new SystemDiagram.Element(19, "System_E_Inductor", 100.0, 100.0);
            var el2 = new SystemDiagram.Element(20, "System_E_Capacitor", 200.0, 100.0);
            var el3 = new SystemDiagram.Element(21, "System_E_Resistor", 300.0, 100.0);

            var elements = new List<SystemDiagram.Element> { el1, el2, el3 };
            LayoutOrientation orientation = BondGraphEmbedder.ResolveOrientation(elements);

            Assert.That(orientation, Is.EqualTo(LayoutOrientation.ElectricalHybrid), "Electrical system must resolve to ElectricalHybrid");
        }

        [Test]
        public void TestSystem1_LinearMassSpringChain_Horizontal() {
            // System 1 (sketch_mass_spring): Mass -> Spring -> Mass -> Spring -> Mass -> Force
            // In horizontal orientation, spine must route along the X-axis (Left-to-Right)
            BondGraph bg = new();
            var seF = new BondGraph.Element("Se:F9", "Se:F", 0);
            var j1_v3 = new BondGraph.Element("1_v3", "1", 0);
            var i_m6 = new BondGraph.Element("I:m6", "I:m", 0);
            var j0_F2 = new BondGraph.Element("0_F2", "0", 0);
            var c_K8 = new BondGraph.Element("C:K8", "C:K", 0);
            var j1_v1 = new BondGraph.Element("1_v1", "1", 0);
            var i_m5 = new BondGraph.Element("I:m5", "I:m", 0);
            var j0_F0 = new BondGraph.Element("0_F0", "0", 0);
            var i_m4 = new BondGraph.Element("I:m4", "I:m", 0);
            var c_K7 = new BondGraph.Element("C:K7", "C:K", 0);

            bg.addElement("Se:F9", seF);
            bg.addElement("1_v3", j1_v3);
            bg.addElement("I:m6", i_m6);
            bg.addElement("0_F2", j0_F2);
            bg.addElement("C:K8", c_K8);
            bg.addElement("1_v1", j1_v1);
            bg.addElement("I:m5", i_m5);
            bg.addElement("0_F0", j0_F0);
            bg.addElement("I:m4", i_m4);
            bg.addElement("C:K7", c_K7);

            bg.addBond(new BondGraph.Bond(0, 1, seF, j1_v3, "", false, false, true, 0, 0));
            bg.addBond(new BondGraph.Bond(1, 2, j1_v3, i_m6, "", false, false, true, 0, 0));
            bg.addBond(new BondGraph.Bond(1, 3, j1_v3, j0_F2, "", false, false, true, 0, 0));
            bg.addBond(new BondGraph.Bond(3, 4, j0_F2, c_K8, "", false, false, true, 0, 0));
            bg.addBond(new BondGraph.Bond(3, 5, j0_F2, j1_v1, "", false, false, true, 0, 0));
            bg.addBond(new BondGraph.Bond(5, 6, j1_v1, i_m5, "", false, false, true, 0, 0));
            bg.addBond(new BondGraph.Bond(5, 7, j1_v1, j0_F0, "", false, false, true, 0, 0));
            bg.addBond(new BondGraph.Bond(7, 8, j0_F0, i_m4, "", false, false, true, 0, 0));
            bg.addBond(new BondGraph.Bond(7, 9, j0_F0, c_K7, "", false, false, true, 0, 0));

            BondGraphEmbedder embedder = new(bg, LayoutOrientation.Horizontal);
            embedder.embedBondGraph();
            Assert.That(embedder.isOptimized(), Is.True);

            // 1. All bonds must be orthogonal
            foreach (var bond in bg.getBonds()) {
                var src = bond.getSource();
                var sink = bond.getSink();
                bool isH = Math.Abs(src.getY() - sink.getY()) < 0.001;
                bool isV = Math.Abs(src.getX() - sink.getX()) < 0.001;
                Assert.That(isH || isV, Is.True, $"Bond between {src.label} and {sink.label} must be orthogonal");
            }

            // 2. Spine nodes must share the same horizontal centerline (Y is constant)
            Assert.That(Math.Abs(j1_v3.getY() - j0_F2.getY()) < 0.001, Is.True, "Backbone nodes 1_v3 and 0_F2 must share the horizontal axis");
            Assert.That(Math.Abs(j0_F2.getY() - j1_v1.getY()) < 0.001, Is.True, "Backbone nodes 0_F2 and 1_v1 must share the horizontal axis");
            Assert.That(Math.Abs(j1_v1.getY() - j0_F0.getY()) < 0.001, Is.True, "Backbone nodes 1_v1 and 0_F0 must share the horizontal axis");

            // 3. Backbone flows Left-to-Right along X (Left end -> Right Force end)
            Assert.That(j1_v3.getX(), Is.GreaterThan(j0_F2.getX()), "Spine must flow Left-to-Right toward Force");
            Assert.That(j0_F2.getX(), Is.GreaterThan(j1_v1.getX()), "Spine must flow Left-to-Right");
            Assert.That(j1_v1.getX(), Is.GreaterThan(j0_F0.getX()), "Spine must flow Left-to-Right from left boundary");
        }

        [Test]
        public void TestSystem2_Unsimplified_SymmetricDiamond() {
            // System 2 Unsimplified:
            // Se:F14 -> 1 -> 1:m11 -> (0:C:K13 || 0:R:b9) -> 1:m10 -> (0:R:b8 || 0:C:K12) -> 1GND
            BondGraph bg = new();
            var seF = new BondGraph.Element("Se:F14", "Se:F", 0);
            var j1_top = new BondGraph.Element("1_top", "1", 0);
            var j1_m11 = new BondGraph.Element("1_m11", "1", 0);
            var i_m11 = new BondGraph.Element("I:m11", "I:m", 0);

            // Parallel stage 1: 0_k13 and 0_b9 between 1_m11 and 1_m10
            var j0_k13 = new BondGraph.Element("0_k13", "0", 0);
            var c_k13 = new BondGraph.Element("C:K13", "C:K", 0);
            var j0_b9 = new BondGraph.Element("0_b9", "0", 0);
            var r_b9 = new BondGraph.Element("R:b9", "R:b", 0);

            var j1_m10 = new BondGraph.Element("1_m10", "1", 0);
            var i_m10 = new BondGraph.Element("I:m10", "I:m", 0);

            // Parallel stage 2: 0_b8 and 0_k12 between 1_m10 and 1GND
            var j0_b8 = new BondGraph.Element("0_b8", "0", 0);
            var r_b8 = new BondGraph.Element("R:b8", "R:b", 0);
            var j0_k12 = new BondGraph.Element("0_k12", "0", 0);
            var c_k12 = new BondGraph.Element("C:K12", "C:K", 0);
            var gnd = new BondGraph.Element("1GND_0", "1GND", 0);

            bg.addElement("Se:F14", seF);
            bg.addElement("1_top", j1_top);
            bg.addElement("1_m11", j1_m11);
            bg.addElement("I:m11", i_m11);
            bg.addElement("0_k13", j0_k13);
            bg.addElement("C:K13", c_k13);
            bg.addElement("0_b9", j0_b9);
            bg.addElement("R:b9", r_b9);
            bg.addElement("1_m10", j1_m10);
            bg.addElement("I:m10", i_m10);
            bg.addElement("0_b8", j0_b8);
            bg.addElement("R:b8", r_b8);
            bg.addElement("0_k12", j0_k12);
            bg.addElement("C:K12", c_k12);
            bg.addElement("1GND_0", gnd);

            // Connections
            bg.addBond(new BondGraph.Bond(0, 1, seF, j1_top, "", false, false, true, 0, 0));
            bg.addBond(new BondGraph.Bond(1, 2, j1_top, j1_m11, "", false, false, true, 0, 0));
            bg.addBond(new BondGraph.Bond(2, 3, j1_m11, i_m11, "", false, false, true, 0, 0));

            // Parallel stage 1 bonds
            bg.addBond(new BondGraph.Bond(2, 4, j1_m11, j0_k13, "", false, false, true, 0, 0));
            bg.addBond(new BondGraph.Bond(4, 5, j0_k13, c_k13, "", false, false, true, 0, 0));
            bg.addBond(new BondGraph.Bond(4, 8, j0_k13, j1_m10, "", false, false, true, 0, 0));

            bg.addBond(new BondGraph.Bond(2, 6, j1_m11, j0_b9, "", false, false, true, 0, 0));
            bg.addBond(new BondGraph.Bond(6, 7, j0_b9, r_b9, "", false, false, true, 0, 0));
            bg.addBond(new BondGraph.Bond(6, 8, j0_b9, j1_m10, "", false, false, true, 0, 0));

            bg.addBond(new BondGraph.Bond(8, 9, j1_m10, i_m10, "", false, false, true, 0, 0));

            // Parallel stage 2 bonds
            bg.addBond(new BondGraph.Bond(8, 10, j1_m10, j0_b8, "", false, false, true, 0, 0));
            bg.addBond(new BondGraph.Bond(10, 11, j0_b8, r_b8, "", false, false, true, 0, 0));
            bg.addBond(new BondGraph.Bond(10, 14, j0_b8, gnd, "", false, false, true, 0, 0));

            bg.addBond(new BondGraph.Bond(8, 12, j1_m10, j0_k12, "", false, false, true, 0, 0));
            bg.addBond(new BondGraph.Bond(12, 13, j0_k12, c_k12, "", false, false, true, 0, 0));
            bg.addBond(new BondGraph.Bond(12, 14, j0_k12, gnd, "", false, false, true, 0, 0));

            BondGraphEmbedder embedder = new(bg, LayoutOrientation.Horizontal);
            embedder.embedBondGraph();
            Assert.That(embedder.isOptimized(), Is.True);

            // 1. Verify symmetric diamond geometry for stage 1:
            // 0_k13 and 0_b9 must share the exact same X coordinate
            Assert.That(Math.Abs(j0_k13.getX() - j0_b9.getX()) < 0.001, Is.True, "Parallel junctions 0_k13 and 0_b9 must share the same X column in diamond layout");

            // 0_k13 and 0_b9 must be equidistant from the spine axis (Y_spine = j1_m11.getY())
            double ySpine = j1_m11.getY();
            double dist1 = Math.Abs(j0_k13.getY() - ySpine);
            double dist2 = Math.Abs(j0_b9.getY() - ySpine);
            Assert.That(Math.Abs(dist1 - dist2) < 0.001, Is.True, $"Parallel junctions must be symmetrically offset from spine (dist1: {dist1}, dist2: {dist2})");
            Assert.That(dist1, Is.GreaterThan(0.1), "Parallel junctions must not lie directly on top of the central spine");

            // Downstream junction 1_m11 toward Force is downstream along the X-axis
            Assert.That(j1_m11.getX(), Is.GreaterThan(j0_k13.getX()), "Downstream junction 1_m11 must be placed forward along X toward Force");
            Assert.That(j0_k13.getX(), Is.GreaterThan(j1_m10.getX()), "Stage 1 diamond must be downstream of mass 10");
            Assert.That(j1_m10.getX(), Is.GreaterThan(gnd.getX()), "Mass 10 must be downstream of Ground on the left");
            Assert.That(Math.Abs(j1_m10.getY() - ySpine) < 0.001, Is.True, "Junction 1_m10 must align with the primary power spine");

            // Outward leaf elements C:K13 and R:b9 extend perpendicularly outwards
            Assert.That(Math.Abs(c_k13.getX() - j0_k13.getX()) < 0.001, Is.True, "Leaf C:K13 must align vertically with junction 0_k13");
            Assert.That(Math.Abs(r_b9.getX() - j0_b9.getX()) < 0.001, Is.True, "Leaf R:b9 must align vertically with junction 0_b9");
        }

        [Test]
        public void TestSystem2_Causal_TargetTopology() {
            // System 2 Causal Graph matching 4_causal_desired_reference.png:
            // Se:F9 -> 1_v1 (with I:m6) -> 0_F2
            // 0_F2 connects to:
            //   - 1_v0 with 3 leaves: C:K7, R:b3, I:m5
            //   - 1_v10 with 2 leaves: R:b4, C:K8
            BondGraph bg = new();
            var seF9 = new BondGraph.Element("Se:F9", "Se:F", 0);
            var j1_v1 = new BondGraph.Element("1_v1", "1", 0);
            var i_m6 = new BondGraph.Element("I:m6", "I:m", 0);

            var j0_F2 = new BondGraph.Element("0_F2", "0", 0);

            var j1_v0 = new BondGraph.Element("1_v0", "1", 0);
            var c_K7 = new BondGraph.Element("C:K7", "C:K", 0);
            var r_b3 = new BondGraph.Element("R:b3", "R:b", 0);
            var i_m5 = new BondGraph.Element("I:m5", "I:m", 0);

            var j1_v10 = new BondGraph.Element("1_v10", "1", 0);
            var r_b4 = new BondGraph.Element("R:b4", "R:b", 0);
            var c_K8 = new BondGraph.Element("C:K8", "C:K", 0);

            bg.addElement("Se:F9", seF9);
            bg.addElement("1_v1", j1_v1);
            bg.addElement("I:m6", i_m6);
            bg.addElement("0_F2", j0_F2);
            bg.addElement("1_v0", j1_v0);
            bg.addElement("C:K7", c_K7);
            bg.addElement("R:b3", r_b3);
            bg.addElement("I:m5", i_m5);
            bg.addElement("1_v10", j1_v10);
            bg.addElement("R:b4", r_b4);
            bg.addElement("C:K8", c_K8);

            bg.addBond(new BondGraph.Bond(0, 1, seF9, j1_v1, "", false, false, true, 0, 0));
            bg.addBond(new BondGraph.Bond(1, 2, j1_v1, i_m6, "", false, false, true, 0, 0));
            bg.addBond(new BondGraph.Bond(1, 3, j1_v1, j0_F2, "", false, false, true, 0, 0));

            // Branch 1: 0_F2 -> 1_v0
            bg.addBond(new BondGraph.Bond(3, 4, j0_F2, j1_v0, "", false, false, true, 0, 0));
            bg.addBond(new BondGraph.Bond(4, 5, j1_v0, c_K7, "", false, false, true, 0, 0));
            bg.addBond(new BondGraph.Bond(4, 6, j1_v0, r_b3, "", false, false, true, 0, 0));
            bg.addBond(new BondGraph.Bond(4, 7, j1_v0, i_m5, "", false, false, true, 0, 0));

            // Branch 2: 0_F2 -> 1_v10
            bg.addBond(new BondGraph.Bond(3, 8, j0_F2, j1_v10, "", false, false, true, 0, 0));
            bg.addBond(new BondGraph.Bond(8, 9, j1_v10, r_b4, "", false, false, true, 0, 0));
            bg.addBond(new BondGraph.Bond(8, 10, j1_v10, c_K8, "", false, false, true, 0, 0));

            BondGraphEmbedder embedder = new(bg, LayoutOrientation.Horizontal);
            embedder.embedBondGraph();
            Assert.That(embedder.isOptimized(), Is.True);

            // 1. All bonds must be orthogonal
            foreach (var bond in bg.getBonds()) {
                var src = bond.getSource();
                var sink = bond.getSink();
                bool isH = Math.Abs(src.getY() - sink.getY()) < 0.001;
                bool isV = Math.Abs(src.getX() - sink.getX()) < 0.001;
                Assert.That(isH || isV, Is.True, $"Bond between {src.label} and {sink.label} must be orthogonal");
            }

            // 2. Zero node overlap: All 3 leaves on 1_v0 (C:K7, R:b3, I:m5) must occupy distinct coordinates
            var leaves1v0 = new[] { c_K7, r_b3, i_m5 };
            for (int i = 0; i < leaves1v0.Length; i++) {
                for (int j = i + 1; j < leaves1v0.Length; j++) {
                    double dist = Math.Sqrt(Math.Pow(leaves1v0[i].getX() - leaves1v0[j].getX(), 2) + Math.Pow(leaves1v0[i].getY() - leaves1v0[j].getY(), 2));
                    Assert.That(dist, Is.GreaterThanOrEqualTo(BondGraphEmbedder.GridSpacingX * 0.9),
                        $"Leaves {leaves1v0[i].label} and {leaves1v0[j].label} must not collide (dist: {dist})");
                }
            }

            // 3. 1_v10 leaves (R:b4, C:K8) must occupy distinct coordinates
            double distV10Leaves = Math.Sqrt(Math.Pow(r_b4.getX() - c_K8.getX(), 2) + Math.Pow(r_b4.getY() - c_K8.getY(), 2));
            Assert.That(distV10Leaves, Is.GreaterThanOrEqualTo(BondGraphEmbedder.GridSpacingX * 0.9), "Leaves on 1_v10 must not collide");
        }

        [Test]
        public void TestDisconnectedComponents() {
            BondGraph bg = new();
            // Component 1
            var a1 = new BondGraph.Element("a1", "1", 0);
            var a2 = new BondGraph.Element("a2", "C:C", 0);
            bg.addElement("a1", a1);
            bg.addElement("a2", a2);
            bg.addBond(new BondGraph.Bond(0, 1, a1, a2, "", false, false, true, 0, 0));

            // Component 2
            var b1 = new BondGraph.Element("b1", "0", 0);
            var b2 = new BondGraph.Element("b2", "I:L", 0);
            bg.addElement("b1", b1);
            bg.addElement("b2", b2);
            bg.addBond(new BondGraph.Bond(2, 3, b1, b2, "", false, false, true, 0, 0));

            BondGraphEmbedder embedder = new(bg);
            embedder.embedBondGraph();
            Assert.That(embedder.isOptimized(), Is.True);

            // Verify all bonds are orthogonal
            foreach (var bond in bg.getBonds()) {
                var src = bond.getSource();
                var sink = bond.getSink();
                bool isH = Math.Abs(src.getY() - sink.getY()) < 0.001;
                bool isV = Math.Abs(src.getX() - sink.getX()) < 0.001;
                Assert.That(isH || isV, Is.True, $"Bond between {src.label} and {sink.label} must be orthogonal");
            }

            // Verify no overlap across components
            var allElements = bg.getElements().Values.ToList();
            for (int i = 0; i < allElements.Count; i++) {
                for (int j = i + 1; j < allElements.Count; j++) {
                    double dist = Math.Sqrt(Math.Pow(allElements[i].getX() - allElements[j].getX(), 2) + Math.Pow(allElements[i].getY() - allElements[j].getY(), 2));
                    Assert.That(dist, Is.GreaterThanOrEqualTo(BondGraphEmbedder.GridSpacingX * 0.9));
                }
            }
        }

        [Test]
        public void TestComplexSystemWithTransformer() {
            // Testing 1 -> TF -> 1 chain
            BondGraph bg = new();
            var top1 = new BondGraph.Element("top1", "1", 0);
            var se1 = new BondGraph.Element("se1", "Se:F", 0);
            var c1 = new BondGraph.Element("c1", "C:C", 0);
            var i1 = new BondGraph.Element("i1", "I:m", 0);

            var mid1 = new BondGraph.Element("mid1", "1", 0);
            var tf = new BondGraph.Element("tf", "TF:R", 0);
            var se2 = new BondGraph.Element("se2", "Se:V", 0);

            var bot1 = new BondGraph.Element("bot1", "1", 0);
            var bot2 = new BondGraph.Element("bot2", "1", 0);
            var c2 = new BondGraph.Element("c2", "C:K", 0);
            var i2 = new BondGraph.Element("i2", "I:J", 0);

            bg.addElement("top1", top1);
            bg.addElement("se1", se1);
            bg.addElement("c1", c1);
            bg.addElement("i1", i1);
            bg.addElement("mid1", mid1);
            bg.addElement("tf", tf);
            bg.addElement("se2", se2);
            bg.addElement("bot1", bot1);
            bg.addElement("bot2", bot2);
            bg.addElement("c2", c2);
            bg.addElement("i2", i2);

            bg.addBond(new BondGraph.Bond(0, 1, se1, top1, "", false, false, true, 0, 0));
            bg.addBond(new BondGraph.Bond(0, 2, top1, c1, "", false, false, true, 0, 0));
            bg.addBond(new BondGraph.Bond(0, 3, top1, i1, "", false, false, true, 0, 0));
            bg.addBond(new BondGraph.Bond(0, 4, top1, mid1, "", false, false, true, 0, 0));
            bg.addBond(new BondGraph.Bond(4, 5, mid1, tf, "", false, false, true, 0, 0));
            bg.addBond(new BondGraph.Bond(5, 6, tf, se2, "", false, false, true, 0, 0));
            bg.addBond(new BondGraph.Bond(5, 7, tf, bot1, "", false, false, true, 0, 0));
            bg.addBond(new BondGraph.Bond(7, 8, bot1, bot2, "", false, false, true, 0, 0));
            bg.addBond(new BondGraph.Bond(8, 9, bot2, c2, "", false, false, true, 0, 0));
            bg.addBond(new BondGraph.Bond(8, 10, bot2, i2, "", false, false, true, 0, 0));

            BondGraphEmbedder embedder = new(bg);
            embedder.embedBondGraph();
            Assert.That(embedder.isOptimized(), Is.True);

            // All bonds orthogonal
            foreach (var bond in bg.getBonds()) {
                var src = bond.getSource();
                var sink = bond.getSink();
                bool isH = Math.Abs(src.getY() - sink.getY()) < 0.001;
                bool isV = Math.Abs(src.getX() - sink.getX()) < 0.001;
                Assert.That(isH || isV, Is.True, $"Bond between {src.label} and {sink.label} must be orthogonal (src: {src.getX()},{src.getY()} - sink: {sink.getX()},{sink.getY()})");
            }
        }

        [Test]
        public void TestLeftToRight_GroundLeft_ForceRight() {
            // Assert that whenever Ground is to the left of Force in the sketch,
            // 1GND has a strictly smaller X coordinate than Se:F in the embedded graph
            var sysElements = new List<SystemDiagram.Element> {
                new(0, "System_MT_Ground", -454.0, 0.0),
                new(0, "System_MT_Mass", -200.0, 0.0),
                new(4, "System_MT_Force_Input", 401.0, 0.0)
            };
            bool flowLeftToRight = BondGraphEmbedder.ResolveFlowLeftToRight(sysElements);
            Assert.That(flowLeftToRight, Is.True, "Sketch with Ground on left and Force on right must resolve to flowLeftToRight = true");

            // Build bond graph with 1GND and Se:F
            BondGraph bg = new();
            var gnd = new BondGraph.Element("1GND_0", "1GND", 0);
            var j1_m = new BondGraph.Element("1_m", "1", 0);
            var seF = new BondGraph.Element("Se:F", "Se:F", 0);
            bg.addElement("1GND_0", gnd);
            bg.addElement("1_m", j1_m);
            bg.addElement("Se:F", seF);
            bg.addBond(new BondGraph.Bond(0, 1, gnd, j1_m, "", false, false, true, 0, 0));
            bg.addBond(new BondGraph.Bond(1, 2, j1_m, seF, "", false, false, true, 0, 0));

            BondGraphEmbedder embedder = new(bg, LayoutOrientation.Horizontal, flowLeftToRight);
            embedder.embedBondGraph();

            Assert.That(gnd.getX(), Is.LessThanOrEqualTo(seF.getX()), "1GND must be to the left of Se:F in horizontal layout");
            Assert.That(j1_m.getX(), Is.GreaterThan(gnd.getX()), "Intermediate mass must be to the right of Ground");
            Assert.That(j1_m.getX(), Is.LessThanOrEqualTo(seF.getX()), "Intermediate mass must be to the left of or aligned with Force");
        }

        [Test]
        public void TestZeroBondNodeIntersections() {
            // Build a graph with central junction and multiple branches
            BondGraph bg = new();
            var center = new BondGraph.Element("1_center", "1", 0);
            var northLeaf = new BondGraph.Element("I:m1", "I:m", 0);
            var southLeaf = new BondGraph.Element("C:K1", "C:K", 0);
            var eastJunction = new BondGraph.Element("0_east", "0", 0);
            var eastLeaf = new BondGraph.Element("R:b1", "R:b", 0);

            bg.addElement("1_center", center);
            bg.addElement("I:m1", northLeaf);
            bg.addElement("C:K1", southLeaf);
            bg.addElement("0_east", eastJunction);
            bg.addElement("R:b1", eastLeaf);

            bg.addBond(new BondGraph.Bond(0, 1, center, northLeaf, "", false, false, true, 0, 0));
            bg.addBond(new BondGraph.Bond(0, 2, center, southLeaf, "", false, false, true, 0, 0));
            bg.addBond(new BondGraph.Bond(0, 3, center, eastJunction, "", false, false, true, 0, 0));
            bg.addBond(new BondGraph.Bond(3, 4, eastJunction, eastLeaf, "", false, false, true, 0, 0));

            BondGraphEmbedder embedder = new(bg, LayoutOrientation.Horizontal);
            embedder.embedBondGraph();

            // Assert that no bond line segment intersects any non-endpoint node
            double nodeRadius = 30.0;
            var elements = bg.getElements().Values.ToList();
            foreach (var bond in bg.getBonds()) {
                var p1 = (x: bond.getSource().getX(), y: bond.getSource().getY());
                var p2 = (x: bond.getSink().getX(), y: bond.getSink().getY());

                foreach (var el in elements) {
                    if (el.Equals(bond.getSource()) || el.Equals(bond.getSink())) continue;

                    // Point-to-segment distance
                    double px = el.getX();
                    double py = el.getY();
                    double dx = p2.x - p1.x;
                    double dy = p2.y - p1.y;
                    double lenSq = dx * dx + dy * dy;

                    double t = lenSq == 0 ? 0 : Math.Clamp(((px - p1.x) * dx + (py - p1.y) * dy) / lenSq, 0.0, 1.0);
                    double projX = p1.x + t * dx;
                    double projY = p1.y + t * dy;
                    double dist = Math.Sqrt((px - projX) * (px - projX) + (py - projY) * (py - projY));

                    Assert.That(dist, Is.GreaterThan(nodeRadius),
                        $"Bond between {bond.getSource().label} and {bond.getSink().label} passes too close to node {el.label} (dist: {dist:F1})");
                }
            }
        }

        [Test]
        public void TestCausalGraph_InheritsSimplifiedCoordinates() {
            // Simplified graph
            BondGraph simp = new();
            var s_0 = new BondGraph.Element("name1", "0", 0);
            var s_1 = new BondGraph.Element("name2", "1", 0);
            var s_m = new BondGraph.Element("name6", "I:m", 0);
            var s_k = new BondGraph.Element("name9", "C:K", 0);
            var s_f = new BondGraph.Element("name11", "Se:F", 0);

            simp.addElement("name1", s_0);
            simp.addElement("name2", s_1);
            simp.addElement("name6", s_m);
            simp.addElement("name9", s_k);
            simp.addElement("name11", s_f);

            simp.addBond(new BondGraph.Bond(0, 1, s_0, s_1, "", false, false, true, 0, 0));
            simp.addBond(new BondGraph.Bond(1, 2, s_1, s_m, "", false, false, true, 0, 0));
            simp.addBond(new BondGraph.Bond(0, 3, s_0, s_k, "", false, false, true, 0, 0));
            simp.addBond(new BondGraph.Bond(4, 1, s_f, s_1, "", false, false, true, 0, 0));

            BondGraphEmbedder simpEmbedder = new(simp, LayoutOrientation.Horizontal);
            simpEmbedder.embedBondGraph();

            // Causal graph with domain labels and causal strokes
            BondGraph causal = new();
            var c_0 = new BondGraph.Element("name1", "0 F", 0);
            var c_1 = new BondGraph.Element("name2", "1 v", 0);
            var c_m = new BondGraph.Element("name6", "I:m", 0);
            var c_k = new BondGraph.Element("name9", "C:K", 0);
            var c_f = new BondGraph.Element("name11", "Se:F", 0);

            causal.addElement("name1", c_0);
            causal.addElement("name2", c_1);
            causal.addElement("name6", c_m);
            causal.addElement("name9", c_k);
            causal.addElement("name11", c_f);

            causal.addBond(new BondGraph.Bond(0, 1, c_0, c_1, "", true, false, true, 0, 0));
            causal.addBond(new BondGraph.Bond(1, 2, c_1, c_m, "", true, true, true, 0, 0));
            causal.addBond(new BondGraph.Bond(0, 3, c_0, c_k, "", true, false, true, 0, 0));
            causal.addBond(new BondGraph.Bond(4, 1, c_f, c_1, "", true, true, true, 0, 0));

            // Inherit layout
            BondGraphEmbedder.InheritLayout(causal, simp, LayoutOrientation.Horizontal);

            // Assert exact coordinate equality between simplified and causal nodes
            foreach (var kvp in causal.getElements()) {
                string key = kvp.Key;
                var cEl = kvp.Value;
                var sEl = simp.getElements()[key];

                Assert.That(Math.Abs(cEl.getX() - sEl.getX()) < 0.001, Is.True,
                    $"Causal node {cEl.getName()} X ({cEl.getX()}) must exactly match simplified node X ({sEl.getX()})");
                Assert.That(Math.Abs(cEl.getY() - sEl.getY()) < 0.001, Is.True,
                    $"Causal node {cEl.getName()} Y ({cEl.getY()}) must exactly match simplified node Y ({sEl.getY()})");
            }
        }

        [Test]
        public void TestCausalGraph_StrictlyOrthogonalBonds() {
            // Assert that every bond in a causal graph laid out horizontally
            // has angle in {0, 90, 180, 270} degrees (strictly orthogonal; no diagonal starburst)
            BondGraph simp = new();
            var s_0 = new BondGraph.Element("name1", "0", 0);
            var s_1 = new BondGraph.Element("name2", "1", 0);
            var s_m = new BondGraph.Element("name6", "I:m", 0);
            var s_k = new BondGraph.Element("name9", "C:K", 0);
            var s_f = new BondGraph.Element("name11", "Se:F", 0);

            simp.addElement("name1", s_0);
            simp.addElement("name2", s_1);
            simp.addElement("name6", s_m);
            simp.addElement("name9", s_k);
            simp.addElement("name11", s_f);

            simp.addBond(new BondGraph.Bond(0, 1, s_0, s_1, "", false, false, true, 0, 0));
            simp.addBond(new BondGraph.Bond(1, 2, s_1, s_m, "", false, false, true, 0, 0));
            simp.addBond(new BondGraph.Bond(0, 3, s_0, s_k, "", false, false, true, 0, 0));
            simp.addBond(new BondGraph.Bond(4, 1, s_f, s_1, "", false, false, true, 0, 0));

            BondGraphEmbedder simpEmbedder = new(simp, LayoutOrientation.Horizontal);
            simpEmbedder.embedBondGraph();

            BondGraph causal = new();
            var c_0 = new BondGraph.Element("name1", "0 F", 0);
            var c_1 = new BondGraph.Element("name2", "1 v", 0);
            var c_m = new BondGraph.Element("name6", "I:m", 0);
            var c_k = new BondGraph.Element("name9", "C:K", 0);
            var c_f = new BondGraph.Element("name11", "Se:F", 0);

            causal.addElement("name1", c_0);
            causal.addElement("name2", c_1);
            causal.addElement("name6", c_m);
            causal.addElement("name9", c_k);
            causal.addElement("name11", c_f);

            causal.addBond(new BondGraph.Bond(0, 1, c_0, c_1, "", true, false, true, 0, 0));
            causal.addBond(new BondGraph.Bond(1, 2, c_1, c_m, "", true, true, true, 0, 0));
            causal.addBond(new BondGraph.Bond(0, 3, c_0, c_k, "", true, false, true, 0, 0));
            causal.addBond(new BondGraph.Bond(4, 1, c_f, c_1, "", true, true, true, 0, 0));

            BondGraphEmbedder.InheritLayout(causal, simp, LayoutOrientation.Horizontal);

            foreach (var b in causal.getBonds()) {
                double dx = Math.Abs(b.getSource().getX() - b.getSink().getX());
                double dy = Math.Abs(b.getSource().getY() - b.getSink().getY());

                bool isOrthogonal = dx < 0.001 || dy < 0.001;
                Assert.That(isOrthogonal, Is.True,
                    $"Causal bond between {b.getSource().label} and {b.getSink().label} must be strictly orthogonal (dx={dx}, dy={dy})");
            }
        }

        [Test]
        public void TestMultiLeafJunction_NoCollinearStacking() {
            // A junction with multiple leaves (e.g. 1_v10 connected to C:K8 and R:b4)
            // must assign distinct orthogonal directions to each leaf (e.g. Left and Up).
            // They must NOT be stacked collinearly along the same ray.
            BondGraph bg = new();
            var j0 = new BondGraph.Element("0_F2", "0", 0);
            var j1 = new BondGraph.Element("1_v10", "1", 0);
            var leafC = new BondGraph.Element("C:K8", "C:K", 0);
            var leafR = new BondGraph.Element("R:b4", "R:b", 0);

            bg.addElement("0_F2", j0);
            bg.addElement("1_v10", j1);
            bg.addElement("C:K8", leafC);
            bg.addElement("R:b4", leafR);

            bg.addBond(new BondGraph.Bond(0, 1, j0, j1, "", false, false, true, 0, 0));
            bg.addBond(new BondGraph.Bond(1, 2, j1, leafC, "", false, false, true, 0, 0));
            bg.addBond(new BondGraph.Bond(1, 3, j1, leafR, "", false, false, true, 0, 0));

            BondGraphEmbedder embedder = new(bg, LayoutOrientation.Horizontal);
            embedder.embedBondGraph();

            // Check vectors from j1 to leafC and leafR
            double dcX = leafC.getX() - j1.getX();
            double dcY = leafC.getY() - j1.getY();
            double drX = leafR.getX() - j1.getX();
            double drY = leafR.getY() - j1.getY();

            // Must both be orthogonal stubs
            Assert.That(Math.Abs(dcX) < 0.001 || Math.Abs(dcY) < 0.001, Is.True, "Leaf C must be orthogonal to parent");
            Assert.That(Math.Abs(drX) < 0.001 || Math.Abs(drY) < 0.001, Is.True, "Leaf R must be orthogonal to parent");

            // Must not be collinear (dot product / normalized directions must not be equal)
            double normC = Math.Sqrt((dcX * dcX) + (dcY * dcY));
            double normR = Math.Sqrt((drX * drX) + (drY * drY));
            double dirCx = dcX / normC;
            double dirCy = dcY / normC;
            double dirRx = drX / normR;
            double dirRy = drY / normR;

            bool isSameRay = Math.Abs(dirCx - dirRx) < 0.001 && Math.Abs(dirCy - dirRy) < 0.001;
            Assert.That(isSameRay, Is.False, "Leaves on the same junction must not be stacked collinearly along the same ray");

            // Must have sufficient distance between leaf C and leaf R (no overlap)
            double leafDist = Math.Sqrt(Math.Pow(leafC.getX() - leafR.getX(), 2) + Math.Pow(leafC.getY() - leafR.getY(), 2));
            Assert.That(leafDist >= 150.0, Is.True, $"Leaves must be separated by at least 150px, was {leafDist}");
        }

        [Test]
        public void TestVerticalFlow_SourceBottomToTop() {
            // Physical sketch with velocity input at bottom (Y ~ 268) and mass at top (Y ~ -100)
            var elMassTop = new SystemDiagram.Element(0, "System_MT_Mass", -183.0, -99.0);
            var elMassMid = new SystemDiagram.Element(0, "System_MT_Mass", -190.0, 139.0);
            var elSpring1 = new SystemDiagram.Element(1, "System_MT_Spring", -251.0, 16.0);
            var elDamper1 = new SystemDiagram.Element(2, "System_MT_Damper", -134.0, 29.0);
            var elSpring2 = new SystemDiagram.Element(1, "System_MT_Spring", -194.0, 253.0);
            var elVInput = new SystemDiagram.Element(6, "System_MT_Velocity_Input", -40.0, 268.0);

            var sketchElements = new List<SystemDiagram.Element> {
                elMassTop, elMassMid, elSpring1, elDamper1, elSpring2, elVInput
            };

            // Orientation must resolve to Vertical
            LayoutOrientation orientation = BondGraphEmbedder.ResolveOrientation(sketchElements);
            Assert.That(orientation, Is.EqualTo(LayoutOrientation.Vertical), "Sketch with DeltaY > DeltaX must resolve to Vertical");

            // FlowBottomToTop must resolve to True because source Y (268) is at the bottom
            bool flowBottomToTop = BondGraphEmbedder.ResolveFlowBottomToTop(sketchElements);
            Assert.That(flowBottomToTop, Is.True, "Sketch with source at bottom must resolve flowBottomToTop=true");

            // Build bond graph corresponding to masses on a spring
            BondGraph bg = new();
            var sf = new BondGraph.Element("Sf:v11", "Sf:v", 0);
            var j1_bot = new BondGraph.Element("1_vbot", "1", 0);
            var j0_mid = new BondGraph.Element("0_Fmid", "0", 0);
            var j1_mid = new BondGraph.Element("1_vmid", "1", 0);
            var i_mid = new BondGraph.Element("I:m8", "I:m", 0);
            var j0_top = new BondGraph.Element("0_Ftop", "0", 0);
            var j1_top = new BondGraph.Element("1_vtop", "1", 0);
            var i_top = new BondGraph.Element("I:m7", "I:m", 0);

            bg.addElement("Sf:v11", sf);
            bg.addElement("1_vbot", j1_bot);
            bg.addElement("0_Fmid", j0_mid);
            bg.addElement("1_vmid", j1_mid);
            bg.addElement("I:m8", i_mid);
            bg.addElement("0_Ftop", j0_top);
            bg.addElement("1_vtop", j1_top);
            bg.addElement("I:m7", i_top);

            bg.addBond(new BondGraph.Bond(0, 1, sf, j1_bot, "", false, false, true, 0, 0));
            bg.addBond(new BondGraph.Bond(1, 2, j1_bot, j0_mid, "", false, false, true, 0, 0));
            bg.addBond(new BondGraph.Bond(2, 3, j0_mid, j1_mid, "", false, false, true, 0, 0));
            bg.addBond(new BondGraph.Bond(3, 4, j1_mid, i_mid, "", false, false, true, 0, 0));
            bg.addBond(new BondGraph.Bond(3, 5, j1_mid, j0_top, "", false, false, true, 0, 0));
            bg.addBond(new BondGraph.Bond(5, 6, j0_top, j1_top, "", false, false, true, 0, 0));
            bg.addBond(new BondGraph.Bond(6, 7, j1_top, i_top, "", false, false, true, 0, 0));

            BondGraphEmbedder embedder = new(bg, orientation, flowLeftToRight: true, flowBottomToTop: true);
            embedder.embedBondGraph();

            // In canvas coordinates, larger Y is at the bottom.
            // Source (Sf:v11) must have larger Y than top mass (I:m7)
            Assert.That(sf.getY() > i_top.getY(), Is.True,
                $"Source Y ({sf.getY()}) must be greater than top mass Y ({i_top.getY()}) to place source at the bottom");
            Assert.That(j1_bot.getY() > j1_top.getY(), Is.True,
                $"Bottom 1-junction Y ({j1_bot.getY()}) must be greater than top 1-junction Y ({j1_top.getY()})");
        }

        [Test]
        public void TestCompactBondLengths() {
            // Asserts that default grid spacing is reduced by roughly 25%-35%
            // Leaf stubs should be <= 120px (down from 160px), and inter-junction bonds <= 235px (down from 320px)
            BondGraph bg = new();
            var j1 = new BondGraph.Element("1_v0", "1", 0);
            var j0 = new BondGraph.Element("0_F2", "0", 0);
            var leaf = new BondGraph.Element("C:K8", "C:K", 0);

            bg.addElement("1_v0", j1);
            bg.addElement("0_F2", j0);
            bg.addElement("C:K8", leaf);

            bg.addBond(new BondGraph.Bond(0, 1, j1, j0, "", false, false, true, 0, 0));
            bg.addBond(new BondGraph.Bond(1, 2, j0, leaf, "", false, false, true, 0, 0));

            BondGraphEmbedder embedder = new(bg, LayoutOrientation.Horizontal);
            embedder.embedBondGraph();

            double leafStubLen = Math.Sqrt(Math.Pow(leaf.getX() - j0.getX(), 2) + Math.Pow(leaf.getY() - j0.getY(), 2));
            double interJunctionLen = Math.Sqrt(Math.Pow(j0.getX() - j1.getX(), 2) + Math.Pow(j0.getY() - j1.getY(), 2));

            // Leaf stub length should be ~112px (within 100px - 120px)
            Assert.That(leafStubLen <= 120.0, Is.True, $"Leaf stub length ({leafStubLen}) must be <= 120px (compacted by ~30%)");
            Assert.That(leafStubLen >= 100.0, Is.True, $"Leaf stub length ({leafStubLen}) must be >= 100px for label clearance");

            // Inter-junction length should be ~224px (within 200px - 235px)
            Assert.That(interJunctionLen <= 235.0, Is.True, $"Inter-junction length ({interJunctionLen}) must be <= 235px (compacted by ~30%)");
            Assert.That(interJunctionLen >= 200.0, Is.True, $"Inter-junction length ({interJunctionLen}) must be >= 200px");
        }

        [Test]
        public void TestInlineTransformerSpacing() {
            // Asserts that 2-port inline elements (TF, GY) on the spine use single-pitch spacing (~112px)
            // without double-pitch stretching (no 320px or 640px gaps)
            BondGraph bg = new();
            var j1_v0 = new BondGraph.Element("1_v0", "1", 0);
            var tf = new BondGraph.Element("TF:R6", "TF:R6", 0);
            var j1_w1 = new BondGraph.Element("1_w1", "1", 0);

            bg.addElement("1_v0", j1_v0);
            bg.addElement("TF:R6", tf);
            bg.addElement("1_w1", j1_w1);

            bg.addBond(new BondGraph.Bond(0, 1, j1_v0, tf, "", false, false, true, 0, 0));
            bg.addBond(new BondGraph.Bond(1, 2, tf, j1_w1, "", false, false, true, 0, 0));

            BondGraphEmbedder embedder = new(bg, LayoutOrientation.Horizontal);
            embedder.embedBondGraph();

            double distToTf = Math.Abs(tf.getX() - j1_v0.getX());
            double distFromTf = Math.Abs(j1_w1.getX() - tf.getX());

            Assert.That(distToTf <= 125.0, Is.True, $"Distance to TF ({distToTf}) must use compact single pitch <= 125px");
            Assert.That(distFromTf <= 125.0, Is.True, $"Distance from TF ({distFromTf}) must use compact single pitch <= 125px");
        }
    }
}
